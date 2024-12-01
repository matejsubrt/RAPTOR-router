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
    public class ConnectionRequest
    {
        public string? srcStopName { get; set; }
        public double srcLat { get; set; }
        public double srcLon { get; set; }
        public string? destStopName { get; set; }
        public double destLat { get; set; }
        public double destLon { get; set; }
        public DateTime? dateTime { get; set; }
        public int rangeLength { get; set; }

        public bool byEarliestDeparture { get; set; }
        public bool range { get; set; }
        public bool srcByCoords { get; set; }
        public bool destByCoords { get; set; }

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
                if (srcCoords.ValidateValue())
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
                if (destCoords.ValidateValue())
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
