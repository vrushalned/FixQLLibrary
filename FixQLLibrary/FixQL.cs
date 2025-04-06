using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Data.Common;
using System.Text.RegularExpressions;
using FixQLLibrary.Anomaly;

namespace FixQLLibrary
{
    public class FixQL
    {
        public static string SanitizeQuery(string sql, object dbContextOrDapperType, out Dictionary<string, object> parameters, out List<string> detections)
        {
            parameters = new Dictionary<string, object>();
            detections = new List<string>();
            string error = null;

            if (sql.Contains("--") || sql.Contains("/*"))
                detections.Add("Comment Detected");

            CheckForPiggyBacking(sql, detections);

            var parser = new TSql150Parser(true);
            IList<ParseError> errors;
            var sqlFragment = parser.Parse(new StringReader(sql), out errors);

            if (errors.Count > 0)
            {
                error = "SQL parse failed.";
                return "[BLOCKED]";
            }

            try
            {
                SanitizeTableAndColumns(sqlFragment, dbContextOrDapperType);
                ApplySecurityVisitors(sqlFragment, parameters, detections);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return "[BLOCKED]";
            }

            if (sqlFragment is TSqlScript script && script.Batches.Count > 1)
            {
                detections.Add("Multiple Statements");
                error = "Multiple SQL statements are not allowed.";
                return "[BLOCKED]";
            }

            var fingerprint = QueryAnomalyDetector.GetFingerprint(sqlFragment);

            if (!ApprovedQueryStore.IsApproved(fingerprint))
            {
                if (InMemoryQueryAnomalyTracker.IsNewFingerprint(fingerprint, out int count))
                {
                    detections.Add("Query Anomaly");
                    detections.Add($"Fingerprint: {fingerprint}");
                    detections.Add($"Seen Count: {count}");

                    AnomalyLogStore.Record(fingerprint, detections, sql);
                }

                if (count == 1)
                {
                    error = "Blocked: Unapproved anomalous query.";
                    return "[BLOCKED]";
                }
            }

            if (detections.Any(d =>
                d.Contains("Piggybacked") ||
                d.Contains("Multiple") ||
                d.Contains("DROP") ||
                d.Contains("UPDATE") ||
                d.Contains("DELETE") ||
                d.Contains("EXEC")))
            {
                error = "Query blocked due to dangerous pattern.";
                return "[BLOCKED]";
            }

            return GetSqlFromFragment(sqlFragment);
        }

        private static void CheckForPiggyBacking(string sql, List<string> detections)
        {
            var cleanSql = sql
                .ToUpperInvariant()
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Replace(";", " ; ")
                .Replace("(", " ")
                .Replace(")", " ")
                .Replace("'", " ")
                .Replace("\"", " ")
                .Replace("=", " ")
                .Replace(",", " ")
                .Replace("--", " ")
                .Replace("/*", " ")
                .Replace("*/", " ");

            if (Regex.IsMatch(cleanSql, @"\bUPDATE\b")) detections.Add("UPDATE Detected");
            if (Regex.IsMatch(cleanSql, @"\bDROP\b")) detections.Add("DROP Detected");
            if (Regex.IsMatch(cleanSql, @"\bDELETE\b")) detections.Add("DELETE Detected");
            if (Regex.IsMatch(cleanSql, @"\bINSERT\b")) detections.Add("INSERT Detected");
            if (Regex.IsMatch(cleanSql, @"\bEXEC\b")) detections.Add("EXEC Detected");

            if (cleanSql.Contains(";"))
                detections.Add("Piggybacked Query");
        }

        private static void SanitizeTableAndColumns(TSqlFragment fragment, object dbContextOrDapperType)
        {
            fragment.Accept(new TableNameVisitor(dbContextOrDapperType));
            fragment.Accept(new ColumnNameVisitor(dbContextOrDapperType));
        }

        private static void ApplySecurityVisitors(TSqlFragment fragment, Dictionary<string, object> parameters, List<string> detections)
        {
            var tautologyVisitor = new TautologyVisitor();
            var unionVisitor = new UnionVisitor();
            var execVisitor = new ExecuteStatementVisitor();
            var waitVisitor = new WaitForVisitor();
            var updateVisitor = new UpdateStatementVisitor();
            var deleteVisitor = new DeleteStatementVisitor();
            var insertVisitor = new InsertStatementVisitor();
            var dropVisitor = new DropStatementVisitor();

            fragment.Accept(tautologyVisitor);
            fragment.Accept(unionVisitor);
            fragment.Accept(execVisitor);
            fragment.Accept(waitVisitor);
            fragment.Accept(updateVisitor);
            fragment.Accept(deleteVisitor);
            fragment.Accept(insertVisitor);
            fragment.Accept(dropVisitor);

            if (tautologyVisitor.Found) detections.Add("Tautology");
            if (unionVisitor.Found) detections.Add("UNION");
            if (execVisitor.Found) detections.Add("EXEC or xp_cmdshell");
            if (waitVisitor.Found) detections.Add("WAITFOR DELAY");
            if (updateVisitor.Found) detections.Add("UPDATE Statement");
            if (deleteVisitor.Found) detections.Add("DELETE Statement");
            if (insertVisitor.Found) detections.Add("INSERT Statement");
            if (dropVisitor.Found) detections.Add("DROP Statement");

            fragment.Accept(new VulnerableJoinVisitor());
            fragment.Accept(new ValueParameterizer(parameters, ""));
        }

        public static string SanitizeConnectionString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException("Connection string is empty");

            var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;
                dict[kv[0].Trim()] = kv[1].Trim();
            }

            dict["Encrypt"] = "True";
            dict["TrustServerCertificate"] = "True";

            if (dict.TryGetValue("User Id", out var user) && user.Equals("sa", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Do not use the 'sa' account.");

            if (!dict.ContainsKey("Connect Timeout"))
                dict["Connect Timeout"] = "30";

            return string.Join(";", dict.Select(kv => $"{kv.Key}={kv.Value}"));
        }

        public static DbCommand GetSanitizedCommand(DbCommand command, string sql, object dbContextOrDapperType, out Dictionary<string, object> parameters, out List<string> detections)
        {
            var sanitizedSql = SanitizeQuery(sql, dbContextOrDapperType, out parameters, out detections);
            command.CommandText = sanitizedSql;

            foreach (var parameter in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = parameter.Key;
                dbParam.Value = parameter.Value;
                command.Parameters.Add(dbParam);
            }

            return command;
        }

        private static string GetSqlFromFragment(TSqlFragment fragment)
        {
            var generator = new Sql150ScriptGenerator();
            generator.GenerateScript(fragment, out string sql);
            return sql;
        }
    }
}
