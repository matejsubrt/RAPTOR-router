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
    internal class GTFS
    {
        public Dictionary<string, GTFSAgency> agencies { get; private set; } = new();
        public Dictionary<string, GTFSCalendar> calendars { get; private set; } = new();
        public Dictionary<string, List<GTFSCalendarDate>> calendarDates { get; private set; } = new();
        public Dictionary<string, GTFSRoute> routes { get; private set; } = new();
        public Dictionary<string, GTFSStop> stops { get; private set; } = new();
        public Dictionary<string, List<GTFSStopTime>> stopTimes { get; private set; } = new();
        public Dictionary<string, GTFSTrip> trips { get; private set; } = new();


        public void LoadFile<T>(string pathToZipFile, string fileName, T type) where T : IIdentifiable
        {
            using (ZipArchive archive = ZipFile.Open(pathToZipFile, ZipArchiveMode.Read))
            {
                ZipArchiveEntry entry = archive.GetEntry(fileName);
                using (StreamReader reader = new StreamReader(entry.Open()))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    List<T> list = csv.GetRecords<T>().ToList();
                    foreach(T t in list)
                    {

                    }
                }
                entry.LastWriteTime = DateTimeOffset.UtcNow.LocalDateTime;
            }
        }
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
        public void LoadStops(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("stops.txt");
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSStop> list = csv.GetRecords<GTFSStop>().ToList();
                foreach (var stop in list)
                {
                    stops.Add(stop.GetId(), stop);
                }
            }
        }
        public void LoadStopTimes(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("stop_times.txt");
            using (StreamReader reader = new StreamReader(entry.Open()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<GTFSStopTime> list = csv.GetRecords<GTFSStopTime>().ToList();
                foreach (var stopTime in list)
                {
                    if (stopTimes.ContainsKey(stopTime.GetId()))
                    {
                        stopTimes[stopTime.GetId()].Add(stopTime);
                    }
                    else
                    {
                        stopTimes.Add(stopTime.GetId(), new List<GTFSStopTime> { stopTime });
                    }
                }
            }
        }
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
        public Dictionary<string, GTFSStop> GetGtfsStops()
        {
            return this.stops;
        }
        public Dictionary<string, GTFSRoute> GetGtfsRoutes()
        {
            return this.routes;
        }
        public Dictionary<string, GTFSCalendar> GetGtfsCalendars()
        {
            return this.calendars;
        }
        public Dictionary<string, GTFSTrip> GetGtfsTrips()
        {
            return this.trips;
        }
        public Dictionary<string, List<GTFSStopTime>> GetGtfsStopTimes()
        {
            return this.stopTimes;
        }
    }
}
