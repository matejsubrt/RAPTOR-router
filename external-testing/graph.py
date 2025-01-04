## THIS FILE IS NOT CONSIDERED PART OF THE BACHELOR THESIS, IT ONLY HELPS CREATE GRAPHS FOR THE THESIS
## AI WAS USED TO GENERATE ITS CONTENTS


import json
import matplotlib.pyplot as plt
import numpy as np
import argparse


# Function to parse command-line arguments
def parse_args():
    parser = argparse.ArgumentParser(description='Process Postman JSON export and generate a graph.')
    parser.add_argument('-i', '--input-file', type=str, default='postman1.json', help='Path to the Postman JSON file')
    parser.add_argument('--compare', action='store_true', help='Enable comparison between weekend and normal day')
    parser.add_argument('-w', '--weekend', type=str, help='Path to the weekend data JSON file')
    parser.add_argument('-n', '--normal-day', type=str, help='Path to the normal day data JSON file')
    return parser.parse_args()


# Function to generate a normal graph from input file (when not in compare mode)
def generate_normal_graph(input_file):
    with open(input_file, 'r') as file:
        data = json.load(file)

    # Extract response times from the "results" field
    response_times = []
    for result in data['results']:
        response_times.extend(result['times'])

    # Filter out times lower than 5ms
    filtered_response_times = [time for time in response_times if time >= 5]

    # If there are no valid times, we can't plot
    if not filtered_response_times:
        print("No valid response times to display.")
        return

    # Define the bin width (10ms) and calculate the maximum time for the x-axis
    bin_width = 10
    max_time = max(filtered_response_times)

    # Create bins for the histogram (0 to max_time with a step of bin_width)
    bins = range(0, max_time + bin_width, bin_width)

    # Create histograms of the filtered response times
    hist, bin_edges = np.histogram(filtered_response_times, bins=bins)

    # Normalize the histogram to get the percentage
    hist_percent = (hist / hist.sum()) * 100
    smoothed_hist = np.convolve(hist_percent, np.ones(3) / 3, mode='same')

    # Plotting the smoothed histogram as an area plot
    plt.fill_between(bin_edges[:-1], smoothed_hist, color='skyblue', alpha=0.6, label="All Data")

    # Calculate median, 1st quartile, and 3rd quartile
    median = np.median(filtered_response_times)
    q1 = np.percentile(filtered_response_times, 25)
    q3 = np.percentile(filtered_response_times, 75)

    # Plot vertical lines for median, Q1, and Q3
    plt.axvline(median, color='red', linestyle='--', label=f'Median: {median} ms')
    plt.axvline(q1, color='green', linestyle='--', label=f'1st Quartile: {q1} ms')
    plt.axvline(q3, color='green', linestyle='--', label=f'3rd Quartile: {q3} ms')

    # Add labels and title to the plot
    plt.title("Random Searches Response Time Histogram")
    plt.xlabel("Time (ms)")
    plt.ylabel("Percentage of Searches (%)")

    # Adjusting the x-axis ticks to show more frequent values (every 100ms)
    plt.xticks(np.arange(0, max_time + bin_width, 100))  # 100ms intervals for axis labels

    # Add legend
    plt.legend()

    # Show the plot
    plt.show()


# Function to generate a graph comparing weekend and normal day data (for compare mode)
def generate_compare_graph(weekend_file, normal_day_file):
    weekend_times = []
    normal_day_times = []

    # Load weekend data
    if weekend_file:
        with open(weekend_file, 'r') as file:
            weekend_data = json.load(file)
        for result in weekend_data['results']:
            weekend_times.extend(result['times'])

    # Load normal day data
    if normal_day_file:
        with open(normal_day_file, 'r') as file:
            normal_day_data = json.load(file)
        for result in normal_day_data['results']:
            normal_day_times.extend(result['times'])

    # If there are no valid times, we can't plot
    if not weekend_times or not normal_day_times:
        print("No valid response times for either weekend or normal day to display.")
        return

    # Define the bin width (10ms) and calculate the maximum time for the x-axis
    bin_width = 10
    max_time = max(max(weekend_times), max(normal_day_times))

    # Create bins for the histogram (0 to max_time with a step of bin_width)
    bins = range(0, max_time + bin_width, bin_width)

    # Create histograms of the weekend and normal day response times
    hist_weekend, bin_edges = np.histogram(weekend_times, bins=bins)
    hist_normal_day, _ = np.histogram(normal_day_times, bins=bins)

    # Normalize the histograms to get the percentage
    hist_weekend_percent = (hist_weekend / hist_weekend.sum()) * 100
    hist_normal_day_percent = (hist_normal_day / hist_normal_day.sum()) * 100

    smoothed_weekend_hist = np.convolve(hist_weekend_percent, np.ones(3) / 3, mode='same')
    smoothed_normal_day_hist = np.convolve(hist_normal_day_percent, np.ones(3) / 3, mode='same')

    # Calculate average of both
    average_hist = (smoothed_weekend_hist + smoothed_normal_day_hist) / 2

    # Calculate the median and quartiles for the average data
    average_response_times = weekend_times + normal_day_times
    median_avg = np.median(average_response_times)
    q1_avg = np.percentile(average_response_times, 25)
    q3_avg = np.percentile(average_response_times, 75)

    # Plotting the smoothed histograms as lines for weekend and normal day
    plt.plot(bin_edges[:-1], smoothed_weekend_hist, color='red', label="Weekend", linewidth=2)
    plt.plot(bin_edges[:-1], smoothed_normal_day_hist, color='blue', label="Normal Day", linewidth=2)

    # Plotting the smoothed histogram for average response time (filled area)
    plt.fill_between(bin_edges[:-1], average_hist, color='purple', alpha=0.6, label="Average of Weekend and Normal Day")

    # Plot vertical lines for median, Q1, and Q3 of the average data
    plt.axvline(median_avg, color='orange', linestyle='--', label=f'Median (Average): {median_avg} ms')
    plt.axvline(q1_avg, color='green', linestyle='--', label=f'1st Quartile (Average): {q1_avg} ms')
    plt.axvline(q3_avg, color='green', linestyle='--', label=f'3rd Quartile (Average): {q3_avg} ms')

    # Add labels and title to the plot
    plt.title("Random Search Response Time Histogram")
    plt.xlabel("Time (ms)")
    plt.ylabel("Percentage of Searches (%)")

    # Adjusting the x-axis ticks to show more frequent values (every 100ms)
    plt.xticks(np.arange(0, max_time + bin_width, 50))  # 100ms intervals for axis labels

    # Add legend
    plt.legend()

    # Show the plot
    plt.show()


# Main execution
if __name__ == '__main__':
    args = parse_args()

    # Validate --compare flag
    if args.compare and (not args.weekend or not args.normal_day):
        print("Error: --compare requires both --weekend and --normal-day to be specified.")
    else:
        if args.compare:
            generate_compare_graph(args.weekend, args.normal_day)
        else:
            generate_normal_graph(args.input_file)


# import json
# import matplotlib.pyplot as plt
# import numpy as np
# import argparse

# # Function to parse command-line arguments
# def parse_args():
#     parser = argparse.ArgumentParser(description='Process Postman JSON export and generate a graph.')
#     parser.add_argument('-i', '--input-file', type=str, default='postman1.json', help='Path to the Postman JSON file')
#     return parser.parse_args()

# # Main function to generate the graph
# def generate_graph(input_file):
#     # Load the Postman JSON export
#     with open(input_file, 'r') as file:
#         data = json.load(file)

#     # Extract response times from the "results" field
#     response_times = []
#     for result in data['results']:
#         response_times.extend(result['times'])

#     # Count the occurrences of response times equal to 2ms
#     count_2ms = response_times.count(2)
#     print(f"Number of response times of 2ms: {count_2ms}")

#     # Filter out times lower than 5ms
#     filtered_response_times = [time for time in response_times if time >= 5]

#     # Define the bin width (10ms) and calculate the maximum time for the x-axis
#     bin_width = 10
#     max_time = max(filtered_response_times)

#     # Create bins for the histogram (0 to max_time with a step of bin_width)
#     bins = range(0, max_time + bin_width, bin_width)

#     # Create a histogram of the filtered response times
#     hist, bin_edges = np.histogram(filtered_response_times, bins=bins)

#     # Normalize the histogram to get the percentage
#     hist_percent = (hist / hist.sum()) * 100

#     # Apply moving average smoothing (with a window size of 3 for example)
#     window_size = 3
#     smoothed_hist = np.convolve(hist_percent, np.ones(window_size) / window_size, mode='same')

#     # Calculate median, 1st quartile, and 3rd quartile
#     median = np.median(filtered_response_times)
#     q1 = np.percentile(filtered_response_times, 25)
#     q3 = np.percentile(filtered_response_times, 75)

#     # Plotting the smoothed histogram as an area plot
#     plt.fill_between(bin_edges[:-1], smoothed_hist, color='skyblue', alpha=0.6)

#     # Plot vertical lines for median, Q1, and Q3
#     plt.axvline(median, color='red', linestyle='--', label=f'Median: {median} ms')
#     plt.axvline(q1, color='green', linestyle='--', label=f'1st Quartile: {q1} ms')
#     plt.axvline(q3, color='green', linestyle='--', label=f'3rd Quartile: {q3} ms')

#     # Add labels and title to the plot
#     plt.title("Random Searches Response Time Histogram")
#     plt.xlabel("Time (ms)")
#     plt.ylabel("Percentage of Searches (%)")

#     # Adjusting the x-axis ticks to show more frequent values (every 100ms)
#     plt.xticks(np.arange(0, max_time + bin_width, 100))  # 100ms intervals for axis labels

#     # Add legend
#     plt.legend()

#     # Show the plot
#     plt.show()

# # Main execution
# if __name__ == '__main__':
#     args = parse_args()
#     generate_graph(args.input_file)
