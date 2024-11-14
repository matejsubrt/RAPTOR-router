using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Transit
{
    /// <summary>
    /// Enum representing the type of the vehicle that serves the route
    /// </summary>
    public enum VehicleType
    {
        TRAM = 0,                       // Tram, Streetcar, Light rail
        METRO = 1,                      // Subway, Metro
        RAIL = 2,                       // Rail (Intercity or long-distance travel)
        BUS = 3,                        // Bus (Short- and long-distance routes)
        FERRY = 4,                      // Ferry
        CABLE_TRAM = 5,                 // Cable tram
        AERIAL_LIFT = 6,                // Aerial lift, suspended cable car
        FUNICULAR = 7,                  // Funicular
        TROLLEYBUS = 11,                // Trolleybus
        MONORAIL = 12                   // Monorail
    }
}
