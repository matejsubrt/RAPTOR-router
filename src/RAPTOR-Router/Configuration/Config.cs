﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Configuration
{
    /// <summary>
    /// Class used for retrieving configuration values from the config.json file
    /// </summary>
    public static class Config
    {
        private static IConfigurationRoot? _config;

        private static string basePath;

        /// <summary>
        /// Constructs the configuration object
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the config.json file was not found in its location</exception>
        static Config()
        {
            basePath = Directory.GetCurrentDirectory();
            basePath = Path.GetFullPath(Path.Combine(basePath, ".."));

            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //{
            //    basePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ".."));
            //}
            //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            //{
            //    basePath = Path.GetFullPath(Path.Combine(basePath, ".."));
            //}


            _config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();

            Console.WriteLine("Base Path: " + basePath);

            if (_config is null)
            {
                throw new FileNotFoundException("Configuration could not be loaded - the file does not exist: " + basePath + "config.json");
            }
        }

        static string GetFullPath(string key)
        {
            var relativePath = _config?[key];

            if (relativePath is null)
            {
                return null;
            }

            var fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));

            return fullPath;
        }

        /// <summary>
        /// The location of the main GTFS zip archive used for transit routing
        /// </summary>
        public static string? DefaultGTFSPath => GetFullPath("gtfsArchivePath");//_config?["gtfsArchivePath"];
        /// <summary>
        /// The location of the test GTFS zip archive used for testing
        /// </summary>
        public static string? TestGTFSArchivePath => GetFullPath("testGtfsArchivePath");//_config?["testGtfsArchivePath"];

        /// <summary>
        /// The location of the OSM file used for bike routing
        /// </summary>
        public static string? OsmFilePath => GetFullPath("osmFilePath");//_config?["osmFilePath"];
        /// <summary>
        /// The location of the routerdb file used for bike routing
        /// </summary>
        /// <remarks>This file can be created using the osm file. This value is not mandatory</remarks>
        public static string? RouterDbFilePath => GetFullPath("routerDbFilePath");//_config?["routerDbFilePath"];

        /// <summary>
        /// The location of the nextbike database file used for bike routing
        /// </summary>
        public static string? NextbikeDbPath => GetFullPath("nextbikeDbPath");//_config?["nextbikeDbPath"];

        public static string? NextbikeDbTestPath => GetFullPath("nextbikeDbTestPath");//_config?["nextbikeTestDbPath"];

        /// <summary>
        /// The location of the forbidden crossing points csv file
        /// </summary>
        public static string? ForbiddenCrossingPointsPath => GetFullPath("forbiddenCrossingPointsPath");//_config?["forbiddenCrossingPointsPath"];
        /// <summary>
        /// The location of the forbidden crossing lines csv file
        /// </summary>
        public static string? ForbiddenCrossingLinesPath => GetFullPath("forbiddenCrossingLinesPath");//_config?["forbiddenCrossingLinesPath"];

        /// <summary>
        /// The location of the test request data file used for testing
        /// </summary>
        public static string? ConnectionTestDataFilePath => GetFullPath("connectionTestDataFilePath");//_config?["testDataFilePath"];

        public static string? AltTripTestDataFilePath => GetFullPath("altTripTestDataFilePath");//_config?["altTripTestDataFilePath"];

        /// <summary>
        /// The location of the file containing the API key for the Golemio API
        /// </summary>
        public static string? GolemioAPIKeyPath => GetFullPath("golemioApiKeyPath");//_config?["golemioApiKeyPath"];

        /// <summary>
        /// The URL of the API used to retrieve real-time trip updates
        /// </summary>
        public static string? GtfsRealtimeTripUpdatesApiUrl => _config?["gtfsRealtimeTripUpdatesApiUrl"];

        /// <summary>
        /// The URL from which to download the GTFS static zip file
        /// </summary>
        public static string? GtfsStaticZipFileUrl => _config?["gtfsStaticZipFileUrl"];
    }
}
