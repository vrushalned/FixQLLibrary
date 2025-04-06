using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class DropStatementVisitor : TSqlFragmentVisitor
    {
        public bool Found { get; private set; } = false;

        public override void Visit(DropObjectsStatement node)
        {
            Found = true;
        }
    }
}
