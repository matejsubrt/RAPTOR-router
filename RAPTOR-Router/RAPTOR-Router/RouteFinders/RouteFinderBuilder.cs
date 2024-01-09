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
        private RAPTORModel? raptorModel;
        /// <summary>
        /// Initializes the builder.
        /// </summary>
        public RouteFinderBuilder()
        {
        }

        /// <summary>
        /// Parses the provided gtfs archive and creates a data model for the connection searches to use
        /// </summary>
        /// <param name="gtfsZipArchiveLocation">The location of the zip gtfs archive</param>
        public void LoadDataFromGtfs(string gtfsZipArchiveLocation)
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
        /// Creates a new BasicRouteFinder instance using the provided settings and the parsed RAPTOR model
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IRouteFinder CreateRouter(Settings settings)
        {
            if(raptorModel is null)
            {
                throw new ApplicationException("Data from a gtfs archive were not loaded yet");
            }
            IRouteFinder router = new BasicRouteFinder(settings, raptorModel);
            return router;
        }

        /// <summary>
        /// Creates a new AdvancedRouteFinder instance using the provided settings and the parsed RAPTOR model
        /// </summary>
        /// <param name="settings">The settings to use</param>
        /// <returns>The constructed RouteFinder</returns>
        /// <exception cref="ApplicationException">Thrown if LoadDataFromGtfs wasn't called yet</exception>
        public IRouteFinder CreateAdvancedRouter(Settings settings)
        {
            if (raptorModel is null)
            {
                throw new ApplicationException("Data from a gtfs archive were not loaded yet");
            }
            IRouteFinder router = new AdvancedRouteFinder(settings, raptorModel);
            return router;
        }
        public bool ValidateStopName(string stopName)
        {
            if (raptorModel is null)
            {
                return false;
            }
            return raptorModel.GetStopsByName(stopName).Count != 0;
        }
        public bool ValidateSettings(Settings settings)
        {
            bool correct = true;

            correct &= Enum.IsDefined(typeof(ComfortBalance), settings.ComfortBalance);
            correct &= Enum.IsDefined(typeof(WalkingPreference), settings.WalkingPreference);
            correct &= Enum.IsDefined(typeof(TransferTime), settings.TransferTime);
            correct &= settings.WalkingPace >= 2 && settings.WalkingPace <= 60;
            correct &= settings.CyclingPace >= 0 && settings.CyclingPace <= 60;

            return correct;
        }
    }
}
