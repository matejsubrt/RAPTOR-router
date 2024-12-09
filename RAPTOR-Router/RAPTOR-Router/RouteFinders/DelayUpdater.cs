using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Models.Static;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// Class used for updating delays of trips, that are part of search results.
    /// Used to provide the client with the most up-to-date information even after the search has been performed.
    /// </summary>
    public class DelayUpdater
    {
        private TransitModel transitModel;
        private DelayModel delayModel;

        internal DelayUpdater(TransitModel transitModel, DelayModel delayModel)
        {
            this.transitModel = transitModel;
            this.delayModel = delayModel;
        }

        /// <summary>
        /// Updates the delays of the trips in the search results.
        /// </summary>
        /// <param name="results">The search results to update delay data for</param>
        public List<SearchResult> UpdateDelays(List<SearchResult> results)
        {
            List<SearchResult> newResults = new();
            foreach (var result in results)
            {
                SearchResult newResult = new();
                newResult.SecondsBeforeFirstTrip = result.SecondsBeforeFirstTrip;
                newResult.SecondsAfterLastTrip = result.SecondsAfterLastTrip;
                newResult.ArrivalDateTime = result.ArrivalDateTime;
                newResult.DepartureDateTime = result.DepartureDateTime;
                newResult.BikeTripCount = result.BikeTripCount;
                newResult.TransferCount = result.TransferCount;
                newResult.TripCount = result.TripCount;
                newResult.UsedBikeTrips = result.UsedBikeTrips;
                newResult.UsedSegmentTypes = result.UsedSegmentTypes;
                newResult.UsedTransfers = result.UsedTransfers;
                newResult.UsedTrips = result.UsedTrips;
                newResult.UsedTripAlternatives = new();

                foreach (var alternatives in result.UsedTripAlternatives)
                {
                    SearchResult.TripAlternatives newAlternatives = new();
                    newAlternatives.Count = alternatives.Count;
                    newAlternatives.CurrIndex = alternatives.CurrIndex;
                    newAlternatives.Alternatives = new();

                    foreach (var trip in alternatives.Alternatives)
                    {
                        SearchResult.UsedTrip newTrip = new SearchResult.UsedTrip
                        {
                            routeName = trip.routeName,
                            color = trip.color,
                            getOffStopIndex = trip.getOffStopIndex,
                            getOnStopIndex = trip.getOnStopIndex,
                            stopPasses = trip.stopPasses,
                            tripId = trip.tripId,
                            vehicleType = trip.vehicleType
                        };

                        DateOnly tripStartDate = DateOnly.FromDateTime(trip.stopPasses[0].DepartureTime);
                        bool tripHasDelayData =
                            delayModel.TripHasDelayData(tripStartDate, trip.tripId);

                        if (tripHasDelayData)
                        {
                            var tripStopDelays = delayModel.GetTripStopDelaysUnsafe(tripStartDate, trip.tripId);

                            bool hasGetOnDelay = tripStopDelays.TryGetStopDelay(trip.getOnStopIndex,
                                                               out int getOnArrivalDelay, out int getOnDepartureDelay);
                            bool hasGetOffDelay = tripStopDelays.TryGetStopDelay(trip.getOffStopIndex,
                                                               out int getOffArrivalDelay, out int getOffDepartureDelay);

                            if (!hasGetOffDelay && trip.getOffStopIndex >= tripStopDelays.Count)
                            {
                                // Bug in the delay data, the trip has more stops than the delay data
                                hasGetOffDelay = true;
                                (getOffArrivalDelay, getOffDepartureDelay) = tripStopDelays.GetLastStopDelay();
                            }

                            if (hasGetOnDelay)
                            {
                                if (hasGetOffDelay)
                                {
                                    // Delay info is only valid if we have both get on and get off delay
                                    newTrip.hasDelayInfo = true;
                                    newTrip.delayWhenBoarded = getOnDepartureDelay;
                                    newTrip.currentDelay = getOffArrivalDelay;

                                    
                                    newAlternatives.Alternatives.Add(newTrip);
                                    continue;
                                }
                            }
                        }

                        newTrip.hasDelayInfo = false;
                        newTrip.delayWhenBoarded = 0;
                        newTrip.currentDelay = 0;

                        newAlternatives.Alternatives.Add(newTrip);
                    }

                    newResult.UsedTripAlternatives.Add(newAlternatives);
                }
                newResults.Add(newResult);
            }

            return newResults;
        }
    }
}
