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
