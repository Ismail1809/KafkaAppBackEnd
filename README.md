# Kafka Tool Application

## Application Definition
Kafka Tool Application is a powerful utility designed to manage and interact with Kafka clusters. It provides an intuitive interface for performing various Kafka operations such as creating and managing topics, managing consumers, and clusters.

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
   https://github.com/Ismail1809/KafkaAppBackEnd.git
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

# Integration (API Description)
## Controllers and Endpoints Definition

Kafka Admin Controller:
- **GET /api/KafkaAdmin/get-topics**: Return a list of all Kafka topics
- **GET /api/KafkaAdmin/get-consumer-groups**: Retrieve a list of all Kafka consumers
- **POST /api/KafkaAdmin/create-topic**: Create a new topic
- **PUT /api/KafkaAdmin/rename-topic**: Rename the given topic's name
- **POST /api/KafkaAdmin/clone-topic**: Create a clone of the given topic
- **GET /api/KafkaAdmin/consume-messages**: Retrieve all consumed messages beginning from provided offset
- **GET /api/KafkaAdmin/get-specific-pages**: Consume and return messages from chosen page(pagination)
- **GET /api/KafkaAdmin/search-by-keys**: Search messages by keys
- **GET /api/KafkaAdmin/search-by-headers**: Search messages by headers
- **GET /api/KafkaAdmin/search-by-timestamps**: Search messages by timestamps
- **GET /api/KafkaAdmin/search-by-partitions**: Search messages by partitions
- **POST /api/KafkaAdmin/produce-n-messages**: Produce specific number of messages to given topic
- **POST /api/KafkaAdmin/produce-batch-messages**: Produce passed batch of messages to given topic
- **POST /api/KafkaAdmin/produce-batch-messages-from-file**: Read messages from file,
- and produce a batch of messages to the given topic
- **POST /api/KafkaAdmin/produce-message**: Produce message to assigned topic
- **POST /api/KafkaAdmin/set-address**: Connect to particular bootstrap server
- **DELETE /api/KafkaAdmin/set-address**: Delete specific topic

Kafka Cluster Controller:
- **Create connection**
