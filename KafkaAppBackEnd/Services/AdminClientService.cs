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
using System.Net.Sockets;
using Docker.DotNet;
using Docker.DotNet.Models;
using Avro.IO;
using Microsoft.DotNet.MSIdentity.Shared;

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
        private readonly IConfiguration _configuration;
        private DockerClient client;

        public AdminClientService(ILogger<AdminClientService> logger, IAdminClient adminClient, 
            IProducer<string, string> producer, IConsumer<string, string> consumer, IConfiguration configuration)
        {
            _logger = logger;
            _adminClient = adminClient;
            _producer = producer;
            _consumer = consumer;
            _configuration = configuration;

            client = new DockerClientConfiguration()
                    .CreateClient();
        }

        public async Task<IEnumerable<GetTopicsResponse>> GetTopics(bool hideInternal)
        {
            var topicsPartitions = await GetTopicSize();
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics;
            DescribeTopicsResult data = _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames.Select(t => t.Topic)), null).Result;
            var visibleData = data.TopicDescriptions.Where(t => (hideInternal ? !t.IsInternal : true) && !t.Name.StartsWith("_confluent") && !t.Name.StartsWith("_schemas"));


            var joinedData = visibleData.GroupJoin(
                topicsPartitions.DefaultIfEmpty(),
                vd => vd.Name,
                tp => tp.partition.Substring(0, tp.partition.LastIndexOf("-")),
                (vd, tpGroup) => new GetTopicsResponse
                {
                    Name = vd.Name,
                    Error = vd.Error,
                    IsInternal = vd.IsInternal,
                    TopicId = vd.TopicId,
                    ReplicationFactor = vd.Partitions.FirstOrDefault()?.Replicas.Count ?? 0,
                    Partitions = tpGroup.Select(tp => new KafkaTopicPartition
                    {
                        PartitionNumber = tp.partition.Split("-").Last(),
                        Size = tp.size,
                    }).ToList(),
                });

            return joinedData;

            return visibleData
                .Select(t => new GetTopicsResponse
                {
                    Name = t.Name,
                    Error = t.Error,
                    IsInternal = t.IsInternal,
                    TopicId = t.TopicId,
                    ReplicationFactor = t.Partitions.FirstOrDefault()?.Replicas.Count ?? 0,
                    Partitions = topicsPartitions?
                        .Where(tp => tp.partition.Substring(0, tp.partition.LastIndexOf("-")) == t.Name)
                        .Select(pt => new KafkaTopicPartition
                        {
                            PartitionNumber = pt.partition.Split("-").Last(),
                            Size = pt.size,
                        }).ToList()
                        
                });
            

        }

        public TopicDescription GetTopic(string topicName)
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics;
            DescribeTopicsResult data = _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames.Select(t => t.Topic)), null).Result;
            var visibleData = data.TopicDescriptions.FirstOrDefault(t => t.Name == topicName);

            return visibleData;
        }

        public async Task<List<DescribeConfigsResult>> GetTopicConfig(string topicName)
        {
            var resource = new ConfigResource
            {
                Type = ResourceType.Topic,
                Name = topicName // Replace with your topic name
            };

            var resourceList = new List<ConfigResource> { resource };

            var configs = await _adminClient.DescribeConfigsAsync(resourceList);

            return configs;
        }

        public async Task<List<LogPartition>> GetTopicSize()
        {
            try
            {
                string? containerName = _configuration.GetSection("ContainerName").Value;

                var outputstream = new MemoryStream();

                var execParams = new ContainerExecCreateParameters()
                {
                    AttachStderr = true,
                    AttachStdout = true,
                    Cmd = new List<string> { "sh", "-c", $"kafka-log-dirs --describe --bootstrap-server localhost:9092" },
                };

                var exec = await client.Exec.ExecCreateContainerAsync(containerName, execParams);

                var stream = await client.Exec.StartAndAttachContainerExecAsync(exec.ID, false);

                await stream.CopyOutputToAsync(
                    null,
                    outputstream,
                    Console.OpenStandardError(),
                    CancellationToken.None);


                outputstream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(outputstream))
                {
                    var brokerId = _adminClient.GetMetadata(TimeSpan.FromSeconds(10)).OriginatingBrokerId;

                    var outputString = await reader.ReadToEndAsync();
                    var json = outputString.Split("\n")[2];

                    var deserializedJson = JsonConvert.DeserializeObject<Root>(json);

                    var partitions = deserializedJson.brokers.Find(b => b.broker == brokerId).logDirs.FirstOrDefault().partitions;

                    return partitions;
                }
            }
            catch(Exception ex)
            {
                return new List<LogPartition>();
            }

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
                GroupId = _configuration.GetSection("ConsumerSettings").GetSection("GroupId").Value,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoOffsetStore = false,
                EnableAutoCommit = _configuration.GetSection("ConsumerSettings").GetSection("AutoCommit").Get<bool>(),
                MaxPollIntervalMs = 120000,
            }).Build();

            _producer = new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = address,
                ClientId = _configuration.GetSection("ProducerSettings").GetSection("ClientId").Value
            }).Build();

            timer.Stop();
            _logger.LogInformation("Time taken on " + address + ": " + timer.ElapsedMilliseconds.ToString());

            return address;
        }
    }
}
