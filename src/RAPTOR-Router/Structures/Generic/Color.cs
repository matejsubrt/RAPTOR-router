namespace RAPTOR_Router.Structures.Generic
{
    /// <summary>
    /// Struct representing a color
    /// </summary>
    /// <remarks>Typically used within this project for colors of different routes parsed from the GTFS data</remarks>
    public struct Color
    {
        /// <summary>
        /// The red component of the color
        /// </summary>
        public byte R { get; private set; }
        /// <summary>
        /// The green component of the color
        /// </summary>
        public byte G { get; private set; }
        /// <summary>
        /// The blue component of the color
        /// </summary>
        public byte B { get; private set; }

        /// <summary>
        /// Creates a new color object from a RGB values string (#RRGGBB)
        /// </summary>
        /// <param name="hexColor">The string to parse from</param>
        /// <exception cref="ArgumentException">Thrown on invalid format of the color string</exception>
        public Color(string hexColor)
        {
            if (hexColor.StartsWith("#"))
                hexColor = hexColor.Substring(1);

            if (hexColor.Length != 6)
                throw new ArgumentException("Hex color must be 6 characters long.");

            R = Convert.ToByte(hexColor.Substring(0, 2), 16);
            G = Convert.ToByte(hexColor.Substring(2, 2), 16);
            B = Convert.ToByte(hexColor.Substring(4, 2), 16);
        }
    }

}
