using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class ColumnNameVisitor : TSqlFragmentVisitor
    {
        private readonly object _dbContextOrDapperType;

        public ColumnNameVisitor(object dbContextOrDapperType)
        {
            _dbContextOrDapperType = dbContextOrDapperType;
        }

        public override void Visit(ColumnReferenceExpression node)
        {
            if (node.MultiPartIdentifier.Identifiers.Count > 1)
            {
                string tableName = node.MultiPartIdentifier.Identifiers[0].Value;
                string columnName = node.MultiPartIdentifier.Identifiers[1].Value;

                if (_dbContextOrDapperType is DbContext dbContext)
                {
                    var entity = dbContext.Model.GetEntityTypes()
                        .FirstOrDefault(e => e.GetTableName() == tableName);

                    if (entity != null)
                    {
                        var allowedColumns = entity.GetProperties()
                            .Select(p => p.GetColumnName())
                            .ToList();

                        if (!allowedColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                            throw new Exception($"Invalid column '{columnName}' in table '{tableName}'");
                    }
                }
                else
                {
                    var allowedColumns = GetDapperColumns(_dbContextOrDapperType, tableName);
                    if (allowedColumns != null && !allowedColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                        throw new Exception($"Invalid column '{columnName}' in table '{tableName}'");
                }
            }
        }

        private List<string> GetDapperColumns(object dapperModel, string tableName)
        {
            if (dapperModel == null) return null;

            foreach (var prop in dapperModel.GetType().GetProperties())
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var type = prop.PropertyType.GetGenericArguments()[0];
                    if (type.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                        return type.GetProperties().Select(p => p.Name).ToList();
                }
            }

            return null;
        }
    }
}
