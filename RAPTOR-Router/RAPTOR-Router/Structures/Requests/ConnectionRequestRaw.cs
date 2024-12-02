using RAPTOR_Router.Structures.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Generic;

namespace RAPTOR_Router.Structures.Requests
{
    /// <summary>
    /// Class representing a request for connection(s) between two stops or coordinates
    /// </summary>
    public class ConnectionRequest
    {
        /// <summary>
        /// Specifies whether the source is given by coordinates or by the name of a stop
        /// </summary>
        public bool srcByCoords { get; set; }
        /// <summary>
        /// The name of the source stop
        /// </summary>
        /// <remarks>Valid only if srcByCoords is false</remarks>
        public string? srcStopName { get; set; }
        /// <summary>
        /// The latitude of the source coordinates
        /// </summary>
        /// <remarks>Valid only if srcByCoords is true</remarks>
        public double srcLat { get; set; }
        /// <summary>
        /// The longitude of the source coordinates
        /// </summary>
        /// <remarks>Valid only if srcByCoords is true</remarks>
        public double srcLon { get; set; }


        /// <summary>
        /// Specifies whether the destination is given by coordinates or by the name of a stop
        /// </summary>
        public bool destByCoords { get; set; }
        /// <summary>
        /// The name of the destination stop
        /// </summary>
        /// <remarks>Valid only if destByCoords is false</remarks>
        public string? destStopName { get; set; }
        /// <summary>
        /// The latitude of the destination coordinates
        /// </summary>
        /// <remarks>Valid only if destByCoords is true</remarks>
        public double destLat { get; set; }
        /// <summary>
        /// The longitude of the destination coordinates
        /// </summary>
        /// <remarks>Valid only if destByCoords is true</remarks>
        public double destLon { get; set; }


        /// <summary>
        /// The date and time of the search begin
        /// </summary>
        /// <remarks>If byEarliestDeparture is true, the earliest possible departure from source, otherwise the latest possible arrival at destination</remarks>
        public DateTime? dateTime { get; set; }

        /// <summary>
        /// Specifies whether to find the best connections within a time range, or only for a single search begin time
        /// </summary>
        public bool range { get; set; }

        /// <summary>
        /// The length of the time range.
        /// </summary>
        /// <remarks>Valid only if range is true</remarks>
        public int rangeLength { get; set; }

        /// <summary>
        /// Specifies whether to search for connections by the earliest departure time or the latest arrival time (i.e. the search direction)
        /// </summary>
        public bool byEarliestDeparture { get; set; }

        /// <summary>
        /// The settings to be used for the search
        /// </summary>
        public Settings? settings { get; set; }






        private bool ValidateStopsNearCoords(Coordinates coords, bool useSharedBikes, TransitModel transitModel, BikeModel bikeModel)
        {
            if(transitModel is null){
                throw new InvalidOperationException("Transit model not loaded");
            }
            if(useSharedBikes){
                if(bikeModel is null)
                {
                    throw new InvalidOperationException("Bike model not loaded");
                }
                return transitModel.NearStopExists(coords, 750) || bikeModel.NearStationExists(coords, 750);
            }
            else
            {
                return transitModel.NearStopExists(coords, 750);
            }
        }

        private bool ValidateStopName(string? stopName, TransitModel transitModel)
        {
            if (transitModel is null)
            {
                throw new InvalidOperationException("Transit model not loaded");
            }

            return stopName is not null && transitModel.GetStopsByName(stopName).Count != 0;
        }

        /// <summary>
        /// Validates the request parameters
        /// </summary>
        /// <param name="transitModel">The transit model used for searches</param>
        /// <param name="bikeModel">The bike model used for searches</param>
        /// <returns>The resulting connection search error object</returns>
        public ConnectionSearchError Validate(TransitModel transitModel, BikeModel bikeModel)
        {
            if (dateTime is null)
            {
                return ConnectionSearchError.InvalidDateTime;
            }

            // Validate the settings
            if (settings is null || !settings.ValidateParameterValues())
            {
                return ConnectionSearchError.InvalidSettings;
            }


            bool srcCoordsValid = true;
            bool srcCoordsHaveStops = true;
            bool destCoordsValid = true;
            bool destCoordsHaveStops = true;
            bool srcStopNameValid = true;
            bool destStopNameValid = true;

            if (srcByCoords)
            {
                Coordinates srcCoords = new Coordinates(srcLat, srcLon);
                if (!srcCoords.ValidateValue())
                {
                    srcCoordsValid = false;
                }

                if (!ValidateStopsNearCoords(srcCoords, settings.UseSharedBikes, transitModel, bikeModel))
                {
                    srcCoordsHaveStops = false;
                }
            }
            else
            {
                if (!ValidateStopName(srcStopName, transitModel))
                {
                    srcStopNameValid = false;
                }
            }

            if (destByCoords)
            {
                Coordinates destCoords = new Coordinates(destLat, destLon);
                if (!destCoords.ValidateValue())
                {
                    destCoordsValid = false;
                }

                if (!ValidateStopsNearCoords(destCoords, settings.UseSharedBikes, transitModel, bikeModel))
                {
                    destCoordsHaveStops = false;
                }
            }
            else
            {
                if (!ValidateStopName(destStopName, transitModel))
                {
                    destStopNameValid = false;
                }
            }

            if (!srcCoordsValid)
            {
                if (!destCoordsValid)
                {
                    return ConnectionSearchError.InvalidBothCoordinates;
                }
                else
                {
                    return ConnectionSearchError.InvalidSrcCoordinates;
                }
            }
            else
            {
                if (!destCoordsValid)
                {
                    return ConnectionSearchError.InvalidDestCoordinates;
                }
            }


            if (!srcCoordsHaveStops)
            {
                if (!destCoordsHaveStops)
                {
                    return ConnectionSearchError.NoStopsNearBothCoords;
                }
                else
                {
                    return ConnectionSearchError.NoStopsNearSrcCoords;
                }
            }
            else
            {
                if (!destCoordsHaveStops)
                {
                    return ConnectionSearchError.NoStopsNearDestCoords;
                }
            }


            if (!srcStopNameValid)
            {
                if (!destStopNameValid)
                {
                    return ConnectionSearchError.InvalidBothStopNames;
                }
                else
                {
                    return ConnectionSearchError.InvalidSrcStopName;
                }
            }
            else
            {
                if (!destStopNameValid)
                {
                    return ConnectionSearchError.InvalidDestStopName;
                }
            }

            return ConnectionSearchError.NoError;
        }

        
    }
}
