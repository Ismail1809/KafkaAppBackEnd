using Microsoft.EntityFrameworkCore;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Controllers;
using NLog.Web;
using NLog;
using KafkaAppBackEnd.Services;
using KafkaAppBackEnd.DbContent;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using KafkaAppBackEnd.Contracts;
using KafkaAppBackEnd.Repositories;
using KafkaAppBackEnd.Mappers;
using KafkaAppBackEnd.Extensions;
using System;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Text;
using System.Net.Security;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

var adminConfig = new AdminClientConfig()
{
    BootstrapServers = "localhost: 9092"
};

var consumerConfig = new ConsumerConfig
{
    BootstrapServers = "localhost: 9092",
    GroupId = "order-reader",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoOffsetStore = false,
    EnableAutoCommit = false
};

var producerConfig = new ProducerConfig
{
    BootstrapServers = "localhost:9092",
    ClientId = "order-producer"
};

var builder = WebApplication.CreateBuilder(args);

var corsSettings = builder.Configuration.GetSection("CorsSettings");

// NLog: Setup NLog for Dependency injection
builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsSettings.GetSection("AllowedOrigins").Get<string[]>()) // take from appsettings.json
              .WithMethods(corsSettings.GetSection("AllowedMethods").Get<string[]>())
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();
var Configuration = builder.Configuration;
builder.Services.AddDbContext<DatabaseContext>(option => option.UseNpgsql(Configuration.GetConnectionString("Testdb")));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(new AdminClientBuilder(adminConfig).Build());
builder.Services.AddSingleton<IAdminClientService, AdminClientService>();
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<IClusterService, ClusterService>();
builder.Services.AddSingleton(new ProducerBuilder<string, string>(producerConfig).Build());
builder.Services.AddSingleton(new ConsumerBuilder<string, string>(consumerConfig).Build());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.ApplyMigrations();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
