using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.GBFSParsing.DataSources;
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Extensions;
using Microsoft.Extensions.Configuration;
using System.Globalization;


namespace RAPTOR_Router.RouteFinders
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


		private BikeModel? bikeModel;
		/// <summary>
		/// Initializes the builder.
		/// </summary>
		public RouteFinderBuilder()
		{
			
		}

		public void LoadAllData()
		{
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\..")
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();
            string gtfsZipArchiveLocation = config["gtfsArchiveLocation"];
            string forbiddenPointsLocation = config["forbiddenCrossingPointsLocation"];
            string forbiddenLinesLocation = config["forbiddenCrossingLinesLocation"];
            if (gtfsZipArchiveLocation is null)
            {
                throw new Exception("No gtfs archive location found in the config file");
            }
            if (forbiddenPointsLocation == null || forbiddenLinesLocation == null)
            {
                throw new Exception("Forbidden crossing points or lines location not found in the config file");
            }




            var forbiddenCrossings = LoadForbiddenCrossings(forbiddenPointsLocation, forbiddenLinesLocation);

            LoadGtfsData(gtfsZipArchiveLocation, forbiddenCrossings);
			LoadGbfsData();
			ConnectModelsThroughTransfers(forbiddenCrossings);
        }
		/// <summary>
		/// Parses the provided gtfs archive and creates a data model for the connection searches to use
		/// </summary>
		/// <param name="gtfsZipArchiveLocation">The location of the zip gtfs archive</param>
		public void LoadGtfsData(string gtfsZipArchiveLocation, List<ForbiddenCrossingLine> forbiddenCrossings)
		{
			RAPTORModel raptor;
			using (GTFS gtfs = GTFS.ParseZipFile(gtfsZipArchiveLocation))
			{
				raptor = new RAPTORModel(gtfs, forbiddenCrossings);
			}
			GC.Collect();
			this.raptorModel = raptor;
		}

		public void LoadGbfsData()
		{
            bikeModel = new BikeModel();

            IBikeDataSource nextbike = new NextbikeDataSource();
			nextbike.LoadStations();
			nextbike.LoadStationDistances();

						
			bikeModel.AddDataSource(nextbike);
            bikeModel.StartUpdateTimer();
        }

        private List<ForbiddenCrossingLine> LoadForbiddenCrossings(string pointsLocation, string linesLocation)
        {
            List<ForbiddenCrossingPoint> points = new();
            Dictionary<int, ForbiddenCrossingPoint> pointsById = new();
            using (StreamReader reader = new StreamReader(pointsLocation))
            {
                string line;
                reader.ReadLine(); // Skip the header
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');


                    int id = int.Parse(parts[0]);
                    double lon = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    double lat = double.Parse(parts[2], CultureInfo.InvariantCulture);
                    ForbiddenCrossingPoint newPoint = new ForbiddenCrossingPoint(new Coordinates(lat, lon), id);
                    points.Add(newPoint);
                    pointsById.Add(id, newPoint);
                }
            }

            List<ForbiddenCrossingLine> lines = new();
            using (StreamReader reader = new StreamReader(linesLocation))
            {
                string line;
                reader.ReadLine(); // Skip the header
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');


                    int id = int.Parse(parts[0]);
                    int p1Id = int.Parse(parts[1]);
                    int p2Id = int.Parse(parts[2]);
                    string comment = parts[3];
                    ForbiddenCrossingLine newLine = new ForbiddenCrossingLine(pointsById[p1Id], pointsById[p2Id], id, comment);
                    lines.Add(newLine);
                }
            }

            return lines;
        }


        private void ConnectModelsThroughTransfers(List<ForbiddenCrossingLine> forbiddenCrossings)
        {
            foreach (Stop stop in raptorModel.stops.Values)
            {
                foreach (BikeStation bikeStation in bikeModel.Stations)
                {
                    if (DistanceExtensions.TooFarInOneDirection(stop, bikeStation, RAPTORModel.MAX_TRANSFER_DISTANCE))
                    {
                        continue;
                    }
                    int distance = (int)DistanceExtensions.SimplifiedDistanceBetween(stop, bikeStation);
                    if (distance <= RAPTORModel.MAX_TRANSFER_DISTANCE && !forbiddenCrossings.ForbidsTransferBetween(stop, bikeStation))
                    {
                        stop.AddBikeTransfer(new ToBikeTransfer(stop, bikeStation, distance));
                        bikeStation.AddTransfer(new FromBikeTransfer(bikeStation, stop, distance));
                    }
                }
            }
        }

		public IBikeRouteFinder CreateBikeRouter(Settings settings)
		{
			if (raptorModel is null)
			{
				throw new ApplicationException("Data from a gtfs archive were not loaded yet");
			}
			if (bikeModel is null)
			{
				throw new ApplicationException("Data from a gbfs api were not loaded yet");
			}
			IBikeRouteFinder router = new ForwardRouteFinder(settings, raptorModel, bikeModel);
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
