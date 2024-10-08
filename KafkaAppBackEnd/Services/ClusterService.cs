﻿using AutoMapper;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Contracts;
using KafkaAppBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace KafkaAppBackEnd.Services
{
    public class ClusterService: IClusterService
    {
        private readonly IConnectionRepository _connectionRepository;
        private IAdminClientService _adminClientService;
        private IAdminClient _adminClient;
        private readonly IMapper _mapper;

        public ClusterService(IConnectionRepository connectionRepository, IAdminClientService adminClientService, IMapper mapper, IAdminClient adminClient)
        {
            _connectionRepository = connectionRepository;
            _adminClientService = adminClientService;
            _mapper = mapper;
            _adminClient = adminClient;
        }

        public async Task<List<DescribeConfigsResult>> GetClusterConfig()
        {
            var clusterInfo = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));

            var resource = new ConfigResource
            {
                Type = ResourceType.Broker,
            };

            resource.Name = clusterInfo.OriginatingBrokerId.ToString();
            var resultsConf = await _adminClient.DescribeConfigsAsync(new List<ConfigResource> { resource });

            return resultsConf;

        }

        public async Task<IEnumerable<Connection>> GetConnections()
        {
            var connections = await _connectionRepository.GetAllAsync();

            return connections;

        }
        public async Task<Connection?> GetConnection(int id)
        {
            var connection = await _connectionRepository.GetById(id);

            return connection;
        }

        public async Task<IEnumerable<string?>> GetBootStrapServers()
        {
            var connections = await _connectionRepository.GetAllAsync();

            return connections.Select(c => c.BootStrapServer);
        }

        public async Task UpdateConnection(int id, UpdateConnectionRequest connection)
        {
            Connection? existingConnection = await _connectionRepository.GetById(id);

            _mapper.Map(connection, existingConnection);

            await _connectionRepository.UpdateAsync(existingConnection);
        }

        public async Task<Connection> PostConnection(CreateConnectionRequest connection)
        {
            var newConnection = await _connectionRepository.AddAsync(_mapper.Map<Connection>(connection));

            return newConnection;
        }

        public void CheckConnection(string address)
        {
            var x = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = address }).Build();
            var metadata = x.GetMetadata(TimeSpan.FromSeconds(10));
        }

        public void SetAddress(string address)
        {
            _adminClientService.SetAddress(address);
            CheckConnection(address);
        }

        public async Task DeleteConnection(int id)
        {
            await _connectionRepository.DeleteAsync(id);
        }

    }
}
