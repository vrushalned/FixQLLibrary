using System.Collections.Concurrent;

namespace FixQLLibrary.Anomaly
{
    public static class InMemoryQueryAnomalyTracker
    {
        private static readonly ConcurrentDictionary<string, int> Fingerprints = new();

        public static bool IsNewFingerprint(string fingerprint, out int count)
        {
            count = Fingerprints.AddOrUpdate(fingerprint, 1, (_, existing) => existing + 1);
            return count == 1;
        }

        public static void Reset() => Fingerprints.Clear();

        public static IEnumerable<(string Fingerprint, int Count)> GetAll()
        {
            return Fingerprints.Select(kvp => (kvp.Key, kvp.Value));
        }
    }
}
