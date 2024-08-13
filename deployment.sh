echo "Running Docker Compose..."
docker-compose -f "lab-deployment.yml" up --build -d

echo "Docker containers were successfully composed"

# Remove the previous publish directory if it exists
if [ -d "/publish-kafka" ]; then
  echo "Deleting previous publish directory..."
  rm -rf /publish-kafka
fi

# Publish the application
echo "Publishing the application..."
dotnet publish -o publish-kafka -c Release --self-contained true
echo "Deployment completed successfully."

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