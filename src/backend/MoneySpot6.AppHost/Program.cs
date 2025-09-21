var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("Postgres")
    .WithPgWeb()
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
    .WithUrl("http://localhost:4200")
    .WaitFor(backend);

builder.Build().Run();
