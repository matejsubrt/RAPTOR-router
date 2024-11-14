using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Models.Static
{
    public class TripStopDelays
    {
        private List<Tuple<int, int>> _stopDelays = new();

        public int Count
        {
            get => _stopDelays.Count;
        }

        public void AddStopDelay(int arrivalDelay, int departureDelay)
        {
            _stopDelays.Add(new Tuple<int, int>(arrivalDelay, departureDelay));
        }

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

        public Tuple<int, int> GetLastStopDelay()
        {
            return _stopDelays[^1];
        }
    }


    public class DelayModel
    {
        private Dictionary<DateOnly, Dictionary<string, TripStopDelays>> delays = new();

        public DelayModel(){}
        public void AddDelay(DateOnly tripStartDate, string tripId, int arrivalDelay, int departureDelay)
        {
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

        public TripStopDelays GetTripStopDelays(DateOnly tripStartDate, string tripId)
        {
            return delays[tripStartDate][tripId];
        }
    }
}
