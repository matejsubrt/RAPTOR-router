using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Custom
{
    public abstract class CustomTransfer : ITransfer
    {
        public int Distance { get; protected set; }
        public abstract int GetTransferTime(int walkingPace);
        public abstract IRoutePoint GetSrcRoutePoint();
        public abstract IRoutePoint GetDestRoutePoint();
    }
    public class FromCustomTransfer : CustomTransfer
    {
        public IRoutePoint From { get; }
        public IRoutePoint To { get; }

        public FromCustomTransfer(IRoutePoint customRP, IRoutePoint normalRP, int dist)
        {
            From = customRP;
            To = normalRP;
            Distance = dist;
        }
        public override string ToString()
        {
            return "Transfer from " + From.Name + " to " + To.Name;
        }
        public override int GetTransferTime(int walkingPace)
        {
            return (int)(Distance / 1000.0 * walkingPace * 60);
        }
        public override IRoutePoint GetSrcRoutePoint()
        {
            return From;
        }
        public override IRoutePoint GetDestRoutePoint()
        {
            return To;
        }
    }
    public class ToCustomTransfer : CustomTransfer
    {
        public IRoutePoint From { get; }
        public IRoutePoint To { get; }
        public ToCustomTransfer(IRoutePoint normalRP, IRoutePoint customRP, int dist)
        {
            From = normalRP;
            To = customRP;
            Distance = dist;
        }
        public override string ToString()
        {
            return "Transfer from " + From.Name + " to " + To.Name;
        }
        public override int GetTransferTime(int walkingPace)
        {
            return (int)(Distance / 1000.0 * walkingPace * 60);
        }
        public override IRoutePoint GetSrcRoutePoint()
        {
            return From;
        }
        public override IRoutePoint GetDestRoutePoint()
        {
            return To;
        }
    }
}
