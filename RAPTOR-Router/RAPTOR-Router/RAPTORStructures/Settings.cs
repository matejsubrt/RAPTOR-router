using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    /// <summary>
    /// Class representing the settings to use for the connection search
    /// </summary>
    internal class Settings
    {
        /// <summary>
        /// The maximum number of trips to be used for the searched connection
        /// </summary>
        public const int ROUNDS = 5;
        /// <summary>
        /// The maximum number of days between the specified departure time and the arrival
        /// </summary>
        public const int MAX_TRIP_LENGTH_DAYS = 1;
        /// <summary>
        /// The used walking speed in minutes per kilometer
        /// </summary>
        public int WalkingSpeed { get; set; } = 12;
        /// <summary>
        /// The used cycling speed in minutes per kilometer
        /// </summary>
        public int CyclingSpeed { get; set; } = 5;
        /// <summary>
        /// Specifies if shared bikes should be considered in the connection search
        /// </summary>
        public bool UseSharedBikes { get; set; } = false;

        /// <summary>
        /// Specifies the selected transfer length to use in the connection search - i.e. how aggressive and risky the transfers can be
        /// </summary>
        public TransferLength TransferLength { get; set; } = TransferLength.Normal;
        /// <summary>
        /// Specifies the comfort balance to be used in the connection search - i.e. how strongly less transfers should be preferred over shortest time
        /// </summary>
        public ComfortBalance ComfortBalance { get; set; } = ComfortBalance.Balanced;
        /// <summary>
        /// Specifies the walking preference to be used in the connection search - i.e. how much walking there can be in the connection
        /// </summary>
        public WalkingPreference WalkingPreference { get; set; } = WalkingPreference.Normal;

        /// <summary>
        /// The default settings to use if none were provided
        /// </summary>
        public static Settings Default { get; } = new Settings();
    }

    /// <summary>
    /// Enum representing the transfer risk value of the user
    /// </summary>
    internal enum TransferLength
    {
        Short = 0,
        Normal = 1,
        Long = 2
    }
    /// <summary>
    /// enum representing the comfort balance of the user
    /// </summary>
    internal enum ComfortBalance
    {
        ShortestTime = 0,
        Balanced = 1,
        LeastTransfers = 2
    }
    /// <summary>
    /// Enum representing the walking preference of the user
    /// </summary>
    internal enum WalkingPreference
    {
        High = 0,
        Normal = 1,
        Low = 2
    }
}
