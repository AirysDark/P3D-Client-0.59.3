using Kolben.Types;

namespace Kolben
{
    /// <summary>
    /// A statement for the <see cref="ScriptProcessor"/> to execute.
    /// </summary>
    internal class ScriptStatement
    {
        internal string Code { get; }
        internal StatementType StatementType { get; }
        internal int LineNumber { get; }

        internal SObject StatementResult;

        internal bool IsCompoundStatement { get; set; }

        internal ScriptStatement(string code)
        {
            Code = code.Trim();
            StatementType = StatementProcessor.GetStatementType(Code, false);
        }

        internal ScriptStatement(string code, StatementType statementType) : this(code, statementType, -1) { }

        internal ScriptStatement(string code, StatementType statementType, int lineNumber)
        {
            Code = code;
            StatementType = statementType;
            LineNumber = lineNumber;
        }
    }
}
