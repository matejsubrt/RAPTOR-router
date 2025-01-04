## THIS FILE IS NOT CONSIDERED PART OF THE BACHELOR THESIS, IT ONLY HELPS CREATE TEST DATA
## AI WAS USED TO GENERATE ITS CONTENTS

import pandas as pd
import random
from datetime import datetime, timedelta
import argparse

# Function to generate random datetime for tomorrow
def generate_random_datetime():
    tomorrow = datetime(2025, 1, 4)
    random_time = timedelta(
        hours=random.randint(0, 23),
        minutes=random.randint(0, 59),
        seconds=random.randint(0, 59)
    )
    random_datetime = tomorrow + random_time
    # Only return the time part after 'T'
    return random_datetime.strftime('%H:%M:%S')

# Main function to generate random stops
def generate_random_stops(input_file, output_file):
    print("Starting parsing")

    # Read the first 16844 rows of the CSV file
    df = pd.read_csv(input_file, nrows=16844)

    # Get all unique stop names
    unique_stop_names = df['stop_name'].unique()

    print("Starting generation")
    # Generate 1000 random rows
    random_data = []
    for _ in range(1000):
        stop_pair = random.sample(list(unique_stop_names), 2)
        random_datetime = generate_random_datetime()
        random_bool = random.choice([True, False])
        
        # Generating additional random values
        walking_pace = random.randint(8, 20)
        cycling_pace = random.randint(3, 15)
        bike_unlock_time = random.randint(0, 59)
        bike_lock_time = random.randint(0, 59)
        use_shared_bikes = random.choice([True, False])
        bike_max_15_min = random.choice([True, False])
        transfer_buffer = random.randint(0, 3)
        comfort_balance = random.randint(0, 3)
        transfer_length = random.randint(0, 2)
        bike_trip_buffer = random.randint(0, 3)
        
        random_data.append([
            stop_pair[0], stop_pair[1], random_datetime, random_bool,
            walking_pace, cycling_pace, bike_unlock_time, bike_lock_time,
            use_shared_bikes, bike_max_15_min, transfer_buffer, comfort_balance,
            transfer_length, bike_trip_buffer
        ])

    # Create a DataFrame with the generated data
    columns = [
        'srcStopName', 'destStopName', 'dateTime', 'forward',
        'walkingPace', 'cyclingPace', 'bikeUnlockTime', 'bikeLockTime',
        'useSharedBikes', 'bikeMax15Min', 'transferBuffer', 'comfortBalance',
        'transferLength', 'bikeTripBuffer'
    ]
    random_stops_df = pd.DataFrame(random_data, columns=columns)

    # Save to a new CSV file, ensuring no quotes in stop names
    random_stops_df['srcStopName'] = random_stops_df['srcStopName'].astype(str).str.strip('"')
    random_stops_df['destStopName'] = random_stops_df['destStopName'].astype(str).str.strip('"')

    random_stops_df.to_csv(output_file, index=False, quoting=2)  # quoting=2 ensures no quotes in output

    print(f"Generated file saved as {output_file}")

# Set up command-line argument parsing
def parse_args():
    parser = argparse.ArgumentParser(description='Generate random stops CSV.')
    parser.add_argument('-i', '--input_file', type=str, default='stops.txt', help='Path to the input CSV file')
    parser.add_argument('-o', '--output_file', type=str, default='generated_random_stops.csv', help='Path to the output CSV file')
    return parser.parse_args()

# Main execution
if __name__ == '__main__':
    args = parse_args()
    generate_random_stops(args.input_file, args.output_file)
