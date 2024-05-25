import json
import requests
import os
import sys

def main():
    # Check if a directory was provided as a command-line argument
    if len(sys.argv) < 2:
        print("Please provide a directory to save images.")
        sys.exit(1)

    save_dir = sys.argv[1]
    os.makedirs(save_dir, exist_ok=True)

    # URL to fetch JSON data
    json_url = 'https://duckduckgo.com/sports.js?q=nba&league=nba&type=standings&o=json'

    # Fetch JSON data from the URL
    response = requests.get(json_url)
    if response.status_code != 200:
        print("Failed to fetch JSON data")
        sys.exit(1)

    # Load JSON data
    data = response.json()

    # Base URL for the assets
    base_url = 'https://duckduckgo.com/'

    # Iterate over each team and download the image
    for team in data['data']['teams']:
        image_url = base_url + team['image']
        response = requests.get(image_url)
        if response.status_code == 200:
            # Extract image name from URL
            image_name = team['image'].split('/')[-1]
            file_path = os.path.join(save_dir, image_name)
            with open(file_path, 'wb') as file:
                file.write(response.content)
            print(f"Downloaded {image_name}")
        else:
            print(f"Failed to download {image_name} from {image_url}")

    print("All downloads complete.")

if __name__ == '__main__':
    main()
