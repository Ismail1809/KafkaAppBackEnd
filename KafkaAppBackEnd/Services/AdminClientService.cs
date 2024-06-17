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

        public IEnumerable<TopicResponse> GetTopics(bool hideInternal)
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics;
            DescribeTopicsResult data = _adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames(topicNames.Select(t => t.Topic)), null).Result;
            var visibleData = data.TopicDescriptions;

            if (hideInternal)
            {
                return visibleData.Where(t => !t.IsInternal && !t.Name.StartsWith("_confluent") && !t.Name.StartsWith("_schemas")).Select(t => new TopicResponse { Name = t.Name, Error = t.Error, IsInternal = t.IsInternal, Partitions = t.Partitions, TopicId = t.TopicId });
            }
            else
            {
                return visibleData.Select(t => new TopicResponse { Name = t.Name, Error = t.Error, IsInternal = t.IsInternal, Partitions = t.Partitions, TopicId = t.TopicId }); ;
            }
        }

        public List<ConsumerGroupResponse> GetConsumerGroup()
        {
            List<ConsumerGroupResponse> consumerGroups = new List<ConsumerGroupResponse>();
            var groups = _adminClient.ListGroups(TimeSpan.FromSeconds(10));

            foreach (var g in groups)
            {
                consumerGroups.Add(new ConsumerGroupResponse { Group = g.Group, Error = g.Error, State = g.State, BrokerId = g.Broker.BrokerId, Host = g.Broker.Host, Port = g.Broker.Port, ProtocolType = g.ProtocolType, Protocol = g.ProtocolType });
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
