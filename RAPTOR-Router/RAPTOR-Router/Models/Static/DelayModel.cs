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

            arrivalDelay = 0;
            departureDelay = 0;
            return false;
        }

        public Tuple<int, int> GetLastStopDelay()
        {
            return _stopDelays[^1];
        }
    }
    //internal class TripDelayInfo
    //{
    //    public 
    //}
    public class DelayModel
    {
        private Dictionary<DateOnly, Dictionary<string, TripStopDelays>> delays = new();
        //public Dictionary<string, List<int>> delays { get; private set; } = new();

        public DelayModel(){}
        public void AddDelay(DateOnly tripStartDate, string tripId, int arrivalDelay, int departureDelay)
        {
            //if (!delays.ContainsKey(tripId))
            //{
            //    delays.Add(tripId, new List<int>());
            //}
            //delays[tripId].Add(delay);
            if (!delays.ContainsKey(tripStartDate))
            {
                delays.Add(tripStartDate, new Dictionary<string, TripStopDelays>());
            }
            var tripDelaysByStartDate = delays[tripStartDate];
            if (!tripDelaysByStartDate.ContainsKey(tripId))
            {
                tripDelaysByStartDate.Add(tripId, new TripStopDelays());
            }

            //Tuple<int, int> delay = new Tuple<int, int>(arrivalDelay, departureDelay);
            tripDelaysByStartDate[tripId].AddStopDelay(arrivalDelay, departureDelay);
        }

        //private Tuple<int, int> GetDelay(DateOnly tripStartDate, string tripId, int stopIndex)
        //{
        //    if (!delays.ContainsKey(tripStartDate))
        //    {
        //        return null;
        //    }
        //    var tripDelaysByStartDate = delays[tripStartDate];
        //    if (!tripDelaysByStartDate.ContainsKey(tripId))
        //    {
        //        return null;
        //    }
        //    var delaysForTrip = tripDelaysByStartDate[tripId];
        //    if (delaysForTrip.Count == 0)
        //    {
        //        return null;
        //    }

        //    return delaysForTrip[stopIndex];
        //}

        public bool TryGetDelay(DateOnly tripStartDate, string tripId, int stopIndex, out int arrivalDelay, out int departureDelay)
        {
            //Tuple<int, int> delay = GetDelay(tripStartDate, tripId, stopIndex);
            //if (delay == null)
            //{
            //    arrivalDelay = 0;
            //    departureDelay = 0;
            //    return false;
            //}
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
