using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using RAPTOR_Router.RAPTORStructures;
using Itinero.LocalGeo;
using System.Diagnostics;

/*
namespace RAPTOR_Router.GBFSParsing
{
	public class GBFS
	{
		static string stationInfoUrl = "https://gbfs.nextbike.net/maps/gbfs/v2/nextbike_tg/cs/station_information.json";
		static string stationStatusUrl = "https://gbfs.nextbike.net/maps/gbfs/v2/nextbike_tg/cs/station_status.json";

		public List<BikeStation> Stations { get; private set; }
		public Dictionary<string, BikeStation> StationsById { get; private set; }

		public int[,] distances;

		public GBFS()
		{
			Stations = new List<BikeStation>();
			StationsById = new Dictionary<string, BikeStation>();
		}

		public void LoadStations()
		{
			using(HttpClient client = new HttpClient())
			{
				try
				{
					HttpResponseMessage response = client.GetAsync(stationInfoUrl).Result;
					response.EnsureSuccessStatusCode();

					GBFSStationInfo root = JsonSerializer.Deserialize<GBFSStationInfo>(response.Content.ReadAsStringAsync().Result);

					int local_id = 0;
					foreach (GBFSStation station in root.Data.Stations)
					{
						BikeStation newStation = new BikeStation(station.StationId, station.Name, station.Lat, station.Lon, station.Capacity, local_id);
						Stations.Add(newStation);
						StationsById.Add(newStation.Id, newStation);
						local_id++;
					}

					distances = new int[Stations.Count, Stations.Count];
				}
				catch (HttpRequestException e)
				{
					// Handle any errors that occurred during the request
					Console.WriteLine("\nException Caught!");
					Console.WriteLine("Message :{0} ", e.Message);
				}
			}
			LoadDistances();
		}

		public void LoadDistances()
		{
			//OSMBikeRouter router = new OSMBikeRouter();
			//router.CreateRouterDb();
			//Coordinate[] coordinates = new Coordinate[Stations.Count];
			//for (int i = 0; i < 20; i++)  //TODO: change to Stations.Count
			//{
			//	coordinates[i] = new Coordinate((float)Stations[i].Lat, (float)Stations[i].Lon);
			//         }
			//router.CalculateDistanceMatrix(coordinates);

			string filePath = "..\\..\\..\\..\\RAPTOR-Router\\data\\distances.csv";

			if(File.Exists(filePath))
			{
				LoadSavedDistancesFromFile(filePath);
            }
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                Dictionary<string, int> unresolvableStations = new Dictionary<string, int>();
                Stopwatch stopwatch = new Stopwatch();
				double totalSeconds = 0.0;
				for (int i = 0; i < Stations.Count; i++) // TODO: back to Stations.Count
				{
					if (unresolvableStations.ContainsKey(Stations[i].Id) && unresolvableStations[Stations[i].Id] > 2)
					{
						for (int j = 0; j < Stations.Count; j++)
						{
							distances[i, j] = -1;
						}
						continue;
					}
					Console.WriteLine("CALCULATING STATION #" + i + "/" + Stations.Count);
					stopwatch.Start();
					for (int j = 0; j < Stations.Count; j++)
					{
						if(i == 186 && j == 860)
						{
							Console.WriteLine();
						}
						// The matrix is symmetric
						// If the distance is already loaded, skip it
						if(i < j && distances[i, j] == 0)
						{
                            // This station has been unresolvable for more than 2 times
                            if (unresolvableStations.ContainsKey(Stations[j].Id) && unresolvableStations[Stations[j].Id] > 2)
                            {
                                distances[i, j] = -1;
								distances[j, i] = -1;
                                continue;
                            }
                            // This station is certainly too far from the current one
                            else if (DistanceExtensions.TooFarInOneDirection(Stations[i].Lat, Stations[i].Lon, Stations[j].Lat, Stations[j].Lon, 3000))
                            {
                                distances[i, j] = -1; // too far
								distances[j, i] = -1; // too far
                            }
							else if (!router.CheckConnectivity((float)Stations[j].Lat, (float)Stations[j].Lon)){
								addUnresolvableStation(Stations[j].Id);
								distances[i, j] = -1;
								distances[j, i] = -1;
								Console.WriteLine("Station " + Stations[j].Id + " is not connected");
							}
							else if (!router.CheckConnectivity((float)Stations[i].Lat, (float)Stations[i].Lon))
							{
								addUnresolvableStation(Stations[i].Id);
								distances[i, j] = -1;
								distances[j, i] = -1;
								Console.WriteLine("Station " + Stations[i].Id + " is not connected");
							}
                            // Calculate the actual distance -> only if i < j -> the matrix is symmetric
                            else
                            {
                                ErrorType errorType;
								int result = router.GetBikingDistance((float)Stations[i].Lat, (float)Stations[i].Lon, (float)Stations[j].Lat, (float)Stations[j].Lon, out errorType);
                                distances[i, j] = result != 0 ? result : 1;
                                distances[j, i] = distances[i, j];

                                

								if(errorType != ErrorType.NO_ERROR)
								{
									if(errorType == ErrorType.START_RESOLVE_ERROR)
									{
										addUnresolvableStation(Stations[i].Id);
                                        Console.WriteLine($"Start station resolve error from {Stations[i].Id}: {Stations[i].Name}");
                                    }
									else if(errorType == ErrorType.END_RESOLVE_ERROR)
									{
										addUnresolvableStation(Stations[j].Id);
                                        Console.WriteLine($"End station resolve error from {Stations[j].Id}: {Stations[j].Name}");
                                    }
									else if(errorType == ErrorType.ROUTE_CALCULATION_ERROR)
									{
										//addUnresolvableStation(Stations[i].Id);
										addUnresolvableStation(Stations[j].Id);
                                        Console.WriteLine($"Route calculation error from {Stations[i].Id}: {Stations[i].Name} to {Stations[j].Id}: {Stations[j].Name}");
                                    }
								}
                            }

                            // Write the distance to the file
                            writer.WriteLine(Stations[i].Id + "," + Stations[j].Id + "," + distances[i, j]);
                            if (distances[i, j] == 0 && i != j)
                            {
                                throw new Exception("Distance is 0 even after processing");
                            }
							writer.Flush(); // TODO: remove this line
                        }
                        
                    }
                    stopwatch.Stop();
                    totalSeconds += stopwatch.Elapsed.TotalSeconds;
                    Console.WriteLine("Time elapsed: " + stopwatch.Elapsed + "s");

                    Console.WriteLine("Total time elapsed: " + totalSeconds + "s");
                    if (i > 0)
                    {
                        double ratio = ((double)Stations.Count - (double)i) / (double)i; // remaining stations / stations processed
                        double estimatedRemainingTime = ratio * totalSeconds;
                        Console.WriteLine("ESTIMATED REMAINING TIME: " + (int)(estimatedRemainingTime / 60) + ":" + (int)(estimatedRemainingTime % 60));
                    }
                    Console.WriteLine("---------------------------------------------------");

                    stopwatch.Reset();

					void addUnresolvableStation(string id)
					{
                        if (unresolvableStations.ContainsKey(id))
                        {
                            unresolvableStations[id]++;
                        }
                        else
                        {
                            unresolvableStations.Add(id, 1);
                        }
                    }
                }
            }			
		}

		private void LoadSavedDistancesFromFile(string path)
		{
			using(StreamReader reader = new StreamReader(path))
			{
                string line;
                while((line = reader.ReadLine()) != null)
				{
					string[] parts = line.Split(',');
					if (!StationsById.ContainsKey(parts[0]) || !StationsById.ContainsKey(parts[1]))
					{
						continue;
					}

                    BikeStation from = StationsById[parts[0]];
                    BikeStation to = StationsById[parts[1]];
                    int distance = int.Parse(parts[2]);
					if(distance == 0 && from.LocalId != to.LocalId)
					{
                        Console.WriteLine();
                    }
					distances[from.LocalId, to.LocalId] = distance;
					distances[to.LocalId, from.LocalId] = distance;
                }
            }
		}

		public void UpdateStationStatus()
		{
			using(HttpClient client = new HttpClient())
			{
				try
				{
					HttpResponseMessage response = client.GetAsync(stationStatusUrl).Result;
					response.EnsureSuccessStatusCode();

					GBFSStationStatus root = JsonSerializer.Deserialize<GBFSStationStatus>(response.Content.ReadAsStringAsync().Result);

					foreach (GBFSSingleStationStatus station in root.Data.Stations)
					{
						BikeStation s = StationsById[station.StationId];
						s.BikeCount = station.NumBikesAvailable;
					}
				}
				catch (HttpRequestException e)
				{
					// Handle any errors that occurred during the request
					Console.WriteLine("\nException Caught!");
					Console.WriteLine("Message :{0} ", e.Message);
				}
			}
		}
	}
}
*/