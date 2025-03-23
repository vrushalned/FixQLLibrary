using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class TableNameVisitor : TSqlFragmentVisitor
    {
        private object dbContextOrDapperType;

        public TableNameVisitor(object dbContextOrDapperType)
        {
            this.dbContextOrDapperType = dbContextOrDapperType;
        }

        public override void Visit(NamedTableReference node)
        {
            string tableName = node.SchemaObject.BaseIdentifier.Value;

            if (dbContextOrDapperType is DbContext dbContext)
            {
                //var entityTypes = dbContext.Model.GetEntityTypes();
                //var allowedTables = entityTypes.Select(e => e.GetTableName()).ToList();
                var tableNames = dbContext.Model
                                    .GetEntityTypes()
                                    .Select(t => t.GetTableName())
                                    .Distinct()
                                    .ToList();

                if (!tableNames.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                {
                    throw new Exception($"Invalid table name: {tableName}");
                }
            }
            else
            {
                //Dapper Implementation
                var allowedTables = GetDapperTables(dbContextOrDapperType);
                if (allowedTables != null && !allowedTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                {
                    throw new Exception($"Invalid table name: {tableName}");
                }
            }

        }

        private List<string> GetDapperTables(object dapperType)
        {
            if (dapperType == null) return null;
            var properties = dapperType.GetType().GetProperties();
            List<string> tableNames = new List<string>();
            foreach (var property in properties)
            {
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var genericArgument = property.PropertyType.GetGenericArguments()[0];
                    tableNames.Add(genericArgument.Name);
                }
            }
            return tableNames;
        }
    }

}
