using Kolben.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kolben.Types.Prototypes;

namespace Kolben
{
    /// <summary>
    /// A class to process Pokemon3D.Scripting scripts.
    /// </summary>
    public partial class ScriptProcessor
    {
        // Set this to true to have script exceptions crash the application.
        internal bool DebugCrashMode = false;

        private struct ElementCapture
        {
            public int StartIndex;
            public int Length;
            public string Identifier;
            public int Depth;
        }

        private const string IdentifierSeparators = "-+*/=!%&|<>,";
        private const string CallLiteral = "call";

        /// <summary>
        /// The <see cref="Pokemon3D.Scripting.ErrorHandler"/> associated with this <see cref="ScriptProcessor"/>.
        /// </summary>
        internal ErrorHandler ErrorHandler { get; }
        
        /// <summary>
        /// Returns the line number of the currently active statement.
        /// </summary>
        internal int GetLineNumber()
        {
            var lineNumber = -1;
            if (_statements != null)
            {
                lineNumber = _index >= _statements.Length ? _statements.Last().LineNumber : _statements[_index].LineNumber;
            }
            if (_hasParent)
            {
                if (lineNumber == -1)
                    lineNumber = 0;

                lineNumber += _parentLineNumber;
            }
            return lineNumber;
        }

        private ScriptStatement[] _statements;
        private int _index;
        private string _source;
        private readonly bool _hasParent;
        private readonly int _parentLineNumber;

        private bool _returnIssued;
        private bool _continueIssued;
        private bool _breakIssued;

        /// <summary>
        /// The <see cref="ScriptContext"/> associated with this <see cref="ScriptProcessor"/>.
        /// </summary>
        internal ScriptContext Context { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="ScriptProcessor"/> and sets a context.
        /// </summary>
        internal ScriptProcessor(ScriptContext context) : this(context, 0) { }

        internal ScriptProcessor(ScriptContext context, int parentLineNumber)
        {
            _hasParent = true;
            _parentLineNumber = parentLineNumber;
            ErrorHandler = new ErrorHandler(this);

            Context = new ScriptContext(this, context);
            Context.Initialize();
        }

        #region Public interface

        /// <summary>
        /// Creates a new instance of the <see cref="ScriptProcessor"/>.
        /// </summary>
        public ScriptProcessor() : this(null, 0) { _hasParent = false; }

        /// <summary>
        /// Creates a new instance of the <see cref="ScriptProcessor"/> with defined prototypes.
        /// </summary>
        /// <param name="inputPrototypes">An enumeration of prototypes.</param>
        public ScriptProcessor(IEnumerable<SObject> inputPrototypes)
            : this(null, 0)
        {
            _hasParent = false;

            foreach (var inObj in inputPrototypes.Where(x => x is Prototype))
                Context.AddPrototype((Prototype)inObj);
        }

        /// <summary>
        /// Runs raw source code and returns the result.
        /// </summary>
        /// <param name="code">The source code to run.</param>
        /// <returns>Either data returned by a "return"-statement or the result of the last statement.</returns>
        public SObject Run(string code)
        {
            _source = code;

            SObject returnObject;
            ErrorHandler.Clean();

            if (_hasParent || DebugCrashMode)
            {
                returnObject = ProcessStatements();
            }
            else
            {
                try
                {
                    returnObject = ProcessStatements();
                }
                catch (ScriptException ex)
                {
                    returnObject = ex.ErrorObject;
                }
            }

            return returnObject;
        }

        #endregion

        internal static readonly string[] ReservedKeywords = new string[] { "if", "else", "while", "for", "function", "class", "using", "var", "static", "new", "extends", "this", "super", "link", "readonly", "break", "continue", "indexer", "get", "set", "throw", "try", "catch", "finally", "property", "async" };

        /// <summary>
        /// Returns if the given string is a valid identifier.
        /// </summary>
        /// <param name="identifier">The string to check.</param>
        internal static bool IsValidIdentifier(string identifier)
        {
            // The string must not be empty string, and start with a unicode letter.
            // Also, it cannot be a reserved keyword.
            // "call" cannot be used as an identifier because it's the default identifier to call a method.
            return !(string.IsNullOrEmpty(identifier) ||
                !char.IsLetter(identifier[0]) ||
                ReservedKeywords.Contains(identifier) ||
                identifier == CallLiteral);
        }

        /// <summary>
        /// The undefined object.
        /// </summary>
        internal SObject Undefined => Context.GetVariable(SObject.LiteralUndefined).Data;

        /// <summary>
        /// The null "object".
        /// </summary>
        internal SObject Null => Context.GetVariable(SObject.LiteralNull).Data;

        /// <summary>
        /// Creates an instance of the string primitive.
        /// </summary>
        internal SString CreateString(string value)
        {
            return CreateString(value, true, false);
        }

        /// <summary>
        /// Creates an instance of the string primitive, also setting the escaped status.
        /// </summary>
        internal SString CreateString(string value, bool escaped, bool interpolate)
        {
            return SString.Factory(this, value, escaped, interpolate);
        }

        /// <summary>
        /// Creates an instance of the number primitive.
        /// </summary>
        internal SNumber CreateNumber(double value)
        {
            return SNumber.Factory(value);
        }

        /// <summary>
        /// Creates an instance of the bool primitive.
        /// </summary>
        internal SBool CreateBool(bool value)
        {
            return SBool.Factory(value);
        }

        internal SArray CreateArray(int length)
        {
            return Context.CreateInstance("Array", new SObject[] { CreateNumber(length) }) as SArray;
        }

        internal SArray CreateArray(SObject[] elements)
        {
            // the array prototype constructor needs the first element
            // in the params list to be the desired length of the array:

            var destElements = new SObject[elements.Length + 1];
            Array.Copy(elements, 0, destElements, 1, elements.Length);
            destElements[0] = CreateNumber(elements.Length);

            return Context.CreateInstance("Array", destElements) as SArray;
        }

        #region Statement processing

        private SObject ProcessStatements()
        {
            var returnObject = Undefined;

            _statements = StatementProcessor.GetStatements(this, _source);

            _index = 0;

            while (_index < _statements.Length)
            {
                returnObject = ExecuteStatement(_statements[_index]);
                if (_continueIssued || _breakIssued || _returnIssued)
                {
                    returnObject = SObject.Unbox(returnObject);
                    return returnObject;
                }

                _index++;
            }

            return SObject.Unbox(returnObject);
        }

        #endregion

        #region Operators

        private string EvaluateReverseBool(string exp)
        {
            // The reverse bool operator ("!") gets evaluated from right to left, instead of left to right.
            // So the list of operators gets reversed.

            const string op = "!";

            var ops = GetOperatorPositions(exp, op).Reverse().ToArray();

            foreach (var cOp in ops)
            {
                var captureRight = CaptureRight(exp, cOp + op.Length);
                var elementRight = captureRight.Identifier.Trim();

                if (!string.IsNullOrWhiteSpace(elementRight))
                {
                    var objRight = ToScriptObject(elementRight);

                    var result = ObjectOperators.NotOperator(this, objRight);

                    exp = exp.Remove(cOp, elementRight.Length + op.Length);
                    exp = exp.Insert(cOp, result);
                }
            }

            return exp;
        }

        private string EvaluateOperator(string exp, string op)
        {
            var ops = GetOperatorPositions(exp, op);

            for (var i = 0; i < ops.Length; i++)
            {
                var cOp = ops[i];

                var needRight = !(op == "++" || op == "--");

                var captureLeft = CaptureLeft(exp, cOp - 1);
                var elementLeft = captureLeft.Identifier.Trim();

                if (!(op == "-" && elementLeft.Length == 0))
                {
                    var result = "";

                    if (string.IsNullOrWhiteSpace(elementLeft))
                        ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, op);

                    var objectLeft = ToScriptObject(elementLeft);

                    ElementCapture captureRight;
                    string elementRight;
                    SObject objectRight = null;

                    if (needRight)
                    {
                        captureRight = CaptureRight(exp, cOp + op.Length);
                        elementRight = captureRight.Identifier.Trim();

                        if (string.IsNullOrWhiteSpace(elementRight))
                            ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of script");

                        if (op != ".")
                            objectRight = ToScriptObject(elementRight);
                    }
                    else
                    {
                        elementRight = "";
                        captureRight = new ElementCapture() { Length = 0 };
                    }

                    if (op != "." || !IsDotOperatorDecimalSeparator(elementLeft, elementRight))
                    {
                        switch (op)
                        {
                            case ".":
                                result = InvokeMemberOrMethod(objectLeft, elementRight).ToScriptObject();
                                break;
                            case "+":
                                result = ObjectOperators.AddOperator(this, objectLeft, objectRight);
                                break;
                            case "-":
                                result = ObjectOperators.SubtractOperator(this, objectLeft, objectRight);
                                break;
                            case "*":
                                result = ObjectOperators.MultiplyOperator(this, objectLeft, objectRight);
                                break;
                            case "/":
                                result = ObjectOperators.DivideOperator(this, objectLeft, objectRight);
                                break;
                            case "%":
                                result = ObjectOperators.ModulusOperator(this, objectLeft, objectRight);
                                break;
                            case "**":
                                result = ObjectOperators.ExponentOperator(this, objectLeft, objectRight);
                                break;
                            case "==":
                                result = ObjectOperators.EqualsOperator(this, objectLeft, objectRight);
                                break;
                            case "===":
                                result = ObjectOperators.TypeEqualsOperator(this, objectLeft, objectRight);
                                break;
                            case "!=":
                                result = ObjectOperators.NotEqualsOperator(this, objectLeft, objectRight);
                                break;
                            case "!==":
                                result = ObjectOperators.TypeNotEqualsOperator(this, objectLeft, objectRight);
                                break;
                            case "<=":
                                result = ObjectOperators.SmallerOrEqualsOperator(this, objectLeft, objectRight);
                                break;
                            case "<":
                                result = ObjectOperators.SmallerOperator(this, objectLeft, objectRight);
                                break;
                            case ">=":
                                result = ObjectOperators.LargerOrEqualsOperator(this, objectLeft, objectRight);
                                break;
                            case ">":
                                result = ObjectOperators.LargerOperator(this, objectLeft, objectRight);
                                break;
                            case "&&":
                                result = ObjectOperators.AndOperator(this, objectLeft, objectRight);
                                break;
                            case "||":
                                result = ObjectOperators.OrOperator(this, objectLeft, objectRight);
                                break;
                            case "++":
                                result = ObjectOperators.IncrementOperator(this, objectLeft);
                                break;
                            case "--":
                                result = ObjectOperators.DecrementOperator(this, objectLeft);
                                break;
                        }

                        exp = exp.Remove(captureLeft.StartIndex, op.Length + captureLeft.Length + captureRight.Length);
                        exp = exp.Insert(captureLeft.StartIndex, result);

                        var offset = result.Length - (op.Length + captureLeft.Length + captureRight.Length);
                        for (var j = i + 1; j < ops.Length; j++)
                        {
                            ops[j] += offset;
                        }
                    }
                }
            }

            return exp;
        }

        private string EvaluateLambda(string exp)
        {
            if (StringEscapeHelper.ContainsWithoutStrings(exp, "=>") && Regex.IsMatch(exp, RegexLambda))
                return BuildLambdaFunction(exp);
            else
                return exp;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Converts a string expression into a script object.
        /// </summary>
        private SObject ToScriptObject(string exp)
        {
            exp = exp.Trim();

            // This means it's either an indexer or an array
            if (exp.EndsWith("]"))
            {
                if (!(exp.StartsWith("[") && !exp.Remove(0, 1).Contains("["))) // When there's no "[" besides the start, and it starts with [, then it is an array. Otherwise, do real check.
                {
                    // It is possible that we are having a simple array declaration here.
                    // We check that by looking if we can find a "[" before the expression ends:

                    var depth = 0;
                    var index = exp.Length - 2;
                    var indexerStartIndex = 0;
                    var foundIndexer = false;
                    StringEscapeHelper escaper = new RightToLeftStringEscapeHelper(exp, index);

                    while (index > 0 && !foundIndexer)
                    {
                        var t = exp[index];
                        escaper.CheckStartAt(index);

                        if (!escaper.IsString)
                        {
                            if (t == ')' || t == '}' || t == ']')
                            {
                                depth++;
                            }
                            else if (t == '(' || t == '{')
                            {
                                depth--;
                            }
                            else if (t == '[')
                            {
                                if (depth == 0)
                                {
                                    if (index > 0)
                                    {
                                        indexerStartIndex = index;
                                        foundIndexer = true;
                                    }
                                }
                                else
                                {
                                    depth--;
                                }
                            }
                        }

                        index--;
                    }

                    if (foundIndexer)
                    {
                        var indexerCode = exp.Substring(indexerStartIndex + 1, exp.Length - indexerStartIndex - 2);

                        var identifier = exp.Remove(indexerStartIndex);

                        var statementResult = ExecuteStatement(new ScriptStatement(indexerCode));
                        return ToScriptObject(identifier).GetMember(this, statementResult, true);
                    }
                }
            }

            // Normal object return procedure:

            // Negative number:
            var isNegative = false;
            if (exp.StartsWith("-"))
            {
                exp = exp.Remove(0, 1);
                isNegative = true;
            }
            
            SObject returnObject;

            if (exp == SObject.LiteralNull)
            {
                returnObject = Null;
            }
            else if (exp == SObject.LiteralUndefined)
            {
                returnObject = Undefined;
            }
            else if (exp == SObject.LiteralBoolFalse)
            {
                returnObject = CreateBool(false);
            }
            else if (exp == SObject.LiteralBoolTrue)
            {
                returnObject = CreateBool(true);
            }
            else if (exp == SObject.LiteralNan)
            {
                returnObject = CreateNumber(double.NaN);
            }
            else if (exp == SObject.LiteralThis)
            {
                returnObject = Context.This;
            }
            else if (SNumber.TryParse(exp, out var dblResult))
            {
                returnObject = CreateNumber(dblResult);
            }
            else if (exp.StartsWith("\"") && exp.EndsWith("\"") || exp.StartsWith("\'") && exp.EndsWith("\'"))
            {
                returnObject = CreateString(exp.Remove(exp.Length - 1, 1).Remove(0, 1), true, false);
            }
            else if (exp.StartsWith("$\"") && exp.EndsWith("\"") || exp.StartsWith("$\'") && exp.EndsWith("\'"))
            {
                returnObject = CreateString(exp.Remove(exp.Length - 1, 1).Remove(0, 2), true, true);
            }
            else if (exp.StartsWith("@\"") && exp.EndsWith("\"") || exp.StartsWith("@\'") && exp.EndsWith("\'"))
            {
                returnObject = CreateString(exp.Remove(exp.Length - 1, 1).Remove(0, 2), false, false);
            }
            else if (exp.StartsWith("{") && exp.EndsWith("}"))
            {
                returnObject = SProtoObject.Parse(this, exp);
            }
            else if (exp.StartsWith("[") && exp.EndsWith("]"))
            {
                returnObject = SArray.Parse(this, exp);
            }
            else if (exp.StartsWith("function") && Regex.IsMatch(exp, RegexFunction))
            {
                returnObject = new SFunction(this, exp);
            }
            else if (Context.IsApiUsing(exp))
            {
                returnObject = Context.GetApiUsing(exp);
            }
            else if (Context.IsVariable(exp))
            {
                returnObject = Context.GetVariable(exp);
            }
            else if (Context.This.HasMember(this, exp))
            {
                returnObject = Context.This.GetMember(this, CreateString(exp), false);
            }
            else if (Context.IsPrototype(exp))
            {
                returnObject = Context.GetPrototype(exp);
            }
            else if (exp.StartsWith("new "))
            {
                returnObject = Context.CreateInstance(exp);
            }
            else if (exp.StartsWith(ObjectBuffer.ObjPrefix))
            {
                var strId = exp.Remove(0, ObjectBuffer.ObjPrefix.Length);

                if (int.TryParse(strId, out var id) && ObjectBuffer.HasObject(id))
                {
                    returnObject = (SObject)ObjectBuffer.GetObject(id);
                }
                else
                {
                    returnObject = ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxInvalidToken, exp);
                }
            }
            else
            {
                returnObject = ErrorHandler.ThrowError(ErrorType.ReferenceError, ErrorHandler.MessageReferenceNotDefined, exp);
            }

            if (isNegative)
                returnObject = ObjectOperators.NegateNumber(this, returnObject);

            return returnObject;
        }

        private static bool IsDotOperatorDecimalSeparator(string elementLeft, string elementRight)
        {
            return Regex.IsMatch(elementLeft.Trim(), RegexNumleftdot) &&
                   Regex.IsMatch(elementRight.Trim(), RegexNumrightdot);
        }

        /// <summary>
        /// Returns positions of the given operator in the expression, sorted from left to right.
        /// </summary>
        private static int[] GetOperatorPositions(string exp, string op)
        {
            var operators = new List<int>();

            StringEscapeHelper escaper = new LeftToRightStringEscapeHelper(exp, 0);
            var depth = 0;
            var index = 0;

            while (index < exp.Length)
            {
                var t = exp[index];
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

                    if (t == op[0] && depth == 0)
                    {
                        if (op.Length > 1 && index + op.Length - 1 < exp.Length)
                        {
                            var correctOperator = true;
                            for (var i = 1; i < op.Length; i++)
                            {
                                if (exp[index + i] != op[i])
                                    correctOperator = false;
                            }

                            if (correctOperator)
                                operators.Add(index);
                        }
                        else if (op.Length == 1)
                        {
                            operators.Add(index);
                        }
                    }
                }
                index++;
            }

            return operators.ToArray();
        }

        /// <summary>
        /// Resolves parentheses and adds ".call" to direct function calls on variables.
        /// </summary>
        private string ResolveParentheses(string exp)
        {
            if (exp.Contains("(") && exp.Contains(")"))
            {
                var index = 0;
                var depth = 0;
                var parenthesesStartIndex = -1;
                var newExpression = new StringBuilder();
                var escaper = new LeftToRightStringEscapeHelper(exp, 0);

                while (index < exp.Length)
                {
                    var t = exp[index];
                    escaper.CheckStartAt(index);

                    if (!escaper.IsString)
                    {
                        if (t == '{' || t == '[')
                        {
                            depth++;
                            if (parenthesesStartIndex == -1)
                                newExpression.Append(t);
                        }
                        else if (t == '}' || t == ']')
                        {
                            depth--;
                            if (parenthesesStartIndex == -1)
                                newExpression.Append(t);
                        }
                        else if (t == '(')
                        {
                            if (depth == 0 && parenthesesStartIndex == -1)
                            {
                                var capture = CaptureLeft(newExpression.ToString(), newExpression.Length - 1);
                                var identifier = capture.Identifier;
                                if (capture.Depth == 0 || identifier == "")
                                {
                                    if (identifier.Length == 0)
                                    {
                                        parenthesesStartIndex = index;
                                    }
                                    else
                                    {
                                        var testExp = newExpression.ToString().Remove(capture.StartIndex).Trim();

                                        if (testExp.Length > 0 && testExp.Last() == '.' || StatementProcessor.ControlStatements.Contains(identifier) || identifier.StartsWith("new "))
                                        {
                                            newExpression.Append(t);
                                        }
                                        else
                                        {
                                            newExpression.Append(".call" + t);
                                        }
                                        depth++;
                                    }
                                }
                                else
                                {
                                    newExpression.Append(t);
                                    depth++;
                                }
                            }
                            else
                            {
                                depth++;
                                if (parenthesesStartIndex == -1)
                                    newExpression.Append(t);
                            }
                        }
                        else if (t == ')')
                        {
                            if (depth == 0 && parenthesesStartIndex > -1)
                            {
                                var parenthesesCode = exp.Substring(parenthesesStartIndex + 1, index - parenthesesStartIndex - 1);

                                if (parenthesesCode.Length > 0)
                                {
                                    if (parenthesesCode.Contains("=>") && Regex.IsMatch(parenthesesCode, RegexLambda))
                                    {
                                        newExpression.Append(BuildLambdaFunction(parenthesesCode));
                                    }
                                    else
                                    {
                                        var returnObject = ExecuteStatement(new ScriptStatement(parenthesesCode));
                                        newExpression.Append(returnObject.ToScriptObject());
                                    }
                                }
                                else
                                {
                                    // check for lambda statement
                                    // if this turns out to be a lambda statement, then the whole expression is this lambda statement.
                                    // therefore, discard everything and just add the converted function code taken from the lambda statement.
                                    var nonParenthesesCode = exp.Substring(index + 1).Trim();
                                    if (nonParenthesesCode.StartsWith("=>"))
                                    {
                                        return BuildLambdaFunction(exp);
                                    }
                                }

                                parenthesesStartIndex = -1;
                            }
                            else
                            {
                                depth--;
                                if (parenthesesStartIndex == -1)
                                    newExpression.Append(t);
                            }
                        }
                        else
                        {
                            if (parenthesesStartIndex == -1)
                                newExpression.Append(t);
                        }
                    }
                    else
                    {
                        if (parenthesesStartIndex == -1)
                            newExpression.Append(t);
                    }

                    index++;
                }

                return newExpression.ToString();
            }

            return exp;
        }

        /// <summary>
        /// Builds a function() {} from a lambda statement.
        /// </summary>
        private static string BuildLambdaFunction(string lambdaCode)
        {
            var signatureCode = lambdaCode.Remove(lambdaCode.IndexOf("=>", StringComparison.Ordinal)).Trim();
            var signatureBuilder = new StringBuilder();

            if (signatureCode != "()")
            {
                var signature = signatureCode.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var t in signature)
                {
                    if (signatureBuilder.Length > 0)
                        signatureBuilder.Append(',');

                    signatureBuilder.Append(t);
                }
            }

            var code = lambdaCode.Remove(0, lambdaCode.IndexOf("=>", StringComparison.Ordinal) + 2).Trim();

            // code without a code block ({ ... }) are a single statement that is implied to follow a "return" statement.
            // e.g. (a) => a + 1 -> function(a) { return a + 1; }
            // code with a code block define the whole function body. This is to ensure compatibility with C#-like lambda statements.
            // e.g. (a) => { return a + 1; } -> function(a) { return a + 1; }.
            // these types of lambda statements are more or less useless because they are just skipping the "function" literal.

            if (code.StartsWith("{") && code.EndsWith("}"))
            {
                return $"function({signatureBuilder}){code}";
            }
            else
            {
                return $"function({signatureBuilder}){{return {code};}}";
            }
        }

        /// <summary>
        /// Captures an element right from the starting index.
        /// </summary>
        private static ElementCapture CaptureRight(string exp, int index)
        {
            if (string.IsNullOrWhiteSpace(exp))
                return new ElementCapture() { Length = 0, StartIndex = 0, Identifier = "", Depth = 0 };

            var identifier = "";
            var foundSeparatorChar = false;
            var depth = 0;
            StringEscapeHelper escaper = new LeftToRightStringEscapeHelper(exp, index);

            while (index < exp.Length && !foundSeparatorChar)
            {
                var t = exp[index];
                escaper.CheckStartAt(index);

                if (!escaper.IsString)
                {
                    if (t == ')' || t == ']' || t == '}')
                    {
                        depth--;
                    }
                    else if (t == '(' || t == '[' || t == '{')
                    {
                        depth++;
                    }
                }
                if (t == '-' && string.IsNullOrWhiteSpace(identifier))
                {
                    identifier += "-";
                }
                else
                {
                    if (!escaper.IsString && depth == 0)
                    {
                        if (t == '.')
                        {
                            // Check if the '.' is not a decimal separator:
                            if (!Regex.IsMatch(identifier.Trim(), RegexNumleftdot))
                            {
                                foundSeparatorChar = true;
                            }
                        }
                        else if (IdentifierSeparators.Contains(t))
                        {
                            foundSeparatorChar = true;
                        }
                    }

                    // Append the char to the identifier:
                    if (!foundSeparatorChar)
                    {
                        identifier += t;
                    }
                }

                index++;
            }

            if (foundSeparatorChar)
                return new ElementCapture() { StartIndex = index - 1 - identifier.Length, Length = identifier.Length, Identifier = identifier.Trim(), Depth = depth };
            else
                return new ElementCapture() { StartIndex = index - identifier.Length, Length = identifier.Length, Identifier = identifier.Trim(), Depth = depth };
        }

        /// <summary>
        /// Captures an element left from the starting index.
        /// </summary>
        private static ElementCapture CaptureLeft(string exp, int index)
        {
            if (string.IsNullOrWhiteSpace(exp))
                return new ElementCapture() { Length = 0, StartIndex = 0, Identifier = "", Depth = 0 };

            var identifier = "";
            var foundSeparatorChar = false;
            var depth = 0;
            StringEscapeHelper escaper = new RightToLeftStringEscapeHelper(exp, index);

            while (index >= 0 && !foundSeparatorChar)
            {
                var t = exp[index];
                escaper.CheckStartAt(index);

                if (!escaper.IsString)
                {
                    if (t == ')' || t == ']' || t == '}')
                    {
                        depth++;
                    }
                    else if (t == '(' || t == '[' || t == '{')
                    {
                        depth--;
                    }
                }

                if (depth < 0)
                {
                    // this is when we walk out of the capture area because we are inside some area and a )]} appeared.
                    foundSeparatorChar = true;
                }
                else
                {
                    if (!escaper.IsString && depth == 0)
                    {
                        if (t == '.')
                        {
                            // Check if the '.' is not a decimal separator:
                            if (!Regex.IsMatch(identifier.Trim(), RegexNumrightdot))
                            {
                                foundSeparatorChar = true;
                            }
                        }
                        else if (IdentifierSeparators.Contains(t))
                        {
                            foundSeparatorChar = true;
                        }
                    }

                    // Append the char to the identifier:
                    if (!foundSeparatorChar)
                    {
                        identifier = t + identifier;
                    }
                }

                index--;
            }

            // Check for a minus in front of the identifier to indicate a negative number:
            if (index >= -1 && exp[index + 1] == '-' && !string.IsNullOrWhiteSpace(identifier))
            {
                identifier = "-" + identifier;
                index--;
            }

            if (foundSeparatorChar)
                return new ElementCapture() { StartIndex = index + 2, Length = identifier.Length, Identifier = identifier, Depth = depth };
            else
                return new ElementCapture() { StartIndex = index + 1, Length = identifier.Length, Identifier = identifier, Depth = depth };
        }

        /// <summary>
        /// Parses a list of parameters into a list of script objects.
        /// </summary>
        internal SObject[] ParseParameters(string exp)
        {
            // When there is only empty space in the parameter expression, we can save the search and just return an empty array:
            if (string.IsNullOrWhiteSpace(exp))
                return new SObject[] { };

            var parameters = new List<SObject>();

            var index = 0;
            var depth = 0;
            string parameter;
            SObject parameterObject;
            var parameterStartIndex = 0;
            var escaper = new LeftToRightStringEscapeHelper(exp, 0);

            while (index < exp.Length)
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
                    else if (t == ',' && depth == 0)
                    {
                        parameter = exp.Substring(parameterStartIndex, index - parameterStartIndex);
                        if (!string.IsNullOrWhiteSpace(parameter))
                        {
                            parameterObject = SObject.Unbox(ExecuteStatement(new ScriptStatement(parameter)));
                            parameters.Add(parameterObject);
                        }

                        parameterStartIndex = index + 1;
                    }
                }

                index++;
            }

            parameter = exp.Substring(parameterStartIndex, index - parameterStartIndex);
            if (!string.IsNullOrWhiteSpace(parameter))
            {
                parameterObject = SObject.Unbox(ExecuteStatement(new ScriptStatement(parameter)));
                parameters.Add(parameterObject);
            }

            return parameters.ToArray();
        }

        /// <summary>
        /// Invokes a member or method on an <see cref="SObject"/> and returns the result.
        /// </summary>
        private SObject InvokeMemberOrMethod(SObject owner, string memberOrMethod)
        {
            owner = SObject.Unbox(owner);
            memberOrMethod = memberOrMethod.Trim();

            var isMethod = memberOrMethod.EndsWith(")");

            return isMethod ? InvokeMethod(owner, memberOrMethod) : InvokeMember(owner, memberOrMethod);
        }

        private SObject InvokeMember(SObject owner, string memberName)
        {
            // When we have an indexer at the end of the member name, we get the member variable, then apply the indexer:

            if (memberName.Last() == ']')
            {
                var exp = memberName;

                var depth = 0;
                var index = exp.Length - 2;
                var indexerStartIndex = 0;
                var foundIndexer = false;
                var escaper = new RightToLeftStringEscapeHelper(exp, index);

                while (index > 0 && !foundIndexer)
                {
                    var t = exp[index];
                    escaper.CheckStartAt(index);

                    if (!escaper.IsString)
                    {
                        if (t == ')' || t == ']' || t == '}')
                        {
                            depth++;
                        }
                        else if (t == '(' || t == '{')
                        {
                            depth--;
                        }
                        else if (t == '[')
                        {
                            if (depth == 0)
                            {
                                if (index > 0)
                                {
                                    indexerStartIndex = index;
                                    foundIndexer = true;
                                }
                            }
                            else
                            {
                                depth--;
                            }
                        }
                    }
                }

                if (foundIndexer)
                {
                    var indexerCode = exp.Substring(indexerStartIndex + 1, exp.Length - indexerStartIndex - 2);
                    var identifier = exp.Remove(indexerStartIndex);

                    var indexerObject = ExecuteStatement(new ScriptStatement(indexerCode));

                    return InvokeMemberOrMethod(owner, identifier).GetMember(this, indexerObject, true);
                }
                else
                {
                    return ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxExpectedExpression, "end of string");
                }
            }
            else
            {
                return owner.GetMember(this, CreateString(memberName), false);
            }
        }

        private SObject InvokeMethod(SObject owner, string methodName)
        {
            var exp = methodName;
            var index = exp.Length - 1;
            var argumentStartIndex = -1;
            var This = owner;

            if (exp.EndsWith("()"))
            {
                argumentStartIndex = exp.Length - 2;
                index = argumentStartIndex - 1;
            }
            else
            {
                var depth = 0;
                var foundArguments = false;
                StringEscapeHelper escaper = new RightToLeftStringEscapeHelper(exp, index);

                while (index > 0 && !foundArguments)
                {
                    var t = exp[index];
                    escaper.CheckStartAt(index);

                    if (!escaper.IsString)
                    {
                        if (t == ')' || t == '}' || t == ']')
                        {
                            depth++;
                        }
                        else if (t == '(' || t == '{' || t == '[')
                        {
                            depth--;
                        }

                        if (depth == 0)
                        {
                            if (index > 0)
                            {
                                foundArguments = true;
                                argumentStartIndex = index;
                            }
                        }

                    }

                    index--;
                }
            }

            methodName = exp.Remove(argumentStartIndex);
            var argumentCode = exp.Remove(0, argumentStartIndex + 1);
            argumentCode = argumentCode.Remove(argumentCode.Length - 1, 1).Trim();
            var parameters = ParseParameters(argumentCode);

            if (methodName == CallLiteral && owner is SFunction)
            {
                This = Context.This;
            }

            // If it has an indexer, parse it again:
            if (index > 0 && exp[index] == ']')
            {
                var member = InvokeMemberOrMethod(owner, methodName);

                if ((member as SVariable)?.Data is SFunction)
                {
                    return owner.ExecuteMethod(this, ((SVariable)member).Identifier, owner, This, parameters);
                }
                else
                {
                    return ErrorHandler.ThrowError(ErrorType.TypeError, ErrorHandler.MessageTypeNotAFunction, methodName);
                }
            }
            else
            {
                return owner.ExecuteMethod(this, methodName, owner, This, parameters);
            }
        }

        #endregion
    }
}
