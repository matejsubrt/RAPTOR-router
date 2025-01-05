#pragma warning disable CS1591

namespace RAPTOR_Router.Structures.Configuration
{
    
    /// <summary>
    /// Class representing the settings to use for the connection search
    /// </summary>
    public class Settings
    {
        // public for easy testing access.
        public const int MAX_WALK_DISTANCE_LOW = 250;
        public const int MAX_WALK_DISTANCE_NORMAL = 400;
        public const int MAX_WALK_DISTANCE_HIGH = 750;

        public const double MOVING_TRANSFER_MPL_NONE = 1.0;
        public const double MOVING_TRANSFER_MPL_SHORT = 1.0;
        public const double MOVING_TRANSFER_MPL_NORMAL = 1.25;
        public const double MOVING_TRANSFER_MPL_LONG = 1.5;

        public const int STATIONARY_TRANSFER_MIN_NONE = 0;
        public const int STATIONARY_TRANSFER_MIN_SHORT = 30;
        public const int STATIONARY_TRANSFER_MIN_NORMAL = 60;
        public const int STATIONARY_TRANSFER_MIN_LONG = 60;

        public const double BIKE_TRIP_MPL_NONE = 1.0;
        public const double BIKE_TRIP_MPL_SHORT = 1.1;
        public const double BIKE_TRIP_MPL_NORMAL = 1.25;
        public const double BIKE_TRIP_MPL_LONG = 1.5;

        public const int TRANSFER_PENALTY_SHORTESTABSOLUTE = 0;
        public const int TRANSFER_PENALTY_SHORTEST = 2 * 60;
        public const int TRANSFER_PENALTY_BALANCED = 4 * 60;
        public const int TRANSFER_PENALTY_LEASTTRANSFERS = 10 * 60;



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
        public TransferBuffer TransferBuffer { get; set; } = TransferBuffer.Normal;
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
        public static Settings DEFAULT
        {
            get => new Settings();
        }

        /// <summary>
        /// Validates the settings parameters
        /// </summary>
        /// <returns>Whether all the settings parameters have correct values</returns>
        public bool ValidateParameterValues()
        {
            bool correct = true;

            correct &= Enum.IsDefined(typeof(ComfortBalance), ComfortBalance);
            correct &= Enum.IsDefined(typeof(WalkingPreference), WalkingPreference);
            correct &= Enum.IsDefined(typeof(TransferBuffer), TransferBuffer);
            correct &= Enum.IsDefined(typeof(BikeTripBuffer), BikeTripBuffer);

            correct &= WalkingPace >= 2 && WalkingPace <= 60;
            correct &= CyclingPace > 0 && CyclingPace <= 60;

            correct &= BikeUnlockTime >= 0 && BikeUnlockTime <= 120;
            correct &= BikeLockTime >= 0 && BikeLockTime <= 120;

            return correct;
        }


        /// <summary>
        /// Gets the multiplier that should be used to calculate minimum transfer time according to the set TransferLength preference
        /// </summary>
        /// <returns>The multiplier to be used on the preclculated transfer time</returns>
        /// <exception cref="InvalidDataException">Thrown when the TransferLength property is set to an invalid value</exception>
        public double GetMovingTransferLengthMultiplier()
        {
            switch (TransferBuffer)
            {
                case TransferBuffer.None:
                    return MOVING_TRANSFER_MPL_NONE;
                case TransferBuffer.Short:
                    return MOVING_TRANSFER_MPL_SHORT;
                case TransferBuffer.Normal:
                    return MOVING_TRANSFER_MPL_NORMAL;
                case TransferBuffer.Long:
                    return MOVING_TRANSFER_MPL_LONG;
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
                    return BIKE_TRIP_MPL_NONE;
                case BikeTripBuffer.Short:
                    return BIKE_TRIP_MPL_SHORT;
                case BikeTripBuffer.Medium:
                    return BIKE_TRIP_MPL_NORMAL;
                case BikeTripBuffer.Long:
                    return BIKE_TRIP_MPL_LONG;
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
            switch (TransferBuffer)
            {
                case TransferBuffer.None:
                    return STATIONARY_TRANSFER_MIN_NONE;
                case TransferBuffer.Short:
                    return STATIONARY_TRANSFER_MIN_SHORT;
                case TransferBuffer.Normal:
                    return STATIONARY_TRANSFER_MIN_NORMAL;
                case TransferBuffer.Long:
                    return STATIONARY_TRANSFER_MIN_LONG;
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
                    return TRANSFER_PENALTY_SHORTESTABSOLUTE;
                case ComfortBalance.ShortestTime:
                    return TRANSFER_PENALTY_SHORTEST;
                case ComfortBalance.Balanced:
                    return TRANSFER_PENALTY_BALANCED;
                case ComfortBalance.LeastTransfers:
                    return TRANSFER_PENALTY_LEASTTRANSFERS;
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
                    return MAX_WALK_DISTANCE_HIGH;
                case WalkingPreference.Normal:
                    return MAX_WALK_DISTANCE_NORMAL;
                case WalkingPreference.Low:
                    return MAX_WALK_DISTANCE_LOW;
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
        /// Gets the time that will be billed to the user performing the bike trip -> Time between finally unlocking the bike and locking it again (i.e. does not include the unlock time)
        /// </summary>
        /// <param name="distance">The length of the bike trip in meters</param>
        /// <returns>The billed bike trip time in seconds</returns>
        public int GetBilledBikeTripTime(int distance)
        {
            return (int)((distance / 1000.0) * CyclingPace * 60 * GetBikeTripLengthMultiplier()) + BikeLockTime;
        }

        /// <summary>
        /// Gets the full time of a bike trip including the unlock and lock times (i.e. time between reaching the source station and being able to leave the destination station)
        /// </summary>
        /// <param name="distance">The length of the bike trip in meters</param>
        /// <returns>The full adjusted bike trip time in seconds, including unlock and lock times</returns>
        public int GetAdjustedBikeTripTime(int distance)
        {
            return (int)((distance / 1000.0) * CyclingPace * 60 * GetBikeTripLengthMultiplier()) + BikeUnlockTime + BikeLockTime;
        }

        /// <summary>
        /// Gets the time it takes to perform a walking transfer of the given distance according to the settings
        /// </summary>
        /// <param name="distance">The length of the transfer in meters</param>
        /// <returns>The time it takes to perform the transfer in seconds</returns>
        public int GetAdjustedWalkingTransferTime(int distance)
        {
            return (int)((distance / 1000.0) * WalkingPace * 60 * GetMovingTransferLengthMultiplier());
        }

        public int GetTransferTime(int distance)
        {
            var walkingTime = GetAdjustedWalkingTransferTime(distance);
            var stationaryTime = GetStationaryTransferMinimumSeconds();

            return Math.Max(walkingTime, stationaryTime);
        }
    }

    /// <summary>
    /// Enum representing the transfer risk value of the user
    /// 
    /// </summary>
    public enum TransferBuffer
    {
        /// <summary>
        /// Moving transfer: calculated time, stationary transfer: 0 minutes allowed
        /// </summary>
        None = 0,
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
        /// <summary>
        /// Prefer absolute shortest time -> no transfer penalty
        /// </summary>
        ShortestTimeAbsolute = 0,

        /// <summary>
        /// Prefer the shortest time -> 2 min penalty for each transfer when comparing connections
        /// </summary>
        ShortestTime = 1,

        /// <summary>
        /// Normal mode -> 4 min penalty for each transfer when comparing connections
        /// </summary>
        Balanced = 2,

        /// <summary>
        /// Prefer less transfers -> 10 min penalty for each transfer when comparing connections
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
#pragma warning restore CS1591
