using RAPTOR_Router.SearchModels;
using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Routers
{
    /// <summary>
    /// An interface to use for any router class - any class that is supposed to find a connection in a SearchModel
    /// </summary>
    public interface IRouter
    {
        /// <summary>
        /// The function of a router, which solves the provided connection search problem (i.e. SearchModel)
        /// </summary>
        /// <param name="sourceStop">The exact name of the source stop</param>
        /// <param name="destStop">The exact name of the destination stop</param>
        /// <param name="departureTime">The departure date and time</param>
        /// <returns>The resulting best connection, null if one id found</returns>
        SearchResult FindConnection(string sourceStop, string destStop, DateTime departureTime);
    }
}
