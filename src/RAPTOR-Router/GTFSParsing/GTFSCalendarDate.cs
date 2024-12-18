using CsvHelper.Configuration.Attributes;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the calendar dates information from the calendar_dates.txt GTFS file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSCalendarDate
    {
        /// <summary>
        /// The unique identifier for the service to which this calendar date entry belongs.
        /// </summary>
        [Name("service_id")]
        public required string ServiceId { get; set; }

        /// <summary>
        /// The specific date for which the exception is being defined, in YYYYMMDD format.
        /// </summary>
        [Name("date")]
        [TypeConverter(typeof(GTFSDateOnlyConverter))]
        public required DateOnly Date { get; set; }

        /// <summary>
        /// The type of exception for the specified date.
        /// A value of 1 indicates a service that operates on the given date despite being excluded from the regular calendar.
        /// A value of 2 indicates a service that does not operate on the given date despite being included in the regular calendar.
        /// </summary>
        [Name("exception_type")]
        public required int ExceptionType { get; set; }
    }

}
