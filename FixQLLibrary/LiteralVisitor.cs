using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace FixQLLibrary
{
    public class LiteralVisitor : TSqlFragmentVisitor
    {
        private readonly Dictionary<string, object> _parameters;
        private int _counter;

        public LiteralVisitor(Dictionary<string, object> parameters)
        {
            _parameters = parameters;
            _counter = 0;
        }

        private string AddParameter(object value)
        {
            string paramName = $"@param{_counter++}";
            _parameters[paramName] = value;
            return paramName;
        }

        public override void Visit(IntegerLiteral node)
        {
            var name = AddParameter(int.Parse(node.Value));
            node.Value = name;
        }

        public override void Visit(RealLiteral node)
        {
            var name = AddParameter(double.Parse(node.Value));
            node.Value = name;
        }

        public override void Visit(NullLiteral node)
        {
            var name = AddParameter(null);
            node.Value = name;
        }
    }
}
