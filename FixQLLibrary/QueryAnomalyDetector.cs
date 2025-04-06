using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public static class QueryAnomalyDetector
    {
        public static string GetFingerprint(TSqlFragment fragment)
        {
            var visitor = new QueryShapeVisitor();
            fragment.Accept(visitor);
            return visitor.Fingerprint.ToString().ToLowerInvariant();
        }
    }
}
