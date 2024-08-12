#!/bin/bash

# Define variables
PROJECT_DIR="$(pwd)" # Use current directory as the project directory
PUBLISH_DIR="$PROJECT_DIR/publish-kafka"
CONFIGURATION="Release"
RUNTIME="osx-x64"

# Navigate to the project directory
cd $PROJECT_DIR

# Remove the previous publish directory if it exists
if [ -d "$PUBLISH_DIR" ]; then
  echo "Deleting previous publish directory..."
  rm -rf $PUBLISH_DIR
fi

# Publish the application
echo "Publishing the application..."
dotnet publish -o publish-kafka --runtime osx-arm64 -c Release --self-contained true

# Check if the publish was successful
if [ $? -eq 0 ]; then
  echo "Publish succeeded."
else
  echo "Publish failed."
  exit 1
fi

# Run the published application (if it's a console app)
echo "Running the published application..."
cd publish-kafka
./KafkaAppBackEnd