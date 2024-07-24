using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Models;
using KafkaAppBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NuGet.Frameworks;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;
using System.Net;
using System.Text;
using SearchOption = KafkaAppBackEnd.Services.SearchOption;

namespace KafkaAppBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KafkaAdminController : ControllerBase
    {
        private readonly IAdminClientService _adminClientService;

        public KafkaAdminController(IAdminClientService adminClientService)
        {
            _adminClientService = adminClientService;
        }

        [HttpGet("get-topic")]
        public async Task<ActionResult<IEnumerable<GetTopicResponse>>> GetTopic([FromQuery] string topicName)
        {
            try
            {
                var topicDescription = _adminClientService.GetTopic(topicName);

                if (topicDescription == null)
                {
                    return base.Ok("List of topics is null");
                }
                return base.Ok(topicDescription);
            }
            catch (Exception ex)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"Error while accessing list of topics: {ex.Message}");
            }
        }

        [HttpGet("get-topics")]
        public async Task<ActionResult<IEnumerable<GetTopicResponse>>> GetTopics([FromQuery] bool hideInternal)
        {
            try
            {
                var listOfTopics = await _adminClientService.GetTopics(hideInternal);

                if (listOfTopics == null)
                {
                    return base.Ok("List of topics is null");
                }
                return base.Ok(listOfTopics);
            }
            catch (Exception ex)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"Error while accessing list of topics: {ex.Message}");
            }
        }

        [HttpGet("get-topic-config")]
        public async Task<ActionResult<List<DescribeConfigsResult>>> GetTopicConfig([FromQuery] string topicName)
        {
            try
            {
                var topicConfig = await _adminClientService.GetTopicConfig(topicName);

                if (topicConfig == null)
                {
                    return base.Ok("List of topics is null");
                }
                return base.Ok(topicConfig);
            }
            catch (Exception ex)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"Error while accessing list of topics: {ex.Message}");
            }
        }

        [HttpGet("get-topic-records-count")]
        public async Task<ActionResult<long>> GetTopicRecordsCount([FromQuery] string topicName)
        {
            try
            {
                var recordsCount = _adminClientService.GetTopicRecordsCount(topicName);

                if (recordsCount == null)
                {
                    return base.Ok("List of topics is null");
                }
                return base.Ok(recordsCount);
            }
            catch (Exception ex)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"Error while accessing list of topics: {ex.Message}");
            }
        }

        [HttpGet("get-consumer-groups")]
        public ActionResult<List<GetConsumerGroupsResponse>> GetConsumerGroups()
        {
            try
            {
                var consumerGroup = _adminClientService.GetConsumerGroups();

                if (consumerGroup == null)
                {
                    return Ok("List of consumers is null");
                }

                return Ok(consumerGroup);
            }
            catch (Exception ex) {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"Error while accessing list of consumers: {ex.Message}");
            }


        }

        [HttpPost("create-topic")]
        public async Task<ActionResult<string>> CreateTopic([FromBody] CreateTopicRequest topicRequest)
        {

            try
            {
                await _adminClientService.CreateTopic(topicRequest);
                return Ok("Topic was successfully created!");
            }
            catch (CreateTopicsException e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
            }
        }

        [HttpPut("rename-topic")]
        public async Task<ActionResult<string>> RenameTopic(string oldTopicName, string newTopicName)
        {
            if (oldTopicName == newTopicName)
            {
                return Ok("Name hasn't been changed!");
            }
            else if (oldTopicName == null || newTopicName == null)
            {
                return Ok("One of the fields is empty!");
            }

            try
            {
                await _adminClientService.RenameTopicAsync(oldTopicName, newTopicName);
                return Ok("Topic was updated");
            }
            catch (DbUpdateConcurrencyException)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, "Error while renaming topic");
            }
        }

        [HttpPost("clone-topic")]
        public async Task<ActionResult<string>> CloneTopic(string oldTopicName, string newTopicName)
        {
            if(oldTopicName == newTopicName)
            {
                return Ok("Topic with this name already exists!");
            }
            else if(oldTopicName == null || newTopicName == null)
            {
                return Ok("One of the fields is empty!");
            }
            
            try
            {
                await _adminClientService.CloneTopic(oldTopicName, newTopicName);
                return Ok("Topic was successfully cloned!");
            }
            catch (CreateTopicsException e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while cloning topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
            }
        }

        [HttpGet("consume-messages")]
        public async Task<ActionResult<List<ConsumeResult<string, string>>>> ConsumeMessages([FromQuery]string topicName, int offset)
        {
            try
            {
                var messages = _adminClientService.GetMessagesFromX(topicName, offset);
                return Ok(messages.Select(m => new ConsumeTopicResponse { Message = m.Message, Partition = m.Partition.Value, Offset = m.Offset.Value}));
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while consuming from topic");
            }
        }

        [HttpGet("get-specific-pages")]
        public async Task<ActionResult<ConsumeTopicResponse[]>> GetSpecificPages([FromQuery] string topic, int pageSize, int pageNumber)
        {
            try
            {
                var messages = _adminClientService.GetSpecificPages(topic, pageSize, pageNumber);
                return Ok(messages.Select(m => new ConsumeTopicResponse { Message = m.Message, Partition = m.Partition.Value, Offset = m.Offset.Value }));
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while consuming from topic");
            }
        }

        [HttpGet("search-by-keys")]
        public async Task<ActionResult<ConsumeTopicResponse[]>> SearchByKeys([FromQuery] List<string> listOfKeys, string topic, SearchOption choice)
        {
            try
            {
                var messages = _adminClientService.SearchByKeys(topic, listOfKeys, choice);
                return Ok(messages.Select(m => new ConsumeTopicResponse { Message = m.Message, Partition = m.Partition.Value, Offset = m.Offset.Value }));
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while consuming from topic");
            }
        }

        [HttpGet("search-by-headers")]
        public async Task<ActionResult<ConsumeTopicResponse[]>> SearchByHeaders([FromQuery] List<string> listOfStrings, string topic, SearchOption choice)
        {
            try
            {
                var messages = _adminClientService.SearchByHeaders(topic, listOfStrings, choice);
                return Ok(messages.Select(m => new ConsumeTopicResponse { Message = m.Message, Partition = m.Partition.Value, Offset = m.Offset.Value, HeaderValue = m.Message.Headers.ToList().Select(h => Encoding.UTF8.GetString(h.GetValueBytes())).FirstOrDefault()}));
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while consuming from topic");
            }
        }

        [HttpGet("search-by-timestamps")]
        public async Task<ActionResult<ConsumeTopicResponse[]>> SearchByTimeStamps([FromQuery] DateTime time1, DateTime time2, string topic)
        {
            try
            {
                var messages = _adminClientService.SearchByTimeStamps(topic, time1, time2);
                return Ok(messages.Select(m => new ConsumeTopicResponse { Message = m.Message, Partition = m.Partition.Value, Offset = m.Offset.Value }));
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while consuming from topic");
            }
        }

        [HttpGet("search-by-partitions")]
        public async Task<ActionResult<ConsumeTopicResponse[]>> SearchByPartitions([FromQuery] string topic, int partition)
        {
            try
            {
                var messages = _adminClientService.SearchByPartitions(topic, partition);
                return Ok(messages.Select(m => new ConsumeTopicResponse { Message = m.Message, Partition = m.Partition.Value, Offset = m.Offset.Value }));
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while consuming from topic");
            }
        }

        [HttpPost("produce-n-messages")]
        public async Task<ActionResult<string>> ProduceSpecificNumberOfMessage(int numberOfMessages, string topic)
        {
            try
            {
                await _adminClientService.ProduceRandomNumberOfMessages(numberOfMessages, topic);
                return Ok($"{numberOfMessages} messages were produced!");
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while producing messages");
            }
        }

        [HttpPost("produce-batch-messages")]
        public async Task<ActionResult<string>> PublishBatchMessages([FromBody]PublishBatchRequest batchRequest)
        {
            if (batchRequest.Key == null)
            {
                return base.StatusCode((int)HttpStatusCode.BadRequest, $"Key cannot be null");
            }
            try
            {
                await _adminClientService.BatchMessages(batchRequest.Topic, batchRequest.ListOfMessages, batchRequest.Key, batchRequest.Headers, batchRequest.partitionId);
                return Ok($"Messages were published!");
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while producing batch of messages");
            }
        }
        [HttpPost("publish-batch-messages-from-file")]
        public async Task<ActionResult<string>> PublishBatchMessagesFromFile([FromForm] Dictionary<string, string> headers, IFormFile formFile, string topic,
            string separator, string key, int partitionId)
        {
            if (key == null)
            {
                return base.StatusCode((int)HttpStatusCode.BadRequest, $"Key cannot be null");
            }
            try
            {
                await _adminClientService.BatchMessagesFromFile(topic, formFile, separator, key, headers, partitionId);
                return Ok($"Messages were published!");
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while producing batch of messages");
            }
        }


        [HttpPost("produce-message")]
        public async Task<ActionResult<string>> ProduceMessage(Message<string,string> message, string topic)
        {
            try
            {
                await _adminClientService.ProduceMessage(message, topic);
                return Ok("Message was produced!");
            }
            catch (Exception e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while producing messages");
            }
        }

        [HttpPost("set-address")]
        public IActionResult SetAddress([FromQuery] string address)
        {
            try
            {
                _adminClientService.SetAddress(address);

                return Ok();
            }
            catch (Exception ex)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"Error while accessing list of consumers: {ex.Message}");
            }


        }

        [HttpDelete("delete-topic")]
        public async Task<ActionResult<string>> DeleteTopic([FromQuery] string topicName)
        {

            try
            {
                await _adminClientService.DeleteTopic(topicName);
                return Ok("Topic was successfully deleted");
            }
            catch (DeleteTopicsException e)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while deleting topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
            }
        }
    }
}
