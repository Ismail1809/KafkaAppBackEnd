[⭠ Back](README.md)
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
- **GET /api/KafkaCluster/get-connection**: Return a particular connection by id
- **GET /api/KafkaCluster/get-connections**: Retrieve all connections
- **PUT /api/KafkaCluster/update-connection**: Find connection by id and update it
- **POST /api/KafkaCluster/create-connection**: Create a connection
- **GET /api/KafkaCluster/check-connection**: Check a connection of particular bootstrap server
- **POST /api/KafkaCluster/set-connection**: Set bootstrap server to given localhost
- **DELETE /api/KafkaCluster/delete-address**: Delete connection


## Integration Models
If you installed everything correctly, you will be able to run the program. After running it you will see the following page:
