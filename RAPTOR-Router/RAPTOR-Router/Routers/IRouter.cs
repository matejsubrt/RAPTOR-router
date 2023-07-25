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
    internal interface IRouter
    {
        /// <summary>
        /// The function of a router, which solves the provided connection search problem (i.e. SearchModel)
        /// </summary>
        /// <param name="searchModel">The initiated search model to be used for the algorithm</param>
        /// <returns>The resulting best connection</returns>
        SearchResult FindConnection(SearchModel searchModel);
    }
}
