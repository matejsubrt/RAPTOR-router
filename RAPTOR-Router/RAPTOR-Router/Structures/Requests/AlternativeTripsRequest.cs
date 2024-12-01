using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Models.Static;

namespace RAPTOR_Router.Structures.Requests
{
    public class AlternativeTripsRequest
    {
        public required string srcStopId { get; set; }
        public required string destStopId { get; set; }
        public required DateTime dateTime { get; set; }
        public required bool previous { get; set; }
        public required int count { get; set; }
        public required string tripId { get; set; }


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
