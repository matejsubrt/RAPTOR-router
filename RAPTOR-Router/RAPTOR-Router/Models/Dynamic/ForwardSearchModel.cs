using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;

namespace RAPTOR_Router.Models.Dynamic
{

	/// <summary>
	/// A class holding all the dynamic data of a single forward connection search. The ForwardRouteFinder uses this class to store the data of the search.
	/// </summary>
	/// <remarks>Is used for searches, where the earliest possible departure time is known, and we need to calculate the earliest possible arrival time to the destination.</remarks>
	internal class ForwardSearchModel : SearchModelBase
	{
		/// <summary>
		/// Dictionary indexed by the RoutePoints, holding the current routing information about each RoutePoint
		/// </summary>
		private readonly Dictionary<IRoutePoint, ForwardStopRoutingInfo> routingInfo = new();

		private readonly DateTime departureTime;
		private DateTime bestCurrentArrivalTime = DateTime.MaxValue;

		/// <summary>
		/// Creates a new ForwardSearchModel object
		/// </summary>
		/// <param name="sourceStops">The list of stops considered as the source stops - typically stops from one node sharing the same name</param>
		/// <param name="destinationStops">The list of stops considered as the destination stops - typically stops from one node sharing the same name</param>
		/// <param name="sourceBikeStations">The list of bikeStations considered as the source stations</param>
		/// <param name="destinationBikeStations">The list of bikeStations considered as the destination stations</param>
		/// <param name="departureTime">The earliest possible departure time of the found connection</param>
		/// <param name="settingsUsed">The settings used for the search</param>
		public ForwardSearchModel(List<Stop> sourceStops, List<Stop> destinationStops, List<BikeStation> sourceBikeStations, List<BikeStation> destinationBikeStations, DateTime departureTime, Settings settingsUsed)
			: base(sourceStops, destinationStops, sourceBikeStations, destinationBikeStations, settingsUsed)
		{
			this.departureTime = departureTime;
		}


		/// <summary>
		/// Extracts the result of the search from the current state of the search model and returns it
		/// </summary>
		/// <returns>The best result found in the search</returns>
		/// <exception cref="ApplicationException">Thrown if the extraction fails, meaning the search model was in an invalid state.</exception>
		public SearchResult? ExtractResult(BikeModel bikeModel)
		{
			// For each round, get the stop with the earliest arrival
			Stop?[] earliestDestStopsRounds = new Stop[Settings.ROUNDS];
			for (int round = 0; round < Settings.ROUNDS; round++)
			{
				earliestDestStopsRounds[round] = GetDestStopWithMinArrivalTimeInRound(round);
			}

			SearchResult?[] resultsRounds = new SearchResult[Settings.ROUNDS];
			for (int round = 0; round < Settings.ROUNDS; round++)
			{
				resultsRounds[round] = CreateResultFromStopInRound(earliestDestStopsRounds[round], round);
			}

			return GetBestResult(resultsRounds, earliestDestStopsRounds);


			SearchResult? GetBestResult(SearchResult?[] results, Stop?[] earliestDestStops)
			{
				int bestRound = -1;
				DateTime bestArrivalTime = DateTime.MaxValue;
				for (int round = 0; round < results.Length; round++)
				{
					if (earliestDestStops[round] is not null)
					{
						var usedSegmentTypes = results[round]!.UsedSegmentTypes;

						bool startsWithTransfer = usedSegmentTypes[0] == SearchResult.SegmentType.Transfer;
						bool endsWithTransfer = usedSegmentTypes[^1] == SearchResult.SegmentType.Transfer;

						int penaltySecondsPerTransfer = settingsUsed.GetTransferPenaltySeconds();

						int transferCount = round == 0 ? round : round - 1;
						if (startsWithTransfer)
						{
							transferCount++;
						}
						if (endsWithTransfer)
						{
							transferCount++;
						}

						//var stopInfo = routingInfo[earliestDestStops[round]!];

						DateTime adjustedArrivalTime = GetEarliestArrivalInRound(earliestDestStops[round]!, round).AddSeconds(transferCount * penaltySecondsPerTransfer);
						//DateTime adjustedArrivalTime = stopInfo.earliestArrivalRounds[round].AddSeconds(transferCount * penaltySecondsPerTransfer);
						//earliestArrivalRounds[round] = adjustedArrivalTime;

						if (adjustedArrivalTime < bestArrivalTime)
						{
							bestArrivalTime = adjustedArrivalTime;
							bestRound = round;
						}
					}
				}

				if (bestRound == -1)
				{
					return null;
				}
				return results[bestRound];
			}

			SearchResult? CreateResultFromStopInRound(Stop? stop, int round)
			{
				if (stop is null)
				{
					return null;
				}
				SearchResult result = new(settingsUsed);
				ForwardStopRoutingInfo currStopInfo = routingInfo[stop];


				if(destinationCustomRoutePoint is not null)
				{
					CustomTransfer transfer = destinationCustomRoutePoint.GetTransferWithNormalRP(stop);
					DateTime arrivalTimeAtDestCustomRP = currStopInfo.Arrivals[round].Time.AddSeconds(transfer.GetTransferTime(settingsUsed.WalkingPace));
					result.AddUsedTransfer(transfer, arrivalTimeAtDestCustomRP, false);
				}

				IRoutePoint nextRoundStartStop = stop;
				int currRound = round;
				while (currRound > 0)
				{
					//bool transferUsed = false;


					IRoutePoint currStop;

					var arrival = currStopInfo.Arrivals[currRound];
					if (arrival is StopRoutingInfoBase.TransferArrival transferArrival)
					{
						result.AddUsedTransfer(transferArrival.Transfer, transferArrival.Time, false);
						//transferUsed = true;
						currStop = transferArrival.Transfer.From;
					}
					else if (arrival is StopRoutingInfoBase.BikeTransferArrival bikeTransferArrival)
					{
						result.AddUsedTransfer(bikeTransferArrival.Transfer, bikeTransferArrival.Time, false);
						//transferUsed = true;
						currStop = bikeTransferArrival.Transfer.GetSrcRoutePoint();
					}
					

					// In current round, no transfer has been used, i.e. we are continuing from the exact same stop -> we add a new 0 length transfer
					else
					{
						currStop = nextRoundStartStop;
						if (currStop is Stop s)
						{
							if (currRound != round)
							{
								//Stop s = (Stop)currStop;
								// in last round, we do not add a transfer
								result.AddUsedTransfer(new Transfer(s, s, 0), currStopInfo.Arrivals[currRound].Time.AddSeconds(settingsUsed.GetStationaryTransferMinimumSeconds()), false);
							}
						}
					}

					currStopInfo = routingInfo[currStop];
					//Trip tripToReachStop = currStopInfo.tripsToReachRounds[currRound];
					//Stop getOnStop = currStopInfo.getOnStopsToReachRounds[currRound];
					arrival = currStopInfo.Arrivals[currRound];

					if (arrival is StopRoutingInfoBase.TripArrival tripArrival)
					{
						Trip tripToReachStop = tripArrival.Trip;
						Stop getOnStop = tripArrival.GetOnStop;
						if (tripToReachStop is null || getOnStop is null)
						{
							throw new ApplicationException("Trip and getOnStop cannot be null in an used round");
						}
						result.AddUsedTrip(tripToReachStop, getOnStop, (Stop)currStop, tripArrival.Time, false);
						currStop = getOnStop;
					}
					else if (arrival is StopRoutingInfoBase.BikeTripArrival bikeTripArrival)
					{
						//TODO: Check use of bike model - shouldnt it be somewhere else?
						result.AddUsedBikeTrip(bikeTripArrival.From, bikeTripArrival.To, bikeModel.GetDistanceBetweenStations(bikeTripArrival.From, bikeTripArrival.To), false);
						currStop = bikeTripArrival.From;
					}



					currStopInfo = routingInfo[currStop];
					nextRoundStartStop = currStop;
					currRound--;
				}

				//TODO: check
				// Add the first transfer to the result -> that would be in round 0 and thus not added in the loop above
				var firstArrival = currStopInfo.Arrivals[0];
				if (firstArrival is StopRoutingInfoBase.TransferArrival transferArrival1)
				{
					result.AddUsedTransfer(transferArrival1.Transfer, firstArrival.Time, false);
				}
				else if (firstArrival is StopRoutingInfoBase.BikeTransferArrival bikeTransferArrival)
				{
					result.AddUsedTransfer(bikeTransferArrival.Transfer, firstArrival.Time, false);
				}
				else if (firstArrival is StopRoutingInfoBase.CustomTransferArrival customTransferArrival)
				{
					result.AddUsedTransfer(customTransferArrival.Transfer, customTransferArrival.Time, false);
				}

				result.SetDepartureAndArrivalTimesByEarliestDeparture(departureTime);

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
						&& GetEarliestArrivalInRound(stop, round) < earliestArrival
						&& (round == 0 || ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(stop, round))
					)
					{
						stopWithMinArrTime = stop;
						earliestArrival = GetEarliestArrivalInRound(stop, round);
					}
				}
				return stopWithMinArrTime;
			}

			bool ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(Stop stop, int round)
			{
				DateTime bestEarlierArrival = DateTime.MaxValue;
				for (int i = 0; i < round; i++)
				{
					DateTime arrivalInRoundI = GetEarliestArrivalInRound(stop, i);
					if (arrivalInRoundI < bestEarlierArrival)
					{
						bestEarlierArrival = arrivalInRoundI;
					}
				}
				return bestEarlierArrival > GetEarliestArrivalInRound(stop, round);
			}
		}

		/// <summary>
		/// Gets the earliest possible departure time of the search
		/// </summary>
		/// <returns>The departure time</returns>
		public DateTime GetDepartureTime()
		{
			return departureTime;
		}
		/// <summary>
		/// Gets the current best/earliest arrival time at the destination
		/// </summary>
		/// <returns>The best current arrival time</returns>
		public DateTime GetCurrentBestArrivalTime()
		{
			return bestCurrentArrivalTime;
		}
		/// <summary>
		/// Gets the earliest currently possible arrival time to the specified RoutePoint
		/// </summary>
		/// <param name="rp">The RoutePoint to get the earliest arrival to</param>
		/// <returns>The earliest possible arrival time to the RoutePoint</returns>
		public DateTime GetEarliestArrival(IRoutePoint rp)
		{
			return GetRoutingInfo(rp).EarliestArrival;
		}


		/// <summary>
		/// Gets the earliest currently possible arrival time from the source RoutePoint to the specified RoutePoint in the specified round (i.e. with exactly so many trips)
		/// </summary>
		/// <param name="rp">The RoutePoint to get the earliest arrival to</param>
		/// <param name="round">The round to get the information in</param>
		/// <returns>The earliest possible arrival time to the RoutePoint in the specified round</returns>
		public DateTime GetEarliestArrivalInRound(IRoutePoint rp, int round)
		{
			var arrival = GetRoutingInfo(rp).Arrivals[round];
			if (arrival is null)
			{
				return DateTime.MaxValue;
			}
			else
			{
				return arrival.Time;
			}
		}

		/// <summary>
		/// Sets the current overall best arrival time to any of the destination stops
		/// </summary>
		/// <param name="arrivalTime">The best arrival time to set</param>
		public void SetCurrentBestArrivalTime(DateTime arrivalTime)
		{
			bestCurrentArrivalTime = arrivalTime;
		}
		/// <summary>
		/// Sets the current overall best arrival time to the specified stop
		/// </summary>
		/// <param name="rp">The stop to set the earliest arrival for</param>
		/// <param name="arrivalTime">The earliest arrival time to set</param>
		public void SetEarliestArrival(IRoutePoint rp, DateTime arrivalTime)
		{
			GetRoutingInfo(rp).EarliestArrival = arrivalTime;
			if (destinationStops.Contains(rp) && arrivalTime < bestCurrentArrivalTime)
			{
				bestCurrentArrivalTime = arrivalTime;
			}
		}

        /// <summary>
        /// Sets an arrival by trip to the specified stop in the specified round.
        /// </summary>
        /// <param name="stop">The stop to which the arrival is being set</param>
        /// <param name="trip">The trip to be taken to the stop</param>
        /// <param name="getOnStop">The stop at which the trip was boarded</param>
        /// <param name="arrivalTime">The time at which the trip arrives at the stop</param>
        /// <param name="round">The round in which to set the arrival</param>
        public void SetTripArrivalInRound(Stop stop, Trip trip, Stop getOnStop, DateTime arrivalTime, int round)
		{
			StopRoutingInfoBase.TripArrival tripArrival = new StopRoutingInfoBase.TripArrival(trip, getOnStop, arrivalTime);
			GetRoutingInfo(stop).Arrivals[round] = tripArrival;
		}

        /// <summary>
        /// Sets an arrival by transfer to the specified RoutePoint in the specified round
        /// </summary>
        /// <param name="rp">The RoutePoint to which the arrival is being set</param>
        /// <param name="transfer">The transfer to use</param>
        /// <param name="arrivalTime">The time at which the RoutePoint sis reached by the transfer</param>
        /// <param name="round">The round in which to set the arrival</param>
        /// <exception cref="NotImplementedException">Thrown if the arrival transfer is not between 2 stops, stop and bike station or custom route point and normal route point</exception>
        public void SetTransferArrivalInRound(IRoutePoint rp, ITransfer transfer, DateTime arrivalTime, int round)
		{
			if (transfer is Transfer t)
			{
				StopRoutingInfoBase.TransferArrival transferArrival = new StopRoutingInfoBase.TransferArrival(t, arrivalTime);
				GetRoutingInfo(rp).Arrivals[round] = transferArrival;
			}
			else if (transfer is BikeTransfer bt)
			{
				StopRoutingInfoBase.BikeTransferArrival bikeTransferArrival = new StopRoutingInfoBase.BikeTransferArrival(bt, arrivalTime);
				GetRoutingInfo(rp).Arrivals[round] = bikeTransferArrival;
			}
			else if(transfer is CustomTransfer ct)
			{
				StopRoutingInfoBase.CustomTransferArrival customTransferArrival = new StopRoutingInfoBase.CustomTransferArrival(ct, arrivalTime);
				GetRoutingInfo(rp).Arrivals[round] = customTransferArrival;
			}
			else
			{
				throw new NotImplementedException();
			}

		}


        /// <summary>
        /// Sets an arrival by bike trip (from the other station) to the specified station in the specified round.
        /// </summary>
        /// <param name="from">The source station</param>
        /// <param name="to">The station to which the arrival is being made</param>
        /// <param name="arrivalTime">The time at which the destination station is reached</param>
        /// <param name="round">The round in which to set the arrival</param>
        public void SetBikeTripArrivalInRound(BikeStation from, BikeStation to, DateTime arrivalTime, int round)
		{
			StopRoutingInfoBase.BikeTripArrival bikeTripArrival = new StopRoutingInfoBase.BikeTripArrival(from, to, arrivalTime);
			GetRoutingInfo(to).Arrivals[round] = bikeTripArrival;
		}

		/// <summary>
		/// Initiates the search by setting the earliest arrival times to all the source stops as the departure time
		/// </summary>
		public void SetSourceStopsEarliestDeparture()
		{
			foreach (Stop sourceStop in sourceStops)
			{
				ForwardStopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceStop);
				stopRoutingInfo.EarliestArrival = departureTime;
				stopRoutingInfo.Arrivals[0] = new StopRoutingInfoBase.ImplicitStartDeparture(departureTime);
			}
		}

		/// <summary>
		/// Initiates the search by setting the earliest arrival times to all the source bike stations as the departure time
		/// </summary>
		public void SetSourceBikeStationsEarliestDeparture()
		{
			foreach (BikeStation sourceBikeStation in sourceBikeStations)
			{
				ForwardStopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceBikeStation);
				stopRoutingInfo.EarliestArrival = departureTime;
				stopRoutingInfo.Arrivals[0] = new StopRoutingInfoBase.ImplicitStartDeparture(departureTime);
			}
		}

        /// <summary>
        /// Finds out whether the specified RoutePoint was reached by a transfer in the specified round
        /// </summary>
        /// <remarks>Typically used to ensure two transfers are not performed after one another</remarks>
        /// <param name="rp">The RoutePoint</param>
        /// <param name="round">The round</param>
        /// <returns>Bool, specifying whether the RoutePoint was reached by a transfer in the round</returns>
        public bool RoutePointIsReachedByTransferInRound(IRoutePoint rp, int round)
		{
			StopRoutingInfoBase.IEntry arrival = GetRoutingInfo(rp).Arrivals[round];
			return arrival is StopRoutingInfoBase.TransferArrival || arrival is StopRoutingInfoBase.BikeTransferArrival || arrival is StopRoutingInfoBase.CustomTransferArrival;
		}

        /// <summary>
        /// Finds out whether the specified RoutePoint was reached by a bike trip in the specified round
        /// </summary>
        /// <remarks>Typically used to ensure two bike trips are not performed after one another</remarks>
        /// <param name="rp">The RoutePoint</param>
        /// <param name="round">The round</param>
        /// <returns>Bool, specifying whether the RoutePoint was reached by a bike trip in the round</returns>
        public bool RoutePointIsReachedByBikeInRound(IRoutePoint rp, int round)
		{
			//var ri = GetRoutingInfo(rp);
			StopRoutingInfoBase.IEntry arrival = GetRoutingInfo(rp).Arrivals[round];
			return arrival is StopRoutingInfoBase.BikeTripArrival;
		}

        /// <summary>
        /// Finds out whether the specified RoutePoint was reached by a public transit trip in the specified round
        /// </summary>
        /// <param name="rp">The RoutePoint</param>
        /// <param name="round">The round</param>
        /// <returns>Bool, specifying whether the RoutePoint was reached by a public transit trip in the round</returns>
        public bool RoutePointIsReachedByTripInRound(IRoutePoint rp, int round)
		{
			return GetRoutingInfo(rp).Arrivals[round] is StopRoutingInfoBase.TripArrival;
		}
		/// <summary>
		/// Gets the routing info for the specified stop if it exists. If not, creates a new one, adds it to the routingInfo and returns it
		/// </summary>
		/// <param name="rp">The RoutePoint for which to get the routing info</param>
		/// <returns>The routing info of the specified RoutePoint</returns>
		private ForwardStopRoutingInfo GetRoutingInfo(IRoutePoint rp)
		{
			if (routingInfo.ContainsKey(rp))
			{
				return routingInfo[rp];
			}
			else
			{
				ForwardStopRoutingInfo stopRoutingInfo = new ForwardStopRoutingInfo();
				routingInfo.Add(rp, stopRoutingInfo);
				return stopRoutingInfo;
			}
		}
	}
}
