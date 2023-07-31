using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Routers
{
    public class RouterBuilder
    {
        private RAPTORModel raptorModel;
        public RouterBuilder(string gtfsZipArchiveLocation)
        {
            RAPTORModel raptor;
            using (GTFS gtfs = GTFS.ParseZipFile(gtfsZipArchiveLocation))
            {
                raptor = new RAPTORModel(gtfs);
            }
            GC.Collect();
            this.raptorModel = raptor;
        }

        public IRouter CreateRouter(Settings settings)
        {
            IRouter router = new BasicRouter(settings, raptorModel);
            return router;
        }
    }
}
