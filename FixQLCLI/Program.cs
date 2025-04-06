using FixQLLibrary;
using System.CommandLine;

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
        Console.WriteLine("Parameters:");
        foreach (var param in parameters)
        {
            Console.WriteLine($"{param.Key} = {param.Value}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, sqlOption);

var csCommand = new Command("cs", "Sanitize DB connection string");
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

root.AddCommand(queryCommand);
root.AddCommand(csCommand);

await root.InvokeAsync(args);
