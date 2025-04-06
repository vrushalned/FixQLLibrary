using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class QueryShapeVisitor : TSqlFragmentVisitor
    {
        public StringBuilder Fingerprint { get; } = new StringBuilder();

        public override void Visit(SelectStatement node)
        {
            Fingerprint.Append("select_");
        }

        public override void Visit(NamedTableReference node)
        {
            var table = node.SchemaObject.BaseIdentifier.Value.ToLowerInvariant();
            Fingerprint.Append("table_" + table + "_");
        }

        public override void Visit(ColumnReferenceExpression node)
        {
            var col = node.MultiPartIdentifier?.Identifiers.LastOrDefault()?.Value.ToLowerInvariant();
            Fingerprint.Append("col_" + col + "_");
        }

        public override void Visit(BooleanComparisonExpression node)
        {
            Fingerprint.Append("compare_" + node.ComparisonType.ToString().ToLowerInvariant() + "_");
        }

        public override void Visit(WhereClause node)
        {
            Fingerprint.Append("where_");
        }

        public override void Visit(UpdateStatement node)
        {
            Fingerprint.Append("update_");
        }

        public override void Visit(DeleteStatement node)
        {
            Fingerprint.Append("delete_");
        }

        public override void Visit(InsertStatement node)
        {
            Fingerprint.Append("insert_");
        }
    }
}
