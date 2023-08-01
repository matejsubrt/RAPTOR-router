# Basic RAPTOR Router user documentation
The project consists of 3 separate parts (subprojects). One of them contains the implementation of the data loading and the algorithm itself - RAPTOR-Router. It has no UI and is to be used only as a library, which the other 2 projects use.

The CLIApp project uses the library interface to provide you with the possibility of using the command line to search for public transit connections. You enter the names of the stops between which you wish to search for a connection and the departure time, and it will show you a text represetation of the resulting connection.

The WebAPI project uses the library interface to run an API with one entry point - /connection with query parameters srcStopName, destStopName and departureTime, which returns a json representation of the resulting best connection.

## Using the program as a console application
To use the program as a console app is very easy. 

First, you will need to download a GTFS zip archive to be used for the connection search. (See [GTFS documentation](https://gtfs.org/schedule/reference/) for more information about the GTFS format). For Prague, you can use the [GTFS archive for Prague public transit](http://data.pid.cz/PID_GTFS.zip).

Second, you configure the path to the zip archive in the config.json file.

Then, you build and run the CLIApp program. It will start processing the GTFS data to build a timetable model to be used by the connection search algorithm. Depending on the performance of your computer, this can take between 20-40 seconds.

After the model has been successfully loaded, it will prompt you to first enter the source stop name and then the destination stop name. You have to enter these exactly as they appear in the GTFS files, i.e. use the correct case and interpunction. (For example, in Prague for the stop "Nádraží Holešovice" neither "Nádraží holešovice" nor "Nadrazi Holesovice" will work). If the input name does not exist in the gtfs archive, the program will prompt you to re-enter the name.

After you press enter, the program will prompt you to enter the departure time in the DD/MM/YYYY hh:mm:ss format, i.e. for 2.1.2023, 3:04:05, you should enter "02/01/2023 03:04:05".

The program will then run the RAPTOR connection-search algorithm itself and show you a text representation of the fastest connection possible between the two stops you entered, that leaves after the specified departure time. Immediately after, you will be able to search for another connection if you wish to.

To exit the program, press Ctrl+C.

## Using the program as a Web API

You can then build and run the program. It will prompt you to enter the path to the GTFS zip archive you want to use. After you provide this, the program will start processing the GTFS data to build a timetable model to be used by the connection search algorithm. Depending on the performance of your computer, this can take between 20-40 seconds.

After it has finished, it will display information about the web application that has been launched.

To use the running API, all you need is to go to the shown address (http://localhost:5000) and send a GET request to /connection with parameters srcStopName, destStopName, dateTime in the query. Once again, the names have to be exact (and encoded for web use) and the departureTime (dateTime) has to be in the format of YYYYMMDDhhmmss. For example http://localhost:5000/connection?srcStopName=Malostransk%C3%A9%20n%C3%A1m%C4%9Bst%C3%AD&destStopName=Kuchy%C5%88ka&dateTime=20230707070707 will return you a json representation of the best connection from Malostranské náměstí to Kuchyňka at 7.7.2023 at 7:07:07.

The resulting json is obtained by serializing the SearchResult object (see [Dev docs](https://matejsubrt.github.io/RAPTOR-router/html/index.html)). 

Once again, to end the application, press Ctrl+C.