using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class UnionVisitor : TSqlFragmentVisitor
    {
        public override void Visit(BinaryQueryExpression node)
        {
            if (node.BinaryQueryExpressionType == BinaryQueryExpressionType.Union)
            {
                throw new InvalidOperationException("UNION queries are not allowed.");
            }

            base.Visit(node);
        }
    }
}
