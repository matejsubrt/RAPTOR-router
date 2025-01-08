# RAPTOR Router API usage documentation

There are 3 distinct POST endpoints - a connection search endpoint, a trip alternatives endpoint, and a delay updating endpoint. Let's go over them and explain their purpose. Note that when running the server-side application in the WebAPI (and not WebAPI-light) configuration, the exact schemas of the requests and responses, as well as other information about the API, can be found at `<BASE-URL>/swagger/index.html`, so we will not be showing them here. More information can be found in the [developer documentation](dev_docs.md).

### Connection Search Endpoint

The connection search endpoint at the address `<BASE-URL>/connection` is the main endpoint for connection searching. It takes a connection request object containing all the search parameters, including the source and target locations, the specified time, and the settings for the search. It returns a list of the first relevant connections for the search parameters. This endpoint is also used for search expanding - in case the client already has some results and wants to request later ones, it can send the same request with the time set to the departure of the last existing result and the `byEarliestDeparture` parameter set to `true`. Similarly, when expanding to the past, the time is set to the arrival time of the earliest existing connection and `byEarliestDeparture` to `false`.

This endpoint may respond with the following codes:

- `200: OK` if the connection search was successfully performed.
- `404: Not Found` in case no connection was found for the parameters.
- `400: Bad Request` in case any of the provided parameters or setting values were invalid. In that case, the response will contain a message with the reason for the failure.

### Trip Alternatives Endpoint

This endpoint exists at the address `<BASE-URL>/alternative-trips` and serves the purpose of finding alternative direct trips. The client needs to provide the IDs of 2 stops between which direct trips run, the ID of the trip for which it wants to find the alternatives, a time, and a parameter specifying whether to find earlier or later alternatives. The API will then respond with a list of alternative direct trips. 

The possible response codes are:

- `200: OK` if alternatives were found correctly.
- `400: Bad Request` when some of the parameters were invalid.
- `404: Not Found` when the alternative trips could not be found.

### Delay Updating Endpoint

This endpoint at the address `<BASE-URL>/update-delays` has the same input and output parameters. It takes a list of existing results, goes through all trip alternatives of all trip segments of all the results, and updates all their delay data. Then it returns these newly updated results.

This endpoint only returns the code:

- `200: OK`, as it is always able to return at least the results in the request as they were in case no delay information could be found.