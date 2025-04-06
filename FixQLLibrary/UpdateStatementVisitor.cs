using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class UpdateStatementVisitor : TSqlFragmentVisitor
    {
        public bool Found { get; private set; } = false;

        public override void Visit(UpdateStatement node)
        {
            Found = true;
        }
    }
}
