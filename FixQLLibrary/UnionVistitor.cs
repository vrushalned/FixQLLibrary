using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class UnionVisitor : TSqlFragmentVisitor
    {
        public bool Found { get; private set; } = false;

        public override void Visit(BinaryQueryExpression node)
        {
            if (node.BinaryQueryExpressionType == BinaryQueryExpressionType.Union)
            {
                Found = true;
            }

            base.Visit(node);
        }
    }
}
