## THIS FILE IS NOT CONSIDERED PART OF THE BACHELOR THESIS, IT ONLY HELPS CREATE TEST DATA
## AI WAS USED TO GENERATE ITS CONTENTS

import json
import argparse
import matplotlib.pyplot as plt

# Function to parse command-line arguments
def parse_args():
    parser = argparse.ArgumentParser(description='Process Postman JSON data and generate pie charts.')
    parser.add_argument('-w', '--weekend', type=str, required=True, help='Path to the weekend data JSON file')
    parser.add_argument('-n', '--normal-day', type=str, required=True, help='Path to the normal day data JSON file')
    return parser.parse_args()

# Function to extract pass/fail counts from the testPassFailCounts field in JSON
def extract_pass_fail_counts(json_data):
    total_pass = 0
    total_fail = 0
    for result in json_data['results']:
        for test_name, test_result in result.get('testPassFailCounts', {}).items():
            total_pass += test_result.get('pass', 0)
            total_fail += test_result.get('fail', 0)
    return total_pass, total_fail

# Function to generate the pie chart
def generate_pie_chart(pass_count, fail_count, chart_title):
    labels = ['Connection found', 'Connection not found']
    sizes = [pass_count, fail_count]
    colors = ['blue', 'red']
    
    plt.pie(sizes, labels=labels, colors=colors, autopct='%1.1f%%', startangle=90, textprops={'color': 'white'})
    plt.title(chart_title)

# Main execution
if __name__ == '__main__':
    # Parse command-line arguments
    args = parse_args()

    # Load weekend and normal day data
    with open(args.weekend, 'r') as file:
        weekend_data = json.load(file)
    
    with open(args.normal_day, 'r') as file:
        normal_day_data = json.load(file)

    # Extract pass/fail counts
    weekend_pass, weekend_fail = extract_pass_fail_counts(weekend_data)
    normal_day_pass, normal_day_fail = extract_pass_fail_counts(normal_day_data)

    # Set up the plot
    fig, axs = plt.subplots(1, 2, figsize=(10, 5))

    # Plot the first pie chart (weekend)
    axs[0].pie([weekend_pass, weekend_fail], labels=['Connection found', 'Connection not found'], colors=['blue', 'red'], autopct='%1.1f%%', startangle=90, textprops={'color': 'white'})
    axs[0].set_title('Weekend')

    # Plot the second pie chart (normal day)
    axs[1].pie([normal_day_pass, normal_day_fail], labels=['Connection found', 'Connection not found'], colors=['blue', 'red'], autopct='%1.1f%%', startangle=90, textprops={'color': 'white'})
    axs[1].set_title('Workday')

    # Add a single common legend below the pies
    fig.legend(['Connection found', 'Connection not found'], loc='lower center', ncol=2, title="Connection Status", frameon=False)


    # Show the pie charts
    plt.show()
