using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Configuration
{
    public static class Config
    {
        private static IConfigurationRoot? _config;
        static Config()
        {
            var basePath = Directory.GetCurrentDirectory();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ".."));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                basePath = Path.GetFullPath(Path.Combine(basePath, ".."));
            }


            _config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string? RetrieveValue(string key)
        {
            if (_config is null)
            {
                throw new InvalidOperationException("Configuration not loaded");
            }
            return _config[key];
        }

        public static string? DefaultGTFSPath { get; } = _config["gtfsArchivePath"];
        public static string? TestGTFSArchivePath { get; } = _config["testGtfsArchivePath"];


        public static string? OsmFilePath { get; } = _config["osmFilePath"];
        public static string? RouterDbFilePath { get; } = _config["routerDbFilePath"];


        public static string? NextbikeDbPath { get; } = _config["nextbikeDbPath"];


        public static string? ForbiddenCrossingPointsPath { get; } = _config["forbiddenCrossingPointsPath"];
        public static string? ForbiddenCrossingLinesPath { get; } = _config["forbiddenCrossingLinesPath"];


        public static string? TestDataFilePath { get; } = _config["testDataFilePath"];

    }
}
