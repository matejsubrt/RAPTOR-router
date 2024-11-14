using RAPTOR_Router.Structures.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Models.Results
{
    /// <summary>
    /// A class representing the result of an alternative trips search
    /// </summary>
    public class AlternativeTripsApiResponseResult
    {
        /// <summary>
        /// The resulting alternative trips
        /// </summary>
        /// <remarks>Shall not be used if Error is not NoError</remarks>
        public List<SearchResult.UsedTrip> Alternatives { get; set; } = new();
        /// <summary>
        /// The error that occurred during the search
        /// </summary>
        public AlternativesSearchError Error { get; set; }
        /// <summary>
        /// Creates a new instance of the class
        /// </summary>
        public AlternativeTripsApiResponseResult() { }
        /// <summary>
        /// Creates a new instance of the class
        /// </summary>
        /// <param name="alternatives">The resulting alternatives</param>
        /// <param name="error">The error that occured during the search</param>
        public AlternativeTripsApiResponseResult(List<SearchResult.UsedTrip> alternatives, AlternativesSearchError error)
        {
            Alternatives = alternatives;
            Error = error;
        }
    }

    /// <summary>
    /// A class representing the result of a connection search
    /// </summary>
    public class ConnectionApiResponseResult
    {
        /// <summary>
        /// The resulting search results
        /// </summary>
        public List<SearchResult>? Results { get; set; }
        /// <summary>
        /// The error that occurred during the search
        /// </summary>
        public ConnectionSearchError Error { get; set; }

        /// <summary>
        /// Creates a new instance of the class
        /// </summary>
        public ConnectionApiResponseResult()
        {
        }

        /// <summary>
        /// Creates a new instance of the class
        /// </summary>
        /// <param name="results">The search results</param>
        /// <param name="error">The error that occured during the search</param>
        public ConnectionApiResponseResult(List<SearchResult> results, ConnectionSearchError error)
        {
            this.Results = results;
            this.Error = error;
        }
    }
}
