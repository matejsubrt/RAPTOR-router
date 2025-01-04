using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.GBFSParsing.DataSources;
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Extensions;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using ProtoBuf;
using System.Net;
using System.Runtime.CompilerServices;
using TransitRealtime;
using System.Runtime.InteropServices;
using System.Timers;
using RAPTOR_Router.Configuration;
using Quartz;
using Quartz.Impl;


namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// A class used for creating separate routers to be used for connection searching.
    /// </summary>
    public static class RouteFinderBuilder
    {
        // Job for reloading GTFS data every day
        private class DailyGtfsJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                string? gtfsZipArchiveLocation = Config.DefaultGTFSPath;
                if (gtfsZipArchiveLocation is null)
                {
                    throw new ApplicationException("GTFS Zip archive location must be set");
                }
                else if (forbiddenCrossingLines is null)
                {
                    throw new ApplicationException("Forbidden crossing lines must be set");
                }
                LoadGtfsData(gtfsZipArchiveLocation, forbiddenCrossingLines);
                Console.WriteLine("GTFS data reloaded at " + DateTime.Now);
                return Task.CompletedTask;
            }
        }

        // Job for updating delay model every 20 seconds
        private class PeriodicDelayUpdateJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                UpdateDelayModelAsync().GetAwaiter().GetResult();
                return Task.CompletedTask;
            }
        }

        private class PeriodicBikeStationStatusUpdateJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                bikeModel!.UpdateAllStationStatus();
                return Task.CompletedTask;
            }
        }



        /// <summary>
        /// The transit model that the routers should use
        /// </summary>
        private static TransitModel? transitModel;

        /// <summary>
        /// The bike model the routers should use
        /// </summary>
        private static BikeModel? bikeModel;

        /// <summary>
        /// The delay model that the routers should use
        /// </summary>
        private static IDelayModel? delayModel;

        /// <summary>
        /// The list of lines forbidden to cross via a transfer
        /// </summary>
        private static List<ForbiddenCrossingLine>? forbiddenCrossingLines;
        
        /// <summary>
        /// The timer that periodically updates the delay model
        /// </summary>
        private static IScheduler? _scheduler;

        private static async Task InitializeScheduler()
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            _scheduler = await factory.GetScheduler();
            await _scheduler.Start();



            IJobDetail dailyGtfsJob = JobBuilder.Create<DailyGtfsJob>()
                .WithIdentity("dailyGtfsJob", "group1")
                .Build();

            ITrigger dailyTrigger = TriggerBuilder.Create()
                .WithIdentity("dailyGtfsTrigger", "group1")
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(3, 00)) // Executes at 3:00 AM every day
                .StartNow()
                .Build();



            IJobDetail periodicDelayUpdateJob = JobBuilder.Create<PeriodicDelayUpdateJob>()
                .WithIdentity("periodicDelayUpdateJob", "group1")
                .Build();

            ITrigger periodic20SecTrigger = TriggerBuilder.Create()
                .WithIdentity("periodicDelayUpdateTrigger", "group1")
                .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(20)
                     .RepeatForever()) // Executes every 20 seconds
                .StartNow()
                .Build();

            IJobDetail periodicBikeStationStatusUpdateJob = JobBuilder.Create<PeriodicBikeStationStatusUpdateJob>()
                .WithIdentity("periodicBikeStationStatusUpdateJob", "group1")
                .Build();

            ITrigger periodicBikeStationStatusUpdateTrigger = TriggerBuilder.Create()
                .WithIdentity("periodicBikeStationStatusUpdateTrigger", "group1")
                .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(60)
                     .RepeatForever()) // Executes every 60 seconds
                .StartNow()
                .Build();



            await _scheduler.ScheduleJob(dailyGtfsJob, dailyTrigger);
            await _scheduler.ScheduleJob(periodicDelayUpdateJob, periodic20SecTrigger);
            await _scheduler.ScheduleJob(periodicBikeStationStatusUpdateJob, periodicBikeStationStatusUpdateTrigger);
        }




        private static void UpdateDelayModel(object? state)
        {
            UpdateDelayModelAsync().GetAwaiter().GetResult();
        }





        private static async Task UpdateDelayModelAsync()
        {
            DelayModel newDelayModel = new();
            using HttpClient client = new();

            string apiUrl = Config.GtfsRealtimeTripUpdatesApiUrl!;

            try
            {
                // Fetch the GTFS Realtime feed asynchronously
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                // Deserialize the feed from the response stream
                using Stream responseStream = await response.Content.ReadAsStreamAsync();
                FeedMessage feed = Serializer.Deserialize<FeedMessage>(responseStream);

                // Process each entity
                foreach (FeedEntity entity in feed.Entities)
                {
                    string tripId = entity.TripUpdate.Trip.TripId;
                    DateOnly tripStartDate = DateOnly.ParseExact(entity.TripUpdate.Trip.StartDate, "yyyyMMdd");
                    foreach (TripUpdate.StopTimeUpdate update in entity.TripUpdate.StopTimeUpdates)
                    {
                        if (update.Departure != null)
                        {
                            bool doIncludeDelay = true;
                            int arrivalDelay = update.Arrival.Delay;
                            int departureDelay = update.Departure.Delay;

                            // This is necessary due to frequent bugs in PID's GTFS realtime feed
                            if (arrivalDelay < -120 || departureDelay < -120)
                            {
                                //arrivalDelay = 0;
                                //departureDelay = 0;
                                doIncludeDelay = false;
                            }

                            if (doIncludeDelay)
                            {
                                newDelayModel.AddDelay(tripStartDate, tripId, arrivalDelay, departureDelay);
                            }
                        }
                    }
                }

                delayModel = newDelayModel;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating delay info: " + ex.Message);
            }
        }

        public static void SetDelayModel(IDelayModel delayModel)
        {
            RouteFinderBuilder.delayModel = delayModel;
        }

        /// <summary>
        /// Loads all the GTFS, GBFS and forbidden crossing data from the locations provided in the config file and bike data sources
        /// </summary>
        /// <exception cref="Exception">The configuration is wrong</exception>
        public static void LoadAllData(string? alternativeGtfsArchiveLocation = null, bool useMocks = false)
        {
            // Retrieve configuration values
            string? gtfsZipArchiveLocation = Config.DefaultGTFSPath;
            string? forbiddenPointsLocation = Config.ForbiddenCrossingPointsPath;
            string? forbiddenLinesLocation = Config.ForbiddenCrossingLinesPath;
            string? nextbikeDbLocation = useMocks ? Config.NextbikeDbTestPath : Config.NextbikeDbPath;

            if (alternativeGtfsArchiveLocation != null)
            {
                Console.WriteLine("Using non-default GTFS location: " + alternativeGtfsArchiveLocation);
                gtfsZipArchiveLocation = alternativeGtfsArchiveLocation;
            }



            // Validate the configuration - check if all values have been specified
            ValidateConfiguration();


            // Load the forbidden crossing data
            var forbiddenCrossings = LoadForbiddenCrossings(forbiddenPointsLocation!, forbiddenLinesLocation!);


            Console.WriteLine("Loading GTFS data...");
            // Load the public transit GTFS data
            LoadGtfsData(gtfsZipArchiveLocation!, forbiddenCrossings, !useMocks);


            Console.WriteLine("Loading GBFS data...");
            // Load the bike providers GBFS data
            LoadGbfsData(nextbikeDbLocation!, useMocks);


            // Connect the transit and bike models through transfers
            ConnectModelsThroughTransfers(forbiddenCrossings);

            if (!useMocks)
            {
                // Start the timer that periodically updates the delay and transit models
                InitializeScheduler().GetAwaiter().GetResult();
            }


            void ValidateConfiguration()
            {
                if (gtfsZipArchiveLocation is null)
                {
                    throw new Exception("No gtfs archive location found in the config file");
                }

                if (forbiddenPointsLocation == null || forbiddenLinesLocation == null)
                {
                    throw new Exception("Forbidden crossing points or lines location not found in the config file");
                }

                if (nextbikeDbLocation is null)
                {
                    throw new Exception("No nextbike db location found in the config file");
                }
            }
        }

        /// <summary>
        /// Parses the provided gtfs zip archive and creates a data model for the connection searches to use
        /// </summary>
        /// <param name="gtfsZipArchiveLocation">The location of the zip gtfs archive</param>
        /// <param name="forbiddenCrossings">The list of lines forbidden to cross via a transfer</param>
        private static void LoadGtfsData(string gtfsZipArchiveLocation, List<ForbiddenCrossingLine> forbiddenCrossings, bool downloadNewIfFileOld = true)
        {
            forbiddenCrossingLines = forbiddenCrossings;

            TransitModel raptor;

            // Load the GTFS data and parse them into a TransitModel
            using (GTFS gtfs = GTFS.DownloadAndParseZipFile(gtfsZipArchiveLocation, downloadNewIfFileOld))
            {
                raptor = new TransitModel(gtfs, forbiddenCrossings);
            }

            // Collect the garbage to free up memory - to get the parsed transit data, we first had to create raw GTFS objects to parse the data from, which we now do not need anymore
            // This is the perfect place to remove them from memory, as from now on, we will only use the TransitModel object and its processed and linked data
            // This helps to keep the memory consumption low
            GC.Collect();


            transitModel = raptor;
        }

        /// <summary>
        /// Loads the data from the bike data sources and creates a data model for the connection searches to use
        /// </summary>
        private static void LoadGbfsData(string nextbikeDbFileLocation, bool useMocks = false)
        {
            // Create a bike model
            bikeModel = new BikeModel();


            // Create the data sources
            IBikeDataSource nextbike = new NextbikeDataSource { DistancesDbFileLocation = nextbikeDbFileLocation };


            // Add the data sources to the model
            bikeModel.AddDataSource(nextbike);


            if (!useMocks)
            {
                // Start the timer that periodically updates the bike counts
                //bikeModel.StartUpdateTimer();
                //bikeModel.UpdateAllStationStatus();
            }
            else
            {
                bikeModel.MockStationStatus();
            }
        }

        /// <summary>
        /// Loads the forbidden crossing data from the provided locations
        /// </summary>
        /// <param name="pointsLocation">Location of the forbidden crossing points file</param>
        /// <param name="linesLocation">Location of the forbidden crossing lines file</param>
        /// <returns>List of all ForbiddenCrossingLine objects</returns>
        private static List<ForbiddenCrossingLine> LoadForbiddenCrossings(string pointsLocation, string linesLocation)
        {
            List<ForbiddenCrossingPoint> points = new();
            Dictionary<int, ForbiddenCrossingPoint> pointsById = new();
            using (StreamReader reader = new StreamReader(pointsLocation))
            {
                string? line;
                reader.ReadLine(); // Skip the header
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');


                    int id = int.Parse(parts[0]);
                    double lon = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    double lat = double.Parse(parts[2], CultureInfo.InvariantCulture);
                    ForbiddenCrossingPoint newPoint = new ForbiddenCrossingPoint(new Coordinates(lat, lon), id);
                    points.Add(newPoint);
                    pointsById.Add(id, newPoint);
                }
            }

            List<ForbiddenCrossingLine> lines = new();
            using (StreamReader reader = new StreamReader(linesLocation))
            {
                string? line;
                reader.ReadLine(); // Skip the header
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');


                    int id = int.Parse(parts[0]);
                    int p1Id = int.Parse(parts[1]);
                    int p2Id = int.Parse(parts[2]);
                    string comment = parts[3];
                    ForbiddenCrossingLine newLine =
                        new ForbiddenCrossingLine(pointsById[p1Id], pointsById[p2Id], id, comment);
                    lines.Add(newLine);
                }
            }

            return lines;
        }

        /// <summary>
        /// Connects the transit and bike models through transfers between stops and bike stations
        /// </summary>
        /// <param name="forbiddenCrossings">The list of lines forbidden to cross on a transfer</param>
        private static void ConnectModelsThroughTransfers(List<ForbiddenCrossingLine> forbiddenCrossings)
        {
            foreach (Stop stop in transitModel!.stops.Values)
            {
                foreach (BikeStation bikeStation in bikeModel!.Stations)
                {
                    if (DistanceExtensions.TooFarInOneDirection(stop, bikeStation, TransitModel.MAX_TRANSFER_DISTANCE))
                    {
                        continue;
                    }

                    int distance = (int)DistanceExtensions.SimplifiedDistanceBetween(stop, bikeStation);
                    if (distance <= TransitModel.MAX_TRANSFER_DISTANCE &&
                        !forbiddenCrossings.ForbidsTransferBetween(stop, bikeStation))
                    {
                        var stopToBikeTransfer = new ToBikeTransfer(stop, bikeStation, distance);
                        var bikeToStopTransfer = new FromBikeTransfer(bikeStation, stop, distance);

                        stopToBikeTransfer.OppositeTransfer = bikeToStopTransfer;
                        bikeToStopTransfer.OppositeTransfer = stopToBikeTransfer;

                        stop.AddBikeTransfer(stopToBikeTransfer);
                        bikeStation.AddTransfer(bikeToStopTransfer);
                    }
                }
            }
        }

        private static void ValidateTransitModelLoaded()
        {
            if (transitModel is null)
            {
                throw new ApplicationException("Data from a GTFS archive were not loaded yet");
            }
        }

        private static void ValidateBikeModelLoaded()
        {
            if (bikeModel is null)
            {
                throw new ApplicationException("Data from a GBFS API were not loaded yet");
            }
        }
        private static void ValidateDelayModelLoaded()
        {
            if (delayModel is null)
            {
                throw new ApplicationException("Delay data were not loaded yet");
            }
        }

        private static void ValidateModelsLoaded()
        {
            ValidateTransitModelLoaded();
            ValidateBikeModelLoaded();
            ValidateDelayModelLoaded();
        }

        /// <summary>
        /// Creates a routing provider that can be provided to a range route finder to provide its basic routing functionality
        /// </summary>
        /// <param name="forward">Whether it will be used to run forward or backward searches</param>
        /// <param name="settings">The settings to use for the searches</param>
        /// <returns>The created routing provider</returns>
        public static ISimpleRoutingProvider CreateRoutingProvider(bool forward, Settings settings)
        {
            ValidateModelsLoaded();

            ISimpleRoutingProvider router = new BasicRouteFinder(forward, settings, transitModel!, bikeModel!, delayModel!);
            return router;
        }

        /// <summary>
        /// Creates a simple route finder that can be used to find a single connection between two points
        /// </summary>
        /// <param name="forward">Whether it will be used to run forward or backward searches</param>
        /// <param name="settings">The settings to use for the searches</param>
        /// <returns>The created simple route finder</returns>
        public static ISimpleRouteFinder CreateSimpleRouteFinder(bool forward, Settings settings)
        {
            ValidateModelsLoaded();

            ISimpleRouteFinder router = new BasicRouteFinder(forward, settings, transitModel!, bikeModel!, delayModel!);
            return router;
        }

        /// <summary>
        /// Creates an alternatives route finder that can be used to find alternative (earlier/later) direct trips between 2 points
        /// </summary>
        /// <returns>The created alternatives route finder</returns>
        public static AlternativesRouteFinder CreateDirectRouteFinder()
        {
            ValidateTransitModelLoaded();
            ValidateDelayModelLoaded();

            AlternativesRouteFinder router = new AlternativesRouteFinder(transitModel!, delayModel!);
            return router;
        }

        /// <summary>
        /// Creates a range route finder that can be used to find the best connections between two points within a certain time range
        /// </summary>
        /// <param name="forward">Whether it will be used to run forward or backward searches</param>
        /// <param name="settings">The settings to use for the searches</param>
        /// <returns>The created range route finder</returns>
        public static IRangeRouteFinder CreateRangeRouteFinder(bool forward, Settings settings)
        {
            ValidateModelsLoaded();

            IRangeRouteFinder router = new RangeRouteFinder(forward, settings, transitModel!, bikeModel!, delayModel!);
            return router;
        }

        /// <summary>
        /// Creates a delay updater that can be used to update the delay data of existing search results
        /// </summary>
        /// <returns>The created delay updater</returns>
        public static DelayUpdater CreateDelayUpdater()
        {
            ValidateTransitModelLoaded();
            ValidateDelayModelLoaded();

            DelayUpdater updater = new DelayUpdater(delayModel!);
            return updater;
        }
    }
}
