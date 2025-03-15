using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<WebApplication1>("TestAPI");

builder.Build().Run();
