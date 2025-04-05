using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class WaitForVisitor : TSqlFragmentVisitor
    {
        public override void Visit(WaitForStatement node)
        {
            throw new InvalidOperationException("Use of WAITFOR is not allowed.");
        }
    }
}
