var builder = DistributedApplication.CreateBuilder(args);

var parameter = builder.AddParameter("postgres-password", true);

var postgres = builder
    .AddPostgres("Postgres", port: 5432, password: parameter)
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("db", "moneyspot");
    
var hbciAdapter = builder
    .AddDockerfile("HBCI-Adapter", "../../hbci-adapter", "../backend/MoneySpot6.AppHost/hbci-adapter.dockerfile")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("hbci-adapter");

var backend = builder
    .AddProject<Projects.MoneySpot6_WebApp>("Backend")
    .WithReference(db)
    .WaitFor(hbciAdapter)
    .WaitFor(postgres);

builder
    .AddNpmApp("Frontend", "../../frontend")
    .WithEndpoint(4200, scheme: "http", isProxied: false)
    .WithHttpHealthCheck("/")
    .WaitFor(backend);

builder.Build().Run();
