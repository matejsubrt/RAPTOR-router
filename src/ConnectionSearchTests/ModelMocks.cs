using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Transit;

namespace ConnectionSearchTests
{
    public class MockDelayModel : IDelayModel
    {
        public void AddDelay(DateOnly tripStartDate, string tripId, int arrivalDelay, int departureDelay)
        {
        }

        public bool TryGetDelay(DateOnly tripStartDate, string tripId, int stopIndex, out int arrivalDelay,
            out int departureDelay)
        {
            string tripIdLast2Chars = tripId.Substring(tripId.Length - 2);
            int delay = int.Parse(tripIdLast2Chars);

            if (delay % 2 == 0)
            {
                arrivalDelay = delay;
                departureDelay = delay;
                return true;
            }
            else
            {
                arrivalDelay = 0;
                departureDelay = 0;
                return false;
            }
        }

        public bool TripHasDelayData(DateOnly tripStartDate, string tripId)
        {
            string tripIdLast2Chars = tripId.Substring(tripId.Length - 2);
            int delay = int.Parse(tripIdLast2Chars);

            return delay % 2 == 0;
        }

        public TripStopDelays GetTripStopDelaysUnsafe(DateOnly tripStartDate, string tripId)
        {
            string tripIdLast2Chars = tripId.Substring(tripId.Length - 2);
            int delay = int.Parse(tripIdLast2Chars);

            if (delay % 2 == 0)
            {
                TripStopDelays delays = new();
                delays.AddStopDelay(delay, delay);
                delays.AddStopDelay(delay, delay);
                return delays;
            }
            else
            {
                return null;
            }
        }
    }
}
