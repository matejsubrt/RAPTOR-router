using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the calendars information from the calendars.txt gtfs file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSCalendar
    {
        /// <summary>
        /// The unique identifier for the service (e.g., a service ID representing a specific route or schedule).
        /// </summary>
        [Name("service_id")]
        public required string ServiceId { get; set; }

        /// <summary>
        /// Indicates whether the service operates on Monday.
        /// </summary>
        [Name("monday")]
        public required bool Monday { get; set; }

        /// <summary>
        /// Indicates whether the service operates on Tuesday.
        /// </summary>
        [Name("tuesday")]
        public required bool Tuesday { get; set; }

        /// <summary>
        /// Indicates whether the service operates on Wednesday.
        /// </summary>
        [Name("wednesday")]
        public required bool Wednesday { get; set; }

        /// <summary>
        /// Indicates whether the service operates on Thursday.
        /// </summary>
        [Name("thursday")]
        public required bool Thursday { get; set; }

        /// <summary>
        /// Indicates whether the service operates on Friday.
        /// </summary>
        [Name("friday")]
        public required bool Friday { get; set; }

        /// <summary>
        /// Indicates whether the service operates on Saturday.
        /// </summary>
        [Name("saturday")]
        public required bool Saturday { get; set; }

        /// <summary>
        /// Indicates whether the service operates on Sunday.
        /// </summary>
        [Name("sunday")]
        public required bool Sunday { get; set; }

        /// <summary>
        /// The start date of the service, in the format YYYYMMDD.
        /// </summary>
        [Name("start_date")]
        [TypeConverter(typeof(GTFSDateOnlyConverter))]
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// The end date of the service, in the format YYYYMMDD.
        /// </summary>
        [Name("end_date")]
        [TypeConverter(typeof(GTFSDateOnlyConverter))]
        public DateOnly EndDate { get; set; }

        /// <summary>
        /// Finds out, whether the service (calendar) is operating on the provided date. Does NOT take into account the exception operations from calendar_dates
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public bool IsOperating(DateOnly date)
    {
        if(date < StartDate || date > EndDate)
        {
            return false;
        }
        switch (date.DayOfWeek)
        {
            case DayOfWeek.Monday:
                return Monday;
            case DayOfWeek.Tuesday:
                return Tuesday;
            case DayOfWeek.Wednesday:
                return Wednesday;
            case DayOfWeek.Thursday:
                return Thursday;
                case DayOfWeek.Friday:
                    return Friday;
                case DayOfWeek.Saturday:
                    return Saturday;
                case DayOfWeek.Sunday:
                    return Sunday;
                default:
                    return false;
            }
        }
    }
}
