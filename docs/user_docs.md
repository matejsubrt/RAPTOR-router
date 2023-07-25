# Basic RAPTOR Router user documentation
The project has two options of running - as a console application or as  a web API. It is set by default to run as a console app. To change the functionality to a web API, uncomment the first line in the [Program.cs](..\RAPTOR-Router\RAPTOR-Router\Program.cs) file.

## Using the program as a console application
To use the program as a console app is very easy. 

First, you will need to download a GTFS zip archive to be used for the connection search. (See [GTFS documentation](https://gtfs.org/schedule/reference/) for more information about the GTFS format). For Prague, you can use the [GTFS archive for Prague public transit](http://data.pid.cz/PID_GTFS.zip).

Second, you build and run the program. It will prompt you to enter the path to the GTFS zip archive you want to use. After you provide this, the program will start processing the GTFS data to build a timetable model to be used by the connection search algorithm. Depending on the performance of your computer, this can take between 10-30 seconds.

After the model has been successfully loaded, it will prompt you to first enter the source stop name and then the destination stop name. You have to enter these exactly as they appear in the GTFS files, i.e. use the correct case and interpunction. (For example, in Prague for the stop "Nádraží Holešovice" neither "Nádraží holešovice" nor "Nadrazi Holesovice" will work). If the input name does not exist in the gtfs archive, the program will prompt you to re-enter the name.

After you press enter, the program will prompt you to enter the departure time in the YYYMMDDhhmmss format, i.e. for 2.1.2023, 3:04:05, you should enter 20230102030405.

The program will then run the RAPTOR connection-search algorithm itself and show you a text representation of the fastest connection possible between the two stops you entered, that leaves after the specified departure time. Immediately after, you will be able to search for another connection if you wish to.

To exit the program, press Ctrl+C.

## Using the program as a Web API
As stated at the beginning, you will need to uncomment the first line in the [Program.cs](..\RAPTOR-Router\RAPTOR-Router\Program.cs) file to use the API instead of the console version.

You can then build and run the program. It will prompt you to enter the path to the GTFS zip archive you want to use. After you provide this, the program will start processing the GTFS data to build a timetable model to be used by the connection search algorithm. Depending on the performance of your computer, this can take between 10-30 seconds.

After it has finished, it will display information about the web application that has been launched - by default, it will be running on port 5000.

To use the running API, all you need is to go to the shown address (http://localhost:5000) and send a GET request to /connection with parameters srcStopName, destStopName, dateTime in the query. Once again, the names have to be exact (and encoded for web use) and the departureTime (dateTime) has to be in the format of YYYYMMDDhhmmss. For example http://localhost:5000/connection?srcStopName=Malostransk%C3%A9%20n%C3%A1m%C4%9Bst%C3%AD&destStopName=Kuchy%C5%88ka&dateTime=20230707070707 will return you a json representation of the best connection from Malostranské náměstí to Kuchyňka at 7.7.2023 at 7:07:07.

The resulting json is obtained by serializing the SearchResult object (see [Dev docs](https://matejsubrt.github.io/RAPTOR-router/html/index.html)). 

Once again, to end the application, press Ctrl+C.