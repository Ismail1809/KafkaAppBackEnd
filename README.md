# Kafka Tool Application

## Application Definition
Kafka Tool Application is a powerful utility designed to manage and interact with Kafka clusters. It provides an intuitive interface for performing various Kafka operations such as creating and managing topics, consumers, and clusters.

## Application Scope
The scope of the Kafka Tool Application includes:
- Managing Kafka topics (create, clone, update, delete)
- Managing Kafka clusters
- Monitoring Kafka consumers and topics
- Offering a user-friendly interface for Kafka administration

## Application Features
- **Topic Management**: Create, clone, update, and delete Kafka topics with ease.
- **Topic Monitoring**: Track and analyze topics.
- **Cluster Monitoring**: Monitor cluster activity and ensure message delivery.
- **Planned Features**:
  - Authentication: Implement user login and registration features.
  - Authorization: Define user roles and permissions for different features.

## How to Run the Application in Docker

## Prerequisite:
To run this quick start, you will need Docker(create an account as well) and Docker Compose installed on a computer with a supported Operating System

## Start and Install the docker-compose file:

Step 1: **Download or copy the contents of the docker-compose file**
   ```bash
   git clone https://github.com/Ismail1809/KafkaAppBackEnd.git
   cd KafkaAppBackEnd
   ```
Step 2: **Start the Confluent Platform stack**
  ```bash
  docker compose up -d
  ```

#### Now, as a next step, we should install .NET on our devices to run the program

## Installing .NET on your device:
- Note: Open the new terminal
   
### Windows:
- You can install it through https://dotnet.microsoft.com/en-us/download/dotnet/8.0 or if you have ```winget``` follow the instruction:
   
Step 1: **Install the .NET 8 runtime**:
  ```bash
  winget install dotnet-runtime-8
  ```
Step 2: **Install the .NET 8 SDK**:
  ```bash
  winget install dotnet-sdk-8
  ```
Step 3: **Finally update an existing installation**:
  ```bash
  winget upgrade
  ```
### Unix/macOS:
Step 1: **Install the Homebrew(package manager)**:
  ```console
  % /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
  ```
Step 2: **Install the .NET packages**:
  ```console
  % brew install --cask dotnet
  ```
## Run application
- #### Now, after installing the .NET, you can run the application by the following command:
  ```bash
  dotnet run
  ```
- #### After running the program you will see this page: 
![Alt text](resources/SwaggerScreen.png)
  
  
- #### To stop your program press CTRL-C

## Next
- **[Integration](/INTEGRATION.md)**



