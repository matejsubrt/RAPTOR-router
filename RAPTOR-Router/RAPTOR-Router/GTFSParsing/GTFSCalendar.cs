using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;

namespace RAPTOR_Router.GTFSParsing
{
    internal class GTFSCalendar : IIdentifiable
    {
        [Name("service_id")]
        public string ServiceId { get; set; }
        [Name("monday")]
        public bool Monday { get; set; }
        [Name("tuesday")]
        public bool Tuesday { get; set; }
        [Name("wednesday")]
        public bool Wednesday { get; set; }
        [Name("thursday")]
        public bool Thursday { get; set; }
        [Name("friday")]
        public bool Friday { get; set; }
        [Name("saturday")]
        public bool Saturday { get; set; }
        [Name("sunday")]
        public bool Sunday { get; set; }
        [Name("start_date")]
        [TypeConverter(typeof(GTFSDateOnlyConverter))]
        public DateOnly StartDate { get; set; }
        [Name("end_date")]
        [TypeConverter(typeof(GTFSDateOnlyConverter))]
        public DateOnly EndDate { get; set; }

        public string GetId()
        {
            return ServiceId;
        }
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
