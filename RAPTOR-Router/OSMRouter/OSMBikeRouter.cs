using Itinero;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Osm.Vehicles;
using Itinero.Elevation;
using System.Diagnostics.Metrics;


namespace OSMRouter
{
    public enum ErrorType
    {
        NO_ERROR,
        START_RESOLVE_ERROR,
        END_RESOLVE_ERROR,
        ROUTE_CALCULATION_ERROR
    }
    public class OSMBikeRouter
    {
        private RouterDb routerDb;
        public OSMBikeRouter()
        {
        }

        public void CreateRouterDb()
        {
            if (File.Exists(@"..\..\..\..\OSMRouter\data\cz.routerdb"))
            {
                routerDb = RouterDb.Deserialize(File.OpenRead(@"..\..\..\..\OSMRouter\data\cz.routerdb"));
            }
            else
            {
                routerDb = new RouterDb();
                using (var stream = new FileInfo("..\\..\\..\\..\\OSMRouter\\data\\czech-republic-latest.osm.pbf").OpenRead())
                {
                    routerDb.LoadOsmData(stream, Vehicle.Bicycle);
                }

                // write the routerdb to disk, so that it can be used later.
                using (var stream = new FileInfo(@"..\..\..\..\OSMRouter\data\cz.routerdb").Open(FileMode.Create))
                {
                    routerDb.Serialize(stream);
                }
            }
            //routerDb.AddElevation();
        }
        public void CalculateDistanceMatrix(Coordinate[] coordinates) {
            var router = new Router(routerDb);
            var matrixCalculator = new Itinero.Algorithms.Matrices.WeightMatrixAlgorithm(
             router, Vehicle.Bicycle.Shortest(), coordinates);
            matrixCalculator.Run();
        }

        public int GetBikingDistance(float startLat, float startLon, float endLat, float endLon, out ErrorType errorType)
        {
            var router = new Router(routerDb);
            
            var profile = Vehicle.Bicycle.Shortest();
            RouterPoint start;
            RouterPoint end;
            Route route;

            Result<RouterPoint> startResult = router.TryResolve(profile, startLat, startLon, 150f);
            Result<RouterPoint> endResult = router.TryResolve(profile, endLat, endLon, 150f);

            if(startResult.IsError)
            {
                errorType = ErrorType.START_RESOLVE_ERROR;
                return -1;
            }
            else if(endResult.IsError)
            {
                errorType = ErrorType.END_RESOLVE_ERROR;
                return -1;
            }
            else
            {
                start = startResult.Value;
                end = endResult.Value;
            }

            Result<Route> routeResult = router.TryCalculate(profile, start, end);
            if(routeResult.IsError)
            {
                errorType = ErrorType.ROUTE_CALCULATION_ERROR;
                return -1;
            }
            else
            {
                route = routeResult.Value;
            }
            
            errorType = ErrorType.NO_ERROR;
            return (int)route.TotalDistance;
        }

        public bool CheckConnectivity(float lat, float lon)
        {
            var profile = Vehicle.Bicycle.Shortest();

            var router = new Router(routerDb);
            var point = router.TryResolve(profile, lat, lon);

            if(point.IsError)
            {
                return false;
            }
            else
            {
                return router.CheckConnectivity(profile, point.Value);
            }
        }
    }
}
