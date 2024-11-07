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
using TransitRealtime;
using System.Runtime.InteropServices;


namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// A class used for creating separate routers to be used for connection searching.
    /// </summary>
    public class RouteFinderBuilder
	{
		/// <summary>
		/// The transit model that the routers should use
		/// </summary>
		private static TransitModel? raptorModel;
        /// <summary>
        /// The bike model the routers should use
        /// </summary>
		private static BikeModel? bikeModel;

        private static DelayModel delayModel;

        private Timer _timer;
        /// <summary>
        /// Initializes the builder.
        /// </summary>
        public RouteFinderBuilder()
		{
			
		}


        


        private static void UpdateDelayModel(object state)
        {
            DelayModel newDelayModel = new();
            try
            {
                // Fetch the GTFS Realtime feed
                WebRequest req = HttpWebRequest.Create("https://api.golemio.cz/v2/vehiclepositions/gtfsrt/trip_updates.pb");
                FeedMessage feed = Serializer.Deserialize<FeedMessage>(req.GetResponse().GetResponseStream());

                // Process each entity (e.g., to find the latest departure delay)
                foreach (FeedEntity entity in feed.Entities)
                {
                    string tripId = entity.TripUpdate.Trip.TripId;
                    DateOnly tripStartDate = DateOnly.ParseExact(entity.TripUpdate.Trip.StartDate, "yyyyMMdd");
                    foreach (TripUpdate.StopTimeUpdate update in entity.TripUpdate.StopTimeUpdates)
                    {
                        if (update.Departure != null)
                        {
                            int arrivalDelay = update.Arrival.Delay;
                            int departureDelay = update.Departure.Delay;
                            newDelayModel.AddDelay(tripStartDate, tripId, arrivalDelay, departureDelay);
                        }
                    }
                }

                delayModel = newDelayModel;
                Console.WriteLine("Successfully updated delay data");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating delay info: " + ex.Message);
            }
        }

        /// <summary>
        /// Loads all the GTFS, GBFS and forbidden crossing data from the locations provided in the config file and bike data sources
        /// </summary>
        /// <exception cref="Exception">The configuration is wrong</exception>
		public void LoadAllData(string alternativeGtfsArchiveLocation = null)
		{
            var basePath = Directory.GetCurrentDirectory();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ".."));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                basePath = Path.GetFullPath(Path.Combine(basePath, ".."));
            }


            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();
            string gtfsZipArchiveLocation = config["gtfsArchiveLocation"];
            string forbiddenPointsLocation = config["forbiddenCrossingPointsLocation"];
            string forbiddenLinesLocation = config["forbiddenCrossingLinesLocation"];

            if(alternativeGtfsArchiveLocation != null)
            {
                Console.WriteLine(alternativeGtfsArchiveLocation);
                gtfsZipArchiveLocation = alternativeGtfsArchiveLocation;
            }

            if (gtfsZipArchiveLocation is null)
            {
                throw new Exception("No gtfs archive location found in the config file");
            }
            if (forbiddenPointsLocation == null || forbiddenLinesLocation == null)
            {
                throw new Exception("Forbidden crossing points or lines location not found in the config file");
            }




            var forbiddenCrossings = LoadForbiddenCrossings(forbiddenPointsLocation, forbiddenLinesLocation);

            LoadGtfsData(gtfsZipArchiveLocation, forbiddenCrossings);
			LoadGbfsData();
			ConnectModelsThroughTransfers(forbiddenCrossings);

            UpdateDelayModel(null);
            _timer = new Timer(UpdateDelayModel, null, 20000, 20000);
        }

		/// <summary>
		/// Parses the provided gtfs zip archive and creates a data model for the connection searches to use
		/// </summary>
		/// <param name="gtfsZipArchiveLocation">The location of the zip gtfs archive</param>
		public void LoadGtfsData(string gtfsZipArchiveLocation, List<ForbiddenCrossingLine> forbiddenCrossings)
		{
			TransitModel raptor;
			using (GTFS gtfs = GTFS.ParseZipFile(gtfsZipArchiveLocation))
			{
				raptor = new TransitModel(gtfs, forbiddenCrossings);
			}
			GC.Collect();
			raptorModel = raptor;
		}

        /// <summary>
        /// Loads the data from the bike data sources and creates a data model for the connection searches to use
        /// </summary>
		public void LoadGbfsData()
		{
            bikeModel = new BikeModel();

            IBikeDataSource nextbike = new NextbikeDataSource();
			nextbike.LoadStations();
			nextbike.LoadStationDistances();

						
			bikeModel.AddDataSource(nextbike);
            bikeModel.StartUpdateTimer();
        }

        /// <summary>
        /// Loads the forbidden crossing data from the provided locations
        /// </summary>
        /// <param name="pointsLocation">Location of the forbidden crossing points file</param>
        /// <param name="linesLocation">Location of the forbidden crossing lines file</param>
        /// <returns>List of all ForbiddenCrossingLine objects</returns>
        private List<ForbiddenCrossingLine> LoadForbiddenCrossings(string pointsLocation, string linesLocation)
        {
            List<ForbiddenCrossingPoint> points = new();
            Dictionary<int, ForbiddenCrossingPoint> pointsById = new();
            using (StreamReader reader = new StreamReader(pointsLocation))
            {
                string line;
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
                string line;
                reader.ReadLine(); // Skip the header
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');


                    int id = int.Parse(parts[0]);
                    int p1Id = int.Parse(parts[1]);
                    int p2Id = int.Parse(parts[2]);
                    string comment = parts[3];
                    ForbiddenCrossingLine newLine = new ForbiddenCrossingLine(pointsById[p1Id], pointsById[p2Id], id, comment);
                    lines.Add(newLine);
                }
            }

            return lines;
        }

        /// <summary>
        /// Connects the transit and bike models through transfers between stops and bike stations
        /// </summary>
        /// <param name="forbiddenCrossings">The list of lines forbidden to cross on a transfer</param>
        private void ConnectModelsThroughTransfers(List<ForbiddenCrossingLine> forbiddenCrossings)
        {
            foreach (Stop stop in raptorModel.stops.Values)
            {
                foreach (BikeStation bikeStation in bikeModel.Stations)
                {
                    if (DistanceExtensions.TooFarInOneDirection(stop, bikeStation, TransitModel.MAX_TRANSFER_DISTANCE))
                    {
                        continue;
                    }
                    int distance = (int)DistanceExtensions.SimplifiedDistanceBetween(stop, bikeStation);
                    if (distance <= TransitModel.MAX_TRANSFER_DISTANCE && !forbiddenCrossings.ForbidsTransferBetween(stop, bikeStation))
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


        public IRouteFinder CreateUniversalRouteFinder(bool forward, Settings settings)
        {
            if (raptorModel is null)
            {
                throw new ApplicationException("Data from a gtfs archive were not loaded yet");
            }

            if (bikeModel is null)
            {
                throw new ApplicationException("Data from a gbfs api were not loaded yet");
            }

            IRouteFinder router = new BasicRouteFinder(forward, settings, raptorModel, bikeModel, delayModel);
            return router;
        }

        public AlternativesRouteFinder CreateDirectRouteFinder()
        {
            if (raptorModel is null)
            {
                throw new ApplicationException("Data from a gtfs archive were not loaded yet");
            }

            if (bikeModel is null)
            {
                throw new ApplicationException("Data from a gbfs api were not loaded yet");
            }

            AlternativesRouteFinder router = new AlternativesRouteFinder(raptorModel, delayModel);
            return router;
        }

        public RangeRouteFinder CreateRangeRouteFinder(bool forward, Settings settings)
        {
            if (raptorModel is null)
            {
                throw new ApplicationException("Data from a gtfs archive were not loaded yet");
            }

            if (bikeModel is null)
            {
                throw new ApplicationException("Data from a gbfs api were not loaded yet");
            }

            RangeRouteFinder router = new RangeRouteFinder(forward, settings, raptorModel, bikeModel, delayModel);
            return router;
        }



        /// <summary>
        /// Validates the provided stop name - checks if it exists in the transit model
        /// </summary>
        /// <param name="stopName">The stop name to check</param>
        /// <returns>If the stop name exists in the transit model</returns>
		public bool ValidateStopName(string stopName)
		{
			if (raptorModel is null)
			{
				return false;
			}
			return raptorModel.GetStopsByName(stopName).Count != 0;
		}
        /// <summary>
        /// Validates the provided coordinates - checks if they are within the allowed range
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <returns>If the values are real coordinates</returns>
        public bool ValidateCoords(double lat, double lon)
        {
            return lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180;
        }

        /// <summary>
        /// Validates that at least one stop or bike station exists within the given radius of the provided coordinates
        /// </summary>
        /// <param name="lat">The latitude of the point</param>
        /// <param name="lon">The longitude of the point</param>
        /// <param name="includeBikes">Whether a bike station is enough for running the search (i.e. whether bikes can be used)</param>
        /// <returns>If there is at least one RoutePoint near the coordinates to run search to/from</returns>
        public bool ValidateStopsNearCoords(double lat, double lon, bool includeBikes)
        {
            if (raptorModel is null)
            {
                return false;
            }

            if (includeBikes)
            {
                return raptorModel.NearStopExists(lat, lon, 750) || bikeModel.NearStationExists(lat, lon, 750);
            }
            else
            {
                return raptorModel.NearStopExists(lat, lon, 750);
            }
        }
	}
}
