using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text.RegularExpressions;

namespace FixQLLibrary
{
    public class ValueParameterizer : TSqlFragmentVisitor
    {
        private Dictionary<string, object> parameters;
        private int parameterCounter = 0;
        private string originalSql; 
        public ValueParameterizer(Dictionary<string, object> parameters, string originalSql) 
        {
            this.parameters = parameters;
            this.originalSql = originalSql; 
        }

        public override void Visit(StringLiteral node)
        {
            string paramName = $"@value{parameterCounter++}";
            string value = node.Value;
            Regex variableRegex = new Regex(@"' \+ (\w+)");
            Match match = variableRegex.Match(originalSql);
            if (match.Success)
            {
                paramName = "@" + match.Groups[1].Value;
            }

            parameters.Add(paramName, value);
            node.Value = paramName;
        }

    }

}
