using System.Net;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks().AddSqlServer(builder.Configuration.GetConnectionString("Database"));
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();
app.UseHealthChecks("/healthz");
app.MapGet("/", async (IConfiguration configuration) =>
{
    using SqlConnection connection = new(configuration.GetConnectionString("Database"));
    using SqlCommand command = connection.CreateCommand();
    command.CommandText = "SELECT CLIENT_NET_ADDRESS FROM SYS.DM_EXEC_CONNECTIONS WHERE SESSION_ID = @@SPID";
    await connection.OpenAsync();
    object? result = await command.ExecuteScalarAsync();

    if (result is not string clientIpAddress)
    {
        return Results.NoContent();
    }

    string databaseHost = connection.DataSource.Split(':')[1].Split(',')[0];
    IPHostEntry hostEntry = await Dns.GetHostEntryAsync(databaseHost);
    string databaseIpAddress = string.Join(",", hostEntry.AddressList.Select(x => x.ToString()));
    
    return Results.Ok($"Client IP: {clientIpAddress}; Database IP: {databaseIpAddress}");
});

await app.RunAsync();
