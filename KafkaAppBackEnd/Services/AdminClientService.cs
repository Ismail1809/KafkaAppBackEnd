using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace KafkaAppBackEnd.Services
{
    public class AdminClientService: IAdminClientService
    {
        private readonly IAdminClient _adminClient;

        public AdminClientService(IAdminClient adminClient)
        {
            _adminClient = adminClient;
        }

        public IEnumerable<string> GetTopics()
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics.Select(t => t.Topic);
            
            DescribeTopicsResult data = _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames), null).Result;
            var topicsMetadata = metadata.Topics;

            var visibleData = data.TopicDescriptions.Where(t => !t.IsInternal && !t.Name.StartsWith("_confluent")).Select(t => t.Name);

            return visibleData;
        }

        public List<ConsumerResponse> GetConsumerGroup()
        {
            List<ConsumerResponse> consumerGroups = new List<ConsumerResponse>();
            var groups = _adminClient.ListGroups(TimeSpan.FromSeconds(10));

            foreach (var g in groups)
            {
                consumerGroups.Add(new ConsumerResponse { Group = g.Group, Error = g.Error, State = g.State, BrokerId = g.Broker.BrokerId, Host = g.Broker.Host, Port = g.Broker.Port, ProtocolType = g.ProtocolType, Protocol = g.ProtocolType });
            }

            return consumerGroups;
        }

        public async Task CreateTopic(TopicRequest topicRequest)
        {
            await _adminClient.CreateTopicsAsync([new TopicSpecification { Name = topicRequest.Name, ReplicationFactor = topicRequest.ReplicationFactor, NumPartitions = topicRequest.Partitions }]);
        }

        public async Task DeleteTopic(string topicName)
        {
            await _adminClient.DeleteTopicsAsync([topicName], null);
        }
    }
}
