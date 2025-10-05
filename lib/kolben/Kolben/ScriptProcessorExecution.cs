using Kolben.Adapters;
using Kolben.Types;
using Kolben.Types.Prototypes;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;

namespace Kolben
{
    partial class ScriptProcessor
    {
        internal SObject ExecuteStatement(ScriptStatement statement)
        {
            switch (statement.StatementType)
            {
                case StatementType.Executable:
                    return ExecuteExecutable(statement);
                case StatementType.If:
                    return ExecuteIf(statement);
                case StatementType.Else:
                    return ExecuteElse();
                case StatementType.ElseIf:
                    return ExecuteElseIf(statement);
                case StatementType.Import:
                    return ExecuteImport(statement);
                case StatementType.Var:
                    return ExecuteVar(statement);
                case StatementType.While:
                    return ExecuteWhile(statement);
                case StatementType.Return:
                    return ExecuteReturn(statement);
                case StatementType.Assignment:
                    return ExecuteAssignment(statement);
                case StatementType.For:
                    return ExecuteFor(statement);
                case StatementType.Function:
                    return ExecuteFunction(statement);
                case StatementType.Class:
                    return ExecuteClass(statement);
                case StatementType.Link:
                    return ExecuteLink(statement);
                case StatementType.Continue:
                    return ExecuteContinue();
                case StatementType.Break:
                    return ExecuteBreak();
                case StatementType.Throw:
                    return ExecuteThrow(statement);
                case StatementType.Try:
                    return ExecuteTry(statement);
                case StatementType.Catch:
                    return ExecuteCatch();
                case StatementType.Finally:
                    return ExecuteFinally();
                case StatementType.Async:
                    return ExecuteAsync(statement);
            }

            return null;
        }

        private SObject ExecuteAsync(ScriptStatement statement)
        {
            // statement inside async block gets executed in its own thread.
            // cannot rely on any variables from outside being set on time.

            string taskName = statement.Code.Remove(0, statement.Code.IndexOf("(", StringComparison.Ordinal) + 1);
            taskName = taskName.Remove(taskName.LastIndexOf(")", StringComparison.Ordinal)).Trim();

            if (string.IsNullOrWhiteSpace(taskName))
                taskName = Guid.NewGuid().ToString();

            _index++;
            var nextStatement = _statements[_index];

            var processor = new ScriptProcessor(Context, GetLineNumber());

            lock (Context.AsyncTasks)
                Context.AsyncTasks.Add(taskName);

            Console.WriteLine($"Start async task ({taskName})");

            Task.Run(() =>
            {
                processor.ExecuteStatement(nextStatement);

                lock (Context.AsyncTasks)
                    Context.AsyncTasks.Remove(taskName);
            });

            return Undefined;
        }

        private SObject ExecuteCatch()
        {
            // The script processor can only reach this, when a catch statement exists without a try statement:
            return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxCatchWithoutTry);
        }

        private SObject ExecuteFinally()
        {
            // The script processor can only reach this, when a finally statement exists without a try statement:
            return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxFinallyWithoutTry);
        }

        private SObject ExecuteTry(ScriptStatement statement)
        {
            var exp = statement.Code;

            if (exp != "try")
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingBeforeTry);

            _index++;
            if (_statements.Length > _index)
            {
                var executeStatement = _statements[_index];
                var encounteredError = false;
                var foundCatch = false;
                var foundFinally = false;
                var foundMatchingCatch = false;
                SObject errorObject = null;
                var returnObject = Undefined;

                try
                {
                    returnObject = ExecuteStatement(executeStatement);
                }
                catch (ScriptException ex)
                {
                    encounteredError = true;
                    errorObject = ex.ErrorObject;
                }

                if (encounteredError)
                {
                    var endedCatchSearch = false;
                    var findCatchIndex = _index + 1;
                    while (findCatchIndex < _statements.Length && !endedCatchSearch)
                    {
                        if (_statements[findCatchIndex].StatementType == StatementType.Catch)
                        {
                            _index = findCatchIndex + 1;

                            if (_statements.Length > _index)
                            {
                                if (!foundMatchingCatch)
                                {
                                    var catchExecuteStatement = _statements[_index];
                                    foundCatch = true;

                                    var catchCode = _statements[findCatchIndex].Code;
                                    var errorVarName = "";

                                    if (catchCode != "catch")
                                    {
                                        catchCode = catchCode.Remove(0, "catch".Length).Trim().Remove(0, 1);
                                        catchCode = catchCode.Remove(catchCode.Length - 1, 1);
                                        errorVarName = catchCode.Trim();
                                    }

                                    if (Regex.IsMatch(catchCode, RegexCatchcondition))
                                    {
                                        errorVarName = catchCode.Remove(catchCode.IndexOf(" ", StringComparison.Ordinal));
                                        var conditionCode = catchCode.Remove(0, catchCode.IndexOf("if", StringComparison.Ordinal) + 3);

                                        var processor = new ScriptProcessor(Context, GetLineNumber());
                                        processor.Context.AddVariable(errorVarName, errorObject);

                                        var conditionResult = processor.ExecuteStatement(new ScriptStatement(conditionCode));

                                        var conditionAsBool = conditionResult as SBool;
                                        foundMatchingCatch = conditionAsBool?.Value ?? conditionResult.ToBool(this).Value;

                                        if (foundMatchingCatch)
                                            returnObject = processor.ExecuteStatement(catchExecuteStatement);
                                    }
                                    else
                                    {
                                        foundMatchingCatch = true;
                                        var processor = new ScriptProcessor(Context, GetLineNumber());
                                        processor.Context.AddVariable(errorVarName, errorObject);
                                        returnObject = processor.ExecuteStatement(catchExecuteStatement);
                                    }
                                }
                            }
                            else
                            {
                                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
                            }
                        }
                        else
                        {
                            // end search if different statement type appears:
                            endedCatchSearch = true;
                        }

                        findCatchIndex += 2;
                    }
                }
                else
                {
                    var findCatchIndex = _index + 1;
                    while (findCatchIndex < _statements.Length)
                    {
                        if (_statements[findCatchIndex].StatementType == StatementType.Catch)
                        {
                            foundCatch = true;
                            _index = findCatchIndex + 1;
                        }
                        else
                        {
                            findCatchIndex = _statements.Length;
                        }

                        findCatchIndex += 2;
                    }
                }

                // if no matching catch was found when an error occurred, it was not caught: throw it!
                if (encounteredError && !foundMatchingCatch)
                    return ErrorHandler.ThrowError(errorObject);

                // now, try to find finally statement:
                var findFinallyIndex = _index + 1;
                while (findFinallyIndex < _statements.Length && !foundFinally)
                {
                    if (_statements[findFinallyIndex].StatementType == StatementType.Finally)
                    {
                        _index = findFinallyIndex + 1;

                        if (_statements.Length > _index)
                        {
                            var finallyExecuteStatement = _statements[_index];
                            foundFinally = true;

                            returnObject = ExecuteStatement(finallyExecuteStatement);
                        }
                        else
                        {
                            return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
                        }
                    }
                    else
                    {
                        findFinallyIndex = _statements.Length;
                    }

                    findFinallyIndex += 2;
                }

                if (!foundCatch && !foundFinally) // when no catch or finally block has been found, throw an error.
                    return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingCatchOrFinally);
                else
                    return returnObject;
            }
            else
            {
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
            }
        }

        private SObject ExecuteThrow(ScriptStatement statement)
        {
            var exp = statement.Code;

            if (exp == "throw")
            {
                return ErrorHandler.ThrowError(ErrorType.UserError, ErrorHandler.MessageUserError);
            }
            else
            {
                exp = exp.Remove(0, "throw ".Length).Trim();

                var errorObject = ExecuteStatement(new ScriptStatement(exp));

                // Set the line number if the object is an error object:
                if (errorObject.TypeOf() == SObject.LiteralTypeError)
                    ((SError)errorObject).SetMember(ErrorPrototype.MemberNameLine, CreateNumber(GetLineNumber()));

                return ErrorHandler.ThrowError(errorObject);
            }
        }

        private SObject ExecuteContinue()
        {
            _continueIssued = true;
            return Undefined;
        }

        private SObject ExecuteBreak()
        {
            _breakIssued = true;
            return Undefined;
        }

        private SObject ExecuteLink(ScriptStatement statement)
        {
            if (Context.HasCallback(CallbackType.ScriptPipeline))
            {
                var exp = statement.Code;
                exp = exp.Remove(0, "link ".Length).Trim();

                var callback = (DScriptPipeline)Context.GetCallback(CallbackType.ScriptPipeline);
                var task = Task<string>.Factory.StartNew(() => callback(this, exp));
                task.Wait();

                var code = task.Result;

                var statements = StatementProcessor.GetStatements(this, code);

                // Convert the current statements into a list, so we can modify them.
                var tempStatements = _statements.ToList();
                // Remove the "link" statement, because we don't want to step into it again if we are in a loop.
                tempStatements.RemoveAt(_index);

                // Insert class, using and link statements right after the current statement.
                var insertIndex = _index;

                for (var i = 0; i < statements.Length; i++)
                {
                    if (statements[i].StatementType == StatementType.Class)
                    {
                        // The class statement needs its body, so we add the class statement and the one afterwards:
                        if (statements.Length > i + 1)
                        {
                            tempStatements.Insert(insertIndex, statements[i]);
                            tempStatements.Insert(insertIndex + 1, statements[i + 1]);

                            insertIndex += 2;
                        }
                        else
                        {
                            ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
                        }
                    }
                    else if (statements[i].StatementType == StatementType.Import || statements[i].StatementType == StatementType.Link)
                    {
                        tempStatements.Insert(insertIndex, statements[i]);
                        insertIndex += 1;
                    }
                }

                // Convert the temp statement list back and reduce the index by one, because we deleted the current statement.
                _statements = tempStatements.ToArray();
                _index--;

                return Undefined;
            }
            else
            {
                return ErrorHandler.ThrowError(ErrorType.ApiError, ErrorHandler.MessageApiNotSupported);
            }
        }

        private SObject ExecuteClass(ScriptStatement statement)
        {
            var exp = statement.Code;

            // The function's body is the next statement:

            _index++;

            if (_index < _statements.Length)
            {
                var classBodyStatement = _statements[_index];

                if (classBodyStatement.IsCompoundStatement)
                {
                    exp += classBodyStatement.Code;

                    var prototype = (Prototype)Prototype.Parse(this, exp);
                    Context.AddPrototype(prototype);

                    return prototype;
                }
                else
                {
                    return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedCompound, classBodyStatement.Code[0]);
                }
            }
            else
            {
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
            }
        }

        private SObject ExecuteFunction(ScriptStatement statement)
        {
            // function <name> ()
            var exp = statement.Code;
            exp = exp.Remove(0, "function".Length).Trim();
            var functionName = exp.Remove(exp.IndexOf("(", StringComparison.Ordinal));

            if (!IsValidIdentifier(functionName))
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingVarName);

            var functionExpression = "function " + exp.Remove(0, exp.IndexOf("(", StringComparison.Ordinal));

            // The function's body is the next statement:

            _index++;

            if (_index < _statements.Length)
            {
                var functionBodyStatement = _statements[_index];

                var functionBody = functionBodyStatement.Code;
                if (!functionBodyStatement.IsCompoundStatement)
                    functionBody = "{" + functionBody + "}";

                functionExpression += functionBody;

                var function = new SFunction(this, functionExpression);
                Context.AddVariable(functionName, function);

                return function;
            }
            else
            {
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
            }
        }

        private SObject ExecuteFor(ScriptStatement statement)
        {
            var exp = statement.Code;

            var forCode = exp.Remove(0, exp.IndexOf("for", StringComparison.Ordinal) + "for".Length).Trim().Remove(0, 1); // Remove "for" and "(".
            forCode = forCode.Remove(forCode.Length - 1, 1); // Remove ")".

            var forStatements = StatementProcessor.GetStatements(this, forCode);

            if (forStatements.Length == 0)
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, ")");
            if (forStatements.Length == 1)
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingForInitializer);
            if (forStatements.Length == 2)
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingForCondition);
            if (forStatements.Length > 3)
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingForControl);

            var processor = new ScriptProcessor(Context, GetLineNumber());

            var forInitializer = forStatements[0];
            var forCondition = forStatements[1];
            var forControl = forStatements[2];

            if (forInitializer.Code.Length > 0)
                processor.ExecuteStatement(forInitializer);

            _index++;

            if (_statements.Length > _index)
            {
                var stayInFor = true;
                var executeStatement = _statements[_index];
                var returnObject = Undefined;

                while (stayInFor)
                {
                    if (forCondition.Code.Length > 0)
                    {
                        var conditionResult = processor.ExecuteStatement(forCondition);

                        var conditionAsBool = conditionResult as SBool;
                        stayInFor = conditionAsBool?.Value ?? conditionResult.ToBool(this).Value;
                    }

                    if (stayInFor)
                    {
                        returnObject = processor.ExecuteStatement(executeStatement);

                        if (processor._returnIssued || processor._breakIssued)
                        {
                            _breakIssued = false;
                            _returnIssued = processor._returnIssued;
                            stayInFor = false;
                        }
                        else if (forControl.Code.Length > 0)
                        {
                            processor.ExecuteStatement(forControl);
                        }
                    }
                }

                return returnObject;
            }
            else
            {
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
            }
        }

        private SObject ExecuteAssignment(ScriptStatement statement)
        {
            var exp = statement.Code;

            var leftSide = "";
            var rightSide = "";
            var assignmentOperator = "";

            // Get left and right side of the assignment:
            {
                var depth = 0;
                var index = 0;
                StringEscapeHelper escaper = new LeftToRightStringEscapeHelper(exp, 0);

                while (index < exp.Length && assignmentOperator.Length == 0)
                {
                    var t = exp[index];
                    escaper.CheckStartAt(index);

                    if (!escaper.IsString)
                    {
                        if (t == '(' || t == '[' || t == '{')
                        {
                            depth++;
                        }
                        else if (t == ')' || t == ']' || t == '}')
                        {
                            depth--;
                        }
                        else if (t == '=' && depth == 0)
                        {
                            var previous = ' ';
                            if (index > 0)
                                previous = exp[index - 1];

                            if (previous == '+' || previous == '-' || previous == '/' || previous == '*')
                            {
                                assignmentOperator = previous.ToString();
                                leftSide = exp.Substring(0, index - 1).TrimEnd();
                            }
                            else
                            {
                                assignmentOperator = "=";
                                leftSide = exp.Substring(0, index).TrimEnd();
                            }

                            rightSide = exp.Substring(index + 1).TrimStart();
                        }
                    }

                    index++;
                }
            }

            // This means it's a function call, which cannot be assigned to:
            if (leftSide.EndsWith(")") || leftSide.Length == 0)
                return ErrorHandler.ThrowError(ErrorType.ReferenceError, ErrorHandler.MessageReferenceInvalidAssignmentLeft);

            var isIndexer = false;
            var host = "";
            var member = "";

            if (leftSide.EndsWith("]"))
            {
                var indexerStartIndex = 0;
                var index = leftSide.Length - 1;
                var depth = 0;

                StringEscapeHelper escaper = new RightToLeftStringEscapeHelper(leftSide, index);
                while (index > 0 && !isIndexer)
                {
                    var t = leftSide[index];
                    escaper.CheckStartAt(index);

                    if (!escaper.IsString)
                    {
                        if (t == '(' || t == '{')
                        {
                            depth--;
                        }
                        else if (t == ')' || t == ']' || t == '}')
                        {
                            depth++;
                        }
                        else if (t == '[')
                        {
                            depth--;
                            if (depth == 0 && index > 0)
                            {
                                isIndexer = true;
                                indexerStartIndex = index;
                            }
                        }
                    }

                    index--;
                }

                if (isIndexer)
                {
                    member = leftSide.Substring(indexerStartIndex + 1);
                    member = member.Remove(member.Length - 1, 1);
                    host = leftSide.Remove(indexerStartIndex);
                }
            }
            else
            {
                var foundMember = false;

                if (leftSide.Contains("."))
                {
                    var index = leftSide.Length - 1;
                    var depth = 0;
                    StringEscapeHelper escaper = new RightToLeftStringEscapeHelper(leftSide, index);

                    while (index > 0 && !foundMember)
                    {
                        var t = leftSide[index];
                        escaper.CheckStartAt(index);

                        if (!escaper.IsString)
                        {
                            if (t == '(' || t == '[' || t == '{')
                            {
                                depth--;
                            }
                            else if (t == ')' || t == ']' || t == '}')
                            {
                                depth++;
                            }
                            else if (t == '.' && depth == 0)
                            {
                                foundMember = true;
                                host = leftSide.Substring(0, index);
                                member = leftSide.Remove(0, index + 1);
                            }
                        }

                        index--;
                    }
                }

                if (!foundMember)
                {
                    host = SObject.LiteralThis;
                    member = leftSide;
                }
            }

            // When it's an indexer, we parse it as statement:
            var accessor = isIndexer ? SObject.Unbox(ExecuteStatement(new ScriptStatement(member))) : CreateString(member);

            var memberHost = ExecuteStatement(new ScriptStatement(host));
            var value = SObject.Unbox(ExecuteStatement(new ScriptStatement(rightSide)));

            if (assignmentOperator == "=")
            {
                memberHost.SetMember(this, accessor, isIndexer, value);
            }
            else
            {
                var memberContent = memberHost.GetMember(this, accessor, isIndexer);

                var result = "";

                switch (assignmentOperator)
                {
                    case "+":
                        result = ObjectOperators.AddOperator(this, memberContent, value);
                        break;
                    case "-":
                        result = ObjectOperators.SubtractOperator(this, memberContent, value);
                        break;
                    case "*":
                        result = ObjectOperators.MultiplyOperator(this, memberContent, value);
                        break;
                    case "/":
                        result = ObjectOperators.DivideOperator(this, memberContent, value);
                        break;
                }

                memberHost.SetMember(this, accessor, isIndexer, ToScriptObject(result));
            }

            return value;
        }

        private SObject ExecuteExecutable(ScriptStatement statement)
        {
            if (statement.IsCompoundStatement)
            {
                var processor = new ScriptProcessor(Context, GetLineNumber());

                // Remove { and }:
                var code = statement.Code.Remove(0, 1);
                code = code.Remove(code.Length - 1, 1);

                var returnObject = processor.Run(code);

                _breakIssued = processor._breakIssued;
                _continueIssued = processor._continueIssued;
                _returnIssued = processor._returnIssued;

                return returnObject;
            }
            else
            {
                var exp = ResolveParentheses(statement.Code).Trim();

                #region QuickConvert

                // have quick conversions for small statements here
                // parameter statements are much faster that way:
                if (exp == SObject.LiteralBoolTrue)
                {
                    return CreateBool(true);
                }
                else if (exp == SObject.LiteralBoolFalse)
                {
                    return CreateBool(false);
                }
                else if (exp == SObject.LiteralUndefined || exp == "")
                {
                    return Undefined;
                }
                else if (exp == SObject.LiteralNull)
                {
                    return Null;
                }
                else if (exp.StartsWith("\"") && exp.EndsWith("\"") && !exp.Remove(exp.Length - 1, 1).Remove(0, 1).Contains("\""))
                {
                    return CreateString(exp.Remove(exp.Length - 1, 1).Remove(0, 1));
                }
                else if (exp.All(char.IsDigit))
                {
                    double num;
                    SNumber.TryParse(exp, out num);
                    return CreateNumber(num);
                }

                #endregion

                if (exp.Contains("=>"))
                    exp = EvaluateLambda(exp);
                if (exp.Contains("."))
                    exp = EvaluateOperator(exp, ".");
                if (exp.Contains("++"))
                    exp = EvaluateOperator(exp, "++");
                if (exp.Contains("--"))
                    exp = EvaluateOperator(exp, "--");
                if (exp.Contains("!"))
                    exp = EvaluateReverseBool(exp);
                if (exp.Contains("**"))
                    exp = EvaluateOperator(exp, "**");
                if (exp.Contains("*"))
                    exp = EvaluateOperator(exp, "*");
                if (exp.Contains("/"))
                    exp = EvaluateOperator(exp, "/");
                if (exp.Contains("%"))
                    exp = EvaluateOperator(exp, "%");
                if (exp.Contains("+"))
                    exp = EvaluateOperator(exp, "+");
                if (exp.Contains("-"))
                    exp = EvaluateOperator(exp, "-");
                if (exp.Contains("<="))
                    exp = EvaluateOperator(exp, "<=");
                if (exp.Contains(">="))
                    exp = EvaluateOperator(exp, ">=");
                if (exp.Contains("<"))
                    exp = EvaluateOperator(exp, "<");
                if (exp.Contains(">"))
                    exp = EvaluateOperator(exp, ">");
                if (exp.Contains("==="))
                    exp = EvaluateOperator(exp, "===");
                if (exp.Contains("!=="))
                    exp = EvaluateOperator(exp, "!==");
                if (exp.Contains("=="))
                    exp = EvaluateOperator(exp, "==");
                if (exp.Contains("!="))
                    exp = EvaluateOperator(exp, "!=");
                if (exp.Contains("&&"))
                    exp = EvaluateOperator(exp, "&&");
                if (exp.Contains("||"))
                    exp = EvaluateOperator(exp, "||");

                return ToScriptObject(exp);
            }
        }

        private SObject ExecuteIf(ScriptStatement statement)
        {
            var exp = statement.Code;

            var condition = exp.Remove(0, exp.IndexOf("if", StringComparison.Ordinal) + "if".Length).Trim().Remove(0, 1); // Remove "if" and "(".
            condition = condition.Remove(condition.Length - 1, 1).Trim(); // Remove ")".

            if (condition.Length == 0)
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, ")");

            var conditionResult = ExecuteStatement(new ScriptStatement(condition));
            statement.StatementResult = conditionResult;

            var conditionAsBool = conditionResult as SBool;
            var conditionEval = conditionAsBool?.Value ?? conditionResult.ToBool(this).Value;

            _index++;

            if (_statements.Length > _index)
            {
                var executeStatement = _statements[_index];

                if (conditionEval)
                {
                    var returnObject = ExecuteStatement(executeStatement);

                    // Jump over all "else if" and "else" statements that follow this if / else if:

                    var searchIndex = _index + 1;
                    var foundIfs = true;

                    while (searchIndex < _statements.Length && foundIfs)
                    {
                        if (_statements[searchIndex].StatementType == StatementType.ElseIf || _statements[searchIndex].StatementType == StatementType.Else)
                            _index += 2;
                        else
                            foundIfs = false;

                        searchIndex += 2;
                    }

                    return returnObject;
                }
                else
                {
                    return Undefined;
                }
            }
            else
            {
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
            }
        }

        private SObject ExecuteElse()
        {
            // Search for an if statement:
            var searchIndex = _index - 2;
            var foundIf = false;

            while (searchIndex >= 0 && !foundIf)
            {
                if (_statements[searchIndex].StatementType == StatementType.If)
                    foundIf = true;

                searchIndex -= 2;
            }

            if (!foundIf)
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "keyword \'else\'");

            _index++;

            if (_statements.Length > _index)
            {
                var executeStatement = _statements[_index];
                return ExecuteStatement(executeStatement);
            }
            else
            {
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
            }
        }

        private SObject ExecuteElseIf(ScriptStatement statement)
        {
            // Search for an if statement:
            var searchIndex = _index - 2;
            var foundIf = false;

            while (searchIndex >= 0 && !foundIf)
            {
                if (_statements[searchIndex].StatementType == StatementType.If)
                    foundIf = true;

                searchIndex -= 2;
            }

            return foundIf ?
                ExecuteIf(statement) :
                ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "keyword \'else if\'");
        }

        private SObject ExecuteImport(ScriptStatement statement)
        {
            // import apiClass from "moduleName"

            var exp = statement.Code;
            var parts = exp.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4 || parts[0] != "import" || parts[2] != "from")
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxInvalidImportStatement);

            var apiClass = parts[1];
            var moduleName = exp.Remove(0, exp.IndexOf("\"", StringComparison.Ordinal));
            moduleName = moduleName.Trim('\"');

            if (!IsValidIdentifier(apiClass))
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingVarName);

            var apiUsing = new SApiUsing(apiClass, moduleName);

            Context.AddApiUsing(apiUsing);
            return apiUsing;
        }

        private SObject ExecuteVar(ScriptStatement statement)
        {
            var exp = statement.Code;

            var identifier = exp.Remove(0, "var ".Length).Trim();
            var data = Undefined;

            if (identifier.Contains("="))
            {
                var assignment = identifier.Remove(0, identifier.IndexOf("=", StringComparison.Ordinal) + 1).Trim();
                identifier = identifier.Remove(identifier.IndexOf("=", StringComparison.Ordinal)).Trim();

                data = SObject.Unbox(ExecuteStatement(new ScriptStatement(assignment)));
            }

            if (!IsValidIdentifier(identifier))
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingVarName);

            var variable = new SVariable(identifier, data);
            Context.AddVariable(variable);

            return variable;
        }

        private SObject ExecuteWhile(ScriptStatement statement)
        {
            var exp = statement.Code;

            var condition = exp.Remove(0, exp.IndexOf("while", StringComparison.Ordinal) + "while".Length).Trim().Remove(0, 1); // Remove "while" and "(".
            condition = condition.Remove(condition.Length - 1, 1).Trim(); // Remove ")".

            if (condition.Length == 0)
                return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, ")");

            _index++;

            if (_statements.Length > _index)
            {
                var stayInWhile = true;
                var executeStatement = _statements[_index];
                var returnObject = Undefined;

                while (stayInWhile)
                {
                    var conditionResult = ExecuteStatement(new ScriptStatement(condition));

                    var conditionAsBool = conditionResult as SBool;
                    stayInWhile = conditionAsBool?.Value ?? conditionResult.ToBool(this).Value;

                    if (stayInWhile)
                    {
                        returnObject = ExecuteStatement(executeStatement);

                        if (_returnIssued || _breakIssued)
                        {
                            _breakIssued = false;
                            stayInWhile = false;
                        }
                    }
                }

                return returnObject;
            }
            return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");
        }

        private SObject ExecuteReturn(ScriptStatement statement)
        {
            var exp = statement.Code;

            if (exp == "return")
            {
                _returnIssued = true;
                return Undefined;
            }
            exp = exp.Remove(0, "return".Length);

            var returnObject = ExecuteStatement(new ScriptStatement(exp));
            _returnIssued = true;

            return returnObject;
        }
    }
}
