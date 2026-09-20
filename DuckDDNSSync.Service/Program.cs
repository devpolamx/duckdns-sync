using DuckDDNSSync.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "DuckDDNSSync");
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
