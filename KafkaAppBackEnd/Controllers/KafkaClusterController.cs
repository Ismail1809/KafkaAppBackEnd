using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KafkaAppBackEnd.DbContent;
using KafkaAppBackEnd.Models;
using KafkaAppBackEnd.Contracts;
using KafkaAppBackEnd.Services;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel.DataAnnotations;
using System.Net;
using AutoMapper;
using Confluent.Kafka.Admin;

namespace KafkaAppBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KafkaClusterController : ControllerBase
    {
        private readonly IClusterService _clusterService;
        private readonly IMapper _mapper;

        public KafkaClusterController(IClusterService clusterService, IMapper mapper)
        {
            _clusterService = clusterService;
            _mapper = mapper;
        }

        [HttpGet("get-cluster-info")]
        public async Task<ActionResult<List<DescribeConfigsResult>>> GetClusterInfo()
        {
            var clusterResult = await _clusterService.GetClusterConfig();

            if (clusterResult == null)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, "Can't get info");
            }

            return Ok(clusterResult);
        }

        [HttpGet("get-connection")]
        public async Task<ActionResult<Connection>> GetConnection([FromQuery]int id)
        {
            var connection = await _clusterService.GetConnection(id);

            if (connection == null)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, "Can't find");
            }

            return Ok(connection);
        }

        [HttpGet("get-connections")]
        public async Task<ActionResult<IEnumerable<Connection>>> GetConnections()
        {
            var connections = await _clusterService.GetConnections();

            if (connections == null)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, "List of connections is null!");
            }

            return Ok(connections);

        }

        [HttpPut("update-connection")]
        public async Task<ActionResult<UpdateConnectionRequest>> UpdateConnection([FromBody] UpdateConnectionRequest connection)
        {
            var existingConnection = await _clusterService.GetConnection(connection.ConnectionId);

            if (existingConnection == null || connection == null)
            {
                return NoContent();
            }

            try
            {
                await _clusterService.UpdateConnection(connection.ConnectionId, connection);
                return Ok(connection);
            }
            catch (DbUpdateConcurrencyException)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, "Error while updating connection");
            }
        }
       
        [HttpPost("create-connection")]
        public async Task<ActionResult<Connection>> CreateConnection([FromBody]CreateConnectionRequest connection)
        {
            if((connection.ConnectionName == null || connection.ConnectionName == "")|| (connection.BootStrapServer == null || connection.BootStrapServer == "")){
                return base.StatusCode((int)HttpStatusCode.BadRequest, "Either connection name or bootstrap server is empty");
            }

            var newConnection = await _clusterService.PostConnection(connection);
            return Ok(newConnection);
        }

        [HttpGet("check-connection")]
        public IActionResult CheckConnection([FromQuery] string address)
        {
            try
            {
                _clusterService.CheckConnection(address);
                return Ok("Connection is stable");
            }
            catch (Exception ex) 
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"Cannot connect to {address}");
            }
        }

        [HttpPost("set-connection")]
        public IActionResult SetConnection([FromQuery] string address)
        {
            try
            {
                _clusterService.SetAddress(address);
                return Ok($"Connected to {address}");
            }
            catch (Exception ex)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"Cannot connect to {address}");
            }
        }

        [HttpDelete("delete-connection")]
        public async Task<IActionResult> DeleteConnection([FromQuery] int id)
        {
            Connection? connection = await _clusterService.GetConnection(id);
            if (connection == null)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, "No such connection");
            }

            try
            {
                await _clusterService.DeleteConnection(id);

                return Ok();
            }
            catch (ValidationException ex)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, $"Unable to delete connection: {ex.Message}");
            }
        }
    }
}
