using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class ExecuteStatementVisitor : TSqlFragmentVisitor
    {
        private readonly string[] dangerousProcedures = new[]
        {
            "xp_cmdshell", "sp_configure", "sp_executesql", "xp_regread", "xp_regwrite"
        };
        public bool Found { get; private set; } = false;

        public override void Visit(ExecuteStatement node)
        {

            if (node.ExecuteSpecification.ExecutableEntity is ExecutableStringList)
            {
                Found = true;
            }

            if (node.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference execProc)
            {
                var procName = execProc.ProcedureReference.ProcedureReference.Name.BaseIdentifier.Value;

                foreach (var dangerousProc in dangerousProcedures)
                {
                    if (procName.Equals(dangerousProc, StringComparison.OrdinalIgnoreCase))
                    {
                        Found = true;
                    }
                }
            }

            base.Visit(node);
        }
    }
}
