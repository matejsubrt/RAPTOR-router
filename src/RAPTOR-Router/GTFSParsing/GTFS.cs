using CsvHelper;
using System.Globalization;
using System.IO.Compression;
using RAPTOR_Router.Configuration;
using System;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing all the loaded GTFS data. Typically used for parsing the GTFS files to objects in memory, which later will be used to construct the objects useful for the connection searching.
    /// </summary>
    public class GTFS : IDisposable
    {
        /// <summary>
        /// A dictionary of all GTFS agencies within the GTFS data, indexed by their id
        /// </summary>
        public Dictionary<string, GTFSAgency> Agencies { get; set; } = new();
        /// <summary>
        /// A dictionary of all GTFS calendars within the GTFS data, indexed by their service id
        /// </summary>
        public Dictionary<string, GTFSCalendar> Calendars { get; set; } = new();
        /// <summary>
        /// A dictionary indexed by service ids, holding for each service the list of its GTFS calendar dates
        /// </summary>
        public Dictionary<string, List<GTFSCalendarDate>> CalendarDates { get; set; } = new();
        /// <summary>
        /// A dictionary of all GTFS routes within the GTFS data, indexed by their id
        /// </summary>
        public Dictionary<string, GTFSRoute> Routes { get; set; } = new();
        /// <summary>
        /// A dictionary of all GTFS stops within the GTFS data, indexed by their id
        /// </summary>
        public Dictionary<string, GTFSStop> Stops { get; set; } = new();
        /// <summary>
        /// A dictionary indexed by trip Ids, holding for each trip the list of its GTFS stop times
        /// </summary>
        public Dictionary<string, List<GTFSStopTime>> StopTimes { get; set; } = new();
        /// <summary>
        /// A dictionary of all GTFS trips within the GTFS data, indexed by their id
        /// </summary>
        public Dictionary<string, GTFSTrip> Trips { get; set; } = new();


        private static async Task DownloadZipFile(string url, string filePath)
        {
            if(url is null || filePath is null)
            {
                throw new ApplicationException("An api url and a path to the gtfs zip archive location need to be specified.");
            }

            using HttpClient client = new();
            try
            {
                Console.WriteLine("Downloading latest GTFS archive release...");
                byte[] fileBytes = await client.GetByteArrayAsync(url);

                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllBytesAsync(filePath, fileBytes);

                Console.WriteLine($"GTFS archive successfully downloaded and saved to {filePath}");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error downloading zip archive: {ex.Message}");
            }
        }




        /// <summary>
        /// Loads the agencies info from the agencies.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadAgencies(ZipArchive archive)
        {
            ZipArchiveEntry? entry = archive.GetEntry("agency.txt");
            if(entry is null)
            {
                throw new FileNotFoundException("The agency.txt file is missing in the archive");
            }
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSAgency> list = csv.GetRecords<GTFSAgency>().ToList();
                foreach (var agency in list)
                {
                    Agencies.Add(agency.Id, agency);
                }
            }
        }
        /// <summary>
        /// Loads the calendars info from the calendars.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadCalendars(ZipArchive archive)
        {
            ZipArchiveEntry? entry = archive.GetEntry("calendar.txt");
            if (entry is null)
            {
                throw new FileNotFoundException("The calendar.txt file is missing in the archive");
            }
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSCalendar> list = csv.GetRecords<GTFSCalendar>().ToList();
                foreach (var calendar in list)
                {
                    Calendars.Add(calendar.ServiceId, calendar);
                }
            }
        }
        /// <summary>
        /// Loads the calendar dates info from the calendar_dates.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadCalendarDates(ZipArchive archive)
        {
            ZipArchiveEntry? entry = archive.GetEntry("calendar_dates.txt");
            if (entry is null)
            {
                throw new FileNotFoundException("The calendar_dates.txt file is missing in the archive");
            }
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSCalendarDate> list = csv.GetRecords<GTFSCalendarDate>().ToList();
                foreach (var calendarDate in list)
                {
                    if (CalendarDates.ContainsKey(calendarDate.ServiceId))
                    {
                        CalendarDates[calendarDate.ServiceId].Add(calendarDate);
                    }
                    else
                    {
                        CalendarDates.Add(calendarDate.ServiceId, new List<GTFSCalendarDate> { calendarDate });
                    }
                }
            }
        }
        /// <summary>
        /// Loads the routes info from the routes.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadRoutes(ZipArchive archive)
        {
            ZipArchiveEntry? entry = archive.GetEntry("routes.txt");
            if (entry is null)
            {
                throw new FileNotFoundException("The routes.txt file is missing in the archive");
            }
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSRoute> list = csv.GetRecords<GTFSRoute>().ToList();
                foreach (var route in list)
                {
                    Routes.Add(route.Id, route);
                }
            }
        }
        /// <summary>
        /// Loads the stops info from the stops.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadStops(ZipArchive archive)
        {
            ZipArchiveEntry? entry = archive.GetEntry("stops.txt");
            if (entry is null)
            {
                throw new FileNotFoundException("The stops.txt file is missing in the archive");
            }
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSStop> list = csv.GetRecords<GTFSStop>().ToList();
                foreach (var stop in list)
                {
                    //virtual stops for trains, where the trains do not stop are present in the file
                    if (stop.Id[0] != 'T')
                    {
                        Stops.Add(stop.Id, stop);
                    }
                }
            }
        }
        /// <summary>
        /// Loads the stop times info from the stop_times.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadStopTimes(ZipArchive archive)
        {
            ZipArchiveEntry? entry = archive.GetEntry("stop_times.txt");
            if (entry is null)
            {
                throw new FileNotFoundException("The stop_times.txt file is missing in the archive");
            }
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSStopTime> list = csv.GetRecords<GTFSStopTime>().ToList();
                foreach (var stopTime in list)
                {
                    string tripId = stopTime.TripId;
                    if (StopTimes.ContainsKey(tripId) && stopTime.StopId[0] != 'T')
                    {
                        StopTimes[tripId].Add(stopTime);
                    }
                    else if (stopTime.StopId[0]!= 'T')
                    {
                        StopTimes.Add(tripId, new List<GTFSStopTime> { stopTime });
                    }
                }
            }
        }
        /// <summary>
        /// Loads the trips info from the trips.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadTrips(ZipArchive archive)
        {
            ZipArchiveEntry? entry = archive.GetEntry("trips.txt");
            if (entry is null)
            {
                throw new FileNotFoundException("The trips.txt file is missing in the archive");
            }
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSTrip> list = csv.GetRecords<GTFSTrip>().ToList();
                foreach (var trip in list)
                {
                    Trips.Add(trip.Id, trip);
                }
            }
        }



        /// <summary>
        /// Loads all the necessary GTFS data from the specified zip archive into strongly typed objects in memory
        /// </summary>
        /// <param name="pathToZipFile">The path to the zip archive with the GTFS data files</param>
        /// <returns>The representation of the loaded GTFS data</returns>
        public static GTFS ParseZipFile(string pathToZipFile)
        {
            GTFS gtfs = new GTFS();
            using (ZipArchive archive = ZipFile.Open(pathToZipFile, ZipArchiveMode.Read))
            {
                //gtfs.LoadAgencies(archive);
                gtfs.LoadCalendars(archive);
                gtfs.LoadCalendarDates(archive);
                gtfs.LoadRoutes(archive);
                gtfs.LoadStops(archive);
                gtfs.LoadStopTimes(archive);
                gtfs.LoadTrips(archive);
            }
            return gtfs;
        }

        /// <summary>
        /// Downloads and parses the GTFS data from the specified zip archive file
        /// </summary>
        /// <param name="pathToZipFile">The path to the zip file that should be overwritten with the new downloaded one</param>
        /// <returns>The parsed GTFS object</returns>
        public static GTFS DownloadAndParseZipFile(string pathToZipFile, bool downloadNewIfFileOld = true)
        {
            GTFS gtfs = new GTFS();

            bool needToDownloadNewFile = true;

            if (File.Exists(pathToZipFile))
            {
                DateTime fileCreationTime = File.GetLastWriteTime(pathToZipFile);

                if(fileCreationTime.Date == DateTime.Now.Date)
                {
                    needToDownloadNewFile = false;
                }
            }


            if (needToDownloadNewFile && downloadNewIfFileOld)
            {
                string? gtfsArchiveUrl = Config.GtfsStaticZipFileUrl;

                try
                {
                    DownloadZipFile(gtfsArchiveUrl!, pathToZipFile).GetAwaiter().GetResult();
                }
                catch (ApplicationException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            
            gtfs = ParseZipFile(pathToZipFile);

            return gtfs;
        }

        /// <summary>
        /// Removes all the pointers to the GTFS data - to be used after the GTFS data is used for creating the RAPTOR routing data and is no longer needed, so that memory can be freed.
        /// </summary>
        public void Dispose()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Stops = null;
            Calendars = null;
            Routes = null;
            StopTimes = null;
            Trips = null;
            CalendarDates = null;
            Agencies = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
}
