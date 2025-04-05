using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class TableNameVisitor : TSqlFragmentVisitor
    {
        private readonly object _dbContextOrDapperType;

        public TableNameVisitor(object dbContextOrDapperType)
        {
            _dbContextOrDapperType = dbContextOrDapperType;
        }

        public override void Visit(NamedTableReference node)
        {
            string tableName = node.SchemaObject.BaseIdentifier.Value;

            if (_dbContextOrDapperType is DbContext dbContext)
            {
                var allowedTables = dbContext.Model
                    .GetEntityTypes()
                    .Select(t => t.GetTableName())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!allowedTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                    throw new Exception($"Invalid table name: {tableName}");
            }
            else
            {
                var allowedTables = GetDapperTables(_dbContextOrDapperType);
                if (allowedTables != null && !allowedTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                    throw new Exception($"Invalid table name: {tableName}");
            }
        }

        private List<string> GetDapperTables(object dapperModel)
        {
            if (dapperModel == null) return null;

            return dapperModel.GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
                .ToList();
        }
    }
}
