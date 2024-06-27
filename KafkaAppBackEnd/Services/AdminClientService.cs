using Avro;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Confluent.SchemaRegistry;
using Schema = Avro.Schema;
using Confluent.SchemaRegistry.Serdes;
using KafkaAppBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using static Confluent.Kafka.ConfigPropertyNames;
using Confluent.Kafka.SyncOverAsync;
using System.Text;
using System.Linq;

namespace KafkaAppBackEnd.Services
{
    public enum Choices
    {
        Contains = 1,
        Exact = 2
    }
    public class AdminClientService: IAdminClientService
    {
        private readonly ILogger<AdminClientService> _logger;
        private IAdminClient _adminClient;
        public string currentHost = "";
        private readonly IProducer<string, string> _producer;
        private readonly IConsumer<string, string> _consumer;

        public AdminClientService(ILogger<AdminClientService> logger, IAdminClient adminClient, IProducer<string, string> producer, IConsumer<string, string> consumer)
        {
            _logger = logger;
            _adminClient = adminClient;
            _producer = producer;
            _consumer = consumer;
        }

        public IEnumerable<GetTopicsResponse> GetTopics(bool hideInternal)
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics;
            DescribeTopicsResult data = _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames.Select(t => t.Topic)), null).Result;
            var visibleData = data.TopicDescriptions;

            if (hideInternal)
            {
                return visibleData.Where(t => !t.IsInternal && !t.Name.StartsWith("_confluent") && !t.Name.StartsWith("_schemas"))
                    .Select(t => new GetTopicsResponse { Name = t.Name, Error = t.Error, IsInternal = t.IsInternal, TopicId = t.TopicId});
            }
            else
            {
                return visibleData.Select(t => new GetTopicsResponse 
                {
                    Name = t.Name, Error = t.Error, IsInternal = t.IsInternal, Partitions = t.Partitions, TopicId = t.TopicId
                });
            }
            
        }

        public TopicDescription GetTopic(string topicName)
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics;
            DescribeTopicsResult data = _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames.Select(t => t.Topic)), null).Result;
            var visibleData = data.TopicDescriptions.FirstOrDefault(t => t.Name == topicName);

            return visibleData;
        }


        public List<GetConsumerGroupsResponse> GetConsumerGroups()
        {

            List<GetConsumerGroupsResponse> consumerGroups = new List<GetConsumerGroupsResponse>();
            var groups = _adminClient.ListGroups(TimeSpan.FromSeconds(10));

            foreach (var g in groups)
            {
                consumerGroups.Add(new GetConsumerGroupsResponse { Group = g.Group, Error = g.Error, State = g.State, BrokerId = g.Broker.BrokerId, Host = g.Broker.Host, Port = g.Broker.Port, ProtocolType = g.ProtocolType, Protocol = g.Protocol });
            }

            return consumerGroups;
        }

        public async Task CreateTopic(CreateTopicRequest topicRequest)
        {
            await _adminClient.CreateTopicsAsync([new TopicSpecification { Name = topicRequest.Name, ReplicationFactor = topicRequest.ReplicationFactor, NumPartitions = topicRequest.Partitions }]);
        }

        public async Task RenameTopicAsync(string oldTopicName, string newTopicName)
        {
            await CloneTopic(oldTopicName, newTopicName);
            await DeleteTopic(oldTopicName);

        }

        public async Task CloneTopic(string oldTopicName, string newTopicName)
        {
            var consumedMessages = GetMessagesFromX(oldTopicName, 0);
            var meta = _adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            var topic = meta.Topics.SingleOrDefault(t => t.Topic == oldTopicName);
            var replicationFactor = (short)topic.Partitions.First().Replicas.Length;
            await _adminClient.CreateTopicsAsync([new TopicSpecification { Name = newTopicName, ReplicationFactor = replicationFactor, NumPartitions = topic.Partitions.Count }]);

            if (consumedMessages != null)
            {
                foreach (var consumedMessage in consumedMessages)
                {
                    await ProduceMessage(consumedMessage.Message, newTopicName);
                }
            }
        }

        public async Task ProduceMessage(Message<string,string> message, string topic)
        {
            //var kafkaMessage = new Message<string, string>
            //{
            //    //Value = JsonConvert.SerializeObject(message)
            //    Value = message.Value

            //};

            await _producer.ProduceAsync(topic, message);
            
        }

        public List<ConsumeResult<string, string>> GetMessagesFromX(string topic, int x)
        {
            _consumer.Subscribe(topic);
            var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(5));

            if (consumeResult == null) 
            {
                return new List<ConsumeResult<string, string>>();
            }

            var offsets = _consumer.QueryWatermarkOffsets(new TopicPartition(consumeResult.Topic, consumeResult.Partition), TimeSpan.FromSeconds(1));

            var lastOffset = offsets.High - 1;

            if (lastOffset < x)
            {
                return new List<ConsumeResult<string, string>>();
            }

            var topicData = GetTopic(topic).Partitions;

            foreach (var topicPartition in topicData)
            {
                _consumer.Seek(new TopicPartitionOffset(consumeResult.Topic, topicPartition.Partition, x));
            }


            List<TopicPartition> topicPartitions = new List<TopicPartition>();
            List<ConsumeResult<string, string>> messages = new List<ConsumeResult<string, string>>();

            while (true)
            {
                consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                if (consumeResult is null)
                {
                    break;
                }
                Console.WriteLine(consumeResult.Message.Value);
                messages.Add(consumeResult);
                topicPartitions.Add(consumeResult.TopicPartition);
            }

            foreach (var tp in topicPartitions.ToHashSet())
            {
                _consumer.Seek(new TopicPartitionOffset(tp.Topic, tp.Partition, Offset.Beginning));
            }

            return messages;
        }

        public List<ConsumeResult<string, string>> GetSpecificPages(string topic, int pageSize, int pageNumber)
        {
            int startOffset = (pageNumber - 1) * pageSize;
            int endOffset = startOffset + pageSize;

            List<ConsumeResult<string, string>> messages = new List<ConsumeResult<string, string>>();
            List<TopicPartition> topicPartitions = new List<TopicPartition>();

            _consumer.Subscribe(topic);
            var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(5));

            var topicData = GetTopic(topic).Partitions;

            foreach (var topicPartition in topicData)
            {
                _consumer.Seek(new TopicPartitionOffset(consumeResult.Topic, topicPartition.Partition, startOffset));
            }

            int coveredPartitions = 0;
            while (startOffset < (pageNumber - 1) * pageSize + pageSize * topicData.Count)
            {
                consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                if (consumeResult == null)
                {
                    coveredPartitions++;
                    if(coveredPartitions == topicData.Count)
                    {
                        break;
                    }
                    continue;
                }
                else if (consumeResult.Offset.Value < (pageNumber - 1) * pageSize + pageSize)
                {
                    Console.WriteLine(consumeResult.Message.Value);
                    messages.Add(consumeResult);
                    topicPartitions.Add(consumeResult.TopicPartition);
                    startOffset++;
                    Console.WriteLine($"{consumeResult.TopicPartitionOffset}");
                }
            }

            foreach (var tp in topicPartitions.ToHashSet())
            {
                _consumer.Seek(new TopicPartitionOffset(tp.Topic, tp.Partition, Offset.Beginning));
            }

            return messages;
        }

        public IEnumerable<ConsumeResult<string, string>> SearchByKeys(string topic, List<string> listOfKeys, Choices choice)
        {
            var messages = GetMessagesFromX(topic, 0);
            if (choice == Choices.Exact)
            {
                return messages.Where(m => listOfKeys.Contains(m.Message.Key));
            }
            else if(choice == Choices.Contains)
            {
                return messages.Where(m => listOfKeys.Any(t => m.Message.Key.Contains(t)));
            }

            return messages;
        }

        public IEnumerable<ConsumeResult<string, string>> SearchByHeaders(string topic, List<string> listOfPairs, Choices choice)
        {
            var messages = GetMessagesFromX(topic, 0);
            if (choice == Choices.Exact)
            {
                return messages.Where(m => m.Message.Headers.Any(h => listOfPairs.Contains(h.Key) 
                || listOfPairs.Contains(Encoding.UTF8.GetString(h.GetValueBytes()))));
            }
            else if (choice == Choices.Contains)
            {
                return messages.Where(m => listOfPairs.Any(t => m.Message.Headers
                .Any(h => h.Key.Contains(t) || Encoding.UTF8.GetString(h.GetValueBytes())
                .Contains(t))))
                .ToList(); 
            }

            return messages;
        }


        public IEnumerable<ConsumeResult<string, string>> SearchByTimeStamps(string topic, DateTime? time1, DateTime? time2)
        {
            var messages = GetMessagesFromX(topic, 0);
            if (time1 == null && time2 == null)
            {
                return messages;
            }
            else if (time1 == null && time2 != null)
            {
                return messages.Where(m => time2 > m.Message.Timestamp.UtcDateTime);
            }
            else if(time2 == null && time1 != null)
            {
                return messages.Where(m => time1 < m.Message.Timestamp.UtcDateTime);
            }
            else
            {
                return messages.Where(m => time1 < m.Message.Timestamp.UtcDateTime && time2 > m.Message.Timestamp.UtcDateTime);
            }
        }

        public IEnumerable<ConsumeResult<string, string>> SearchByPartitions(string topic, int partition)
        {
            var messages = GetMessagesFromX(topic, 0);
            return messages.Where(m => m.Partition.Value == partition);
            
        }


        public async Task ProduceAvroMessage(Message<string, string> message, string topic)
        {

            string brokerList = "localhost:9092";
            string schemaRegistryUrl = "localhost:8081";
            var avroTopic = "avro-topic";

            var jsonMessage = new { id = 1, name = "John Doe", email = "john.doe@example.com" };

            var config = new ProducerConfig { BootstrapServers = brokerList };

            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
            {
                using (var avroProducer = new ProducerBuilder<string, GenericRecord>(config)
                                            .SetValueSerializer(new AvroSerializer<GenericRecord>(schemaRegistry))
                                            .Build())
                {
                    var avroSchema = @"
                {
                    ""type"": ""record"",
                    ""name"": ""User"",
                    ""fields"": [
                        { ""name"": ""id"", ""type"": ""int"" },
                        { ""name"": ""name"", ""type"": ""string"" },
                        { ""name"": ""email"", ""type"": ""string"" }
                    ]
                }";
                    var schema = (RecordSchema)Schema.Parse(avroSchema);

                    for (int i = 0; i < 10; i++)
                    {
                        // Produce JSON message
                        await _producer.ProduceAsync(topic, new Message<string, string> { Key = null, Value = JsonConvert.SerializeObject(message) });

                        // Produce Avro message
                        var avroRecord = new GenericRecord(schema);
                        avroRecord.Add("id", jsonMessage.id);
                        avroRecord.Add("name", jsonMessage.name);
                        avroRecord.Add("email", jsonMessage.email);

                        await avroProducer.ProduceAsync(avroTopic, new Message<string, GenericRecord> { Key = null, Value = avroRecord });
                    }
                }
            }

            CompareMessageSizes(brokerList, schemaRegistryUrl, topic, avroTopic);

        }

        public void CompareMessageSizes(string brokerList, string schemaRegistryUrl, string jsonTopic, string avroTopic)
        {
            var config = new ConsumerConfig
            {
                GroupId = "test-consumer-group",
                BootstrapServers = brokerList,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
            using (var avroConsumer = new ConsumerBuilder<string, GenericRecord>(config)
                                        .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistry).AsSyncOverAsync())
                                        .Build())
            {
                _consumer.Subscribe(jsonTopic);
                avroConsumer.Subscribe(avroTopic);

                var jsonSizes = new List<int>();
                var avroSizes = new List<int>();

                // Consume JSON messages
                for (int i = 0; i < 10; i++)
                {
                    var jsonMessage = _consumer.Consume();
                    jsonSizes.Add(jsonMessage.Message.Value.Length);
                }

                // Consume Avro messages
                for (int i = 0; i < 10; i++)
                {
                    var avroMessage = avroConsumer.Consume();
                    var avroBytes = Encoding.ASCII.GetBytes(avroMessage.Message.Value.ToString());
                    avroSizes.Add(avroBytes.Length);
                }

                Console.WriteLine("Average JSON message size: " + jsonSizes.Average());
                Console.WriteLine("Average Avro message size: " + avroSizes.Average());
            }
        }

        public async Task ProduceRandomNumberOfMessages(int numberOfMessages, string topic)
        {
            Random rnd = new Random();
            for (int i = 0; i < numberOfMessages; i++)
            {
                var headers = new Headers
                {
                    { i.ToString(), Encoding.UTF8.GetBytes(rnd.Next(1, 10).ToString()) },
                };
                await ProduceMessage(new Message<string, string> { Key = i.ToString(), Value = i.ToString(), Headers = headers },topic);
            };
        }

        public async Task DeleteTopic(string topicName)
        {
            await _adminClient.DeleteTopicsAsync([topicName], null);
        }

        public string SetAddress(string address)
        {
            currentHost = address;
            var timer = new Stopwatch();
            timer.Start();
            _adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = address }).Build();
            timer.Stop();
            _logger.LogInformation("Time taken on " + currentHost + ": " + timer.ElapsedMilliseconds.ToString());

            return address;
        }
    }
}
