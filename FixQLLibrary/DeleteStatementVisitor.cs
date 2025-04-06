using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class DeleteStatementVisitor : TSqlFragmentVisitor
    {
        public bool Found { get; private set; } = false;

        public override void Visit(DeleteStatement node)
        {
            Found = true;
        }
    }
}
