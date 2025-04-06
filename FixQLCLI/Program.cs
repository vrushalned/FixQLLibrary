using FixQLLibrary;
using FixQLLibrary.Anomaly;
using System.CommandLine;
using Microsoft.Extensions.Configuration;
using FixQLLibrary.Paths;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

FixQLStorage.Initialize(config);

var root = new RootCommand("FixQL CLI - Secure your SQL queries");

var queryCommand = new Command("query", "Analyze and sanitize a SQL query");
var sqlOption = new Option<string>("--sql", "SQL query to analyze") { IsRequired = true };
queryCommand.AddOption(sqlOption);

queryCommand.SetHandler((string sql) =>
{
    try
    {
        var result = FixQL.SanitizeQuery(sql, null, out var parameters, out var detections);
        Console.WriteLine("Sanitized SQL:");
        Console.WriteLine(result);
        Console.WriteLine();

        if (parameters.Count > 0)
        {
            Console.WriteLine("Parameters:");
            foreach (var param in parameters)
            {
                Console.WriteLine($"{param.Key} = {param.Value}");
            }
        }

        if (detections.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Detections:");
            foreach (var d in detections)
                Console.WriteLine($"- {d}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, sqlOption);

var csCommand = new Command("cs", "Sanitize a DB connection string");
var csOption = new Option<string>("--string", "The raw connection string") { IsRequired = true };
csCommand.AddOption(csOption);

csCommand.SetHandler((string cs) =>
{
    try
    {
        var sanitized = FixQL.SanitizeConnectionString(cs);
        Console.WriteLine("Sanitized connection string:");
        Console.WriteLine(sanitized);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, csOption);

var approveCommand = new Command("approve", "Approve a fingerprint so it's no longer blocked");
var fpOption = new Option<string>("--fingerprint", "The fingerprint to approve") { IsRequired = true };
approveCommand.AddOption(fpOption);

approveCommand.SetHandler((string fingerprint) =>
{
    ApprovedQueryStore.Approve(fingerprint);
    Console.WriteLine($"Approved fingerprint: {fingerprint}");
}, fpOption);

var logCommand = new Command("log", "View fingerprint anomaly log");

logCommand.SetHandler(() =>
{
    var logs = AnomalyLogStore.GetAll()
        .OrderByDescending(l => l.LastSeen)
        .Take(50);

    foreach (var entry in logs)
    {
        var isApproved = ApprovedQueryStore.IsApproved(entry.Fingerprint);

        Console.WriteLine("----------------------------------------");
        Console.WriteLine($"Fingerprint: {entry.Fingerprint}");
        Console.WriteLine($"Count: {entry.Count}");
        Console.WriteLine($"First Seen: {entry.FirstSeen}");
        Console.WriteLine($"Last Seen : {entry.LastSeen}");
        Console.WriteLine($"Approved: {isApproved}");
        Console.WriteLine($"Detections: {string.Join(", ", entry.Detections)}");
        Console.WriteLine($"Sample Query: {entry.SampleQuery}");
    }
});

root.AddCommand(queryCommand);
root.AddCommand(csCommand);
root.AddCommand(approveCommand);
root.AddCommand(logCommand);

await root.InvokeAsync(args);
