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
To run this quick start, you will need Docker and Docker Compose installed on a computer with a supported Operating System

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

## Start Kafka Tool Application:

Step 1: **Open Folder with Project**
  ```bash
  cd KafkaAppBackEnd
  ```
Step 2: **Start the application**
  ```bash
  dotnet run
  ```

## Next
- **[Integration](/INTEGRATION.MD)**



