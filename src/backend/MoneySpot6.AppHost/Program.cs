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

var frontend = builder
    .AddNpmApp("Frontend", "../../frontend")
    .WithHttpEndpoint(name: "http", port: 4200)
    .WithHttpHealthCheck("/")
    .WaitFor(backend);

frontend.WithArgs(context =>
 {
     context.Args.Add("--");
     context.Args.Add("--port");
     context.Args.Add(frontend.GetEndpoint("http").Property(EndpointProperty.TargetPort));
 });

builder.Build().Run();
