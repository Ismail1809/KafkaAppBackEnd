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

        [HttpGet("get-connection")]
        public async Task<ActionResult<Connection>> GetConnection(int id)
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
        public async Task<ActionResult<ConnectionRequest>> UpdateConnection(int id, ConnectionRequest connection)
        {
            var existingConnection = await _clusterService.GetConnection(id);

            if (existingConnection == null || connection == null)
            {
                return NoContent();
            }

            try
            {
                await _clusterService.UpdateConnection(id, connection);
                return Ok(connection);
            }
            catch (DbUpdateConcurrencyException)
            {
                return base.StatusCode((int)HttpStatusCode.InternalServerError, "Error while updating connection");
            }
        }
       
        [HttpPost("create-connection")]
        public async Task<ActionResult<Connection>> CreateConnection(ConnectionRequest connection)
        {
            var connections = await _clusterService.GetBootStrapServers();

            if(connections.Contains(connection.BootStrapServer)){
                return Ok("Connection already exist!");
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
