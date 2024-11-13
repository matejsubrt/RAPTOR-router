using System;
using System.Data.SQLite;
using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.Structures.Bike;

public class BikeDistanceDatabase
{
    private readonly string dbPath;

    public BikeDistanceDatabase(string databaseFilePath = "bike_distances.db")
    {
        dbPath = $"Data Source={databaseFilePath}";
        CreateDatabaseAndTable();
    }

    private void CreateDatabaseAndTable()
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Distances (
                    StationA TEXT,
                    StationB TEXT,
                    Distance INTEGER,
                    PRIMARY KEY (StationA, StationB)
                )";

            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    public void AddOrUpdateDistance(string stationA, string stationB, double distance)
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();

            // Insert or replace the distance value for a given pair
            string insertQuery = @"
            INSERT OR REPLACE INTO Distances (StationA, StationB, Distance) 
            VALUES (@StationA, @StationB, @Distance)";

            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@StationA", stationA);
                command.Parameters.AddWithValue("@StationB", stationB);
                command.Parameters.AddWithValue("@Distance", distance);
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    public double GetDistance(string stationA, string stationB)
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();

            string selectQuery = @"
                SELECT Distance 
                FROM Distances 
                WHERE (StationA = @StationA AND StationB = @StationB) 
                   OR (StationA = @StationB AND StationB = @StationA)";

            using (var command = new SQLiteCommand(selectQuery, connection))
            {
                command.Parameters.AddWithValue("@StationA", stationA);
                command.Parameters.AddWithValue("@StationB", stationB);

                var result = command.ExecuteScalar();
                return result == null ? -1 : Convert.ToDouble(result);
            }
        }
    }

    private void RemoveStation(SQLiteConnection connection, string station)
    {
        string deleteQuery = @"
            DELETE FROM Distances 
            WHERE StationA = @Station OR StationB = @Station";

        using (var command = new SQLiteCommand(deleteQuery, connection))
        {
            command.Parameters.AddWithValue("@Station", station);
            command.ExecuteNonQuery();
        }
    }

    public StationDistanceMatrix GetDistanceMatrixAndRemoveNonExistentStations(Dictionary<string, BikeStation> stationsById)
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            var matrix = new StationDistanceMatrix();

            connection.Open();

            string selectQuery = "SELECT StationA, StationB, Distance FROM Distances";

            using (var command = new SQLiteCommand(selectQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string stationA = reader.GetString(0);
                        string stationB = reader.GetString(1);
                        int distance = reader.GetInt32(2);

                        if (!stationsById.ContainsKey(stationA))
                        {
                            RemoveStation(connection, stationA);
                            continue;
                        }

                        if (!stationsById.ContainsKey(stationB))
                        {
                            RemoveStation(connection, stationB);
                            continue;
                        }

                        BikeStation src = stationsById[stationA];
                        BikeStation dest = stationsById[stationB];


                        matrix.AddDistance(src, dest, distance);
                    }
                    return matrix;
                }
            }
        }
    }
}
