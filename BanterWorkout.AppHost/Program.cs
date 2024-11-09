var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.BanterWorkout_ApiService>("apiservice");

builder.AddProject<Projects.BanterWorkout_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
