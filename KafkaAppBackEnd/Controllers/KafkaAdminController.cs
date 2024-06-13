using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Models;
using KafkaAppBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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

        [HttpGet("get-topics")]
        public IActionResult GetTopics()
        {
            try
            {
                var listOfTopics = _adminClientService.GetTopics();

                if (listOfTopics == null)
                {
                    return base.Ok("List of topics is null");
                }
                return base.Ok(listOfTopics);
            }
            catch (ValidationException ex)
            {
                return base.BadRequest($"Error while accessing list of topics: {ex.Message}");
            }
        }

        [HttpGet("get-consumers")]
        public IActionResult GetConsumersGroup()
        {
            try
            {
                var consumerGroup = _adminClientService.GetConsumerGroup();

                if (consumerGroup == null)
                {
                    return Ok("List of consumers is null");
                }

                return Ok(consumerGroup);
            }
            catch (ValidationException ex) {
                return BadRequest($"Error while accessing list of consumers: {ex.Message}");
            }


        }

        [HttpPost("create-topic")]
        public async Task<IActionResult> CreateTopic([FromBody] TopicRequest topicRequest)
        {

            try
            {
                await _adminClientService.CreateTopic(topicRequest);
                return Ok("Topic was successfully created!");
            }
            catch (CreateTopicsException e)
            {
                return BadRequest($"An error occurred creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
            }
        }

        [HttpDelete("delete-topic")]
        public async Task<IActionResult> DeleteTopic([FromQuery] string topicName)
        {

            try
            {
                await _adminClientService.DeleteTopic(topicName);
                return Ok("Topic was successfully deleted");
            }
            catch (CreateTopicsException e)
            {
                return BadRequest($"An error occurred deleting topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
            }
        }
    }
}
