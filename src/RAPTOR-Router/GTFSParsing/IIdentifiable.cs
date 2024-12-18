namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// An Interface indicating, that the object has a unique string Id
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// The unique string id of the object
        /// </summary>
        public string Id { get; }
    }
}
