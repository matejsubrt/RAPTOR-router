using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Models.Dynamic;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Structures.Generic;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// Class used for finding the quickest connection from source to destination by earliest possible departure time
    /// </summary>
    public class ForwardRouteFinder : IRouteFinder
    {
        /// <summary>
        /// The transit model holding all the static information about the transit network
        /// </summary>
        private TransitModel raptorModel;
        /// <summary>
        /// The bike model holding all the information about the shared bike systems and their stations
        /// </summary>
        private BikeModel bikeModel;
        /// <summary>
        /// The search model, that the router will use for the connection searching algorithm
        /// </summary>
        private ForwardSearchModel searchModel;


        /// <summary>
        /// A set of currently marked stops
        /// </summary>
        private HashSet<Stop> markedStops = new();
        /// <summary>
        /// A set of all currently marked bike stations
        /// </summary>
        private HashSet<BikeStation> markedBikeStations = new();
        /// <summary>
        /// A dictionary storing for every currently marked route the stop at which it first can be boarded - i.e. the first marked stop it passes through
        /// </summary>
        private Dictionary<Route, Stop> markedRoutesWithGetOnStops = new();


        /// <summary>
        /// The settings to be used for the connection search
        /// </summary>
        private Settings settings;

        /// <summary>
        /// The current round of the RAPTOR algorithm
        /// </summary>
        private int round = 0;

        /// <summary>
        /// Creates a new BasicRouter object
        /// </summary>
        /// <param name="settings">The settings to be used for the connection search</param>
        /// <param name="transitModel">The transit model holding all the static information about the transit network</param>
        /// <param name="bikeModel">The bike model holding all the information about the shared bike systems and their stations</param>
        internal ForwardRouteFinder(Settings settings, TransitModel transitModel, BikeModel bikeModel)
        {
            this.settings = settings;
            this.raptorModel = transitModel;
            this.bikeModel = bikeModel;
        }

        

        /// <summary>
        /// Initiates the search by setting earliest arrival for source stops, marks them and improves arrival times for their neighbors in round 0
        /// </summary>
        /// <remarks>To be used for searches by stop name</remarks>
        private void InitiateSearchFromStops(bool useSharedBikes)
        {
            searchModel.SetSourceStopsEarliestDeparture();
            if(useSharedBikes)
            {
                searchModel.SetSourceBikeStationsEarliestDeparture();
            }

            MarkSourceStops();
            if(useSharedBikes)
            {
                MarkSourceBikeStations();
            }

            //TODO: check this after implementing search from coordinates
            ImproveByTransfers(useSharedBikes, true); // only from stops -> in 0th round, only transfers from source stops are considered
            

            void MarkSourceStops()
            {
                foreach (Stop sourceStop in searchModel.sourceStops)
                {
                    markedStops.Add(sourceStop);
                }
            }
            void MarkSourceBikeStations()
            {
                foreach(BikeStation sourceBikeStation in searchModel.sourceBikeStations)
                {
                    markedBikeStations.Add(sourceBikeStation);
                }
            }
        }

        /// <summary>
        /// Initiates the search by setting earliest arrival for all stops that can be reached from the custom source route point, marks them and improves arrival times in round 0
        /// </summary>
        /// <remarks>To be used for searches by coordinates</remarks>
        private void InitiateSearchFromCustomRoutePoint(CustomRoutePoint customSrcRP, bool useSharedBikes)
        {
            foreach(ITransfer transfer in customSrcRP.possibleTransfers)
            {
                IRoutePoint rp = transfer.GetDestRoutePoint();
                if(transfer.Distance > settings.GetMaxTransferDistance())
                {
                    continue;
                }
                if(rp is Stop)
                {
                    DateTime arrivalTime = searchModel.GetDepartureTime().AddSeconds(transfer.GetTransferTime(settings.WalkingPace));
                    searchModel.SetEarliestArrival(rp, arrivalTime);
                    searchModel.SetTransferArrivalInRound(rp, transfer, arrivalTime, round);
                    markedStops.Add((Stop)rp);
                }
                else if(useSharedBikes && rp is BikeStation)
                {
                    DateTime arrivalTime = searchModel.GetDepartureTime().AddSeconds(transfer.GetTransferTime(settings.WalkingPace) + settings.BikeUnlockTime);
                    searchModel.SetEarliestArrival(rp, arrivalTime);
                    searchModel.SetTransferArrivalInRound(rp, transfer, arrivalTime, round);
                    markedBikeStations.Add((BikeStation)rp);
                }
            }
        }


        /// <summary>
        /// Accumulates all routes passing through the marked stops and finds the earliest marked stop for them
        /// </summary>
        private void AccumulateRoutes()
        {
            markedRoutesWithGetOnStops.Clear();
            foreach (Stop markedStop in markedStops)
            {
                foreach (Route route in markedStop.StopRoutes)
                {
                    if (markedRoutesWithGetOnStops.ContainsKey(route))
                    {
                        if (route.GetFirstStopIndex(markedRoutesWithGetOnStops[route]) > route.GetFirstStopIndex(markedStop))
                        {
                            markedRoutesWithGetOnStops[route] = markedStop;
                        }
                    }
                    else
                    {
                        markedRoutesWithGetOnStops.Add(route, markedStop);
                    }
                }
                //TODO: Is this neccessary?
                markedStops.Remove(markedStop);
            }
        }

        /// <summary>
        /// Traverses all the marked routes, improving the arrival times and info for all stops where it is possible
        /// </summary>
        private void TraverseMarkedRoutes()
        {
            foreach (KeyValuePair<Route, Stop> pair in markedRoutesWithGetOnStops)
            {
                Route route = pair.Key;
                Stop getOnStop = pair.Value;
                DateOnly tripDate;

                //TODO: shouldnt this be just trip arrival?
                DateTime earliestArrivalAtGetOnStopLastRound = searchModel.GetEarliestArrivalInRound(getOnStop, round - 1);
                // in round 1, the arrival time is the start time -> no need for buffer
                if (round > 1 && searchModel.RoutePointIsReachedByTripInRound(getOnStop, round - 1))
                {
                    earliestArrivalAtGetOnStopLastRound = earliestArrivalAtGetOnStopLastRound.AddSeconds(settings.GetStationaryTransferMinimumSeconds());
                }

                Trip trip = route.GetEarliestTripDepartingAfterTimeAtStop(
                    getOnStop,
                    DateOnly.FromDateTime(earliestArrivalAtGetOnStopLastRound),
                    TimeOnly.FromDateTime(earliestArrivalAtGetOnStopLastRound),
                    Settings.MAX_TRIP_LENGTH_DAYS,
                    out tripDate
                );

                TraverseRoute(route, getOnStop, trip, tripDate);
            }

            void TraverseRoute(Route route, Stop getOnStop, in Trip trip, DateOnly tripDate)
            {
                Trip currTrip = trip;
                
                for (int i = route.GetFirstStopIndex(getOnStop); i < route.RouteStops.Count; i++)
                {
                    Stop currStop = route.RouteStops[i];

                    

                    if (currTrip is not null)
                    {
                        StopTime stopTime = currTrip.StopTimes[i];                        

                        DateOnly realDate;
                        if (TripGoesOverMidnight(currTrip, route.GetFirstStopIndex(getOnStop), i))
                            realDate = tripDate.AddDays(1);
                        else
                            realDate = tripDate;


                        DateTime arrivalTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.ArrivalTime);
                        DateTime departureTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.DepartureTime);

                        bool improved = searchModel.TryImproveArrivalByTrip(currStop, arrivalTime, currTrip, getOnStop, round);
                        if (improved)
                        {
                            markedStops.Add(currStop);
                        }

                        if (DepartureIsLaterThanLastRoundArrival(currStop, departureTime))
                        {
                            //TODO: same as above
                            DateTime earliestArrivalLastRound = searchModel.GetEarliestArrivalInRound(currStop, round - 1);
                            // in first round, no buffer needed
                            if (round != 1)
                            {
                                if(searchModel.RoutePointIsReachedByTripInRound(currStop, round - 1))
                                {
                                    earliestArrivalLastRound = earliestArrivalLastRound.AddSeconds(settings.GetStationaryTransferMinimumSeconds());
                                }                                
                            }

                            if(earliestArrivalLastRound > departureTime)
                            {
                                continue;
                            }

                            DateOnly earliestArrivalLastRoundDate = DateOnly.FromDateTime(earliestArrivalLastRound);
                            TimeOnly earliestArrivalLastRoundTime = TimeOnly.FromDateTime(earliestArrivalLastRound);



                            Trip newTrip = route.GetEarliestTripDepartingAfterTimeAtStop(
                                currStop,
                                earliestArrivalLastRoundDate,
                                earliestArrivalLastRoundTime,
                                Settings.MAX_TRIP_LENGTH_DAYS,
                                out tripDate);
                            if (newTrip != currTrip || searchModel.GetEarliestArrival(currStop) < searchModel.GetEarliestArrival(getOnStop))
                            {
                                currTrip = newTrip;
                                getOnStop = currStop;
                            }
                        }
                    }
                }

                bool TripGoesOverMidnight(Trip trip, int getOnStopIndex, int currStopIndex)
                {
                    return trip.StopTimes[getOnStopIndex].DepartureTime > trip.StopTimes[currStopIndex].ArrivalTime;
                }
                
                bool DepartureIsLaterThanLastRoundArrival(Stop stop, DateTime departureTime)
                {
                    return searchModel.GetEarliestArrivalInRound(stop, round - 1) < departureTime;
                }
                /*bool ArrivalTimeImprovesCurrBest(DateTime arrivalTime, Stop stop)
                {
                    return arrivalTime < searchModel.GetEarliestArrival(stop)
                            && arrivalTime < searchModel.GetCurrentBestArrivalTime()
                            && arrivalTime <= searchModel.GetDepartureTime().AddDays(Settings.MAX_TRIP_LENGTH_DAYS);
                }
                void ImproveArrivalByTrip(Stop stop, DateTime arrivalTime, Trip trip, Stop getOnStop)
                {
                    searchModel.SetTripArrivalInRound(stop, trip, getOnStop, arrivalTime, round);
                    //TODO: check
                    searchModel.SetEarliestArrival(stop, arrivalTime);

                    if (searchModel.destinationStops.Contains(stop) && arrivalTime < searchModel.GetCurrentBestArrivalTime())
                    {
                        searchModel.SetCurrentBestArrivalTime(arrivalTime);
                    }
                }*/
            }
        }

        /// <summary>
        /// For all marked bike stations, traverses all the possible bike trips from them and improves the arrival times for all stops where it is possible
        /// </summary>
        void TraverseBikeRoutes()
        {
            HashSet<BikeStation> newMarkedBikeStations = new();
            foreach(BikeStation markedBikeStation in markedBikeStations)
            {
                if(markedBikeStation.BikeCount == 0)
                {
                    continue;
                }
                if (searchModel.RoutePointIsReachedByBikeInRound(markedBikeStation, round - 1))
                {
                    continue;
                }
                Dictionary<BikeStation, int> distances = bikeModel.GetDistancesFromStation(markedBikeStation);
                foreach(KeyValuePair<BikeStation, int> pair in distances)
                {
                    BikeStation destBikeStation = pair.Key;
                    if (destBikeStation.Name == "P8 - Palmovka Open Park")
                    {
                        Console.WriteLine();
                    }
                    int distance = pair.Value;



                    if (distance == -1)
                    {
                        continue;
                    }
                    if(settings.BikeMax15Minutes && GetCyclingTime(distance) > 15 * 60)
                    {
                        continue;
                    }
                    

                    int cyclingTimeSeconds = GetCyclingTime(distance);
                    DateTime srcStopArrivalTime = searchModel.GetEarliestArrivalInRound(markedBikeStation, round - 1);
                    DateTime arrivalUsingBicycle = srcStopArrivalTime.AddSeconds(cyclingTimeSeconds + settings.BikeUnlockTime);

                    bool improved = searchModel.TryImproveArrivalByBikeTrip(markedBikeStation, destBikeStation, arrivalUsingBicycle, round);
                    if (improved)
                    {
                        newMarkedBikeStations.Add(destBikeStation);
                    }
                }
            }
            markedBikeStations = newMarkedBikeStations;
            int GetCyclingTime(int distance)
            {
                return (int)((distance / 1000.0 * settings.CyclingPace * 60) * settings.GetBikeTripLengthMultiplier());
            }
            /*bool ArrivalTimeImprovesCurrBest(DateTime arrivalTime, BikeStation bikeStation)
            {
                return arrivalTime < searchModel.GetEarliestArrival(bikeStation)
                        && arrivalTime < searchModel.GetCurrentBestArrivalTime()
                        && arrivalTime <= searchModel.GetDepartureTime().AddDays(Settings.MAX_TRIP_LENGTH_DAYS);
            }
            void ImproveArrivalByBikeTrip(BikeStation fromBikeStation, BikeStation toBikeStation, DateTime arrivalTime)
            {
                searchModel.SetBikeTripArrivalInRound(fromBikeStation, toBikeStation, arrivalTime, round);
                //TODO: check
                searchModel.SetEarliestArrival(toBikeStation, arrivalTime);
                if (searchModel.destinationBikeStations.Contains(toBikeStation) && arrivalTime < searchModel.GetCurrentBestArrivalTime())
                {
                    searchModel.SetCurrentBestArrivalTime(arrivalTime);
                }
            }*/
        }

        /// <summary>
        /// Takes all the stops that have been improved in current round and tries to improve all their neighbors by transfers
        /// </summary>
        /// <param name="DoNotImproveToRoutePoint">To be used when the destination is a custom RoutePoint - in that case, its near stops (from which it can be accessed by foot) may NOT also be accessed by foot</param>
        private void ImproveByTransfers(bool useSharedBikes, bool onlyFromStops = false, Func<IRoutePoint, bool> DoNotImproveToRoutePoint = null)
        {
            HashSet<Stop> newMarkedStops = new();
            HashSet<BikeStation> newMarkedBikeStations = new();
            foreach (Stop markedStop in markedStops)
            {
                foreach (Transfer transfer in markedStop.Transfers)
                {
                    // Improve by Stop-to-Stop transfers
                    bool improved = searchModel.TryImproveArrivalByTransfer(transfer, false, round, DoNotImproveToRoutePoint);
                    if (improved)
                    {
                        newMarkedStops.Add(transfer.To);
                    }
                }
                if (useSharedBikes)
                {
                    foreach (BikeTransfer bikeTransfer in markedStop.BikeTransfers)
                    {
                        // Improve by Stop-to-BikeStation transfers
                        bool improved = searchModel.TryImproveArrivalByTransfer(bikeTransfer, true, round, DoNotImproveToRoutePoint);
                        if (improved)
                        {
                            newMarkedBikeStations.Add((BikeStation)bikeTransfer.GetDestRoutePoint());
                        }
                    }
                }                
            }
            if (useSharedBikes && !onlyFromStops)
            {
                foreach (BikeStation markedBikeStation in markedBikeStations)
                {
                    foreach (BikeTransfer bikeTransfer in markedBikeStation.Transfers)
                    {
                        // Improve by BikeStation-to-Stop transfers
                        bool improved = searchModel.TryImproveArrivalByTransfer(bikeTransfer, false, round, DoNotImproveToRoutePoint);
                        if (improved)
                        {
                            newMarkedStops.Add((Stop)bikeTransfer.GetDestRoutePoint());
                        }
                    }
                }
            }
            
            markedStops.UnionWith(newMarkedStops);
            if (useSharedBikes)
            {
                markedBikeStations.UnionWith(newMarkedBikeStations);
            }
            /*bool TransferImprovesArrivalTime(ITransfer transfer)
            {
                IRoutePoint from = transfer.GetSrcRoutePoint();
                IRoutePoint to = transfer.GetDestRoutePoint();
                //DateTime currEarliestArrival = searchModel.GetEarliestArrivalInRound(transfer.To, round);
                DateTime currEarliestArrival = searchModel.GetEarliestArrival(to);
                DateTime earliestArrivalWithTransfer = searchModel.GetEarliestArrivalInRound(from, round);

                int stationaryTransferSeconds = settings.GetStationaryTransferMinimumSeconds();
                if (transfer.Distance == 0)
                {
                    // if transfer length is 0, the transfer is stationary -> it takes the time of the stationary transfer minimum
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(stationaryTransferSeconds);
                }
                else
                {
                    // if transfer length is not 0, the transfer is moving -> it takes the time of the moving transfer length, but if this is lower than the stationary transfer minimum, the stationary transfer minimum is used
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(Math.Max((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()), stationaryTransferSeconds));
                }

                return currEarliestArrival > earliestArrivalWithTransfer;
            }
            void ImproveArrivalByTransfer(ITransfer transfer, bool toBikeStation, int extraSeconds = 0)
            {
                IRoutePoint from = transfer.GetSrcRoutePoint();
                IRoutePoint to = transfer.GetDestRoutePoint();
                DateTime earliestArrivalWithTransfer = searchModel.GetEarliestArrivalInRound(from, round);

                int stationaryTransferSeconds = settings.GetStationaryTransferMinimumSeconds();
                if (transfer.Distance == 0)
                {
                    // if transfer length is 0, the transfer is stationary -> it takes the time of the stationary transfer minimum
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(stationaryTransferSeconds + extraSeconds);
                }
                else
                {
                    // If the transfer is to a bike station, we do not need the safety buffer for transfers -> the bike is always there
                    if(toBikeStation)
                    {
                        earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()) + extraSeconds);
                    }
                    else
                    {
                        // if transfer length is not 0, the transfer is moving -> it takes the time of the moving transfer length, but if this is lower than the stationary transfer minimum, the stationary transfer minimum is used
                        earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(Math.Max((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()), stationaryTransferSeconds) + extraSeconds);
                    }
                }

                //searchModel.SetEarliestArrivalInRound(transfer.To, round, earliestArrivalWithTransfer);
                searchModel.SetTransferArrivalInRound(to, transfer, earliestArrivalWithTransfer, round);
                if (searchModel.GetEarliestArrival(to) > searchModel.GetEarliestArrivalInRound(to, round))
                {
                    searchModel.SetEarliestArrival(to, searchModel.GetEarliestArrivalInRound(to, round));
                }
            }*/
        }

        /// <summary>
        /// Finds the connection with the earliest arrival to one of the destinationStations in the searchModel
        /// </summary>
        /// <param name="searchModel">The search model to be used for finding the connection</param>
        /// <returns>The quickest connection from one of the sourceStops to one of the destinationStops in the searchModel</returns>
        /*internal SearchResult FindConnection(ForwardSearchModel searchModel)
        {
            this.searchModel = searchModel;

            InitiateSearchFromStops(settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                ImproveByTransfers(settings.UseSharedBikes);
            }
            markedStops.Clear();
            markedRoutesWithGetOnStops.Clear();
            markedBikeStations.Clear();
            return searchModel.ExtractResult(bikeModel);
        }*/

        public Tuple<List<Stop>, List<BikeStation>> GetNearRoutePoints(double lat, double lon)
        {
            List<Stop> nearStops = raptorModel.GetStopsByLocation(lat, lon, settings.GetMaxTransferDistance());
            List<BikeStation> nearBikeStations = bikeModel.GetNearStations(lat, lon, settings.GetMaxTransferDistance());
            return new Tuple<List<Stop>, List<BikeStation>>(nearStops, nearBikeStations);
        }

        public Tuple<List<Stop>, List<BikeStation>> GetNearRoutePoints(string stopName)
        {
            List<Stop> stops = raptorModel.GetStopsByName(stopName);
            List<BikeStation> bikeStations = new List<BikeStation>();
            return new Tuple<List<Stop>, List<BikeStation>>(stops, bikeStations);
        }



        private SearchResult FindConnection(List<Stop> srcStops, List<BikeStation> srcBikeStations, List<Stop> destStops, List<BikeStation> destBikeStations, DateTime departureTime, bool srcByCoord, bool destByCoord, Coordinates srcCoords = default, Coordinates destCoords = default)
        {
            if (settings.UseSharedBikes)
            {
                if ((srcStops.Count == 0 && srcBikeStations.Count == 0) || (destStops.Count == 0 && destBikeStations.Count == 0))
                {
                    return null;
                }
            }
            else
            {
                if (srcStops.Count == 0 || destStops.Count == 0)
                {
                    return null;
                }
            }
            

            this.searchModel = new ForwardSearchModel(srcStops, destStops, srcBikeStations, destBikeStations, departureTime, settings);


            HashSet<IRoutePoint> destRoutePoints = new HashSet<IRoutePoint>();
            destRoutePoints.UnionWith(destStops);
            destRoutePoints.UnionWith(destBikeStations);

            CustomRoutePoint sourceRoutePoint = new CustomRoutePoint("srcId", "Source", srcCoords);
            CustomRoutePoint destRoutePoint = new CustomRoutePoint("destId", "Destination", destCoords);


            if (srcByCoord)
            {
                // set the starting custom route point
                
                foreach (Stop srcStop in srcStops)
                {
                    sourceRoutePoint.AddTransferToRoutePoint(srcStop);
                }
                foreach (BikeStation srcStation in srcBikeStations)
                {
                    sourceRoutePoint.AddTransferToRoutePoint(srcStation);
                }

                this.searchModel.sourceCustomRoutePoint = sourceRoutePoint;
            }


            if (destByCoord)
            {
                // set the ending custom route point
                
                foreach (Stop destStop in destStops)
                {
                    destRoutePoint.AddTransferFromRoutePoint(destStop);
                }
                foreach (BikeStation destStation in destBikeStations)
                {
                    destRoutePoint.AddTransferFromRoutePoint(destStation);
                }


                this.searchModel.destinationCustomRoutePoint = destRoutePoint;
            }


            if (srcByCoord)
            {
                InitiateSearchFromCustomRoutePoint(sourceRoutePoint, settings.UseSharedBikes);
            }
            else
            {
                InitiateSearchFromStops(settings.UseSharedBikes);
            }

            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                if (settings.UseSharedBikes)
                {
                    TraverseBikeRoutes();
                }

                if (destByCoord)
                {
                    ImproveByTransfers(settings.UseSharedBikes, false, (x) => destRoutePoints.Contains(x));
                }
                else
                {
                    ImproveByTransfers(settings.UseSharedBikes);
                }
            }


            markedStops.Clear();
            markedRoutesWithGetOnStops.Clear();
            return searchModel.ExtractResult(bikeModel);
        }

        /// <summary>
        /// Finds the connection with the earliest arrival to a destination stop with the provided name, that departs from the source stop after the specified time.
        /// </summary>
        /// <param name="sourceStop">The exact name of the source stop</param>
        /// <param name="destStop">The exact name of the destination stop</param>
        /// <param name="departureTime">The departure date and time</param>
        /// <returns>The result of the search, null if no conection could be found.</returns>
        public SearchResult FindConnection(string sourceStop, string destStop, DateTime departureTime)
        {
            /*//bikeModel.UpdateStationStatus();
            if (sourceStop == destStop)
            {
                return null;
            }
            List<Stop> sourceStops = raptorModel.GetStopsByName(sourceStop);
            List<Stop> destStops = raptorModel.GetStopsByName(destStop);

            // If bikes cannot be used and either the source or the destination stop is not found, return null
            if (sourceStops.Count == 0 || destStops.Count == 0)
            {
                //Console.WriteLine("Incorrect stop name/s");
                return null;
            }
            //TODO: THIS NEEDS TO BE MODIFIED!!!!
            List<BikeStation> sourceBikeStations = bikeModel.GetNearStations(sourceStops[0], 100);
            List<BikeStation> destBikeStations = bikeModel.GetNearStations(destStops[0], 100);

            // If bikes can be used, but either the source or the destination stop or bike station is not found, return null
            if (settings.UseSharedBikes && (sourceStops.Count + sourceBikeStations.Count == 0 || destStops.Count + destBikeStations.Count == 0))
            {
                return null;
            }


            this.searchModel = new ForwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, departureTime, settings);


            InitiateSearchFromStops(settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                if (settings.UseSharedBikes)
                {
                    TraverseBikeRoutes();
                }
                ImproveByTransfers(settings.UseSharedBikes);
            }
            markedStops.Clear();
            markedRoutesWithGetOnStops.Clear();
            return searchModel.ExtractResult(bikeModel);*/

            List<Stop> srcStops = raptorModel.GetStopsByName(sourceStop);
            List<Stop> destStops = raptorModel.GetStopsByName(destStop);
            List<BikeStation> srcBikeStations = new List<BikeStation>();
            List<BikeStation> destBikeStations = new List<BikeStation>();
            return FindConnection(srcStops, srcBikeStations, destStops, destBikeStations, departureTime, false, false);
        }

        /// <summary>
        /// Finds the connection with the earliest arrival to the destination custom route point, that departs from the source custom route point after the specified time.
        /// </summary>
        /// <remarks>Used for searches by coordinates</remarks>
        /// <param name="srcLat">The latitude of the source point</param>
        /// <param name="srcLon">The longitude of the source point</param>
        /// <param name="destLat">The latitude of the destination point</param>
        /// <param name="destLon">The longitude of the destination point</param>
        /// <param name="departureTime">The arrival date and time</param>
        /// <returns>The result of the search, null if no conection could be found.</returns>
        public SearchResult FindConnection(double srcLat, double srcLon, double destLat, double destLon, DateTime departureTime)
        {
            /*CustomRoutePoint source = new CustomRoutePoint("srcId", "Source", new Coordinates(srcLat, srcLon));
            CustomRoutePoint dest = new CustomRoutePoint("destId", "Destination", new Coordinates(destLat, destLon));



            List<Stop> sourceStops = raptorModel.GetStopsByLocation(srcLat, srcLon, settings.GetMaxTransferDistance());
            List<Stop> destStops = raptorModel.GetStopsByLocation(destLat, destLon, settings.GetMaxTransferDistance());

            List<BikeStation> sourceBikeStations = bikeModel.GetNearStations(srcLat, srcLon, settings.GetMaxTransferDistance());
            List<BikeStation> destBikeStations = bikeModel.GetNearStations(destLat, destLon, settings.GetMaxTransferDistance());

            if ((sourceStops.Count == 0 && sourceBikeStations.Count == 0) || (destStops.Count == 0 && destBikeStations.Count == 0))
            {
                //Console.WriteLine("Incorrect stop name/s");
                return null;
            }


            foreach (Stop srcStop in sourceStops)
            {
                source.AddTransferToRoutePoint(srcStop);
            }
            foreach (BikeStation srcStation in sourceBikeStations)
            {
                source.AddTransferToRoutePoint(srcStation);
            }

            foreach (Stop destStop in destStops)
            {
                dest.AddTransferFromRoutePoint(destStop);
            }
            foreach (BikeStation destStation in destBikeStations)
            {
                dest.AddTransferFromRoutePoint(destStation);
            }


            HashSet<IRoutePoint> destRoutePoints = new HashSet<IRoutePoint>();
            destRoutePoints.UnionWith(destStops);
            destRoutePoints.UnionWith(destBikeStations);




            this.searchModel = new ForwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, departureTime, settings);
            this.searchModel.sourceCustomRoutePoint = source;
            this.searchModel.destinationCustomRoutePoint = dest;

            InitiateSearchFromCustomRoutePoint(source, settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                TraverseBikeRoutes();
                ImproveByTransfers(settings.UseSharedBikes, false, (x) => destRoutePoints.Contains(x));
            }
            markedStops.Clear();
            markedRoutesWithGetOnStops.Clear();
            return searchModel.ExtractResult(bikeModel);*/

            var (srcStops, srcBikeStations) = GetNearRoutePoints(srcLat, srcLon);
            var (destStops, destBikeStations) = GetNearRoutePoints(destLat, destLon);
            return FindConnection(srcStops, srcBikeStations, destStops, destBikeStations, departureTime, true, true, new Coordinates(srcLat, srcLon), new Coordinates(destLat, destLon));

        }
    }
}
