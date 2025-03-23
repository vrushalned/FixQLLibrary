using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class ColumnNameVisitor : TSqlFragmentVisitor
    {
        private object dbContextOrDapperType;

        public ColumnNameVisitor(object dbContextOrDapperType)
        {
            this.dbContextOrDapperType = dbContextOrDapperType;
        }

        public override void Visit(ColumnReferenceExpression node)
        {
            if (node.MultiPartIdentifier.Identifiers.Count > 1)
            {
                string tableName = node.MultiPartIdentifier.Identifiers[0].Value;
                string columnName = node.MultiPartIdentifier.Identifiers[1].Value;

                if (dbContextOrDapperType is DbContext dbContext)
                {
                    var entityType = dbContext.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName() == tableName);
                    if (entityType != null)
                    {
                        var allowedColumns = entityType.GetProperties().Select(p => p.GetColumnName()).ToList();
                        if (!allowedColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                        {
                            throw new Exception($"Invalid column name: {columnName} in table {tableName}");
                        }
                    }
                }
                else
                {
                    
                    var allowedColumns = GetDapperColumns(dbContextOrDapperType, tableName);
                    if (allowedColumns != null && !allowedColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception($"Invalid column name: {columnName} in table {tableName}");
                    }
                }
            }
        }

        private List<string> GetDapperColumns(object dapperType, string tableName)
        {
            if (dapperType == null) return null;
            var properties = dapperType.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var genericArgument = property.PropertyType.GetGenericArguments()[0];
                    if (genericArgument.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        return genericArgument.GetProperties().Select(p => p.Name).ToList();
                    }
                }
            }
            return null;
        }
    }
}
