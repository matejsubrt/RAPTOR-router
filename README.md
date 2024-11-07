# A Prague public transport app
- Finds the fastest possible connection between two stops in Prague using public transit and Nextbike shared bikes.
    - Easily extendable to support additional shared bike providers in the future.
- Offers advanced user configuration options for more personalized transit planning.
- Designed to function as a backend server-side application with a REST API.
    - The Android app [PragO](https://github.com/matejsubrt/PragO) is being developed as the primary frontend interface for the user accessing the search algorithm.
    - For development purposes, the app also includes basic CLI and GUI interfaces for connection searching.

------------

Please note that this is a work in progress and most parts of the application are still being developed. Currently, the RAPTOR-Router, WebAPI-light and CLIApp modules are up to date. The rest of the project will get updated when the core base functionality is finalized.

------------

## Documentation

### Developer documentation

For generated developer documentation, see [Dev docs](https://matejsubrt.github.io/RAPTOR-router/html/index.html). This documentation is related to an older version of the project and will be updated soon.

### RAPTOR Algorithm documentation

For information about the RAPTOR algorithm that is being used by the application, see [RAPTOR docs](https://www.microsoft.com/en-us/research/wp-content/uploads/2012/01/raptor_alenex.pdf)


### MFF Individual project documentation

- For Individual project developer documentation, see [Individual Project Dev Docs](docs/individual_project/developer.md)
- For Individual project user documentation, see [Individual Project User Docs](docs/individual_project/user.md)
- (Note that the information in this documentation is now partly deprecated, as the application is still being developed even after the Individual project part of it has been finished)