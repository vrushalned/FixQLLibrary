using System.Text.Json;

namespace FixQLLibrary.Anomaly
{
    public static class ApprovedQueryStore
    {
        private static List<string> _cache = Load();

        public static bool IsApproved(string fingerprint) =>
            _cache.Contains(fingerprint);

        public static void Approve(string fingerprint)
        {
            if (!_cache.Contains(fingerprint))
            {
                _cache.Add(fingerprint);
                Save();
            }
        }

        public static List<string> GetAll() => _cache;

        private static string FilePath => Paths.FixQLStorage.ApprovedFile;

        private static List<string> Load()
        {
            if (!File.Exists(FilePath))
                return new();

            var raw = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<string>>(raw) ?? new();
        }

        private static void Save()
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
