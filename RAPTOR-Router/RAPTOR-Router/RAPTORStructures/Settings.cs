using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    internal class Settings
    {
        /// <summary>
        /// The maximum number of trips to be used for the searched connection
        /// </summary>
        public const int ROUNDS = 5;
        public const int MAX_TRIP_LENGTH_DAYS = 1;
        /// <summary>
        /// The used walking speed in minutes per kilometer
        /// </summary>
        public int WalkingSpeed { get; set; } = 12;
        /// <summary>
        /// The used cycling speed in minutes per kilometer
        /// </summary>
        public int CyclingSpeed { get; set; } = 5;
        public bool UseSharedBikes { get; set; } = false;

        public TransferLength TransferLength { get; set; } = TransferLength.Normal;
        public ComfortBalance ComfortBalance { get; set; } = ComfortBalance.Balanced;
        public WalkingPreference WalkingPreference { get; set; } = WalkingPreference.Normal;

        public static Settings Default { get; } = new Settings();
    }

    internal enum TransferLength
    {
        Short = 0,
        Normal = 1,
        Long = 2
    }
    internal enum ComfortBalance
    {
        ShortestTime = 0,
        Balanced = 1,
        LeastTransfers = 2
    }
    internal enum WalkingPreference
    {
        High = 0,
        Normal = 1,
        Low = 2
    }
}
