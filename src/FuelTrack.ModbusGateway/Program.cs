using FuelTrack.ModbusGateway;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ModbusPollingService>();

var host = builder.Build();
host.Run();
