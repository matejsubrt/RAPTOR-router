using RAPTOR_Router.SearchModels;
using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.GTFSParsing;

namespace RAPTOR_Router.Routers
{
    /// <summary>
    /// Basic router used for finding the best connection from a source stop to a destination stop using only public transit. It only takes the arrival time into account, i.e. is to be used in situations, where the arrival time is the only important factor (doesn't take into account comfort/transfers/...)
    /// </summary>
    public class AdvancedRouteFinder : IRouteFinder
    {
        private RAPTORModel raptorModel;
        /// <summary>
        /// The search model, that the router will use for the connection searching algorithm
        /// </summary>
        private SearchModel searchModel;
        /// <summary>
        /// A set of currently marked stops
        /// </summary>
        private HashSet<Stop> markedStops = new();
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
        internal AdvancedRouteFinder(Settings settings, RAPTORModel raptorModel)
        {
            this.settings = settings;
            this.raptorModel = raptorModel;
        }

        /// <summary>
        /// Initiates the search by setting earliest arrival for source stops, marks them and improves arrival times for their neighbors in round 0
        /// </summary>
        private void InitiateSearch()
        {
            searchModel.SetSourceStopsEarliestArrival();
            MarkSourceStops();
            ImproveByTransfers();

            void MarkSourceStops()
            {
                foreach (Stop sourceStop in searchModel.sourceStops)
                {
                    markedStops.Add(sourceStop);
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
                        if (route.GetStopIndex(markedRoutesWithGetOnStops[route]) > route.GetStopIndex(markedStop))
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

                DateTime earliestArrivalAtGetOnStopLastRound = searchModel.GetEarliestArrivalInRound(getOnStop, round - 1);
                // in round 1, the arrival time is the start time -> no need for buffer
                if(round > 1)
                {
                    earliestArrivalAtGetOnStopLastRound = earliestArrivalAtGetOnStopLastRound.AddSeconds(settings.GetStationaryTransferMinimumSeconds());
                }

                Trip trip = route.GetEarliestTripAtStop(
                    getOnStop,
                    DateOnly.FromDateTime(earliestArrivalAtGetOnStopLastRound),
                    TimeOnly.FromDateTime(earliestArrivalAtGetOnStopLastRound),
                    Settings.MAX_TRIP_LENGTH_DAYS,
                    out tripDate
                );

                TraverseRoute(route, getOnStop, trip, tripDate);
            }

            void TraverseRoute(Route route, Stop getOnStop, Trip trip, DateOnly tripDate)
            {
                for (int i = route.GetStopIndex(getOnStop); i < route.RouteStops.Count; i++)
                {
                    Stop currStop = route.RouteStops[i];

                    if (trip is not null)
                    {
                        StopTime stopTime = trip.StopTimes[i];

                        DateOnly realDate;
                        if (TripGoesOverMidnight(trip, route.GetStopIndex(getOnStop), i))
                            realDate = tripDate.AddDays(1);
                        else
                            realDate = tripDate;


                        DateTime arrivalTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.ArrivalTime);
                        DateTime departureTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.DepartureTime);

                        if (ArrivalTimeImprovesCurrBest(arrivalTime, currStop))
                        {
                            ImproveArrivalByTrip(currStop, arrivalTime, trip, getOnStop);
                            markedStops.Add(currStop);
                        }

                        if (DepartureIsLaterThanLastRoundArrival(currStop, departureTime))
                        {
                            DateTime earliestArrivalLastRound = searchModel.GetEarliestArrivalInRound(currStop, round - 1);
                            // in first round, no buffer needed
                            if(round != 1)
                            {
                                earliestArrivalLastRound = earliestArrivalLastRound.AddSeconds(settings.GetStationaryTransferMinimumSeconds());
                            }

                            DateOnly earliestArrivalLastRoundDate = DateOnly.FromDateTime(earliestArrivalLastRound);
                            TimeOnly earliestArrivalLastRoundTime = TimeOnly.FromDateTime(earliestArrivalLastRound);



                            Trip newTrip = route.GetEarliestTripAtStop(
                                currStop,
                                earliestArrivalLastRoundDate,
                                earliestArrivalLastRoundTime,
                                Settings.MAX_TRIP_LENGTH_DAYS,
                                out tripDate);
                            if (newTrip != trip || searchModel.GetEarliestArrival(currStop) < searchModel.GetEarliestArrival(getOnStop))
                            {
                                trip = newTrip;
                                getOnStop = currStop;
                            }
                        }
                    }
                }

                bool TripGoesOverMidnight(Trip trip, int getOnStopIndex, int currStopIndex)
                {
                    return trip.StopTimes[getOnStopIndex].DepartureTime > trip.StopTimes[currStopIndex].ArrivalTime;
                }
                bool ArrivalTimeImprovesCurrBest(DateTime arrivalTime, Stop stop)
                {
                    return arrivalTime < searchModel.GetEarliestArrival(stop)
                            && arrivalTime < searchModel.GetCurrentBestArrivalTime()
                            && arrivalTime <= searchModel.GetDepartureTime().AddDays(Settings.MAX_TRIP_LENGTH_DAYS);
                }
                bool DepartureIsLaterThanLastRoundArrival(Stop stop, DateTime departureTime)
                {
                    return searchModel.GetEarliestArrivalInRound(stop, round - 1) <= departureTime;
                }
                void ImproveArrivalByTrip(Stop stop, DateTime arrivalTime, Trip trip, Stop getOnStop)
                {
                    searchModel.SetEarliestArrivalInRound(stop, round, arrivalTime);
                    searchModel.SetEarliestArrival(stop, arrivalTime);

                    searchModel.SetTripToReachInRound(stop, round, trip);
                    searchModel.SetGetOnStopToReachInRound(stop, round, getOnStop);
                    searchModel.SetTransferToReachInRound(stop, round, null);

                    if (searchModel.destinationStops.Contains(stop) && arrivalTime < searchModel.GetCurrentBestArrivalTime())
                    {
                        searchModel.SetCurrentBestArrivalTime(arrivalTime);
                    }
                }
            }
        }
        /// <summary>
        /// Takes all the stops that have been improved in current round and tries to improve all their neighbors by transfers
        /// </summary>
        private void ImproveByTransfers()
        {
            HashSet<Stop> newMarkedStops = new();
            foreach (Stop markedStop in markedStops)
            {
                foreach (Transfer transfer in markedStop.Transfers)
                {
                    if ((transfer.Distance <= settings.GetMaxTransferDistance()  || transfer.From.Name == transfer.To.Name) && TransferImprovesArrivalTime(transfer) && !searchModel.StopIsReachedByTransferInRound(markedStop, round))
                    {
                        ImproveArrivalByTransfer(transfer);
                        newMarkedStops.Add(transfer.To);
                    }
                }
            }
            markedStops.UnionWith(newMarkedStops);


            bool TransferImprovesArrivalTime(Transfer transfer)
            {
                DateTime currEarliestArrival = searchModel.GetEarliestArrivalInRound(transfer.To, round);
                DateTime earliestArrivalWithTransfer = searchModel.GetEarliestArrivalInRound(transfer.From, round);
                
                if(transfer.From == transfer.To)
                {
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(Math.Max(transfer.GetTransferTime(settings.WalkingPace), settings.GetStationaryTransferMinimumSeconds()));
                }
                else
                {
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()));
                }
                
                return currEarliestArrival > earliestArrivalWithTransfer;
            }
            void ImproveArrivalByTransfer(Transfer transfer)
            {
                DateTime earliestArrivalWithTransfer = searchModel.GetEarliestArrivalInRound(transfer.From, round);

                if (transfer.From == transfer.To)
                {
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(Math.Max(transfer.GetTransferTime(settings.WalkingPace), settings.GetStationaryTransferMinimumSeconds()));
                }
                else
                {
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()));
                }

                searchModel.SetEarliestArrivalInRound(transfer.To, round, earliestArrivalWithTransfer);
                if (searchModel.GetEarliestArrival(transfer.To) > searchModel.GetEarliestArrivalInRound(transfer.To, round))
                {
                    searchModel.SetEarliestArrival(transfer.To, searchModel.GetEarliestArrivalInRound(transfer.To, round));

                    searchModel.SetTripToReachInRound(transfer.To, round, null);
                    searchModel.SetGetOnStopToReachInRound(transfer.To, round, null);
                    searchModel.SetTransferToReachInRound(transfer.To, round, transfer);
                }
            }
        }
        /// <summary>
        /// Finds the connection with the earliest arrival to one of the destinationStations in the searchModel
        /// </summary>
        /// <param name="searchModel">The search model to be used for finding the connection</param>
        /// <returns>The quickest connection from one of the sourceStops to one of the destinationStops in the searchModel</returns>
        internal SearchResult FindConnection(SearchModel searchModel)
        {
            this.searchModel = searchModel;

            InitiateSearch();
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                ImproveByTransfers();
            }
            markedStops.Clear();
            markedRoutesWithGetOnStops.Clear();
            return searchModel.ExtractResult();
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
            if(sourceStop == destStop)
            {
                return null;
            }
            List<Stop> sourceStops = raptorModel.GetStopsByName(sourceStop);
            List<Stop> destStops = raptorModel.GetStopsByName(destStop);
            if (sourceStops.Count == 0 || destStops.Count == 0)
            {
                //Console.WriteLine("Incorrect stop name/s");
                return null;
            }
            this.searchModel = new SearchModel(sourceStops, destStops, departureTime, settings);


            InitiateSearch();
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                ImproveByTransfers();
            }
            markedStops.Clear();
            markedRoutesWithGetOnStops.Clear();
            return searchModel.ExtractResult();
        }
    }
}
