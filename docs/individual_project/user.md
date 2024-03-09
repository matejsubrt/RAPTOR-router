# RAPTOR Router Individual project documentation
## User documentation

### Introduction
This document is intended to provide a user guide for the RAPTOR Router project, partirularly the Graphic User Interface. It will provide a brief overview of the project, and then provide a step-by-step guide to using the project.

### Overview
The RAPTOR-Router is an application used for searching for the best public transport connections between two stops. It is based on the RAPTOR algorithm, which is a public transport routing algorithm. The project provides three different ways of using the application: a command line interface, a graphical user interface, and a web interface. This document will focus on the graphical user interface, as that is the most user-friendly way of using the application.

### Using the application
After starting the application, the user will be presented with a window that prompts him to enter the details of the search. The window contains the following fields:
- **From**: the name of the stop where the user wants to start the search. This name has to be entered exactly as it is in the database, including the diacritics. As of now, there is no autocomplete functionality implemented, but this is planned for the future.
- **To**: the name of the stop where the user wants to end the search. This name has to be entered exactly as it is in the database, including the diacritics. As of now, there is no autocomplete functionality implemented, but this is planned for the future.
- **Date and Time**: the date and time when the user wants to start the search. Currently, there is no way to specify the latest arrival time for a connection, meaning the only option right now is to specify the earliest departure time. Again, this is planned for the future.
- The settings for the search:
	- **Walking Pace**: The walking pace of the user in minutes per kilometre. This is used to calculate the time needed to walk between stops. Note that the distance between stops is only calculated as the straight-line distance between the two stops, meaning this value should be roughly 20% higher than the actual normal walking pace of the user.
	- **Transfer Time slider**: This sets the minimum time the user will have to transfer between two stops. 
		- If set to the UltraShort position, any transfer determined by simply calculating the time using the walking pace can be used. Sometimes the transfer time can be very short. If using the UltraShort option, if a transfer happens at the exact same stop (meaning 0m transfer distance), the transfer time can be 0. This is very risky, as the user may not be able to make the transfer in time.
		- If set to to Short, a transfer with 0m transfer distance will have a minimum transfer time of 30 seconds. This means, that assuming all connections are on time, the user will always have enough time to make the transfer. For transfers with longer distance, this option works in the same way as the UltraShort one, i.e. the transfer time multiplier is set to 1.0.
		- If set to Normal, the minimum transfer time is again 30s for 0m transfers. For longer transfers, the time calculated using the distance and walking pace is further multiplicated by 1.25 to get the minimum transfer time. This is the recommended option for most users, as it takes into account a little extra time making the transfer less risky.
		- If set to Long, the multiplier is set to 1.5. Otherwise behaves the same as the Normal option.
	- **Comfort Balance slider**: Determines the overall comfort priority of the search. Shortest time means the result will be the fastest connection, regardless of the number of transfers. Least transfers option will return the connection with the least number of transfers, that is reasonable. The two values in between try to find a baance between these two extremes. The Balanced (default) option is recommended for most users.
	- **Walking preference slider**: Determins the maximum distance the user is willing to walk during a transfer. Following settings are possible:
		- Long: 750m
		- Medium: 500m
		- Short: 250m
		- Please note, that even when using the Short option, the result can still contain transfers with longer distance, assuming they are performed inside a single station node, i.e. if a transfer from a bus to a tram inside the same node (i.e. stations with the same name) happens to be longer than the limit, the transfer can still be used.

- After clicking Search, the result of the search will be displayed in a simply understandable graphical form, assuming a result was found. If the connection was not possible for whatever reason, the user will be returned to the Search screen to try to find a possible connection.