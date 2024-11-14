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

            if (_config is null)
            {
                throw new Exception("Configuration could not be loaded - the file does not exist: " + basePath + "config.json");
            }
        }

        public static string? DefaultGTFSPath => _config?["gtfsArchivePath"];
        public static string? TestGTFSArchivePath => _config?["testGtfsArchivePath"];


        public static string? OsmFilePath => _config?["osmFilePath"];
        public static string? RouterDbFilePath => _config?["routerDbFilePath"];


        public static string? NextbikeDbPath => _config?["nextbikeDbPath"];


        public static string? ForbiddenCrossingPointsPath => _config?["forbiddenCrossingPointsPath"];
        public static string? ForbiddenCrossingLinesPath => _config?["forbiddenCrossingLinesPath"];


        public static string? TestDataFilePath => _config?["testDataFilePath"];


    }
}
