using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class InsertStatementVisitor : TSqlFragmentVisitor
    {
        public bool Found { get; private set; } = false;

        public override void Visit(InsertStatement node)
        {
            Found = true;
        }
    }
}
