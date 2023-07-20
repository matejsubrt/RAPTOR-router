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
    /// An interface to use for any router class - any class that is supposet to find a connection in a SearchModel
    /// </summary>
    internal interface IRouter
    {
        SearchResult FindConnection(SearchModel searchModel);
    }
}
