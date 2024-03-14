namespace RAPTOR_Router.Structures.Configuration
{
    /// <summary>
    /// Class representing the settings to use for the connection search
    /// </summary>
    public class Settings
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
        /// The used walking pace in minutes per kilometer
        /// </summary>
        public int WalkingPace { get; set; } = 12;
        /// <summary>
        /// The used cycling pace in minutes per kilometer
        /// </summary>
        public int CyclingPace { get; set; } = 5;

        /// <summary>
        /// The time it takes to unlock a bike
        /// </summary>
        public int BikeUnlockTime { get; set; } = 30;
        /// <summary>
        /// The time it takes to lock a bike
        /// </summary>
        public int BikeLockTime { get; set; } = 15;


        /// <summary>
        /// Specifies if shared bikes should be considered in the connection search
        /// </summary>
        public bool UseSharedBikes { get; set; } = false;
        /// <summary>
        /// Specifies if the bike trip length should be limited to 15 minutes
        /// </summary>
        public bool BikeMax15Minutes { get; set; } = true;

        /// <summary>
        /// Specifies the selected transfer length to use in the connection search - i.e. how aggressive and risky the transfers can be
        /// </summary>
        public TransferTime TransferTime { get; set; } = TransferTime.Normal;
        /// <summary>
        /// Specifies the comfort balance to be used in the connection search - i.e. how strongly less transfers should be preferred over shortest time
        /// </summary>
        public ComfortBalance ComfortBalance { get; set; } = ComfortBalance.Balanced;
        /// <summary>
        /// Specifies the walking preference to be used in the connection search - i.e. how much walking there can be in the connection
        /// </summary>
        public WalkingPreference WalkingPreference { get; set; } = WalkingPreference.Normal;
        /// <summary>
        /// Specifies how large the time buffer for bike trips should be
        /// </summary>
        public BikeTripBuffer BikeTripBuffer { get; set; } = BikeTripBuffer.Medium;

        /// <summary>
        /// The default settings to use if none were provided
        /// </summary>
        public static Settings GetDefaultSettings()
        {
            return new Settings();
        }




        /// <summary>
        /// Gets the multiplier that should be used to calculate minimum transfer time according to the set TransferLength preference
        /// </summary>
        /// <returns>The multiplier to be used on the preclculated transfer time</returns>
        /// <exception cref="InvalidDataException">Thrown when the TransferLength property is set to an invalid value</exception>
        public double GetMovingTransferLengthMultiplier()
        {
            switch (TransferTime)
            {
                case TransferTime.UltraShort:
                    return 1.0;
                case TransferTime.Short:
                    return 1.0;
                case TransferTime.Normal:
                    return 1.25;
                case TransferTime.Long:
                    return 1.5;
                default:
                    throw new InvalidDataException("Invalid value of enum TransferLength");
            }
        }
        /// <summary>
        /// Gets the multiplier that should be used to calculate the length of a bike trip according to the set BikeTripBuffer preference
        /// </summary>
        /// <returns>The multiplier to be used on the precalculated transfer time</returns>
        /// <exception cref="InvalidDataException">Thrown when the BikeTripBuffer property has an invalid value</exception>
        public double GetBikeTripLengthMultiplier()
        {
            switch (BikeTripBuffer)
            {
                case BikeTripBuffer.None:
                    return 1.0;
                case BikeTripBuffer.Short:
                    return 1.1;
                case BikeTripBuffer.Medium:
                    return 1.25;
                case BikeTripBuffer.Long:
                    return 1.5;
                default:
                    throw new InvalidDataException("Invalid value of enum BikeTripBuffer");
            }
        }
        /// <summary>
        /// Gets the minimum allowed time between an arrival and departure in the transfer stop (i.e. when arrival and departure are at the same exact stop)
        /// </summary>
        /// <returns>The minimum allowed stationary transfer time in seconds</returns>
        /// <exception cref="InvalidDataException">Thrown when the TransferLength property is set to an invalid value</exception>
        public int GetStationaryTransferMinimumSeconds()
        {
            switch (TransferTime)
            {
                case TransferTime.UltraShort:
                    return 0;
                case TransferTime.Short:
                    return 30;
                case TransferTime.Normal:
                    return 60;
                case TransferTime.Long:
                    return 60;
                default:
                    throw new InvalidDataException("Invalid value of enum TransferLength");
            }
        }

        /// <summary>
        /// Gets the penalty that should be applied to a connection for each its transfer when comparing different connections between each other;
        /// </summary>
        /// <returns>The penalty to be applied in seconds</returns>
        /// <exception cref="InvalidDataException">Thrown when the ComfortBalance property is set to an invalid value</exception>
        public int GetTransferPenaltySeconds()
        {
            switch (ComfortBalance)
            {
                case ComfortBalance.ShortestTimeAbsolute:
                    return 0;
                case ComfortBalance.ShortestTime:
                    return 2 * 60;
                case ComfortBalance.Balanced:
                    return 4 * 60;
                case ComfortBalance.LeastTransfers:
                    return 10 * 60;
                default:
                    throw new InvalidDataException("Invalid value of enum ComfortBalance");
            }
        }

        /// <summary>
        /// Gets the maximum transfer distance according to the WalkingPreference property. The maximum distance does NOT apply to transfers within same node (i.e. stops with the same name)
        /// </summary>
        /// <returns>The maximum transfer distance in meters</returns>
        /// <exception cref="InvalidDataException">Thrown when the WalkingPreference property is set to an invalid value</exception>
        public int GetMaxTransferDistance()
        {
            switch (WalkingPreference)
            {
                case WalkingPreference.High:
                    return 750;
                case WalkingPreference.Normal:
                    return 400;
                case WalkingPreference.Low:
                    return 200;
                default:
                    throw new InvalidDataException("Invalid value of enum WalkingPreference");
            }
        }

        /// <summary>
        /// Gets the time it takes to perform a bike trip of the given distance
        /// </summary>
        /// <param name="distance">The distance of the bike trip in meters</param>
        /// <returns>The time it takes to perform the trip in seconds</returns>
        public int GetBikeTripTime(int distance)
        {
            return (int)(distance / 1000.0 * CyclingPace * 60);
        }
        /// <summary>
        /// Gets the time it takes to perform a transfer of the given distance
        /// </summary>
        /// <param name="distance">The distance of the transfer in meters</param>
        /// <returns>The time it takes to perform the transfer in seconds</returns>
        public int GetWalkingTransferTime(int distance)
        {
            return (int)(distance / 1000.0 * WalkingPace * 60);
        }
    }

    /// <summary>
    /// Enum representing the transfer risk value of the user
    /// 
    /// </summary>
    public enum TransferTime
    {
        /// <summary>
        /// Moving transfer: calculated time, stationary transfer: 0 minutes allowed
        /// </summary>
        UltraShort = 0,
        /// <summary>
        /// Moving transfer: calculated time, stationary transfer: minimum 30 seconds
        /// </summary>
        Short = 1,
        /// <summary>
        /// Moving transfer: calculated time + 25%, stationary transfer: minimum 1 minute
        /// </summary>
        Normal = 2,
        /// <summary>
        /// Moving transfer: calculated time + 50%, stationary transfer: minimum 1 minute
        /// </summary>
        Long = 3
    }
    /// <summary>
    /// enum representing the comfort balance of the user
    /// </summary>
    public enum ComfortBalance
    {
        ShortestTimeAbsolute = 0,
        /// <summary>
        /// Prefer shortest travel time only
        /// </summary>
        ShortestTime = 1,
        /// <summary>
        /// Normal mode -> each transfer adds a 2 min penalty to the connection during comparison
        /// </summary>
        Balanced = 2,
        /// <summary>
        /// Prefer less transfers -> each transfer adds a 10 min penalty to the connection used during comparison
        /// </summary>
        LeastTransfers = 3
    }
    /// <summary>
    /// Enum representing the walking preference of the user
    /// </summary>
    public enum WalkingPreference
    {
        /// <summary>
        /// Any transfer can be up to the upper limit in length (750m)
        /// </summary>
        High = 0,
        /// <summary>
        /// Limit transfer length to normal limit (400m) or longer if in the same node
        /// </summary>
        Normal = 1,
        /// <summary>
        /// Limit transfer length to lower limit (250m) or longer if in the same node
        /// </summary>
        Low = 2
    }
    /// <summary>
    /// Enum representing the time buffer used for bike trips
    /// </summary>
    public enum BikeTripBuffer
    {
        /// <summary>
        /// The exact calculated time by distance * pace will be used
        /// </summary>
        None = 0,
        /// <summary>
        /// A short buffer will be added to the calculated time
        /// </summary>
        Short = 1,
        /// <summary>
        /// A medium buffer will be added to the calculated time
        /// </summary>
        Medium = 2,
        /// <summary>
        /// A long buffer will be added to the calculated time
        /// </summary>
        Long = 3
    }
}
