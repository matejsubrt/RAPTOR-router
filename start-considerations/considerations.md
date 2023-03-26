# Considerations for the transit planner app

## **Funcionality**
- Set transfer times to more/less aggressive
    - **3/5 value switch** - long/normal/short transfers
    - i.e. set the risk level I'm willing to take (for example when running late for a lecture, one might set the risk level to high, as he has nothing to lose. When one is for example travelling with their grandma or kids, he may set the risk to very low, to accomodate longer transfers)
- Set balance between shortest time and least transfers
    - **3 value switch** - shortest time/normal/least transfers
    - i.e. if I need to get somewhere very fast and don't mind about comfort, the app will not disprefer connections with more transfers, even if they don't save much time. If I'm travelling with a lot of luggage, I may want to set the comfort to high, so that I don't have to carry the bags many times during transfer.
- Set my own walking/cycling speed
    - **in km/h or min/km**
    - The speed the app will use to plan non-public transit connections. If I know, that my walking speed is faster/slower than the default one (for the average person), I can set it to my speed.
- Manually set transfer time at certain stations
    - **in seconds**
    - for example if I know that a certain transfer at a certain station takes me less time than the app usually plans, I may manually set it to less, so that the app finds me an earlier connection
    - can be set for long/normal/short
- Set walking reluctance walue - how much I do/don't mind walking, (if it saves time)
    - **2/3/5 value switch** - don't mind walking/less walking (i.e. like other apps)
- Find the connection from more than just the one nearest starting stop
    - i.e. if one stop is 100m from me, but only has 1 bus in 20 minutes, but there is a second stop 500m away with a bus in 8 minutes, the app should prefer the second one
    - depends on the walking reluctance value - if reluctance is set to high, only nearer stops will be considered
- Include current delay in route-planning
    - Some apps are able to show me current delay, but don't include it in route-searching. This means, that if the first bus in the route has 10 minutes delay, the app still plans a connecting bus with only 2 minutes transfer time
- Show the exact departure times in seconds, mainly in the metro
    - So that everyone can see what time thay have left until it departs
- Ideally include shared bikes (at least nextbikes) in the route-planning (probably mainly in the first and/or last leg of the trip)
    - **true/false switch** - this can be turned on and off, or the maximum time can be set to 15 minutes

- use as little mobile data as possible

-----------------
## **Available data**

- **Overview page**
    - https://pid.cz/o-systemu/opendata/
----------------
- Schedules of the Prague transit in **GTFS** format
    - static source: http://data.pid.cz/PID_GTFS.zip
    - or golemio API: https://api.golemio.cz/v2/pid/docs/openapi/#/%F0%9F%A7%BE%20GTFS
    - **Documentation**: https://pid.cz/o-systemu/opendata/#h-gtfs
    - **GTFS overview**: https://developers.google.com/transit/gtfs
- Real-time vehicle positions (i.e. delays) as **JSON API**
    - https://api.golemio.cz/v2/pid/docs/openapi/#/%F0%9F%9B%A4%20RealTime%20Vehicle%20Positions/get_vehiclepositions
- List of all the stops (including coordinates, names, lines, ...) as a **JSON** file
    - https://data.pid.cz/stops/json/stops.json
    - **Documentation**: https://pid.cz/o-systemu/opendata/#h-stops
- GeoData (routes, lines, stops, metro entrances coordinates) in multiple formats (**GeoJSON, ...**)
    - https://opendata.praha.eu/datasets/https%3A%2F%2Fapi.opendata.praha.eu%2Flod%2Fcatalog%2F7a430e0e-eb8d-4039-b35e-57fd3a55c9c9

- Nextbike shared bikes positions API
    - **JSON GBFS** for Prague: https://api.nextbike.net/maps/nextbike-live.json?city=661
    - **GBFS documentation**: https://gbfs.mobilitydata.org/specification/reference/

- All bikes API?
    - **JSON API** mojepraha: https://mojepraha.eu/apispecs#for-mobile-app-only-shared-bikes-get
    - **Documentation**: https://mojeprahaapi.docs.apiary.io/#introduction/versions

------------------
## **App mockups**

- Online on [Figma](https://www.figma.com/file/nEyf3GvAmGA7qbupyGQsxn/Untitled?node-id=0%3A1&t=KiPEdD8hvWAdyEQN-1)
- local [PDF](TravelPlanner-app-mockups.pdf)

-------------

## **Existing options comparison**

| Functionality\Apps | Jízdní řády | IDOS | Lítačka | Google maps
| --- | --- | --- | --- | --- |
| **Transfer aggressivity** | <r>NO</r> | <r>NO</r> | <g>YES</g> | <r>NO</r>
| **Time/Comfort balance** | <r>ONLY DIRECT</r> | <r>ONLY DIRECT</r> | <o>ONLY MAX TRANSFERS</o> | <y>PARTLY<y>
| **Custom walking/cycling speed** | <r>NO</r> | <r>NO</r> | <r>NO</r> | <r>NO</r>
| **Manual transfer times** | <r>NO</r> | <o>ONLY GLOBAL</o> | <r>NO</r> | <r>NO</r>
| **Walking reluctance settings** | <r>NO</r> | <r>NO</r> | <r>NO</r> | <g>ALMOST</g>
| **Shared bikes support** | <r>NO</r> | <r>NO</r> | <y>POSSIBLY in future</y> | <r>NO</r>
| **More start/end stops** | <g>YES</g> | <r>NO</r> | <r>NO</r> | <g>YES</g>
| **Delay included in planning** | <r>NO</r> | <y>IDK</y> | <r>NO</r> | <y>PROBABLY</y>
| **Exact metro departure times** | <r>NO</r> | <r>NO</r> | <r>NO</r> | <r>NO</r>
| **Low data usage** | <g>YES</g> | <g>YES</g> | <g>YES</g> | <r>NO</r>


<style>
r { color: Red }
y { color: Yellow }
g { color: LightGreen }
o { color: Orange }
</style>
