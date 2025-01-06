# RAPTOR Router

## Overview

- Application for routing in Prague's public transit network
- Finds the fastest possible connection between two stops in Prague using public transit and Nextbike shared bikes.
    - Easily extendable to support additional shared bike providers in the future.
- Offers advanced user configuration options for more personalized transit planning, including setting own walking pace, safety time buffers and more
- Designed to function as a backend server-side application with a REST API.
    - The Android app [PragO](https://github.com/matejsubrt/PragO) is being developed as the primary frontend interface for the user accessing the search algorithm.
- Implemented completely in C\# language

------------

## Documentation

### User documentation

For information on how to use the API, please refer to section 3.3 of the [thesis](https://github.com/matejsubrt/bachelor-thesis) related to this project.

### Developer documentation

For generated developer documentation, see [Dev docs](https://matejsubrt.github.io/RAPTOR-router/html/index.html). For installation instructions and an overview of the codebase, please refer to section 4.1 of the [thesis](https://github.com/matejsubrt/bachelor-thesis) related to this project.

### RAPTOR Algorithm documentation

For information about the RAPTOR algorithm that the application uses and extends, see [RAPTOR docs](https://www.microsoft.com/en-us/research/wp-content/uploads/2012/01/raptor_alenex.pdf).



### Deprecated - MFF Individual project documentation

- For Individual project developer documentation, see [Individual Project Dev Docs](docs/individual_project/developer.md)
- For Individual project user documentation, see [Individual Project User Docs](docs/individual_project/user.md)
- (Note that the information in this documentation is now partly deprecated, as the application is still being developed even after the work on the Individual project part has been finished)