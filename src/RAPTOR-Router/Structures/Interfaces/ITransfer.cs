﻿namespace RAPTOR_Router.Structures.Interfaces
{
    /// <summary>
    /// An interface for a transfer between two route points
    /// </summary>
    public interface ITransfer
    {
        /// <summary>
        /// Gets the source point of the transfer
        /// </summary>
        /// <returns>The source point</returns>
        public IRoutePoint GetSrcRoutePoint();
        /// <summary>
        /// Gets the destination point of the transfer
        /// </summary>
        /// <returns>The destination point</returns>
        public IRoutePoint GetDestRoutePoint();
        /// <summary>
        /// The length of the transfer in meters
        /// </summary>
        public int Distance { get; }
    }
}
