using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    internal class Transfer
    {
        public Stop From { get; }
        public Stop To { get;}
        public int Time { get;}
        public int Distance { get;}

        public Transfer(Stop from, Stop to, int dist)
        {
            From = from;
            To = to;
            Distance = dist;
            //TODO:Calculate time
            Time = (int)(dist / 1000.0 * 720);
        }
    }
}
