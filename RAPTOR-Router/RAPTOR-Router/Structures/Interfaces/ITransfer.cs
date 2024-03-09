using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Interfaces
{
    public interface ITransfer
    {
        public IRoutePoint GetSrcRoutePoint();
        public IRoutePoint GetDestRoutePoint();
        public int GetTransferTime(int walkingPace);
        public int Distance { get; }
    }
}
