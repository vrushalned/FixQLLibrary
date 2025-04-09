using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class FunctionCallVisitor : TSqlFragmentVisitor
    {
        private readonly HashSet<string> _dangerousFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "xp_cmdshell", "OPENROWSET", "sp_executesql", "sp_configure", "CHAR", "WAITFOR"
        };

        public bool Found { get; private set; } = false;

        public override void Visit(FunctionCall node)
        {
            string funcName = node.FunctionName?.Value?.Trim();

            if (!string.IsNullOrEmpty(funcName) && _dangerousFunctions.Contains(funcName))
            {
                Found = true;
            }
        }
    }
}
