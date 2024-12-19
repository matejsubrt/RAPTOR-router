#!/bin/bash

# Default OSM path
OSM_PATH="data/osm_routing"

# Flags
LAUNCH=false
NOHUP=false

# Parse arguments
while [[ "$#" -gt 0 ]]; do
    case $1 in
        --osmPath) OSM_PATH="$2"; shift ;;   # Set a custom OSM path
        -l|--launch) LAUNCH=true ;;          # Enable launching the Web-API
        -n|--nohup) NOHUP=true ;;            # Launch with nohup (requires --launch)
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
    shift
done

# Ensure --nohup is only used with --launch
if [ "$NOHUP" = true ] && [ "$LAUNCH" = false ]; then
    echo "Error: -n/--nohup can only be used with -l/--launch."
    exit 1
fi

# Ensure the target directory exists
mkdir -p "$OSM_PATH"

# Define the target file
TARGET_FILE="$OSM_PATH/czech-republic-latest.osm.pbf"

# Check if the file already exists
if [ -f "$TARGET_FILE" ]; then
    echo "File already exists: $TARGET_FILE. Skipping download."
else
    echo "Downloading OSM map file into $OSM_PATH..."
    wget -P "$OSM_PATH" https://download.geofabrik.de/europe/czech-republic-latest.osm.pbf
    echo "Download finished."
fi

# Check if launch flag is set
if [ "$LAUNCH" = true ]; then
    cd WebAPI || { echo "WebAPI directory not found!"; exit 1; }

    if [ "$NOHUP" = true ]; then
        echo "Launching Web-API as a nohup background service..."
        nohup dotnet run -c Release > webapi.log 2>&1 &
        echo "Web-API is running in the background. Logs are being written to webapi.log."
    else
        echo "Launching WebAPI as a standard application... (Use -n/--nohup to launch it as a background service)"
        dotnet run -c Release
    fi
else
    echo "Launch flag not provided. Skipping Web-API launch. (Use -l/--launch to launch the application after initialization)"
    echo "(Alternatively, you can launch the app by running \"dotnet run -c Release\" in the WebAPI or WebAPI-light directory)"
fi
