using RAPTOR_Router.Problems;
using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Routers
{
    internal class BasicRouter : IRouter
    {
        private JourneySearchModel searchModel;
        private HashSet<Stop> markedStops = new();
        private Dictionary<Route, Stop> markedRoutesWithGetOnStops = new();
        private Settings settings;

        private int round = 0;

        public BasicRouter(Settings settings)
        {
            this.settings = settings;
        }
        private void InitiateSearch()
        {
            searchModel.SetSourceStopsEarliestArrival();
            MarkSourceStops();

            void MarkSourceStops()
            {
                foreach (Stop sourceStop in searchModel.sourceStops)
                {
                    markedStops.Add(sourceStop);
                }
            }
        }
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
        private void TraverseMarkedRoutes()
        {
            foreach (KeyValuePair<Route, Stop> pair in markedRoutesWithGetOnStops)
            {
                Route route = pair.Key;
                Stop getOnStop = pair.Value;
                DateOnly tripDate;

                Trip trip = route.GetEarliestTripAtStop(
                    getOnStop,
                    DateOnly.FromDateTime(searchModel.GetEarliestArrivalInRound(getOnStop, round - 1)),
                    TimeOnly.FromDateTime(searchModel.GetEarliestArrivalInRound(getOnStop, round - 1)),
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
                            trip = route.GetEarliestTripAtStop(
                                currStop,
                                DateOnly.FromDateTime(searchModel.GetEarliestArrivalInRound(currStop, round - 1)),
                                TimeOnly.FromDateTime(searchModel.GetEarliestArrivalInRound(currStop, round - 1)),
                                Settings.MAX_TRIP_LENGTH_DAYS,
                                out tripDate);
                            getOnStop = currStop;
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
        private void ImproveByTransfers()
        {
            HashSet<Stop> newMarkedStops = new();
            foreach (Stop markedStop in markedStops)
            {
                foreach (Transfer transfer in markedStop.Transfers)
                {
                    if (TransferImprovesArrivalTime(transfer) && !searchModel.StopIsReachedByTransferInRound(markedStop, round))
                    {
                        ImproveArrivalByTransfer(transfer);
                        newMarkedStops.Add(transfer.To);
                    }
                }
            }
            markedStops.UnionWith(newMarkedStops);


            bool TransferImprovesArrivalTime(Transfer transfer)
            {
                return searchModel.GetEarliestArrivalInRound(transfer.To, round) > searchModel.GetEarliestArrivalInRound(transfer.From, round).AddSeconds(transfer.Time);
            }
            void ImproveArrivalByTransfer(Transfer transfer)
            {
                searchModel.SetEarliestArrivalInRound(transfer.To, round, searchModel.GetEarliestArrivalInRound(transfer.From, round).AddSeconds(transfer.Time));
                if (searchModel.GetEarliestArrival(transfer.To) > searchModel.GetEarliestArrivalInRound(transfer.To, round))
                {
                    searchModel.SetEarliestArrival(transfer.To, searchModel.GetEarliestArrivalInRound(transfer.To, round));

                    searchModel.SetTripToReachInRound(transfer.To, round, null);
                    searchModel.SetGetOnStopToReachInRound(transfer.To, round, null);
                    searchModel.SetTransferToReachInRound(transfer.To, round, transfer);
                }
            }
        }


        public SearchResult FindConnection(JourneySearchModel searchModel)
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
            return new SearchResult(searchModel);
        }
    }
}
