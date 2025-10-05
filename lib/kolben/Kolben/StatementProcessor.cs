using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kolben
{
    internal class StatementProcessor
    {
        internal static readonly string[] ControlStatements = { "if", "else", "else if", "while", "for", "function", "class", "try", "catch", "finally", "async" };

        private const string ObjectDiscoverToken = "+*-/&|<>.[(;";

        internal static ScriptStatement[] GetStatements(ScriptProcessor processor, string code)
        {
            var statements = new List<ScriptStatement>();

            var statement = new StringBuilder();

            var index = 0;
            var depth = 0;
            var lineNumber = 1;
            var lineNumberBuffer = 0; // buffers line counts from the start of a statement.
            var isComment = false;
            var isControlStatement = false; // If the current statement is a control statement.
            var isCompoundStatement = false; // If the current statement is bunch of statements wrapped in { ... }

            StringEscapeHelper escaper = new LeftToRightStringEscapeHelper(code, 0);

            while (index < code.Length)
            {
                var t = code[index];

                if (!isComment)
                    escaper.CheckStartAt(index);
                else
                    escaper.JumpTo(index);

                if (!escaper.IsString)
                {
                    // Check if a block comment is starting (/*):
                    if (!isComment && t == '/' && index + 1 < code.Length && code[index + 1] == '*')
                    {
                        isComment = true;
                        index++; // Jump over * char.
                    }

                    if (!isComment)
                    {
                        // Check if a line comment is starting (//):
                        if (t == '/' && index + 1 < code.Length && code[index + 1] == '/')
                        {
                            // We jump to the end of the line and ignore everything between the current index and the end of the line:
                            if (code.IndexOf("\n", index + 1, StringComparison.Ordinal) > -1)
                                index = code.IndexOf("\n", index + 1, StringComparison.Ordinal) + 1;
                            else
                                index = code.Length;

                            continue;
                        }

                        statement.Append(t);

                        if (t == '(')
                        {
                            depth++;
                        }
                        else if (t == ')')
                        {
                            depth--;

                            if (isControlStatement)
                            {
                                var statementStr = statement.ToString();
                                var s = statementStr.Trim();
                                if (s.StartsWith("if") || s.StartsWith("else if") || s.StartsWith("function") || s.StartsWith("for") || s.StartsWith("while") || s.StartsWith("catch"))
                                {
                                    var extraLines = statementStr.Replace("\r", "").TakeWhile(c => c == '\n').Count(); // count the starting lines
                                    statements.Add(new ScriptStatement(s, GetStatementType(s, true), lineNumber + extraLines));
                                    statement.Clear();
                                    lineNumber += lineNumberBuffer;
                                    lineNumberBuffer = 0;

                                    isControlStatement = false;
                                }
                            }
                        }
                        else if (t == '{')
                        {
                            depth++;

                            if (depth == 1)
                            {
                                var statementStr = statement.ToString();
                                var s = statementStr.Trim();

                                if (isControlStatement)
                                {
                                    var extraLines = statementStr.Replace("\r", "").TakeWhile(c => c == '\n').Count(); // count the starting lines

                                    s = s.Remove(s.Length - 1, 1).Trim();
                                    statements.Add(new ScriptStatement(s, GetStatementType(s, true), lineNumber + extraLines));

                                    lineNumber += lineNumberBuffer;
                                    lineNumberBuffer = 0;

                                    statement.Clear();
                                    statement.Append('{');
                                    isCompoundStatement = true;
                                    isControlStatement = false;
                                }
                                else
                                {
                                    if (s == "{")
                                    {
                                        isCompoundStatement = true;
                                    }
                                }
                            }
                        }
                        else if (t == '}')
                        {
                            depth--;
                            if (depth == 0 && isCompoundStatement)
                            {
                                // This could also be an object declaration...
                                // In the case that the statement started with "{" (example statement: {} + []), this will happen.
                                // To check if this is in fact an object, we look right and see if there is:
                                //   - an operator => object ("+*-/&|<>.[(")
                                //   - nothing => statement

                                var foundOperator = false;
                                var charFindIndex = index + 1;

                                while (!foundOperator && charFindIndex < code.Length)
                                {
                                    var testChar = code[charFindIndex];
                                    if (ObjectDiscoverToken.Contains(testChar))
                                    {
                                        if (testChar == '/' && // next statement is actually a comment, not a / operator (followed by / or *)
                                            charFindIndex + 1 < code.Length && 
                                            (code[charFindIndex + 1] == '/' || code[charFindIndex + 1] == '*'))
                                            charFindIndex = code.Length;
                                        else
                                            foundOperator = true;
                                    }
                                    else if (!char.IsWhiteSpace(testChar)) // We found something that is not an operator or whitespace, so this is the end of a compound statement.
                                    {
                                        charFindIndex = code.Length;
                                    }

                                    charFindIndex++;
                                }

                                if (!foundOperator)
                                {
                                    var statementStr = statement.ToString();
                                    var extraLines = statementStr.Replace("\r", "").TakeWhile(c => c == '\n').Count(); // count the starting lines
                                    var s = statementStr.Trim();
                                    statements.Add(new ScriptStatement(s, StatementType.Executable, lineNumber + extraLines) { IsCompoundStatement = true });
                                    statement.Clear();
                                    lineNumber += lineNumberBuffer;
                                    lineNumberBuffer = 0;
                                }

                                isCompoundStatement = false;
                            }
                        }
                        else if (t == ';' && depth == 0)
                        {
                            var statementStr = statement.ToString();
                            var extraLines = statementStr.Replace("\r", "").TakeWhile(c => c == '\n').Count(); // count the starting lines
                            var s = statementStr.Trim().TrimEnd(';');
                            statements.Add(new ScriptStatement(s, GetStatementType(s, false), lineNumber + extraLines));
                            statement.Clear();
                            lineNumber += lineNumberBuffer;
                            lineNumberBuffer = 0;
                        }
                        else if (!isCompoundStatement && !isControlStatement)
                        {
                            var statementStr = statement.ToString();
                            var extraLines = statementStr.Replace("\r", "").TakeWhile(c => c == '\n').Count(); // count the starting lines
                            var s = statementStr.TrimStart();

                            var nextChar = 'X'; // Set to something that is not matching with the condition below.
                            if (code.Length > index + 1)
                                nextChar = code[index + 1];

                            // Check if it's actually a control statement by looking if the next char matches (whitespace, ";" or "(")
                            if ((char.IsWhiteSpace(nextChar) || nextChar == ';' || nextChar == '(') && ControlStatements.Contains(s))
                            {
                                isControlStatement = true;
                                if (s.StartsWith("else"))
                                {
                                    if (index + 3 < code.Length)
                                    {
                                        var check = code.Substring(index + 1, 3);
                                        if (check != " if")
                                        {
                                            statements.Add(new ScriptStatement("else", StatementType.Else, lineNumber + extraLines));
                                            statement.Clear();
                                            isControlStatement = false;
                                            lineNumber += lineNumberBuffer;
                                            lineNumberBuffer = 0;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        // Check if a block comment is ending (/*):
                        if (t == '*' && index + 1 < code.Length && code[index + 1] == '/')
                        {
                            isComment = false;
                            index++; // Jump over / char.
                        }
                    }
                }
                else
                {
                    statement.Append(t);
                }

                if (t == '\n')
                    lineNumberBuffer++;

                index++;
            }

            if (isCompoundStatement)
                processor.ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingEndOfCompoundStatement);

            if (isComment)
                processor.ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxUnterminatedComment);

            if (isControlStatement)
                processor.ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");

            // an executable statement not closed with ";" is getting added here:
            var leftOver = statement.ToString().Trim();
            if (leftOver.Length > 0)
            {
                statements.Add(new ScriptStatement(leftOver, GetStatementType(leftOver, false), lineNumber));
            }

            return statements.ToArray();
        }

        /// <summary>
        /// Returns the correct statement type for an expression.
        /// </summary>
        internal static StatementType GetStatementType(string code, bool isControlStatement)
        {
            if (isControlStatement)
            {
                if (code.StartsWith("if"))
                {
                    return StatementType.If;
                }
                else if (code.StartsWith("else if"))
                {
                    return StatementType.ElseIf;
                }
                else if (code.StartsWith("else"))
                {
                    return StatementType.Else;
                }
                else if (code.StartsWith("while"))
                {
                    return StatementType.While;
                }
                else if (code.StartsWith("for"))
                {
                    return StatementType.For;
                }
                else if (code.StartsWith("function"))
                {
                    return StatementType.Function;
                }
                else if (code.StartsWith("class"))
                {
                    return StatementType.Class;
                }
                else if (code.StartsWith("try"))
                {
                    return StatementType.Try;
                }
                else if (code.StartsWith("catch"))
                {
                    return StatementType.Catch;
                }
                else if (code.StartsWith("finally"))
                {
                    return StatementType.Finally;
                }
                else if (code.StartsWith("async"))
                {
                    return StatementType.Async;
                }
            }
            else
            {
                if (code.StartsWith("var "))
                {
                    return StatementType.Var;
                }
                else if (code.StartsWith("import "))
                {
                    return StatementType.Import;
                }
                else if (code.StartsWith("link "))
                {
                    return StatementType.Link;
                }
                else if (code.StartsWith("return ") || code == "return")
                {
                    return StatementType.Return;
                }
                else if (code == "continue")
                {
                    return StatementType.Continue;
                }
                else if (code == "break")
                {
                    return StatementType.Break;
                }
                else if (code.StartsWith("throw"))
                {
                    return StatementType.Throw;
                }
                else if (IsAssignmentStatement(code))
                {
                    return StatementType.Assignment;
                }
                else
                {
                    return StatementType.Executable;
                }
            }
            return StatementType.Executable;
        }

        /// <summary>
        /// Returns if the expression is an assignment statement.
        /// </summary>
        private static bool IsAssignmentStatement(string code)
        {
            if (StringEscapeHelper.ContainsWithoutStrings(code, "="))
            {
                // Replace "=" that are not the assignment operator with placeholders:
                code = code.Replace("===", "---");
                code = code.Replace("!==", "---");
                code = code.Replace("==", "--");
                code = code.Replace("!=", "--");
                code = code.Replace("=>", "--");
                code = code.Replace("<=", "--");
                code = code.Replace(">=", "--");

                return StringEscapeHelper.ContainsWithoutStrings(code, "=");
            }
            else
            {
                return false;
            }
        }
    }
}
