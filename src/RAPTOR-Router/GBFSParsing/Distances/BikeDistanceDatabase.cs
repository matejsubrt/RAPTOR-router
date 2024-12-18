using System;
using Microsoft.Data.Sqlite;
using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.Structures.Bike;

/// <summary>
/// A class used for creating, reading and editing a SQLite database containing distances between bike stations of a single bike network
/// </summary>
public class BikeDistanceDatabase
{
    private readonly string dbPath;

    /// <summary>
    /// Creates a new instance of the BikeDistanceDatabase class and creates the database file if it does not exist
    /// </summary>
    /// <param name="databaseFilePath">The path to the database file</param>
    public BikeDistanceDatabase(string databaseFilePath)
    {
        dbPath = $"Data Source={databaseFilePath}";
        CreateDatabaseAndTable();
    }

    /// <summary>
    /// Creates a new SQLite database file and a table for storing distances between bike stations
    /// </summary>
    private void CreateDatabaseAndTable()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Distances (
                    StationA TEXT,
                    StationB TEXT,
                    Distance INTEGER,
                    PRIMARY KEY (StationA, StationB)
                )";

            using (var command = new SqliteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }


    /// <summary>
    /// Adds or updates the distance between two bike stations in the database
    /// </summary>
    /// <param name="stationAId">The id of the first station</param>
    /// <param name="stationBId">The id of the second station</param>
    /// <param name="distance">The distance between the stations</param>
    public void AddOrUpdateDistance(string stationAId, string stationBId, double distance)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            // Insert or replace the distance value for a given pair
            string insertQuery = @"
            INSERT OR REPLACE INTO Distances (StationA, StationB, Distance) 
            VALUES (@StationA, @StationB, @Distance)";

            using (var command = new SqliteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@StationA", stationAId);
                command.Parameters.AddWithValue("@StationB", stationBId);
                command.Parameters.AddWithValue("@Distance", distance);
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    /// <summary>
    /// Retrieves a distance between two bike stations from the database
    /// </summary>
    /// <param name="stationAId">The id of the first station</param>
    /// <param name="stationBId">The id of the second station</param>
    /// <returns>The distance between the stations in meters</returns>
    /// <remarks>The order of the 2 parameters does not matter</remarks>
    public double GetDistance(string stationAId, string stationBId)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            string selectQuery = @"
                SELECT Distance 
                FROM Distances 
                WHERE (StationA = @StationA AND StationB = @StationB) 
                   OR (StationA = @StationB AND StationB = @StationA)";

            using (var command = new SqliteCommand(selectQuery, connection))
            {
                command.Parameters.AddWithValue("@StationA", stationAId);
                command.Parameters.AddWithValue("@StationB", stationBId);

                var result = command.ExecuteScalar();
                return result == null ? -1 : Convert.ToDouble(result);
            }
        }
    }

    /// <summary>
    /// Removes a station from the database, along with all distances involving it
    /// </summary>
    /// <param name="connection">The SQLite connection to use for the removal</param>
    /// <param name="stationId">The id of the station to remove</param>
    private void RemoveStation(SqliteConnection connection, string stationId)
    {
        string deleteQuery = @"
            DELETE FROM Distances 
            WHERE StationA = @Station OR StationB = @Station";

        using (var command = new SqliteCommand(deleteQuery, connection))
        {
            command.Parameters.AddWithValue("@Station", stationId);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Creates a StationDistanceMatrix object from the distances stored in the database and removes all distances involving stations that are not in the provided dictionary
    /// </summary>
    /// <param name="stationsById">A dictionary of all the currently existent stations of the system indexed by their ids</param>
    /// <returns>The matrix with the loaded distances</returns>
    public StationDistanceMatrix GetDistanceMatrixAndRemoveNonExistentStations(Dictionary<string, BikeStation> stationsById)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            var matrix = new StationDistanceMatrix();

            connection.Open();

            string selectQuery = "SELECT StationA, StationB, Distance FROM Distances";

            using (var command = new SqliteCommand(selectQuery, connection))
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
