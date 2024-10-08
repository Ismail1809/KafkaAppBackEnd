﻿using Avro;
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
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Net.Http;

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
        private static readonly HttpClient _httpClient = new HttpClient();

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

            _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        }

        public async Task<GetTopicResponse> GetTopicInfo(string topicName)
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics;
            DescribeTopicsResult data = _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames.Select(t => t.Topic)), null).Result;
            var visibleData = data.TopicDescriptions.First(t => t.Name == topicName);
            var topicMetrics = await GetTopicsRecordsProm("kafka_log_log_logendoffset{topic='" + topicName + "'}");
            var topicRecordsCount = topicMetrics.Select(tc => Convert.ToInt32(tc.value[1])).Sum();

            var response = new GetTopicResponse()
            {
                Name = visibleData.Name,
                Error = visibleData.Error,
                IsInternal = visibleData.IsInternal,
                TopicId = visibleData.TopicId,
                ReplicationFactor = visibleData.Partitions.FirstOrDefault()?.Replicas.Count ?? 0,
                Partitions = visibleData.Partitions.Select(p => new KafkaTopicPartition { PartitionNumber = p.Partition.ToString() }).ToList(),
                RecordsCount = topicRecordsCount
            };

            return response;
        }


        public async Task<TopicDescription> GetTopic(string topicName)
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics;
            DescribeTopicsResult data = await _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames.Select(t => t.Topic)), null);
            var visibleData = data.TopicDescriptions.FirstOrDefault(t => t.Name == topicName);

            return visibleData;
        }

        public async Task<IEnumerable<GetTopicResponse>> GetTopics(bool hideInternal)
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics;
            var data = await _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames.Select(t => t.Topic)), null);
            var topics = data.TopicDescriptions.Where(t => (hideInternal ? !t.IsInternal : true) && !t.Name.StartsWith("_confluent") && !t.Name.StartsWith("_schemas"));

            var result = new List<GetTopicResponse>();

            var topicMetrics = await GetTopicsRecordsProm("kafka_log_log_logendoffset");

            foreach (var topic in topics) {
                var response = new GetTopicResponse()
                {
                    Name = topic.Name,
                    Error = topic.Error,
                    IsInternal = topic.IsInternal,
                    TopicId = topic.TopicId,
                    ReplicationFactor = topic.Partitions.FirstOrDefault()?.Replicas.Count ?? 0,
                    Partitions = topic.Partitions.Select(p => new KafkaTopicPartition { PartitionNumber = p.Partition.ToString() }).ToList(),
                    RecordsCount = topicMetrics.Where(tc => tc.metric.topic == topic.Name).Select(tc => Convert.ToInt32(tc.value[1])).Sum()
                };

                result.Add(response);
            }

            return result;
        }

        public async Task<int> GetTopicRecordsCount(string topicName)
        {
            var topicMetrics = await GetTopicsRecordsProm("kafka_log_log_logendoffset{topic='" + topicName + "'}");
            var topicRecordsCount = topicMetrics.Select(tc => Convert.ToInt32(tc.value[1])).Sum();

            return topicRecordsCount;
        }

        public async Task<List<Result>> GetTopicsRecordsProm(string? param)
        {
            var query = param;
            var url = $"http://localhost:9090/api/v1/query?query={query}";

            try
            {
                // Send the HTTP GET request to Prometheus
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Parse the JSON response
                var content = await response.Content.ReadAsStringAsync();
                var prometheusResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<PrometheusResponse>(content);

                return prometheusResponse.data.result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Kafka log end offset from Prometheus: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<GetTopicSizeResponse>> GetTopicsSizeInfo(bool hideInternal)
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics;
            var data = await _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames.Select(t => t.Topic)), null);
            var topics = data.TopicDescriptions.Where(t => (hideInternal ? !t.IsInternal : true) && !t.Name.StartsWith("_confluent") && !t.Name.StartsWith("_schemas"));

            var result = new List<GetTopicSizeResponse>();

            var topicSizeInfo = await GetTopicsRecordsProm("kafka_log_log_size");

            foreach (var topic in topics)
            {
                var response = new GetTopicSizeResponse()
                {
                    Name = topic.Name,
                    Partitions = topicSizeInfo.Where(tc => tc.metric.topic == topic.Name).Select(tc => new KafkaTopicPartition() { PartitionNumber = tc.metric.partition, Size = Convert.ToInt32(tc.value[1]) }).ToList(),
                };

                result.Add(response);
            }

            return result;
        }


        public async Task<List<DescribeConfigsResult>> GetTopicConfig(string topicName)
        {
            var resource = new ConfigResource
            {
                Type = ResourceType.Topic,
                Name = topicName 
            };

            var resourceList = new List<ConfigResource> { resource };

            var configs = await _adminClient.DescribeConfigsAsync(resourceList);

            return configs;
        }

        public async Task<List<PartitionOffsets>> GetPartitionRecordsCount(string topicName)
        {

            var topicData = await GetTopic(topicName);
            var listPartitionsOffsets = new List<PartitionOffsets>();

            WatermarkOffsets offsets;
            long lastOffset = 0;

            foreach (var topicPartition in topicData.Partitions)
            {
                offsets = _consumer.QueryWatermarkOffsets(new TopicPartition(topicName, topicPartition.Partition), TimeSpan.FromSeconds(1));
                lastOffset = offsets.High;

                listPartitionsOffsets.Add(new PartitionOffsets { Partition = topicPartition.Partition, Offset = lastOffset });
            }

            return listPartitionsOffsets;
        }

        public async Task<long> GetTopicRecordsCountKafka(string topicName)
        {

            var topicData = await GetTopic(topicName);

            WatermarkOffsets offsets;
            long lastOffset = 0;

            foreach (var topicPartition in topicData.Partitions)
            {
                offsets = _consumer.QueryWatermarkOffsets(new TopicPartition(topicName, topicPartition.Partition), TimeSpan.FromSeconds(1));
                lastOffset += offsets.High;
            }

            return lastOffset;
        }

        public async Task<List<GetConsumerGroupsResponse>> GetConsumerGroups()
        {
            List<GetConsumerGroupsResponse> consumerGroups = new List<GetConsumerGroupsResponse>();
            var groups = _adminClient.ListGroups(TimeSpan.FromSeconds(10));

            var groupsInfo = await _adminClient.DescribeConsumerGroupsAsync(groups.Where(g => g.Group != "schema-registry").Select(g => g.Group));

            var groupsInfoLIst = groupsInfo.ConsumerGroupDescriptions;

            foreach (var g in groupsInfoLIst)
            {
                var overallLag = g.GroupId.Contains("ConfluentTelemetryReporterSampler") ? 0 : GetOverAllLag(g.Members);

                List<string> distinctTopicsList = new List<string>();

                g.Members.ForEach(cg => cg.Assignment.TopicPartitions.Select(tp => tp.Topic).Distinct().ToList().ForEach(topic => distinctTopicsList.Add(topic)));

                consumerGroups.Add(new GetConsumerGroupsResponse
                {
                    Group = g.GroupId,
                    Members = g.Members.Count(),
                    Error = g.Error,
                    State = Enum.GetName(typeof(ConsumerGroupState), g.State),
                    BrokerId = g.Coordinator.Id,
                    Host = g.Coordinator.Host,
                    Port = g.Coordinator.Port,
                    AssignedTopics = distinctTopicsList, 
                    OverallLag = overallLag
                });
            }

            return consumerGroups;
        }

        public long GetOverAllLag(List<MemberDescription> members)
        {
            var overallLag = 0L;
            foreach (var member in members)
            {
                _consumer.Assign(member.Assignment.TopicPartitions);

                List<TopicPartitionOffset> tpos = _consumer.Committed(TimeSpan.FromSeconds(40));
                foreach (TopicPartitionOffset tpo in tpos)
                {
                    WatermarkOffsets w = _consumer.QueryWatermarkOffsets(tpo.TopicPartition, TimeSpan.FromSeconds(40));
                    long commited = tpo.Offset.Value;
                    long log_end_offset = w.High.Value;
                    long lag = log_end_offset - commited;

                    overallLag += lag;
                }
            }

            return overallLag;
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

        public async Task CreateTopics(List<CreateTopicRequest> topicsRequest)
        {
            await _adminClient.CreateTopicsAsync(topicsRequest.Select(topicRequest => new TopicSpecification
            {
                Name = topicRequest.Name,
                ReplicationFactor = topicRequest.ReplicationFactor,
                NumPartitions = topicRequest.Partitions
            }));
        }

        public async Task RenameTopicAsync(string oldTopicName, string newTopicName)
        {
            await CloneTopic(oldTopicName, newTopicName);
            await DeleteTopic(oldTopicName);
        }

        public async Task CloneTopic(string oldTopicName, string newTopicName)
        {
            var consumedMessages = await GetMessagesFromX(oldTopicName, 0);
            var meta = _adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            var topic = meta.Topics.SingleOrDefault(t => t.Topic == oldTopicName);
            var replicationFactor = (short)topic.Partitions.First().Replicas.Length;
            await _adminClient.CreateTopicsAsync([new TopicSpecification { Name = newTopicName, ReplicationFactor = replicationFactor, NumPartitions = topic.Partitions.Count }]);

            if (consumedMessages != null)
            {
                foreach (var consumedMessage in consumedMessages)
                {
                    await ProduceMessage(consumedMessage.Message.Key, consumedMessage.Message.Value, consumedMessage.Message.Headers, newTopicName);
                }
            }
        }

        public async Task<List<ConsumeResult<string, string>>> GetMessagesFromBeginning(string topic)
        {
            var topicData = await GetTopic(topic);
            var partitions = topicData.Partitions.Select(partition => new TopicPartitionOffset(topicData.Name, new Partition(partition.Partition), new Offset(0))).ToList();
            var count = 0;

            _consumer.Assign(partitions);

            List<ConsumeResult<string, string>> messages = new List<ConsumeResult<string, string>>();

            while (true)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(2));

                    if (consumeResult.IsPartitionEOF && consumeResult.Offset == 0)
                    {
                        count ++;

                        if (count == topicData.Partitions.Count)
                        {
                            return messages;
                        }
                        continue;
                    }

                    if (consumeResult.IsPartitionEOF) 
                    {
                        return messages;
                    }

                    messages.Add(consumeResult);
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.Local_MaxPollExceeded)
                {
                    _consumer.Subscribe(topic);

                    var consumeResult = _consumer.Consume();

                    messages.Add(consumeResult);
                }
            }
        }

        public async Task<List<ConsumeResult<string, string>>> GetMessagesFromX(string topic, int x)
        {
            var topicData = await GetTopic(topic);
            var partitions = topicData.Partitions.Select(partition => new TopicPartitionOffset(topicData.Name, new Partition(partition.Partition), x)).ToList();

            _consumer.Assign(partitions);

            List<ConsumeResult<string, string>> messages = new List<ConsumeResult<string, string>>();

            while (true)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(2));

                    if (consumeResult.IsPartitionEOF && consumeResult.Offset == 0)
                    {
                        continue;
                    }

                    if (consumeResult.IsPartitionEOF)
                    {
                        return messages.Where(m => m.Offset >= x).ToList();
                    }

                    messages.Add(consumeResult);
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.Local_MaxPollExceeded)
                {
                    _consumer.Subscribe(topic);

                    var consumeResult = _consumer.Consume();

                    messages.Add(consumeResult);
                }
            }
        }

        public async Task<List<ConsumeResult<string, string>>> GetSpecificPages(string topic, int pageSize, int pageNumber)
        {
            var topicMetrics= await GetTopicsRecordsProm("kafka_log_log_logendoffset{topic='" + topic + "'}");
            var topicRecordCount = topicMetrics.Select(tc => Convert.ToInt32(tc.value[1])).Sum();
            if (pageSize * pageNumber - pageSize >= topicRecordCount)
            {
                return [];
            }
            int startOffset = (pageNumber - 1) * pageSize;
            int endOffset = startOffset + pageSize;
            long diff = startOffset;

            PartitionOffsets offsets = null;

            var topicData = await GetTopic(topic);

            var listPartitionOffsets = await GetPartitionRecordsCount(topic);

            var partitions = topicData.Partitions.Select(partition => new TopicPartitionOffset(topicData.Name, new Partition(partition.Partition), startOffset)).ToList();

            _consumer.Assign(partitions);
            foreach (var partition in partitions)
            {
                offsets = listPartitionOffsets.Find(p => p.Partition == partition.Partition);
                
                if(diff > Math.Max(offsets.Offset - 1, 0))
                {
                    diff -= offsets.Offset;
                }
                else
                {
                    _consumer.Assign(new TopicPartitionOffset(topicData.Name, new Partition(partition.Partition), diff));
                    break;
                }
            }


            List<ConsumeResult<string, string>> messages = new List<ConsumeResult<string, string>>();
            int topicPartitionCount = topicData.Partitions.Count;

            if(topicPartitionCount == 0)
            {
                return messages;
            }

            int remainingMessages = pageSize;

            if (topicRecordCount - remainingMessages <= 0) remainingMessages = topicRecordCount;

            while (remainingMessages > 0)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(2));

                    if (consumeResult.IsPartitionEOF && consumeResult.Offset == 0) 
                    {
                        offsets = listPartitionOffsets.Find(p => p.Partition == consumeResult.Partition + 1);
                        _consumer.Assign(new TopicPartitionOffset(topic, consumeResult.Partition + 1, Offset.Beginning));
                        continue;
                    }

                    if (consumeResult.Offset >= offsets.Offset - 1 && consumeResult.Partition < topicPartitionCount -1)
                    {
                        if (consumeResult.Message != null)
                        {
                            messages.Add(consumeResult);
                            remainingMessages--;
                        }
                        offsets = listPartitionOffsets.Find(p => p.Partition == consumeResult.Partition + 1);
                        _consumer.Assign(new TopicPartitionOffset(consumeResult.Topic, consumeResult.Partition + 1, Offset.Beginning));
                        continue;
                    }
                    else if (consumeResult.Offset >= offsets.Offset - 1 && consumeResult.Partition == topicPartitionCount - 1)
                    {
                        messages.Add(consumeResult);
                        remainingMessages--;
                        return messages;
                    }


                    if (consumeResult != null && !consumeResult.IsPartitionEOF)
                    {
                        messages.Add(consumeResult);
                        remainingMessages--;
                    }

                    else if (consumeResult.IsPartitionEOF)
                    {
                        _consumer.Unassign();
                        break;
                    }
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.Local_MaxPollExceeded)
                {
                    _consumer.Subscribe(topic);

                    var consumeResult = _consumer.Consume();

                    messages.Add(consumeResult);
                }
            }

            return messages.ToList(); 
        }


        public async Task<IEnumerable<ConsumeResult<string, string>>> SearchByKeys(string topic, List<string> listOfKeys, int choice)
        {
            var messages = await GetMessagesFromBeginning(topic);
            if (choice == (int)SearchOption.Exact)
            {
                return messages.Where(m => listOfKeys.Contains(m.Message.Key.ToString()));
            }
            else if(choice == (int)SearchOption.Contains)
            {
                return messages.Where(m => listOfKeys.Any(t => m.Message.Key.Contains(t)));
            }

            return messages;
        }

        public async Task<IEnumerable<ConsumeResult<string, string>>> SearchByHeaders(string topic, List<string> listOfPairs, int choice)
        {
            var messages = await GetMessagesFromBeginning(topic);
            if (choice == (int)SearchOption.Exact)
            {
                return messages.Where(m => m.Message.Headers.Any(h => listOfPairs.Contains(h.Key) 
                || listOfPairs.Contains(Encoding.UTF8.GetString(h.GetValueBytes()))));
            }
            else if (choice == (int)SearchOption.Contains)
            {
                return messages.Where(m => listOfPairs.Any(t => m.Message.Headers
                .Any(h => h.Key.Contains(t) || Encoding.UTF8.GetString(h.GetValueBytes())
                .Contains(t))))
                .ToList(); 
            }

            return messages;
        }


        public async Task<IEnumerable<ConsumeResult<string, string>>> SearchByTimeStamps(string topic, DateTime? time1, DateTime? time2)
        {
            var messages = await GetMessagesFromBeginning(topic);
            var startTime = time1 ?? DateTime.MinValue;
            var endTime = time2 ?? DateTime.MaxValue;

            return messages.Where(m => startTime < m.Message.Timestamp.UtcDateTime && endTime > m.Message.Timestamp.UtcDateTime);
        }

        public async Task<IEnumerable<ConsumeResult<string, string>>> SearchByPartitions(string topic, int partition)
        {
            var messages = await GetMessagesFromBeginning(topic);
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

                return avroMessages;
            }
        }
        public async Task ProduceMessage(string key, string value, Headers headers, string topic)
        {
            await _producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = value, Headers = headers });
        }

        public async Task ProduceMessageWithCustomHeaders(string key, string value, List<HeaderRequest> headers, string topic)
        {
            var headersResult = new Headers();

            foreach (var header in headers)
            {
                headersResult.Add(new Header(JsonConvert.SerializeObject(header.Key.ToString()), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(header.Value))));
            }

            await _producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = value, Headers = headersResult });
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
                await ProduceMessage(i.ToString(), i.ToString(), headers, topic);
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
                EnablePartitionEof = true
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
