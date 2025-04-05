using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;

namespace FixQLLibrary
{
    public class ValueParameterizer : TSqlFragmentVisitor
    {
        private readonly Dictionary<string, object> _parameters;
        private int _parameterCounter;

        public ValueParameterizer(Dictionary<string, object> parameters, string originalSql)
        {
            _parameters = parameters;
            _parameterCounter = 0;
        }

        public override void Visit(BooleanComparisonExpression node)
        {
            if (node.SecondExpression is StringLiteral strLiteral)
            {
                ReplaceWithVariable(node, strLiteral.Value);
            }
            else if (node.SecondExpression is IntegerLiteral intLiteral)
            {
                ReplaceWithVariable(node, int.Parse(intLiteral.Value));
            }
            else if (node.SecondExpression is RealLiteral realLiteral)
            {
                ReplaceWithVariable(node, double.Parse(realLiteral.Value));
            }
            else if (node.SecondExpression is NullLiteral)
            {
                ReplaceWithVariable(node, null);
            }

            base.Visit(node); 
        }

        private void ReplaceWithVariable(BooleanComparisonExpression node, object value)
        {
            string paramName = $"@param{_parameterCounter++}";
            _parameters[paramName] = value;

            var variable = new VariableReference { Name = paramName };
            node.SecondExpression = variable;
        }
    }
}
