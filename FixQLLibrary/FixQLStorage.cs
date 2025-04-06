using Microsoft.Extensions.Configuration;

namespace FixQLLibrary.Paths
{
    public static class FixQLStorage
    {
        public static string RootPath { get; private set; }

        public static string AnomalyLogFile => Path.Combine(RootPath, "fixql_anomalies.json");
        public static string ApprovedFile => Path.Combine(RootPath, "approved_fingerprints.json");

        public static void Initialize(IConfiguration config)
        {
            var path = config["FixQL:StoragePath"];
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("FixQL:StoragePath not configured.");

            RootPath = Path.GetFullPath(path);
            Directory.CreateDirectory(RootPath);
        }
    }
}
