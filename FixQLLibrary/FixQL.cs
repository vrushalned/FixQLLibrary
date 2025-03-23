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

            if (errors.Count > 0)
            {
                return sql;
            }

            SanitizeTableAndColumns(sqlFragment, dbContextOrDapperType);
            string sanitizedSql = ParameterizeValues(sqlFragment, parameters, sql); 

            return sanitizedSql;
        }

        private static void SanitizeTableAndColumns(TSqlFragment fragment, object dbContextOrDapperType)
        {
            var visitors = new List<TSqlFragmentVisitor>
        {
            new TableNameVisitor(dbContextOrDapperType),
            new ColumnNameVisitor(dbContextOrDapperType)
        };

            foreach (var visitor in visitors)
            {
                fragment.Accept(visitor);
            }
        }

        public static string SanitizeConnectionString(string connectionString)
        {
            if (!connectionString.ToLower().Contains("encrypt=true"))
            {
                connectionString += ";Encrypt=True";
            }
            return connectionString;
        }

        private static string ParameterizeValues(TSqlFragment fragment, Dictionary<string, object> parameters, string originalSql) 
        {
            var valueVisitor = new ValueParameterizer(parameters, originalSql); 
            fragment.Accept(valueVisitor);
            return fragment.ToString();
        }

        public static DbCommand GetSanitizedCommand(DbCommand command, string sql, object dbContextOrDapperType, out Dictionary<string, object> parameters)
        {
            string sanitizedSql = FixQL.SanitizeQuery(sql, dbContextOrDapperType, out parameters);
            command.CommandText = sanitizedSql;
            foreach (var parameter in parameters)
            {
                DbParameter dbParameter = command.CreateParameter();
                dbParameter.ParameterName = parameter.Key;
                dbParameter.Value = parameter.Value;
                command.Parameters.Add(dbParameter);
            }

            return command;
        }
    }
}
