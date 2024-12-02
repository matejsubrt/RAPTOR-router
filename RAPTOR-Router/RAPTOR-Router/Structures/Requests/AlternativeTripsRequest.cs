using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Models.Static;

namespace RAPTOR_Router.Structures.Requests
{
    /// <summary>
    /// Class representing a request for alternative (earlier/later) direct trips between two stops
    /// </summary>
    public class AlternativeTripsRequest
    {
        /// <summary>
        /// The id of the source stop
        /// </summary>
        public required string srcStopId { get; set; }

        /// <summary>
        /// The id of the destination stop
        /// </summary>
        public required string destStopId { get; set; }

        /// <summary>
        /// The date and time of the departure of the trip for which to find alternatives
        /// </summary>
        public required DateTime dateTime { get; set; }

        /// <summary>
        /// Whether to find the previous or following trips
        /// </summary>
        public required bool previous { get; set; }

        /// <summary>
        /// The count of alternative trips to find
        /// </summary>
        public required int count { get; set; }

        /// <summary>
        /// The id of the trip for which to find alternatives
        /// </summary>
        public required string tripId { get; set; }

        /// <summary>
        /// Validates the request
        /// </summary>
        /// <param name="transitModel">The transit model used for searches</param>
        /// <returns>The error type for the request object</returns>
        public AlternativesSearchError Validate(TransitModel transitModel)
        {
            if (dateTime < DateTime.Now.AddDays(-14) || dateTime > DateTime.Now.AddDays(14))
            {
                return AlternativesSearchError.InvalidDateTime;
            }

            bool srcStopIdValid = srcStopId is not null && transitModel.stops.ContainsKey(srcStopId);
            bool destStopIdValid = destStopId is not null && transitModel.stops.ContainsKey(destStopId);

            if(!srcStopIdValid && !destStopIdValid)
            {
                return AlternativesSearchError.NonExistentBothStopIds;
            }
            else if (!srcStopIdValid)
            {
                return AlternativesSearchError.NonExistentSrcStopId;
            }
            else if (!destStopIdValid)
            {
                return AlternativesSearchError.NonExistentDestStopId;
            }

            if (count <= 0 || count > 10)
            {
                return AlternativesSearchError.InvalidCount;
            }


            return AlternativesSearchError.NoError;
        }
    }
}
