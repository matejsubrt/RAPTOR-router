using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Generic
{
    public struct Color
    {
        public byte R { get; private set; }
        public byte G { get; private set; }
        public byte B { get; private set; }

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
