using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Structures.Transit;

namespace RAPTOR_Router.Structures.Bike
{
    public abstract class BikeTransfer : ITransfer
    {
        public int Distance { get; protected set; }
        public abstract int GetTransferTime(int walkingPace);
        public abstract IRoutePoint GetSrcRoutePoint();
        public abstract IRoutePoint GetDestRoutePoint();
        public BikeTransfer OppositeTransfer { get; set; }
    }
    public class FromBikeTransfer : BikeTransfer
    {
        public BikeStation From { get; }
        public Stop To { get; }

        public FromBikeTransfer(BikeStation bikeStation, Stop stop, int dist)
        {
            From = bikeStation;
            To = stop;
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
    public class ToBikeTransfer : BikeTransfer
    {
        public Stop From { get; }
        public BikeStation To { get; }
        public ToBikeTransfer(Stop stop, BikeStation bikeStation, int dist)
        {
            From = stop;
            To = bikeStation;
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
