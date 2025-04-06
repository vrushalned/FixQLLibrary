using System.Text.Json;
using System.Text.Json.Serialization;

namespace FixQLLibrary.Anomaly
{
    public static class AnomalyLogStore
    {
        private static readonly string FilePath = Paths.FixQLStorage.AnomalyLogFile;
        private static Dictionary<string, AnomalyLogEntry> _log = Load();

        public static void Record(string fingerprint, List<string> detections, string sql)
        {
            if (_log.TryGetValue(fingerprint, out var entry))
            {
                entry.Count++;
                entry.LastSeen = DateTime.UtcNow;

                foreach (var detection in detections)
                {
                    if (!entry.Detections.Contains(detection))
                        entry.Detections.Add(detection);
                }
            }
            else
            {
                entry = new AnomalyLogEntry
                {
                    Fingerprint = fingerprint,
                    Count = 1,
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    Detections = new List<string>(detections),
                    SampleQuery = sql,
                    Approved = ApprovedQueryStore.IsApproved(fingerprint)
                };
            }

            _log[fingerprint] = entry;
            Save();
        }

        private static Dictionary<string, AnomalyLogEntry> Load()
        {
            if (!File.Exists(FilePath))
                return new Dictionary<string, AnomalyLogEntry>();

            var raw = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Dictionary<string, AnomalyLogEntry>>(raw) ?? new();
        }

        private static void Save()
        {
            var json = JsonSerializer.Serialize(_log, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        public static AnomalyLogEntry? Get(string fingerprint)
        {
            _log.TryGetValue(fingerprint, out var entry);
            return entry;
        }

        public static IEnumerable<AnomalyLogEntry> GetAll() => _log.Values;
    }

    public class AnomalyLogEntry
    {
        public string Fingerprint { get; set; } = "";
        public int Count { get; set; } = 1;
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public List<string> Detections { get; set; } = new();
        public string SampleQuery { get; set; } = "";
        public bool Approved { get; set; }
    }
}
