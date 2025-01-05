using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Models.Static
{
    /// <summary>
    /// Interface for a Delay Model
    /// </summary>
    public interface IDelayModel
    {
        /// <summary>
        /// Adds a delay value to the model
        /// </summary>
        /// <param name="tripStartDate">The start date of the trip for which to add the delay</param>
        /// <param name="tripId">The id of the trip</param>
        /// <param name="arrivalDelay">The arrival delay</param>
        /// <param name="departureDelay">The departure delay</param>
        public void AddDelay(DateOnly tripStartDate, string tripId, int arrivalDelay, int departureDelay);

        /// <summary>
        /// Tries to get the delay for the specified trip
        /// </summary>
        /// <param name="tripStartDate">The start date of the trip for which to get the delay</param>
        /// <param name="tripId">The id of the trip</param>
        /// <param name="stopIndex">The index of the desired stop</param>
        /// <param name="arrivalDelay">The arrival delay</param>
        /// <param name="departureDelay">The departure delay</param>
        /// <returns>Whether the delay was present</returns>
        public bool TryGetDelay(DateOnly tripStartDate, string tripId, int stopIndex, out int arrivalDelay, out int departureDelay);

        /// <summary>
        /// Finds out whether there is delay data present for the trip
        /// </summary>
        /// <param name="tripStartDate">The start date of the trip for which to get the information</param>
        /// <param name="tripId">The id of the trip</param>
        /// <returns>Whether delay data for the trip is present</returns>
        public bool TripHasDelayData(DateOnly tripStartDate, string tripId);

        /// <summary>
        /// Gets the trip stop delays for the trip.
        /// </summary>
        /// <param name="tripStartDate">The start date of the trip for which to get the information</param>
        /// <param name="tripId">The id of the trip</param>
        /// <remarks>Fails if trip does not have delay data present</remarks>
        /// <returns>The trip's stops' delays</returns>
        public TripStopDelays? GetTripStopDelaysUnsafe(DateOnly tripStartDate, string tripId);
    }



    /// <summary>
    /// Class representing the delays for a single trip
    /// </summary>
    public class TripStopDelays
    {
        private List<Tuple<int, int>> _stopDelays = new();

        /// <summary>
        /// The number of stops for which we have delay data
        /// </summary>
        public int Count
        {
            get => _stopDelays.Count;
        }

        /// <summary>
        /// Adds a new delay entry to the list
        /// </summary>
        /// <param name="arrivalDelay">The delay on arrival</param>
        /// <param name="departureDelay">The delay on departure</param>
        public void AddStopDelay(int arrivalDelay, int departureDelay)
        {
            _stopDelays.Add(new Tuple<int, int>(arrivalDelay, departureDelay));
        }

        /// <summary>
        /// Tries to get the delay data for a specific stop
        /// </summary>
        /// <param name="stopIndex">The index of the stop</param>
        /// <param name="arrivalDelay">The delay at the stop on arrival</param>
        /// <param name="departureDelay">The delay at the stop on departure</param>
        /// <returns>Whether the data was found and valid</returns>
        /// <remarks>Sometimes the delay data is missing in the source json for the last few stops of a trip,
        /// so this function uses the last available delay data before that if that happens.</remarks>
        public bool TryGetStopDelay(int stopIndex, out int arrivalDelay, out int departureDelay)
        {
            if (stopIndex < _stopDelays.Count)
            {
                arrivalDelay = _stopDelays[stopIndex].Item1;
                departureDelay = _stopDelays[stopIndex].Item2;

                if (arrivalDelay < -600 || departureDelay < -600)
                {
                    arrivalDelay = 0;
                    departureDelay = 0;
                    return false;
                }
                return true;
            }
            else
            {
                // There is sometimes no data for the last few stops, so we return the last delay in the data
                arrivalDelay = _stopDelays[^1].Item1;
                departureDelay = _stopDelays[^1].Item2;
                return true;
            }
        }

        /// <summary>
        /// Gets the delay at the last stop we have data for
        /// </summary>
        /// <returns>A tuple of the arrival and departure delays at the last stop</returns>
        public Tuple<int, int> GetLastStopDelay()
        {
            //TODO: check if this was not replaced by the TryGetStopDelay method
            return _stopDelays[^1];
        }
    }

    /// <summary>
    /// Class holding all the current delay data for all active trips
    /// </summary>
    public class DelayModel : IDelayModel
    {
        private Dictionary<DateOnly, Dictionary<string, TripStopDelays>> delays = new();

        /// <summary>
        /// Adds delay data for a specific trip
        /// </summary>
        /// <param name="tripStartDate">The start date of the trip</param>
        /// <param name="tripId">The ID of the trip</param>
        /// <param name="arrivalDelay">The arrival delay</param>
        /// <param name="departureDelay">The departure delay</param>
        public void AddDelay(DateOnly tripStartDate, string tripId, int arrivalDelay, int departureDelay)
        {
            //TODO: change the implementation of this
            if (!delays.ContainsKey(tripStartDate))
            {
                delays.Add(tripStartDate, new Dictionary<string, TripStopDelays>());
            }
            var tripDelaysByStartDate = delays[tripStartDate];
            if (!tripDelaysByStartDate.ContainsKey(tripId))
            {
                tripDelaysByStartDate.Add(tripId, new TripStopDelays());
            }

            tripDelaysByStartDate[tripId].AddStopDelay(arrivalDelay, departureDelay);
        }

        /// <summary>
        /// Tries to get the delay data for a specific trip
        /// </summary>
        /// <param name="tripStartDate">The start date of the trip</param>
        /// <param name="tripId">The ID of the trip</param>
        /// <param name="stopIndex">The index of the desired stop within the trip</param>
        /// <param name="arrivalDelay">The arrival delay at the stop</param>
        /// <param name="departureDelay">The departure delay at the stop</param>
        /// <returns>Whether delay data for the stop was successfully found</returns>
        public bool TryGetDelay(DateOnly tripStartDate, string tripId, int stopIndex, out int arrivalDelay, out int departureDelay)
        {
            if (!delays.ContainsKey(tripStartDate))
            {
                arrivalDelay = 0;
                departureDelay = 0;
                return false;
            }

            if (!delays[tripStartDate].ContainsKey(tripId))
            {
                arrivalDelay = 0;
                departureDelay = 0;
                return false;
            }

            var tripStopDelays = delays[tripStartDate][tripId];

            bool haveDelay = tripStopDelays.TryGetStopDelay(stopIndex, out arrivalDelay, out departureDelay);

            return haveDelay;
        }

        /// <summary>
        /// Checks if a specific trip has delay data present
        /// </summary>
        /// <param name="tripStartDate">The start date of the trip</param>
        /// <param name="tripId">The ID of the trip</param>
        /// <returns>Whether the delay data for the trip is present</returns>
        public bool TripHasDelayData(DateOnly tripStartDate, string tripId)
        {
            if (!delays.ContainsKey(tripStartDate))
            {
                return false;
            }
            var tripDelaysByStartDate = delays[tripStartDate];
            if (!tripDelaysByStartDate.ContainsKey(tripId))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the TripStopDelays object for a specific trip
        /// </summary>
        /// <param name="tripStartDate">The start date of the trip</param>
        /// <param name="tripId">The ID of the trip</param>
        /// <returns>The TripStopDelays object of the trip</returns>
        /// <remarks>Fails if the data is not present</remarks>
        public TripStopDelays GetTripStopDelaysUnsafe(DateOnly tripStartDate, string tripId)
        {
            return delays[tripStartDate][tripId];
        }
    }
}
