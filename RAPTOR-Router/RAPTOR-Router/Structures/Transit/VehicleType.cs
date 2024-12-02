using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Transit
{
    /// <summary>
    /// Enum representing the type of the vehicle that serves the route.
    /// </summary>
    public enum VehicleType
    {
        /// <summary>
        /// Tram, Streetcar, Light rail.
        /// </summary>
        TRAM = 0,

        /// <summary>
        /// Subway, Metro.
        /// </summary>
        METRO = 1,

        /// <summary>
        /// Rail (Intercity or long-distance travel).
        /// </summary>
        RAIL = 2,

        /// <summary>
        /// Bus (Short- and long-distance routes).
        /// </summary>
        BUS = 3,

        /// <summary>
        /// Ferry.
        /// </summary>
        FERRY = 4,

        /// <summary>
        /// Cable tram.
        /// </summary>
        CABLE_TRAM = 5,

        /// <summary>
        /// Aerial lift, suspended cable car.
        /// </summary>
        AERIAL_LIFT = 6,

        /// <summary>
        /// Funicular.
        /// </summary>
        FUNICULAR = 7,

        /// <summary>
        /// Trolleybus.
        /// </summary>
        TROLLEYBUS = 11,

        /// <summary>
        /// Monorail.
        /// </summary>
        MONORAIL = 12
    }

}
