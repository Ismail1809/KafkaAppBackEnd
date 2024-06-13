using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaAppBackEnd.Controllers;
using KafkaAppBackEnd.Services;

var builder = WebApplication.CreateBuilder(args);

var adminConfig = new AdminClientConfig()
{
    BootstrapServers = "localhost: 9092"
};


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(new AdminClientBuilder(adminConfig).Build());
builder.Services.AddScoped<IAdminClientService, AdminClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
