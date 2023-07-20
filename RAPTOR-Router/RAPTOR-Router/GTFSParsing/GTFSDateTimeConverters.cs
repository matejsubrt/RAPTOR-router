using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class for converting DateTimes in gtfs text form to DateTime objects
    /// </summary>
    public class GTFSDateTimeConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return new DateTime(int.Parse(text.Substring(0, 4)), int.Parse(text.Substring(4, 2)), int.Parse(text.Substring(6, 2)));
        }
    }
    /// <summary>
    /// Class for converting Dates in gtfs text form to DateOnly objects
    /// </summary>
    public class GTFSDateOnlyConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            return new DateOnly(int.Parse(text.Substring(0, 4)), int.Parse(text.Substring(4, 2)), int.Parse(text.Substring(6, 2)));
        }
    }
    /// <summary>
    /// Class for converting Times in gtfs text form to TimeOnly objects
    /// </summary>
    public class GTFSTimeOnlyConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            var values = text.Split(":");
            return new TimeOnly(int.Parse(values[0])%24, int.Parse(values[1]), int.Parse(values[2]));
        }
    }
}
