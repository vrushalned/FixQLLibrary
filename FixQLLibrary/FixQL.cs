using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Data.Common;


namespace FixQLLibrary
{
    public class FixQL
    {
        public static string SanitizeQuery(string sql, object dbContextOrDapperType, out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();
            TSqlParser parser = new TSql150Parser(true);
            IList<ParseError> errors;
            
            TSqlFragment sqlFragment = parser.Parse(new System.IO.StringReader(sql), out errors);

            
            if (sqlFragment is TSqlScript script && script.Batches.Count > 1)
            {
                throw new InvalidOperationException("Multiple SQL statements are not allowed.");
            }

            if (errors.Count > 0)
                throw new InvalidOperationException("SQL parse failed.");
            if (sql.Contains("--") || sql.Contains("/*"))
            {
                throw new InvalidOperationException("SQL comments are not allowed.");
            }

            SanitizeTableAndColumns(sqlFragment, dbContextOrDapperType);
            ApplySecurityVisitors(sqlFragment, parameters);
            return GetSqlFromFragment(sqlFragment);
        }

        private static void SanitizeTableAndColumns(TSqlFragment fragment, object dbContextOrDapperType)
        {
            fragment.Accept(new TableNameVisitor(dbContextOrDapperType));
            fragment.Accept(new ColumnNameVisitor(dbContextOrDapperType));
        }

        private static void ApplySecurityVisitors(TSqlFragment fragment, Dictionary<string, object> parameters)
        {
            fragment.Accept(new TautologyVisitor());
            fragment.Accept(new FunctionCallVisitor());
            fragment.Accept(new ExecuteStatementVisitor());
            fragment.Accept(new UnionVisitor());
            fragment.Accept(new WaitForVisitor());
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
            dict["TrustServerCertificate"] = "False";

            if (dict.TryGetValue("User Id", out var user) && user.Equals("sa", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Do not use the 'sa' account.");

            if (!dict.ContainsKey("Connect Timeout"))
                dict["Connect Timeout"] = "30";

            return string.Join(";", dict.Select(kv => $"{kv.Key}={kv.Value}"));
        }

        public static DbCommand GetSanitizedCommand(DbCommand command, string sql, object dbContextOrDapperType, out Dictionary<string, object> parameters)
        {
            string sanitizedSql = SanitizeQuery(sql, dbContextOrDapperType, out parameters);
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

