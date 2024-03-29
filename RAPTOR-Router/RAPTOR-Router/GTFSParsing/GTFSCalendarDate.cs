﻿using CsvHelper.Configuration.Attributes;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the calendar dates information from the calendar_dates.txt gtfs file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSCalendarDate : IIdentifiable
    {
        [Name("service_id")]
        public string ServiceId { get; set; }

        [Name("date")]
        [TypeConverter(typeof(GTFSDateOnlyConverter))]
        public DateOnly Date { get; set; }

        [Name("exception_type")]
        public int ExceptionType { get; set; }

        public string GetId()
        {
            return ServiceId;
        }
    }
}
