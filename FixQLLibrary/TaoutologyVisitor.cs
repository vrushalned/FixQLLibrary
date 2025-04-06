using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class TautologyVisitor : TSqlFragmentVisitor
    {
        public bool Found { get; private set; } = false;
        public override void Visit(BooleanBinaryExpression node)
        {
            if (node.BinaryExpressionType == BooleanBinaryExpressionType.Or)
            {
                if (IsLiteralComparison(node.FirstExpression) || IsLiteralComparison(node.SecondExpression))
                {
                   Found = true;
                }
            }

            base.Visit(node);
        }

        private bool IsLiteralComparison(BooleanExpression expr)
        {
            if (expr is BooleanComparisonExpression cmp)
            {
                return cmp.FirstExpression is Literal && cmp.SecondExpression is Literal;
            }
            return false;
        }
    }
}
