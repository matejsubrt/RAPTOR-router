using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing all the loaded GTFS data
    /// </summary>
    internal class GTFS : IDisposable
    {
        public Dictionary<string, GTFSAgency> agencies { get; private set; } = new();
        public Dictionary<string, GTFSCalendar> calendars { get; private set; } = new();
        public Dictionary<string, List<GTFSCalendarDate>> calendarDates { get; private set; } = new();
        public Dictionary<string, GTFSRoute> routes { get; private set; } = new();
        public Dictionary<string, GTFSStop> stops { get; private set; } = new();
        public Dictionary<string, List<GTFSStopTime>> stopTimes { get; private set; } = new();
        public Dictionary<string, GTFSTrip> trips { get; private set; } = new();

        /// <summary>
        /// Loads the agencies info from the agencies.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadAgencies(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("agency.txt");
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSAgency> list = csv.GetRecords<GTFSAgency>().ToList();
                foreach (var agency in list)
                {
                    agencies.Add(agency.GetId(), agency);
                }
            }
        }
        /// <summary>
        /// Loads the calendars info from the calendars.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadCalendars(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("calendar.txt");
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSCalendar> list = csv.GetRecords<GTFSCalendar>().ToList();
                foreach (var calendar in list)
                {
                    calendars.Add(calendar.GetId(), calendar);
                }
            }
        }
        /// <summary>
        /// Loads the calůendar dates info from the calendar_dates.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadCalendarDates(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("calendar_dates.txt");
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSCalendarDate> list = csv.GetRecords<GTFSCalendarDate>().ToList();
                foreach (var calendarDate in list)
                {
                    if (calendarDates.ContainsKey(calendarDate.GetId()))
                    {
                        calendarDates[calendarDate.GetId()].Add(calendarDate);
                    }
                    else
                    {
                        calendarDates.Add(calendarDate.GetId(), new List<GTFSCalendarDate> { calendarDate });
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
            ZipArchiveEntry entry = archive.GetEntry("routes.txt");
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSRoute> list = csv.GetRecords<GTFSRoute>().ToList();
                foreach (var route in list)
                {
                    routes.Add(route.GetId(), route);
                }
            }
        }
        /// <summary>
        /// Loads the stops info from the stops.txt GTFS file
        /// </summary>
        /// <param name="archive">The zip archive the file is located in</param>
        public void LoadStops(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("stops.txt");
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSStop> list = csv.GetRecords<GTFSStop>().ToList();
                foreach (var stop in list)
                {
                    //virtual stops for trains, where the trains do not stop are present in the file
                    if (stop.Id[0] != 'T')
                    {
                        stops.Add(stop.GetId(), stop);
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
            ZipArchiveEntry entry = archive.GetEntry("stop_times.txt");
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSStopTime> list = csv.GetRecords<GTFSStopTime>().ToList();
                foreach (var stopTime in list)
                {
                    string tripId = stopTime.GetId();
                    if (stopTimes.ContainsKey(tripId) && stopTime.StopId[0] != 'T')
                    {
                        stopTimes[tripId].Add(stopTime);
                    }
                    else if (stopTime.StopId[0]!= 'T')
                    {
                        stopTimes.Add(tripId, new List<GTFSStopTime> { stopTime });
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
            ZipArchiveEntry entry = archive.GetEntry("trips.txt");
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSTrip> list = csv.GetRecords<GTFSTrip>().ToList();
                foreach (var trip in list)
                {
                    trips.Add(trip.GetId(), trip);
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
                gtfs.LoadAgencies(archive);
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
        /// Removes all the pointers to the GTFS data - to be used after the GTFS data is used for creating the RAPTOR routing data and is no longer needed
        /// </summary>
        public void Dispose()
        {
            stops = null;
            calendars = null;
            routes = null;
            stopTimes = null;
            trips = null;
            calendarDates = null;
            agencies = null;
        }
    }
}
