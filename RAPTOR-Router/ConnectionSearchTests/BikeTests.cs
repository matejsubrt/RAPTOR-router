using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.GBFSParsing;
using RAPTOR_Router.SearchModels;

namespace ConnectionSearchTests
{
    [TestClass]
    public class BikeTests
    {
        [TestMethod]
        public void ResolveCoordinatesTest()
        {
            IBikeDataSource nextbike = new NextbikeDataSource();
            List<BikeStation> stations;
            Dictionary<string, BikeStation> stationsById;
            StationDistanceMatrix distances;
            nextbike.LoadStations(out stations, out stationsById, out distances);
            BikeModel bikeModel = new BikeModel(stations, stationsById, distances);

            BikeStation station = bikeModel.ResolveCoordinates(50.0755, 14.4378, 1000);
            Assert.AreEqual("P2 - náměstí Míru (dopravní stín před Bruxx - výstup E1)", station.Name);
        }
    }
}
