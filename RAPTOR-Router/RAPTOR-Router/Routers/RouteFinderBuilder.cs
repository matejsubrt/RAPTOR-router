using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Routers
{
    /// <summary>
    /// A class used for creating separate routers to be used for connection searching.
    /// </summary>
    public class RouteFinderBuilder
    {
        /// <summary>
        /// The RAPTOR model that the routers should use
        /// </summary>
        private RAPTORModel raptorModel;
        /// <summary>
        /// Initializes the builder by parsing the GTFS data from the zip archive and preparing the RAPTOR model
        /// </summary>
        /// <param name="gtfsZipArchiveLocation">The path to the zip gtfs archive.</param>
        public RouteFinderBuilder(string gtfsZipArchiveLocation)
        {
            RAPTORModel raptor;
            using (GTFS gtfs = GTFS.ParseZipFile(gtfsZipArchiveLocation))
            {
                raptor = new RAPTORModel(gtfs);
            }
            GC.Collect();
            this.raptorModel = raptor;
        }

        /// <summary>
        /// Creates a new BasicRouter instance using the provided settings and the parsed RAPTOR model
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IRouteFinder CreateRouter(Settings settings)
        {
            IRouteFinder router = new BasicRouteFinder(settings, raptorModel);
            return router;
        }

        public IRouteFinder CreateAdvancedRouter(Settings settings)
        {
            IRouteFinder router = new AdvancedRouteFinder(settings, raptorModel);
            return router;
        }
    }
}
