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
using SolTechnology.Avro;
using NuGet.Protocol;
using Microsoft.Hadoop;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Hadoop.Avro;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Plugins;

namespace KafkaAppBackEnd.Services
{
    public enum SearchOption
    {
        Contains = 1,
        Exact = 2
    }
    public class AdminClientService: IAdminClientService
    {
        private readonly ILogger<AdminClientService> _logger;
        private IAdminClient _adminClient;
        private IProducer<string, string> _producer;
        private IConsumer<string, string> _consumer;

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
                    .Select(t => new GetTopicsResponse
                    {
                        Name = t.Name,
                        Error = t.Error,
                        IsInternal = t.IsInternal,
                        TopicId = t.TopicId
                    });
            }
            else
            {
                return visibleData.Select(t => new GetTopicsResponse
                {
                    Name = t.Name,
                    Error = t.Error,
                    IsInternal = t.IsInternal,
                    Partitions = t.Partitions,
                    TopicId = t.TopicId
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

        public async Task<string> GetTopicSize(string topicName)
        {
            string line = "";
            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "powershell.exe";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            p.StartInfo = info;

            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            StreamWriter sw = p.StandardInput;
            if (sw.BaseStream.CanWrite)
            {
                sw.WriteLine("winpty docker exec -it 3c71196a7b3ea653fa0fb8bbcd833e4ae7762e5eab380bceff1349cf4cdba899 /bin/sh");
                sw.WriteLine("kafka-log-dirs --describe --bootstrap-server localhost:9092 --topic-list first-topic");
            }

            string output = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();

            // Write the redirected output to this application's window.
            Console.WriteLine(output);

            p.WaitForExit();

            return output;
            //        DockerClient client = new DockerClientConfiguration(
            //new Uri("npipe://./pipe/docker_engine"))
            // .CreateClient();

            //        IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(
            //            new ContainersListParameters()
            //            {
            //                Limit = 10,
            //            });

        }


        public List<GetConsumerGroupsResponse> GetConsumerGroups()
        {
            List<GetConsumerGroupsResponse> consumerGroups = new List<GetConsumerGroupsResponse>();
            var groups = _adminClient.ListGroups(TimeSpan.FromSeconds(10));

            foreach (var g in groups)
            {
                consumerGroups.Add(new GetConsumerGroupsResponse
                {
                    Group = g.Group,
                    Error = g.Error,
                    State = g.State,
                    BrokerId = g.Broker.BrokerId,
                    Host = g.Broker.Host,
                    Port = g.Broker.Port,
                    ProtocolType = g.ProtocolType,
                    Protocol = g.Protocol
                });
            }

            return consumerGroups;
        }

        public async Task CreateTopic(CreateTopicRequest topicRequest)
        {
            await _adminClient.CreateTopicsAsync([new TopicSpecification
            {
                Name = topicRequest.Name,
                ReplicationFactor = topicRequest.ReplicationFactor,
                NumPartitions = topicRequest.Partitions
            }]);
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
            await _producer.ProduceAsync(topic, message);
        }

        public List<ConsumeResult<string, string>> GetMessagesFromX(string topic, int x)
        {
            //Error with reading from first time
            // increase max poll???

            _consumer.Subscribe(topic);
            var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(30));

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
                consumeResult = _consumer.Consume(TimeSpan.FromSeconds(30));
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
            var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(30));

            var topicData = GetTopic(topic).Partitions;

            foreach (var topicPartition in topicData)
            {
                _consumer.Seek(new TopicPartitionOffset(consumeResult.Topic, topicPartition.Partition, startOffset));
            }

            int coveredPartitions = 0;
            while (startOffset < (pageNumber - 1) * pageSize + pageSize * topicData.Count)
            {
                consumeResult = _consumer.Consume(TimeSpan.FromSeconds(30));

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

        public IEnumerable<ConsumeResult<string, string>> SearchByKeys(string topic, List<string> listOfKeys, SearchOption choice)
        {
            var messages = GetMessagesFromX(topic, 0);
            if (choice == SearchOption.Exact)
            {
                return messages.Where(m => listOfKeys.Contains(m.Message.Key));
            }
            else if(choice == SearchOption.Contains)
            {
                return messages.Where(m => listOfKeys.Any(t => m.Message.Key.Contains(t)));
            }

            return messages;
        }

        public IEnumerable<ConsumeResult<string, string>> SearchByHeaders(string topic, List<string> listOfPairs, SearchOption choice)
        {
            var messages = GetMessagesFromX(topic, 0);
            if (choice == SearchOption.Exact)
            {
                return messages.Where(m => m.Message.Headers.Any(h => listOfPairs.Contains(h.Key) 
                || listOfPairs.Contains(Encoding.UTF8.GetString(h.GetValueBytes()))));
            }
            else if (choice == SearchOption.Contains)
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
            var startTime = time1 ?? DateTime.MinValue;
            var endTime = time2 ?? DateTime.MaxValue;

            return messages.Where(m => startTime < m.Message.Timestamp.UtcDateTime && endTime > m.Message.Timestamp.UtcDateTime);
        }

        public IEnumerable<ConsumeResult<string, string>> SearchByPartitions(string topic, int partition)
        {
            var messages = GetMessagesFromX(topic, 0);
            return messages.Where(m => m.Partition.Value == partition);
        }


        public async Task<List<string>> ProduceAvroMessage(string topic)
        {
            string brokerList = "localhost:9092";
            string schemaRegistryUrl = "localhost:8081";
            var avroTopic = "avro-topic";

            var jsonMessage = new { id = 1, name = "John Doe", email = "john.doe@example.com" };

            var config = new ProducerConfig { BootstrapServers = brokerList };

            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
            {
                using (var avroProducer = new ProducerBuilder<string, object>(config)
                                            .SetValueSerializer(new AvroSerializer<object>(schemaRegistry))
                                            .Build())
                {
                    var avroSchema = (RecordSchema)RecordSchema.Parse(@"
                    {
                        ""type"": ""record"",
                        ""name"": ""User"",
                        ""fields"": [
                            { ""name"": ""id"", ""type"": ""int"" },
                            { ""name"": ""name"", ""type"": ""string"" },
                            { ""name"": ""email"", ""type"": ""string"" }
                        ]
                    }");

                    for (int i = 0; i < 10; i++)
                    {
                        // Produce JSON message
                        await _producer.ProduceAsync(topic, new Message<string, string> { Key = null, Value = JsonConvert.SerializeObject(jsonMessage) });

                        // Produce Avro message

                        await avroProducer.ProduceAsync(avroTopic, new Message<string, object> { Key = null, Value = jsonMessage });
                    }
                }
            }

            return CompareMessageSizes(topic);

        }

        public async Task<string> CompareSizes()
        {
            var person = new Person { Id = 1, Name = "John", Description = "Working"};

            using (var buffer = new MemoryStream())
            {

                // Serialize the data.
                var avroSerializer = AvroSerializer.Create<Person>();

                avroSerializer.Serialize(buffer, person);
                var jsonSerializer = JsonConvert.SerializeObject(person);

                // Return the contents of the buffer.
                buffer.Seek(0, SeekOrigin.Begin);
                return "Size of avro message: " + buffer.Length.ToString() + ", size of json message: " + ASCIIEncoding.Unicode.GetByteCount(jsonSerializer);

            }
        }

        public List<string> CompareMessageSizes(string jsonTopic)
        {

            string brokerList = "localhost:9092";
            string schemaRegistryUrl = "localhost:8081";
            var avroTopic = "avro-topic";
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost: 9092",
                GroupId = "order-reader",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoOffsetStore = false,
                EnableAutoCommit = false
            };

            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
            using (var avroConsumer = new ConsumerBuilder<string, GenericRecord>(config)
                                        .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistry).AsSyncOverAsync())
                                        .Build())
            {
                _consumer.Subscribe(jsonTopic);
                avroConsumer.Subscribe(avroTopic);

                List<TopicPartition> topicPartitions = new List<TopicPartition>();
                var jsonSizes = new List<int>();
                var avroMessages = new List<string>();
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
                    var avroMessage = avroConsumer.Consume(TimeSpan.FromSeconds(30));

                    if (avroMessage == null)
                    {
                        break;
                    }

                    RuntimeTypeHandle th = avroMessage.GetType().TypeHandle;
                    int size;
                    unsafe { size = *(*(int**)&th + 1); }

                    avroSizes.Add(size);

                    var avroString = avroMessage.Message.Value.ToJson();
                    topicPartitions.Add(avroMessage.TopicPartition);
                    avroMessages.Add(avroString);
                }

                

                foreach (var tp in topicPartitions.ToHashSet())
                {
                    avroConsumer.Seek(new TopicPartitionOffset(tp.Topic, tp.Partition, Offset.Beginning));
                }

                Console.WriteLine("Average JSON message size: " + jsonSizes.Average());
                Console.WriteLine("Average Avro message size: " + string.Join(", ", avroSizes));

                return avroMessages;
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

        public async Task BatchMessages(string topic, List<string> listOfMessages, string key, Dictionary<string, string> headers, int? partitionId)
        {
            var multiHeaders = new Headers { };
            foreach (var header in headers) 
            {
                multiHeaders.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
            }
            var messages = listOfMessages.Select(message => 
            {
                var newMessage = new Message<string, string>
                {
                    Key = key,
                    Value = message,
                    Headers = multiHeaders
                };

                if (partitionId != null)
                {
                    var topicPart = new TopicPartition(topic, new Partition((int)partitionId));
                    return _producer.ProduceAsync(topicPart, newMessage);
                }
                else
                {
                    return _producer.ProduceAsync(topic, newMessage);
                }
            });

            await Task.WhenAll(messages);
        }

        public async Task BatchMessagesFromFile(string topic, IFormFile formFile, string separator, string key, Dictionary<string, string> headers, int? partitionId)
        {
            List<string> listOfMessages = new List<string>();


            var result = new StringBuilder();
            var reader = new StreamReader(formFile.OpenReadStream());
            while (reader.Peek() >= 0) { 
                    result.Append(await reader.ReadLineAsync() + " ");
            }

            Console.WriteLine(result.ToString());

            listOfMessages = result.ToString().Split(separator).ToList();

            var multiHeaders = new Headers { };
            foreach (var header in headers)
            {
                multiHeaders.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
            }
            var messages = listOfMessages.Select(message =>
            {
                var newMessage = new Message<string, string>
                {
                    Key = key,
                    Value = message,
                    Headers = multiHeaders
                };

                if (partitionId != null)
                {
                    var topicPart = new TopicPartition(topic, new Partition((int)partitionId));
                    return _producer.ProduceAsync(topicPart, newMessage);
                }
                else
                {
                    return _producer.ProduceAsync(topic, newMessage);
                }
            });

            await Task.WhenAll(messages);
        }

        public async Task DeleteTopic(string topicName)
        {
            await _adminClient.DeleteTopicsAsync([topicName], null);
        }

        public string SetAddress(string address)
        {
            var timer = Stopwatch.StartNew();
            _adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = address }).Build();
            _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig{
                BootstrapServers = address,
                GroupId = "order-reader",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoOffsetStore = false,
                EnableAutoCommit = false,
                MaxPollIntervalMs = 120000,
            }).Build();

            _producer = new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = address,
                ClientId = "order-producer"
            }).Build();

            timer.Stop();
            _logger.LogInformation("Time taken on " + address + ": " + timer.ElapsedMilliseconds.ToString());

            return address;
        }
    }
}
