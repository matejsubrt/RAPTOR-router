using Microsoft.AspNetCore.Routing;
using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.SearchModels
{
	/// <summary>
	/// Class representing a single connection search. One exists for every connection search problem being solved.
	/// Typically is first initiated by adding sourceStops, destinationStops and the departure time, and then is provided to a router, which uses the object as the data holding object for its connection search algorithm.
	/// </summary>
	internal class SearchModel
	{
		/// <summary>
		/// List of stops considered as the source
		/// </summary>
		internal List<Stop> sourceStops { get; set; }
		/// <summary>
		/// List of stops considered as the destination
		/// </summary>
		internal List<Stop> destinationStops { get; set; }
		/// <summary>
		/// A dictionary containing the current routing information for every stop
		/// </summary>
		private Dictionary<Stop, StopRoutingInfo> routingInfo = new();
		/// <summary>
		/// The departure time, at which the connection search starts - i.e. the earliest possible time, at which the first trip/transfer leaves the source stop
		/// </summary>
		private DateTime departureTime;
		/// <summary>
		/// The currently best found arrival time to the destination stop
		/// </summary>
		private DateTime bestCurrentArrivalTime = DateTime.MaxValue;
		/// <summary>
		/// The settings used for the connection search
		/// </summary>
		private Settings settingsUsed;

		/// <summary>
		/// Creates a new SearchModel object
		/// </summary>
		/// <param name="sourceStops">The list of stops considered as the source stops - typically stops from one node sharing the same name</param>
		/// <param name="destinationStops">The list of stops considered as the destination stops - typically stops from one node sharing the same name</param>
		/// <param name="departureTime">The earliest possible departure time of the found connection</param>
		public SearchModel(List<Stop> sourceStops, List<Stop> destinationStops, DateTime departureTime, Settings settingsUsed)
		{
			this.sourceStops = sourceStops;
			this.destinationStops = destinationStops;
			this.departureTime = departureTime;
			this.settingsUsed = settingsUsed;
		}

		/// <summary>
		/// Class representing the routing information about a certain stop
		/// </summary>
		internal class StopRoutingInfo
		{
			/// <summary>
			/// The current earliest possible arrival time at the stop
			/// </summary>
			internal DateTime earliestArrival;
			/// <summary>
			/// The current earliest possible arrival at the stop, separately for each round
			/// </summary>
			internal DateTime[] earliestArrivalRounds;
			/// <summary>
			/// The trip used to reach the stop for each round.
			/// </summary>
			/// <remarks>Null if stop cannot be reached in said round, or is reached sooner by a transfer in said round.</remarks>
			internal Trip[] tripsToReachRounds;
			/// <summary>
			/// The get on stop used to reach the stop for each round - the stop, on which the tripToReach in the same round was boarded to reach the stop.
			/// </summary>
			/// <remarks>Null if stop cannot be reached in said round, or is reached sooner by a transfer in said round.</remarks>
			internal Stop[] getOnStopsToReachRounds;
			/// <summary>
			/// The transfer used to reach the stop for each round.
			/// </summary>
			/// <remarks>Null if stop cannot be reached in said round, or is reached sooner by a trip in said round.</remarks>
			internal Transfer[] transfersToReachRounds;

			/// <summary>
			/// Creates a new StopRoutingInfo object with all the arrivalTimes set to the maxValue
			/// </summary>
			internal StopRoutingInfo()
			{
				earliestArrival = DateTime.MaxValue;
				earliestArrivalRounds = new DateTime[Settings.ROUNDS + 1];
				Array.Fill(earliestArrivalRounds, DateTime.MaxValue);

				tripsToReachRounds = new Trip[Settings.ROUNDS + 1];
				getOnStopsToReachRounds = new Stop[Settings.ROUNDS + 1];
				transfersToReachRounds = new Transfer[Settings.ROUNDS + 1];
			}
		}

		public SearchResult ExtractResult()
		{
            // For each round, get the stop with earliest arrival
            Stop[] earliestDestStopsRounds = new Stop[Settings.ROUNDS];
            for (int round = 0; round < Settings.ROUNDS; round++)
            {
                earliestDestStopsRounds[round] = GetDestStopWithMinArrivalTimeInRound(round);
            }

			SearchResult[] resultsRounds = new SearchResult[Settings.ROUNDS];
			for(int round = 0; round < Settings.ROUNDS; round++)
			{
				resultsRounds[round] = CreateResultFromStopInRound(earliestDestStopsRounds[round], round);
			}

			return GetBestResult(resultsRounds, earliestDestStopsRounds);


			SearchResult GetBestResult(SearchResult[] results, Stop[] earliestDestStops)
			{
				int bestRound = -1;
				DateTime bestArrivalTime = DateTime.MaxValue;
				for(int round = 0; round < results.Length; round++)
				{
					if (earliestDestStops[round] is not null)
					{
						var usedSegments = results[round].UsedSegments;

                        bool startsWithTransfer = usedSegments[0].segmentType == SearchResult.SegmentType.Transfer;
						bool endsWithTransfer = usedSegments[usedSegments.Count - 1].segmentType == SearchResult.SegmentType.Transfer;

                        int penaltySecondsPerTransfer = settingsUsed.GetTransferPenaltySeconds();

                        int transferCount = (round == 0 ? round : round - 1);
						if (startsWithTransfer)
						{
							transferCount++;
						}
						if (endsWithTransfer)
						{
							transferCount++;
						}

                        var stopInfo = routingInfo[earliestDestStops[round]];

                        DateTime adjustedArrivalTime = stopInfo.earliestArrivalRounds[round].AddSeconds(transferCount * penaltySecondsPerTransfer);
                        //earliestArrivalRounds[round] = adjustedArrivalTime;

                        if (adjustedArrivalTime < bestArrivalTime)
                        {
                            bestArrivalTime = adjustedArrivalTime;
                            bestRound = round;
                        }
                    }
				}

				if(bestRound == -1)
				{
					return null;
				}
				return results[bestRound];
			}

            SearchResult CreateResultFromStopInRound(Stop stop, int round)
            {
				if(stop is null)
				{
					return null;
				}
                SearchResult result = new(settingsUsed);
                StopRoutingInfo currStopInfo = routingInfo[stop];



                Stop nextRoundStartStop = stop;
                int currRound = round;
                while (currRound > 0)
                {
                    bool transferUsed = false;

                    Transfer transferToReachStop = currStopInfo.transfersToReachRounds[currRound];
                    if (transferToReachStop is not null)
                    {
                        result.AddUsedTransfer(transferToReachStop);
                        transferUsed = true;
                    }

                    Stop currStop;
                    // In current round, transfer has been used after  the trip
                    if (transferUsed)
                    {
                        currStop = transferToReachStop.From;
                    }
                    // In current round, no transfer has been used, i.e. we are continuing from the exact same stop -> we add a new 0 length transfer
                    else
                    {
                        currStop = nextRoundStartStop;
                        // in last round, we do not add a transfer
                        if (currRound != round)
                        {
                            result.AddUsedTransfer(new Transfer(currStop, currStop, 0));
                        }
                    }

                    currStopInfo = routingInfo[currStop];
                    Trip tripToReachStop = currStopInfo.tripsToReachRounds[currRound];
                    Stop getOnStop = currStopInfo.getOnStopsToReachRounds[currRound];
                    if (tripToReachStop is null || getOnStop is null)
                    {
                        throw new ApplicationException("Trip and getOnStop cannot be null in an used round");
                    }
                    result.AddUsedTrip(tripToReachStop, getOnStop, currStop);

                    currStop = getOnStop;
                    currStopInfo = routingInfo[currStop];
                    nextRoundStartStop = currStop;
                    currRound--;
                }

                Transfer transferToReachFirstTripGetOnStop = currStopInfo.transfersToReachRounds[0];
                if (transferToReachFirstTripGetOnStop is not null)
                {
                    result.AddUsedTransfer(transferToReachFirstTripGetOnStop);
                }

                return result;
            }

            Stop? GetDestStopWithMinArrivalTimeInRound(int round)
            {
                Stop? stopWithMinArrTime = null;
                DateTime earliestArrival = DateTime.MaxValue;
                foreach (Stop stop in destinationStops)
                {
                    //arrival is earlier than best we found so far AND it is better than in last round - otherwise we do not process this round
                    if (
                        routingInfo.ContainsKey(stop)
                        && routingInfo[stop].earliestArrivalRounds[round] < earliestArrival
                        && (round == 0 || ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(stop, round))
                    )
                    {
                        stopWithMinArrTime = stop;
                        earliestArrival = routingInfo[stop].earliestArrivalRounds[round];
                    }
                }
                return stopWithMinArrTime;
            }

            bool ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(Stop stop, int round)
            {
                DateTime bestEarlierArrival = DateTime.MaxValue;
                for (int i = 0; i < round; i++)
                {
                    DateTime arrivalInRoundI = routingInfo[stop].earliestArrivalRounds[i];
                    if (arrivalInRoundI < bestEarlierArrival)
                    {
                        bestEarlierArrival = arrivalInRoundI;
                    }
                }
                return bestEarlierArrival > routingInfo[stop].earliestArrivalRounds[round];
            }
        }

        /// <summary>
        /// Generates the result of the search after the search algorithm has been finished
        /// </summary>
        /// <returns>The result of the search - i.e. the representation of the fastest possible connection</returns>
        public SearchResult ExtractResultOld()
		{
			// For each round, get the stop with earliest arrival
			Stop[] earliestDestStopsRounds = new Stop[Settings.ROUNDS];
            for (int round = 0; round < Settings.ROUNDS; round++)
            {
                earliestDestStopsRounds[round] = GetDestStopWithMinArrivalTimeInRound(round);
            }

			// Calculate the arrival times in each round including the penalties according to settings used
			// Find out which round is fastest
			// Array -> possible future returning of multiple results if it makes sense
			DateTime[] earliestArrivalRounds = new DateTime[Settings.ROUNDS];
			DateTime earliestArrival = DateTime.MaxValue;
			int fastestRound = -1;
			for(int round = 0; round < Settings.ROUNDS; round++)
			{
				Stop? earliestDestStopInCurrRound = earliestDestStopsRounds[round];
				if(earliestDestStopInCurrRound is null)
				{
					earliestArrivalRounds[round] = DateTime.MaxValue;
                }
				else
				{
					int penaltySecondsPerTransfer = settingsUsed.GetTransferPenaltySeconds();
					int transferCount = round == 0 ? round : round-1;
					var stopInfo = routingInfo[earliestDestStopInCurrRound];

                    DateTime arrivalTimeWithPenalty = stopInfo.earliestArrivalRounds[round].AddSeconds(transferCount * penaltySecondsPerTransfer);
                    earliestArrivalRounds[round] = arrivalTimeWithPenalty;

					if(arrivalTimeWithPenalty < earliestArrival)
					{
						earliestArrival = arrivalTimeWithPenalty;
						fastestRound = round;
					}
                }
            }

			// No connection could be found
			if(fastestRound == -1)
			{
				return null;
			}

			// Get stop with earliest adjusted arrival
			Stop arrivalStop = earliestDestStopsRounds[fastestRound];
            var result = CreateResultFromStopInRound(arrivalStop, fastestRound);
			return result;



			SearchResult CreateResultFromStopInRound(Stop stop, int round)
			{
				SearchResult result = new(settingsUsed);
				StopRoutingInfo currStopInfo = routingInfo[stop];

				

				Stop nextRoundStartStop = stop;				
                int currRound = round;
                while (currRound > 0)
				{
                    bool transferUsed = false;

                    Transfer transferToReachStop = currStopInfo.transfersToReachRounds[currRound];
					if(transferToReachStop is not null)
					{
						result.AddUsedTransfer(transferToReachStop);
						transferUsed = true;
					}

					Stop currStop;
					// In current round, transfer has been used after  the trip
                    if (transferUsed)
					{
						currStop = transferToReachStop.From;
					}
					// In current round, no transfer has been used, i.e. we are continuing from the exact same stop -> we add a new 0 length transfer
					else
					{						
						currStop = nextRoundStartStop;
						// in last round, we do not add a transfer
						if(currRound != round)
						{
                            result.AddUsedTransfer(new Transfer(currStop, currStop, 0));
                        }
                    }

					currStopInfo = routingInfo[currStop];
					Trip tripToReachStop = currStopInfo.tripsToReachRounds[currRound];
					Stop getOnStop = currStopInfo.getOnStopsToReachRounds[currRound];
					if(tripToReachStop is null || getOnStop is null)
					{
						throw new ApplicationException("Trip and getOnStop cannot be null in an used round");
					}
					result.AddUsedTrip(tripToReachStop, getOnStop, currStop);

					currStop = getOnStop;
					currStopInfo = routingInfo[currStop];
					nextRoundStartStop = currStop;
					currRound--;
				}

				Transfer transferToReachFirstTripGetOnStop = currStopInfo.transfersToReachRounds[0];
				if(transferToReachFirstTripGetOnStop is not null)
				{
					result.AddUsedTransfer(transferToReachFirstTripGetOnStop);
				}

				return result;
			}

            Stop? GetDestStopWithMinArrivalTimeInRound(int round)
            {
                Stop? stopWithMinArrTime = null;
                DateTime earliestArrival = DateTime.MaxValue;
                foreach (Stop stop in destinationStops)
                {
                    //arrival is earlier than best we found so far AND it is better than in last round - otherwise we do not process this round
                    if (
                        routingInfo.ContainsKey(stop)
                        && routingInfo[stop].earliestArrivalRounds[round] < earliestArrival
                        && (round == 0 || ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(stop, round))
                    )
                    {
                        stopWithMinArrTime = stop;
                        earliestArrival = routingInfo[stop].earliestArrivalRounds[round];
                    }
                }
                return stopWithMinArrTime;
            }

            bool ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(Stop stop, int round)
            {
                DateTime bestEarlierArrival = DateTime.MaxValue;
                for (int i = 0; i < round; i++)
                {
                    DateTime arrivalInRoundI = routingInfo[stop].earliestArrivalRounds[i];
                    if (arrivalInRoundI < bestEarlierArrival)
                    {
                        bestEarlierArrival = arrivalInRoundI;
                    }
                }
                return bestEarlierArrival > routingInfo[stop].earliestArrivalRounds[round];
            }
        }		

		/// <summary>
		/// Finds out if it is possible and fastest to reach the stop by transfer in the specified round, rather by by trip
		/// </summary>
		/// <param name="stop">The stop to use</param>
		/// <param name="round">The round in which the reaching method is to be found</param>
		/// <returns></returns>
		public bool StopIsReachedByTransferInRound(Stop stop, int round)
		{
			return GetRoutingInfo(stop).transfersToReachRounds[round] is not null;
		}
		/// <summary>
		/// Gets the earliest departure time of the search
		/// </summary>
		/// <returns>The departure time</returns>
		public DateTime GetDepartureTime()
		{
			return departureTime;
		}
		/// <summary>
		/// Gets the current best/earliest arrival time
		/// </summary>
		/// <returns>The best current arrival time</returns>
		public DateTime GetCurrentBestArrivalTime()
		{
			return bestCurrentArrivalTime;
		}
		/// <summary>
		/// Gets the earliest currently possible arrival time to the specified stop
		/// </summary>
		/// <param name="stop">The stop to get earliest arrival to</param>
		/// <returns>The earliest possible arrival time to the stop</returns>
		public DateTime GetEarliestArrival(Stop stop)
		{
			return GetRoutingInfo(stop).earliestArrival;
		}
		/// <summary>
		/// Gets the earliest currently possible arrival to the specified stop in the specified round (i.e. using the specified number of trips)
		/// </summary>
		/// <param name="stop">The stop to get earliest arrival to</param>
		/// <param name="round">The round to get the earliest arrival in</param>
		/// <returns>The earliest possible arrival time to the stop in the specified round</returns>
		public DateTime GetEarliestArrivalInRound(Stop stop, int round)
		{
			return GetRoutingInfo(stop).earliestArrivalRounds[round];
		}

		/// <summary>
		/// Sets the current overall best arrival time to one of the destination stops
		/// </summary>
		/// <param name="arrivalTime">The best arrival time to set</param>
		public void SetCurrentBestArrivalTime(DateTime arrivalTime)
		{
			this.bestCurrentArrivalTime = arrivalTime;
		}
		/// <summary>
		/// Sets the transfer to be used to reach the specified stop the fastest in the specified round
		/// </summary>
		/// <param name="stop">The stop to set the transfer to reach for</param>
		/// <param name="round">The round to set the transfer to reach in</param>
		/// <param name="transfer">The transfer to set as the transfer to reach</param>
		public void SetTransferToReachInRound(Stop stop, int round, Transfer transfer)
		{
			GetRoutingInfo(stop).transfersToReachRounds[round] = transfer;
		}
		/// <summary>
		/// Sets the earliest arrival to the stop in the specified round
		/// </summary>
		/// <param name="stop">The stop to set the earliest arrival for</param>
		/// <param name="round">The round to set the earliest arrival in</param>
		/// <param name="arrivalTime">The earliest arrival to set</param>
		public void SetEarliestArrivalInRound(Stop stop, int round, DateTime arrivalTime)
		{
			GetRoutingInfo(stop).earliestArrivalRounds[round] = arrivalTime;
		}
		/// <summary>
		/// Sets the overall best arrival time to the specified stop
		/// </summary>
		/// <param name="stop">The stop to set the earliest arrival for</param>
		/// <param name="arrivalTime">The earliest arrival time to set</param>
		public void SetEarliestArrival(Stop stop, DateTime arrivalTime)
		{
			GetRoutingInfo(stop).earliestArrival = arrivalTime;
			if (destinationStops.Contains(stop) && arrivalTime < bestCurrentArrivalTime)
			{
				bestCurrentArrivalTime = arrivalTime;
			}
		}
		/// <summary>
		/// Sets the trip to be used to reach the specified stop the fastest in the specified round
		/// </summary>
		/// <param name="stop">The stop to set the trip to reach for</param>
		/// <param name="round">The round to set the trip to reach in</param>
		/// <param name="transfer">The trip to set as the trip to reach</param>
		public void SetTripToReachInRound(Stop stop, int round, Trip trip)
		{
			GetRoutingInfo(stop).tripsToReachRounds[round] = trip;
		}
		/// <summary>
		/// Sets the getOnStop to reach the specified stop the fastest in the specified round using its tripToReach on the specified date
		/// </summary>
		/// <param name="stop">The stop to set the getOnStop for</param>
		/// <param name="round">The round to set the getOnSTop in</param>
		/// <param name="getOnStop">The stop to be set as the getOnStop</param>
		public void SetGetOnStopToReachInRound(Stop stop, int round, Stop getOnStop)
		{
			GetRoutingInfo(stop).getOnStopsToReachRounds[round] = getOnStop;
		}
		/// <summary>
		/// Initiates the search by setting the earliest arrival times to all the source stops as the departure time
		/// </summary>
		public void SetSourceStopsEarliestArrival()
		{
			foreach(Stop sourceStop in sourceStops)
			{
				StopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceStop);
				stopRoutingInfo.earliestArrival = departureTime;
				stopRoutingInfo.earliestArrivalRounds[0] = departureTime;
			}
		}
		/// <summary>
		/// Gets the routing info for the specified stop if it exists. If not, creates on, adds it to the routingInfo and returns it
		/// </summary>
		/// <param name="stop"></param>
		/// <returns></returns>
		private StopRoutingInfo GetRoutingInfo(Stop stop)
		{
			if (routingInfo.ContainsKey(stop))
			{
				return routingInfo[stop];
			}
			else
			{
				StopRoutingInfo stopRoutingInfo = new StopRoutingInfo();
				routingInfo.Add(stop, stopRoutingInfo);
				return stopRoutingInfo;
			}
		}
	}
}
