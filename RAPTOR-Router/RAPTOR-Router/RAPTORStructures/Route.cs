using RAPTOR_Router.GTFSParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
	/// <summary>
	/// Class representing an unique public transit route, there is a different instance for each variation of one "normal" route - i.e. tram trips returning to depot, ...
	/// </summary>
	internal class Route
	{
		/// <summary>
		/// The Id of this unique route - combination of the GTFSId and the stop Id's of the unique route. Is unique for every Route object.
		/// </summary>
		public string Id { get; }
		/// <summary>
		/// The GTFS Id of the GTFS Route entry associated with the unique route. 
		/// Is NOT unique for every Route object - for example a single tram line (single GTFS Route) can have multiple unique Routes associated with it (trams returning to depot/coming out of it, shortened trips, ...)
		/// </summary>
		public string GTFSId { get; }
		/// <summary>
		/// The short name of the associated GTFS Route
		/// </summary>
		public string ShortName { get; }
		/// <summary>
		/// The long name of the associated GTFS Route
		/// </summary>
		public string LongName { get; }
		/// <summary>
		/// The type of the vehicle that serves the route (i.e. bus/tram/metro/...)
		/// </summary>
		public VehicleType Type { get; }
		/// <summary>
		/// The color that should be used for the route
		/// </summary>
		public Color Color { get; }
		/// <summary>
		/// The list of all stops on the route in correct order on the route
		/// </summary>
		public List<Stop> RouteStops { get; set; } = new();
		/// <summary>
		/// A dictionary, which for every date when the Route has at least one operating trip contains the list of trips that operate on the day. This is necessary, as a trip does NOT contain information about the day, only about time.
		/// </summary>
		public Dictionary<DateOnly, List<Trip>> RouteTrips { get; set; } = new();
		/// <summary>
		/// Creates a new Route object
		/// </summary>
		/// <param name="id">The unique Route Id to use (Should be unique for each Route object)</param>
		/// <param name="gtfsId">The Id of the associated GTFS route (does not have to be unique for each Route object)</param>
		/// <param name="shortName">The short name of the route</param>
		/// <param name="longName">The long name of the route</param>
		public Route(string id, string gtfsId, string shortName, string longName)
		{
			Id = id;
			GTFSId = gtfsId;
			ShortName = shortName;
			LongName = longName;
		}

        /// <summary>
        /// Creates a new Route object
        /// </summary>
        /// <param name="id">The unique Route Id to use (Should be unique for each Route object)</param>
        /// <param name="gtfsRoute">The GTFSRoute object, from which the Route should be constructed</param>
        public Route(string id, GTFSRoute gtfsRoute)
		{
			Id = id;
			GTFSId = gtfsRoute.Id;
			ShortName = gtfsRoute.ShortName;
			LongName = gtfsRoute.LongName;
			Type = (VehicleType)gtfsRoute.Type;
			Color = new Color(gtfsRoute.Color);
		}
		/// <summary>
		/// Finds the index of a specified stop in the route's list of stops
		/// </summary>
		/// <param name="stop">The stop to find the index of</param>
		/// <returns>The index of the stop in the stops list</returns>
		/// <exception cref="InvalidOperationException">Thrown if stop is not found in the stops list</exception>
		public int GetStopIndex(Stop stop)
		{
			int res = RouteStops.IndexOf(stop);
			if (res == -1)
			{
				throw new InvalidOperationException("Stop not found");
			}
			return res;
		}
		/// <summary>
		/// Finds the earliest trip serving the route at the specified stop leaving after the specified time
		/// </summary>
		/// <param name="stop">The stop to find the earliest trip from</param>
		/// <param name="date">The earliest possible date of the trip</param>
		/// <param name="time">The earliest possible time of the trip</param>
		/// <param name="maxDaysAfter">The maximum number of days between the specified earliest time and the trip departure time</param>
		/// <param name="tripDate">The date on which the trip actually leaves -> if the first found trip is after midnight, this date is different than the date input parameter</param>
		/// <returns>The earliest trip, that leaves the stop after the specified time on the route, null if no trip is found</returns>
		public Trip GetEarliestTripAtStop(Stop stop, DateOnly date, TimeOnly time, int maxDaysAfter, out DateOnly tripDate)
		{
			int stopIndex = GetStopIndex(stop);
			DateOnly currDate = date;
			DateOnly maxDate = date.AddDays(maxDaysAfter);

			List<Trip> tripsOnDate;
			if (RouteTrips.ContainsKey(currDate))
			{
				tripsOnDate = RouteTrips[currDate];

				TimeOnly departureTime;
				//Scan the first day for trips leaving after specified time
				for (int i = 0; i < tripsOnDate.Count; i++)
				{
					departureTime = tripsOnDate[i].StopTimes[stopIndex].DepartureTime;

					if(departureTime < tripsOnDate[i].StopTimes[0].DepartureTime)
					{
						tripDate = currDate.AddDays(1);
						return tripsOnDate[i];
					}

					if (departureTime >= time)
					{
						tripDate = currDate;
						return tripsOnDate[i];
					}
				}
			}

			
			//scan the following days till maxDay and select first available trip
			while(currDate < maxDate)
			{
				currDate = currDate.AddDays(1);

				if(RouteTrips.ContainsKey(currDate) && RouteTrips[currDate].Count > 0)
				{
					tripDate = currDate;
					return RouteTrips[currDate][0];
				}
			}
			//No trip found in the specified timeframe
			tripDate = new DateOnly();
			return null;
		}

		public override string ToString()
		{
			return ShortName + ": From " + RouteStops[0] + " to " + RouteStops[RouteStops.Count - 1];
		}



		public enum VehicleType
		{
			TRAM = 0,                       // Tram, Streetcar, Light rail
			METRO = 1,                      // Subway, Metro
			RAIL = 2,                       // Rail (Intercity or long-distance travel)
			BUS = 3,                        // Bus (Short- and long-distance routes)
			FERRY = 4,                      // Ferry
			CABLE_TRAM = 5,                 // Cable tram
			AERIAL_LIFT = 6,                // Aerial lift, suspended cable car
			FUNICULAR = 7,					// Funicular
			TROLLEYBUS = 11,				// Trolleybus
			MONORAIL = 12					// Monorail
		}
	}
}
