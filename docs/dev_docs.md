# RAPTOR Router Developer Documentation

The back-end side of our application is implemented in the C# programming language, and thus it needs the .NET environment to run. In this section, we will first go over the application's directory structure and the installation process. Then we will go through the server's implementation and the code structure.

## Directory structure

At the top level, there are two main directories:
- The `src` directory contains all the code.
- The `docs` directory contains the documentation. Here, you can find information on all the classes and interfaces within the project.

Within the `src` directory, you will find the following items:

- **`Tests`** is a directory containing all the code tests. More details can be found in [Unit Tests](#unit_tests).
- The **`RAPTOR-Router`** directory contains the main part of the application. Here, you will find all of the routing functionality.
- The **`WebAPI`** and **`WebAPI-light`** directories contain the implementation of the API. They use the `RAPTOR-Router` project to find the resulting connections and return these results. Their code is essentially identical and they provide the exact same functionality and API endpoints. The difference is that the light version uses a more stripped-out version of the .NET API to consume less system memory. We implemented this version to be able to run the application in strongly memory-constrained environments. In case your system has larger amounts of memory available, we recommend using the standard version, as it also provides automatically generated Swagger API documentation.
- The **`data`** directory contains all the necessary data that the application uses to run and provide routing.
- The **`config.json`** is a file that the program uses to find locations of necessary files and URLs of used APIs. It contains default locations for all files, however, you may change these if necessary.
- The **`init.sh`** is an initialization script for the application. More information on the usage of this file is presented in the following section.

## Installation and launch

To install and successfully run the server application, you will need to perform the following steps:

1. First, you will need to ensure that the .NET runtime is installed on your machine. Our application requires .NET 7.0 or newer. For more information on this step, see Microsoft's official .NET installation guide ([link](https://learn.microsoft.com/en-us/dotnet/core/install/)).

2. After ensuring the .NET Runtime is installed:
   - If running the application on Linux, you can use the provided `src/init.sh` initialization script. It ensures everything is prepared for the launch. As most of the data is provided as part of the application's repository, this mainly includes downloading the OSM map file, which is too large to be included with the application. You can provide the following options to the script:
     - `--osmPath <PATH>` specifies the directory into which to download the OSM file. By default, this is set to `src/data/osm_routing`. Please note that when changing the location, it will be necessary to change the path in the `src/config.json` file accordingly.
     - `-l/--launch` ensures that the script immediately launches the standard `WebAPI` application after preparing the necessary data.
     - `-n/--nohup` ensures that if `-l/--launch` is set, the application is launched as a service running in the background. Otherwise, the application is launched normally within the console.
     
     If you wish to only launch the application later, you can choose the version to run (i.e. WebAPI or WebAPI-light) and run the `dotnet run -c Release` command in the corresponding directory.
   
   - Else, you will need to perform the steps manually. This includes:
     - Downloading the Open Street Maps map file, which will be used for bike trip routing. This `.osm.pbf` format file can be downloaded from the OSM website ([link](https://download.geofabrik.de/europe/czech-republic.html)). We recommend putting it into the `src/data/osm_routing` directory, which is the default location where the program will look for the file.
     - If it was downloaded into a different location, it will be necessary to adjust the `src/config.json` file accordingly.
     - The application can be launched from either the `WebAPI` or `WebAPI-light` directory by running the command `dotnet run -c Release`. Additionally, the `WebAPI` version can be launched in an https configuration using the command `dotnet run -c Release --launch-profile https`. When using this profile, Swagger API documentation will be available at `<URL>/swagger/index.html`.

## API implementation

As mentioned above, the `WebAPI` and `WebAPI-light` projects are essentially the same, except the light version is intended to run in memory-constrained environments and does not provide Swagger API documentation. The implementation of the API is very simple. In C#, there are 2 main options as to how to implement an API - a controller-based and a minimal API approach. As our API is very small and simple with only 3 simple POST endpoints, we decided to use the minimal API approach to minimize unnecessary code complexity.

As explained earlier, there are 3 endpoints (`/connection`, `/alternative-trips`, and `/update-delays`). Each of these takes its parameters and calls its `Handle` function using them. The `Handle` functions only perform basic data checks before calling the routing methods from the `RAPTOR-Router` part of the project, which we describe in detail in the next section. After these functions return their results, the `Handle` functions check these results for errors and issue an HTTP response accordingly.

## RAPTOR-Router implementation

This is the main and largest part of the application. It contains all of the routing functionality that the API uses to handle requests. In particular, the routing is done by objects called `RouteFinders`. These objects can be created by using the `RouteFinderBuilder`.

### `RouteFinderBuilder`

This is the central object of the application. It is a static class that serves the purpose of first loading and parsing all of the necessary data and then providing this data to the created `RouteFinder` objects that handle the routing.

To be able to use the routing functionality, first, the `RouteFinderBuilder`'s `LoadAllData()` method needs to be called. This is where all of the necessary loading and parsing is performed. In particular, this includes the following:

- First, through the `Config` static class, it ensures that all of the necessary configuration values in the `src/config.json` file have been provided.
- Second, it loads the data about forbidden crossing lines
- Then it performs the most complicated step of the parsing process - loading and parsing the raw GTFS files into useful objects that will be used during the searches. This includes first directly parsing the files into corresponding GTFS objects in our application (these are defined in the `GTFSParsing` directory). Second, as the GTFS files are essentially a "database" with entries in the different files being linked through their ids, we will need to combine this raw data into useful objects. In particular, as the `routes.txt` file only contains one entry per real-world line and not per every variation of it, we need to construct the `Route` objects ourselves. This means going through all trips of each line, finding all the different stop variations and creating the useful objects with this information - we create `Routes`, `Trips`, and `Stops`. Every trip then has a `Route` with its set of stops, and a list of its `StopTimes`. Furthermore, all the `Transfers` are also calculated based on the stops' distances from each other. All these objects are defined in the `Structures/Transit` directory. The resulting object holding all of this static information about the public transit network is called a `TransitModel`.
- The next step is to parse and load the GBFS data describing the bikesharing network. The source files containing this functionality are located in the `GBFSParsing` directory. This step consists of calling the GBFS API, loading the bike station information from it and loading the distances between the bike stations from our local database file (`src/data/bike_distances/bike_distances.db` by default). Sometimes, the bike-sharing provider may have added new stations, for which we do not yet have the distance information precalculated. If such a situation occurs, we use the `BikeDistanceCalculator` class in `GBFSParsing/Distances` to calculate these new distances and add them to the database. The opposite situation might also happen, where the provider removes some of the existing bike stations. In that case, this step ensures that the corresponding entries are deleted from the database. All the loaded information is contained in the `BikeModel` object.
- Lastly, it is necessary to connect these two resulting objects to be able to include bikes in public transit searches. Thus, we add a step that adds transfers between all pairs of stops and bike stations that are close enough to each other.

Apart from the initial loading of the data and the creation of new `RouteFinder` objects, the last responsibility of the `RouteFinderBuilder` is to periodically update the data to ensure it stays up to date. This includes creating and updating the `DelayModel` holding all public transit delay information every 20 seconds, updating the `BikeModel`'s data every 60 seconds, and updating the `TransitModel` daily using the newly released PID GTFS data.


### `RouteFinders`

`RouteFinders` are the actual objects used to perform the searches. Our application contains two standard `RouteFinders` used to perform standard connection searches (`BasicRouteFinder` and `RangeRouteFinder`), one specialized `AlternativesRouteFinder` that is used for finding alternatives to individual trip segments of a connection, and a `DelayUpdater` used to update the delays of existing connections. First, let's go over the two standard `RouteFinders`:

- **`BasicRouteFinder`** contains the actual implementation of the RAPTOR algorithm, modified to support shared bikes inclusion It implements two interfaces: `ISimpleRouteFinder` and `ISimpleRoutingProvider`.
    - `ISimpleRouteFinder` is an interface that any class directly providing the connection searching functionality has to implement. It contains a single `FindConnection` method that serves this purpose. In our case, we only implemented a single `BasicRouteFinder` which uses the RAPTOR algorithm, but thanks to this interface, it would be possible to add other modified implementations as well.
    - `ISimpleRoutingProvider` specifies a slightly different thing - instead of directly taking requests for connections and returning complete responses, this is an interface which the `RangeRouteFinder` uses to get its partial results. As we'll see in a moment, the `RangeRouteFinder` uses the rRAPTOR algorithm, which essentially performs simple single searches across a longer time range and combines them together. This interface is implemented by classes that can provide this functionality to a `RangeRouteFinder`. In our case, the `BasicRouteFinder` implements both the interfaces.

    The `BasicRouteFinder` holds its own `SearchModel` object, which it uses to hold and modify the search-specific data like the arrays of best reach times and best reaching segments at all reached stops.

- **`RangeRouteFinder`** performs searches across a time range instead of just for a single time. This is the `RouteFinder` that the client will actually use to find multiple consecutive alternative connections through the `/connection` API endpoint. It implements the `IRangeRouteFinder` interface, which serves the same purpose for `RangeRouteFinders` as the `ISimpleRouteFinder` does for simple `RouteFinders`. In our case, it implements the rRAPTOR algorithm.

    Essentially, what the `RangeRouteFinder` does is that it finds the first N times after (or before, depending on the search direction) the specified search begin time, at which any `Trip` leaves any of the `Stops` reachable from the source. Then, for all these times, it runs separate standard connection searches using a `ISimpleRoutingProvider` in parallel, and combines their results into a list of results for the range.

    To better illustrate this, let's imagine the following scenario: let's say that the search asks for the best connections from stop A to stop B leaving after 8:00. From stop A, it is possible to make a transfer to stop C, which takes 5 minutes of walking. At stop A, first trips depart at 8:00, 8:10, and 8:20. At stop C, first trips depart at 8:00, 8:10, and 8:20 as well. If we set the N parameter to 5, then the first 5 possible departure times from A are 8:00, 8:05, 8:10, 8:15, and 8:20 (it is possible to depart from A directly at 8:00, 8:10 or 8:20, or to start the 5 minute long transfer to a trip at B, for which we would have to leave at either 7:55, 8:05 or 8:15; 7:55 is before the search begin time and so we are left with the times described). Then, it runs a separate simple search for all of these new search begin times.

    When combining their results, it may happen that some of the results get dominated by other results. This means that result A's departure time is earlier than result B's, but their arrival is the same. Then, it does not make sense to use the connection of result A and it is thus discarded. For more information on dominating trips, see [RAPTOR algorithm documentation](https://www.microsoft.com/en-us/research/wp-content/uploads/2012/01/raptor_alenex.pdf).

    After the results are cleaned up, the `RangeRouteFinder` returns the list of non-dominated connections as its results. If the client wants to expand the range, they can use the approach described in [user documentation](user_docs.md).

The other objects within the `RouteFinders` directory are the `AlternativesRouteFinder` and `DelayUpdater`. Their responsibilities correspond to the functionalities required for the `/alternative-trips` and `/update-delays` API endpoints. As both of their implementations are relatively straightforward, we'll leave out the details in this document. As with all other objects described here, detailed information on their functioning can be found in the documentation comments within the source files, in the generated documentation in the `docs` directory of the project or in the [online documentation](https://matejsubrt.github.io/RAPTOR-router/html/annotated.html).

### `SearchModel`

As described above, `SearchModel` is the object used by the `BasicRouteFinder` to store, modify and access the intermediate results of the search. For this purpose, it contains a dictionary holding for every currently reached `RoutePoint` the information on how it was best reached in each round. For this, it uses the `StopRoutingInfo` class.

After the search has finished, the `SearchModel` class is also used by the `RouteFinder` to create the resulting `SearchResult` object based on the final information stored in the `SearchModel`. For this purpose, the `RouteFinder` can call `SearchModel`'s `ExtractResultWithAlternatives` or `ExtractResult` methods. As the names suggest, the second one only returns the absolute best one, while the first one returns a list containing also slightly worse (longer) connections, that use fewer trips than the best one.

### `SearchResult`

The `SearchResult` is the object representing a single found connection. This object is serialized and returned as part of the response of the API. It is designed to include all the data necessary to display all required information to the user, while leaving out other irrelevant data. In particular, it contains the lists of used trips, transfers, and bike trips (`UsedTrips`, `UsedTransfers`, and `UsedBikeTrips` lists). The order in which the separate segment types are to be performed is specified in the `UsedSegmentTypes` list, where 0 represents a transfer, 1 a trip, and 2 a bike trip.

Furthermore, to make the client's implementation easier, it contains the `UsedTripAlternatives` list. A `TripAlternatives` object consists of a list of trips and the currently selected index. After the search, this is simply initialized to a list with a single item - the used trip present in `UsedTrips`. However, when the user requests earlier or later alternatives, this list is expanded to store them. Also, when updating the delays, this list is used to update not just the delays of the one displayed trip, but the other already fetched alternatives as well. Essentially, the `UsedTrips` list is used only to make it easier to debug the application and to make it easier to implement clients that do not need the trip alternatives functionality. The `UsedTripAlternatives` is used to support this feature.

Lastly, the result contains other pre-calculated information about the connection, such as the number of seconds spent on transfers and/or bike trips before boarding the first and after disembarking the last transit trips. While this information can be extracted from the other data fields, it is included on top of them to keep the work that the client needs to perform at a minimum.

### Other relevant classes

Other relevant parts of the code-base are the `Config` static class used to retrieve the configuration values from the `src/config.json` file, the `Request` classes in the `Structures/Requests` directory, which define the required schema for the API requests, and the `Response` objects in the `Models/Results/ApiResponseResults.cs` file, which are used by the `RouteFinders` to return their results together with an error code specifying what (if any) issues occurred during the calculation, so that this information can be sent further to the client. Specific information on all these classes and all other classes we couldn't fit into this document can be found in the `/data` directory and in the [online documentation](https://matejsubrt.github.io/RAPTOR-router/html/annotated.html).

### Other implementation remarks

One of the main problems faced during implementation was the requirement for the application to work in both directions, i.e. both by giving it the earliest allowed departure time from the source and the latest allowed arrival time at the destination. This essentially requires us to be able to run most of the algorithms both in the forward and backward directions. While we used separate implementations at first with the intention of keeping the code as readable as possible, it quickly became obvious that the benefits of this are far outweighed by the drawbacks, mainly in the form of very limited and complicated code maintenance and extendability due to having to implement everything twice.

Thus, instead of this approach, we settled on having only one implementation for all the classes and having them be parametrized by the direction in which we need them to run the algorithm. Specifically, this mostly affected the `BasicRouterFinder`, `RangeRouteFinder`, and `SearchModel` classes. As terms such as a "time improvement" or one index "preceding" another one mean different things while running the algorithm in opposite directions (specifically, if running the search forward, a reach time at a certain stop is better than another one if it is earlier, while when running the search backwards, the arrival time at the destination is fixed and we are trying to maximize the departure time, and thus a reach time is better if it is later than the other one). To limit the number of control sequences solving this problem throughout the code-base, we implemented simple `Comparators` for time and indices, which are parametrized by the search direction. These contain a single comparing method that works according to the direction. These can be found in the `Extensions/Comparators.cs` file.


## Used libraries

As our application has a rather large scope, to keep the development manageable, we have used third-party libraries for certain tasks that would either require a lot of uninteresting boilerplate code, or the implementation of which would exceed the scope of this project. In particular, we have used the following:

### Itinero

As was mentioned earlier, this library was used to provide the routing of the shared bikes in between their bike stations. Upon the application's first launch, it uses the provided OSM map file and parses it into a proprietary `.routerdb` file, which can then be loaded very easily during later launches. This file contains the graph parsed from the OSM file, which the library then uses to perform the routing. More information can be found on the project's website [Itinero](https://www.itinero.tech/).

### GtfsRealtimeBindings and protobuf-net

These libraries were used to simplify parsing the real-time GTFS data, which is provided in the protocol buffer format. More information can be found on the websites [GtfsRealtimeBindings](https://github.com/MobilityData/gtfs-realtime-bindings/) and [protobuf-net](https://github.com/protobuf-net/protobuf-net).

### Quartz

This library was used to schedule the periodic data updates necessary for the application to run. Unfortunately, C# does not support this functionality well within the standard library and thus we had to use this one. More information can be found on its website [Quartz Scheduler](https://www.quartz-scheduler.net/).

### Microsoft.Data.Sqlite

This official library by Microsoft was used for storing the database of distances between pairs of different bike stations. Initially, we have used a simple `.csv` file for this purpose, but an Sqlite database proved to be the better solution, particularly thanks to the much easier removing of deprecated data. More information can be found in Microsoft's official documentation [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/).

### CsvHelper

This library was used to simplify the parsing of the GTFS `.csv` files. More information on this library can be found on the project's website [CsvHelper](https://joshclose.github.io/CsvHelper/).
