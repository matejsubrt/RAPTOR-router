using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Extensions
{
    /// <summary>
    /// Class used for comparing two times during a connection search based on the search direction.
    /// </summary>
    public class TimeComparator
    {
        private readonly Func<DateTime, DateTime, bool> _improvesTime;

        /// <summary>
        /// Creates a new TimeComparator object
        /// </summary>
        /// <param name="forward">Whether the search that uses the comparator runs forward or backward</param>
        public TimeComparator(bool forward)
        {
            _improvesTime = forward ?
                (a, b) => a < b :
                (a, b) => a > b;
        }

        /// <summary>
        /// Compares two times and returns whether the first time is better than the second time based on the search direction.
        /// </summary>
        /// <remarks>If the search runs forward, this means the first time is earlier than the second.</remarks>
        /// <param name="a">The first time to compare</param>
        /// <param name="b">The second time to compare</param>
        /// <returns>Whether time a is better than time b in the search direction</returns>
        public bool ImprovesTime(DateTime a, DateTime b)
        {
            return _improvesTime(a, b);
        }

        /// <summary>
        /// Compares two times and returns whether the first time is better or same as the second time based on the search direction.
        /// </summary>
        /// <remarks>If the search runs forward, this means the first time is earlier or same as the second.</remarks>
        /// <param name="a">The first time to compare</param>
        /// <param name="b">The second time to compare</param>
        /// <returns>Whether time a is better or same as time b in the search direction</returns>
        public bool ImprovesOrEqualsTime(DateTime a, DateTime b)
        {
            return a == b || _improvesTime(a, b);
        }
    }


    /// <summary>
    /// Class used for comparing two indices during a connection search based on the search direction.
    /// </summary>
    public class IndexComparator
    {
        private readonly Func<int, int, bool> _precedesInSearchDirection;

        /// <summary>
        /// Creates a new IndexComparator object
        /// </summary>
        /// <param name="forward">>Whether the search that uses the comparator runs forward or backward</param>
        public IndexComparator(bool forward)
        {
            _precedesInSearchDirection = forward ?
                (a, b) => a < b :
                (a, b) => a > b;
        }

        /// <summary>
        /// Compares two indices and returns whether the first index precedes the second index based on the search direction.
        /// </summary>
        /// <remarks>If the search runs forward, this means that the first index is less than the second.</remarks>
        /// <param name="a">The first index</param>
        /// <param name="b">The second index</param>
        /// <returns>Whether index a precedes index b in the search direction</returns>
        public bool PrecedesInSearchDirection(int a, int b)
        {
            return _precedesInSearchDirection(a, b);
        }
    }
}
