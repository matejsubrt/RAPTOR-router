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
        public string? srcStopId { get; set; }
        public string? destStopId { get; set; }
        public DateTime? dateTime { get; set; }
        public bool previous { get; set; }
        public int count { get; set; }


        public AlternativesSearchError Validate(TransitModel transitModel)
        {
            if (dateTime is null)
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
