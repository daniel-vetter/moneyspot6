using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var hbciAdapter = builder
    .AddDockerfile("HBCI-Adapter", "../../hbci-adapter", "../backend/MoneySpot6.AppHost/hbci-adapter.dockerfile")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("hbci-adapter");

var backend = builder
    .AddProject<Projects.MoneySpot6_WebApp>("Backend")
    .WaitFor(hbciAdapter);

var frontend = builder
    .AddNpmApp("Frontend", "../../frontend")
    .WithHttpEndpoint(name: "http", port: 4200)
    .WithHttpHealthCheck("/")
    .WithReference(backend)
    .WaitFor(backend);

frontend.WithArgs(context =>
 {
     context.Args.Add("--");
     context.Args.Add("--port");
     context.Args.Add(frontend.GetEndpoint("http").Property(EndpointProperty.TargetPort));
 });

var dbProvider = builder.Configuration.GetSection("DB_PROVIDER").Get<string>();
if (dbProvider == "postgres")
{
    var postgresPassword = builder.AddParameter("postgres-password", true);
    var postgres = builder
        .AddPostgres("Postgres", password: postgresPassword)
        .WithLifetime(ContainerLifetime.Persistent);

    var db = postgres.AddDatabase("db", "moneyspot");

    backend
        .WithReference(db, "db")
        .WaitFor(postgres);
}
else if (dbProvider == "sqlite")
{
    var db = builder.AddConnectionString("db", x => x.AppendLiteral($"Data Source={Path.Combine(AppContext.BaseDirectory, "data", "data.db")}"));
    backend
        .WithReference(db, "db");
}
else
    throw new Exception("Unknown DB_PROVIDER: " + dbProvider);

builder.Build().Run();
