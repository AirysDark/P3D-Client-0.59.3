using Kolben.Types;

namespace Kolben
{
    /// <summary>
    /// Handles <see cref="ScriptProcessor"/> errors.
    /// </summary>
    internal class ErrorHandler
    {
        #region Error Messages

        public const string MessageTypeNotAFunction = "{0} is not a function";
        public const string MessageTypeAbstractNoExtends = "an abstract class must extend Object.";
        public const string MessageTypeAbstractNoInstance = "abstract prototypes cannot be instantiated";
        public const string MessageTypeGetterSetterNotAFunction = "cannot set a non-function object as indexer getter or setter.";

        public const string MessageReferenceNotDefined = "{0} is not defined";
        public const string MessageReferenceNoPrototype = "{0} is not defined or not a prototype";
        public const string MessageReferenceInvalidAssignmentLeft = "invalid assignment left-hand side";

        public const string MessageSyntaxInvalidIncrement = "invalid increment operand";
        public const string MessageSyntaxInvalidDecrement = "invalid decrement operand";
        public const string MessageSyntaxMissingFormalParameter = "missing formal parameter";
        public const string MessageSyntaxMissingFunctionBody = "missing function body";
        public const string MessageSyntaxInvalidClassSignature = "invalid class signature";
        public const string MessageSyntaxMissingVarName = "missing variable name";
        public const string MessageSyntaxExpectedExpression = "expected expression, got {0}";
        public const string MessageSyntaxUnterminatedComment = "unterminated comment";
        public const string MessageSyntaxMissingEndOfCompoundStatement = "missing } in compound statement";
        public const string MessageSyntaxInvalidToken = "invalid token \"{0}\"";
        public const string MessageSyntaxMissingForInitializer = "missing ; after for-loop initializer";
        public const string MessageSyntaxMissingForCondition = "missing ; after for-loop condition";
        public const string MessageSyntaxMissingForControl = "missing ) after for-loop control";
        public const string MessageSyntaxBreakOutsideLoop = "break must be inside loop or switch";
        public const string MessageSyntaxExpectedCompound = "expected compound statement, got {0}";
        public const string MessageSyntaxMissingBeforeTry = "missing { before try block";
        public const string MessageSyntaxMissingCatchOrFinally = "missing catch or finally after try";
        public const string MessageSyntaxCatchWithoutTry = "catch without try";
        public const string MessageSyntaxFinallyWithoutTry = "finally without try";
        public const string MessageSyntaxInvalidImportStatement = "invalid import statement";

        public const string MessageSyntaxClassExtendsMissing = "expected identifier after \"extends\" keyword";
        public const string MessageSyntaxClassIdentifierMissing = "expected class identifier";
        public const string MessageSyntaxClassInvalidStatement = "only var and function statements are allowed inside class definitions";
        public const string MessageSyntaxClassDuplicateDefinition = "{0} is already declared in {1}.";
        public const string MessageSyntaxClassInvalidVarDeclaration = "invalid variable declaration";
        public const string MessageSyntaxClassFunctionIndexerExpectedType = "function indexer expected indexer type";
        public const string MessageSyntaxClassFunctionIndexerInvalidType = "{0} is not a valid function indexer type";
        public const string MessageSyntaxClassInvalidFunctionSignature = "invalid function signature";
        public const string MessageSyntaxClassFunctionPropertyExpectedType = "function property expected property type";
        public const string MessageSyntaxClassFunctionPropertyInvalidType = "{0} is not a valid function property type";
        public const string MessageSyntaxClassIncompatibleSignature = "incompatible attributes assigned to class function signature";

        public const string MessageApiNotSupported = "this functionality is not supported";

        public const string MessageUserError = "throw statement executed";

        #endregion

        private readonly ScriptProcessor _processor;
        private SObject _errorObject;

        public bool ThrownError => _errorObject != null;

        public SObject ErrorObject
        {
            get { return _errorObject; }
            private set
            {
                if (_errorObject == null)
                    _errorObject = value;
            }
        }

        public ErrorHandler(ScriptProcessor processor)
        {
            _processor = processor;
        }

        internal void Clean()
        {
            _errorObject = null;
        }

        /// <summary>
        /// Throws an error with the given error object.
        /// </summary>
        public SObject ThrowError(SObject errorObject)
        {
            ErrorObject = errorObject;

            throw new ScriptException(ErrorObject);
        }

        /// <summary>
        /// Throws an error with the given <see cref="ErrorType"/> and error message.
        /// </summary>
        public SObject ThrowError(ErrorType errorType, string message, params object[] messageArgs)
        {
            var formattedMessage = string.Format(message, messageArgs);

            var errorObject = _processor.Context.CreateInstance("Error", new SObject[] { _processor.CreateString(formattedMessage),
                                                                                         _processor.CreateString(errorType.ToString()),
                                                                                         _processor.CreateNumber(_processor.GetLineNumber()) });

            return ThrowError(errorObject);
        }
    }
}
