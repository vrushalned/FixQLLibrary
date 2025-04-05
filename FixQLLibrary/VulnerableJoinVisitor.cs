using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class VulnerableJoinVisitor : TSqlFragmentVisitor
    {
        public override void Visit(QualifiedJoin node)
        {
            if (node.SearchCondition == null)
            {
                throw new InvalidOperationException("Join without ON clause is not allowed.");
            }

            if (IsTautology(node.SearchCondition))
            {
                throw new InvalidOperationException("Join condition is always true — possible Cartesian product.");
            }

            if (ContainsFunctionCall(node.SearchCondition))
            {
                throw new InvalidOperationException("Function calls in JOIN conditions are not allowed.");
            }

            base.Visit(node);
        }

        private bool IsTautology(BooleanExpression expr)
        {
            if (expr is BooleanComparisonExpression cmp)
            {
                if (cmp.FirstExpression is Literal && cmp.SecondExpression is Literal)
                    return true;

                if (cmp.FirstExpression is ColumnReferenceExpression left &&
                    cmp.SecondExpression is ColumnReferenceExpression right)
                {
                    return left.MultiPartIdentifier.ToString() == right.MultiPartIdentifier.ToString();
                }
            }

            return false;
        }

        private bool ContainsFunctionCall(BooleanExpression expr)
        {
            var functionFinder = new FunctionCallFinder();
            expr.Accept(functionFinder);
            return functionFinder.Found;
        }

        private class FunctionCallFinder : TSqlFragmentVisitor
        {
            public bool Found { get; private set; }

            public override void Visit(FunctionCall node)
            {
                Found = true;
            }
        }
    }
}
