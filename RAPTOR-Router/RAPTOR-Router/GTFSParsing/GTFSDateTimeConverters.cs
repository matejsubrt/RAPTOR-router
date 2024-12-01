using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvHelper;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class for converting Dates in GTFS text form (YYYYMMDD) to DateOnly objects.
    /// </summary>
    public class GTFSDateOnlyConverter : DefaultTypeConverter
    {
        /// <summary>
        /// Converts the provided Date string (YYYYMMDD) to a DateOnly object.
        /// </summary>
        /// <param name="text">The Date string in YYYYMMDD format to convert.</param>
        /// <param name="row">The reader row being processed (not used in this implementation but required by the base method signature).</param>
        /// <param name="memberMapData">The metadata for the current member being mapped (not used in this implementation but required by the base method signature).</param>
        /// <returns>The new DateOnly object representing the provided date.</returns>
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            return new DateOnly(int.Parse(text!.Substring(0, 4)), int.Parse(text!.Substring(4, 2)), int.Parse(text!.Substring(6, 2)));
        }
    }

    /// <summary>
    /// Class for converting Times in GTFS text form (HH:MM:SS) to TimeOnly objects.
    /// </summary>
    public class GTFSTimeOnlyConverter : DefaultTypeConverter
    {
        /// <summary>
        /// Converts the provided Time string (HH:MM:SS) to a TimeOnly object.
        /// </summary>
        /// <param name="text">The Time string in HH:MM:SS format to convert.</param>
        /// <param name="row">The reader row being processed (not used in this implementation but required by the base method signature).</param>
        /// <param name="memberMapData">The metadata for the current member being mapped (not used in this implementation but required by the base method signature).</param>
        /// <returns>The new TimeOnly object representing the provided time.</returns>
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            var values = text!.Split(":");
            return new TimeOnly(int.Parse(values[0]) % 24, int.Parse(values[1]), int.Parse(values[2]));
        }
    }
}
