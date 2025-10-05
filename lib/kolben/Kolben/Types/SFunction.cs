using System;
using System.Linq;
using Kolben.Adapters;

namespace Kolben.Types
{
    /// <summary>
    /// Represents a function object that can be called in the script.
    /// </summary>
    internal class SFunction : SObject
    {
        private readonly string[] _parameters;

        /// <summary>
        /// The code body of the function.
        /// </summary>
        public string Body { get; set; }

        public FunctionUsageType FunctionUsage { get; set; }

        public BuiltInMethod Method { get; set; }

        public DotNetBuiltInMethod DotNetMethod { get; set; }

        private bool HasBuiltInMethod => Method != null || DotNetMethod != null;

        public SFunction(string body, string[] parameters)
        {
            Body = body;
            _parameters = parameters;
        }

        /// <summary>
        /// Initializes an instance with a script code signature and body.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="sourceCode">The source code, format: <code>function (params) { code }</code></param>
        public SFunction(ScriptProcessor processor, string sourceCode)
        {
            sourceCode = sourceCode.Trim();
            var paramCode = sourceCode.Remove(0, "function".Length).Trim().Remove(0, 1); //Removes "function", then any spaces between "function" and "(", then removes "(".
            paramCode = paramCode.Remove(paramCode.IndexOf(")", StringComparison.Ordinal));

            _parameters = paramCode.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();

            var allIdentifiersValid = true;
            var i = 0;

            while (i < _parameters.Length - 1 && allIdentifiersValid)
            {
                if (!ScriptProcessor.IsValidIdentifier(_parameters[i]))
                {
                    allIdentifiersValid = false;
                }

                i++;
            }

            if (!allIdentifiersValid)
            {
                processor.ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingFormalParameter);
            }
            else
            {
                if (!sourceCode.Contains("{") || !sourceCode.EndsWith("}"))
                {
                    processor.ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingFunctionBody);
                }
                else
                {
                    Body = sourceCode.Remove(sourceCode.Length - 1, 1).Remove(0, sourceCode.IndexOf("{", StringComparison.Ordinal) + 1).Trim();
                }
            }
        }

        /// <summary>
        /// Initializes an instance with a built in method.
        /// </summary>
        public SFunction(BuiltInMethod method)
            : this((Delegate)method)
        { }

        /// <summary>
        /// Initializes an instance with a built in method.
        /// </summary>
        public SFunction(Delegate method)
        {
            if (method.GetType() == typeof(BuiltInMethod))
            {
                Method = (BuiltInMethod)method;
            }
            else if (method.GetType() == typeof(DotNetBuiltInMethod))
            {
                DotNetMethod = (DotNetBuiltInMethod)method;
            }
        }

        internal override string TypeOf()
        {
            return LiteralTypeFunction;
        }

        internal override double SizeOf()
        {
            return HasBuiltInMethod ? 1 : Body.Length;
        }

        private const string FunctionSourceFormat = "function({0}) {{ {1} }}";
        private const string FunctionNativeCodeSource = "[native code]";

        internal override string ToScriptSource()
        {
            var paramSource = "";
            if (_parameters != null)
            {
                foreach (var par in _parameters)
                {
                    if (paramSource.Length > 0)
                    {
                        paramSource += ", ";
                    }
                    paramSource += par;
                }
            }

            var bodySource = HasBuiltInMethod ? FunctionNativeCodeSource : Body;

            return string.Format(FunctionSourceFormat, paramSource, bodySource);
        }

        internal override string ToScriptObject()
        {
            return ObjectBuffer.ObjPrefix + ObjectBuffer.GetObjectId(this).ToString();
        }

        internal override SObject ExecuteMethod(ScriptProcessor processor, string methodName, SObject caller, SObject This, SObject[] parameters)
        {
            return Call(processor, caller, This, parameters);
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <param name="processor">The processor with context that called this functions.</param>
        /// <param name="caller">The calling object.</param>
        /// <param name="This">The "This" reference used in the call context.</param>
        /// <param name="parameters">The parameters used in this function call.</param>
        public SObject Call(ScriptProcessor processor, SObject caller, SObject This, SObject[] parameters)
        {
            var functionProcessor = new ScriptProcessor(processor.Context, processor.GetLineNumber());
            SObject functionReturnObject;

            if (HasBuiltInMethod)
            {
                if (Method != null)
                {
                    functionReturnObject = Method(functionProcessor, caller, This, parameters);
                }
                else
                {
                    var dotNetParams = parameters.Select(ScriptOutAdapter.Translate);
                    var dotNetReturnObj = DotNetMethod(ScriptOutAdapter.Translate(caller), new ScriptObjectLink(processor, caller), dotNetParams.ToArray());
                    functionReturnObject = ScriptInAdapter.Translate(processor, dotNetReturnObj);
                }
            }
            else
            {
                for (var i = 0; i < _parameters.Length; i++)
                {
                    functionProcessor.Context.AddVariable(_parameters[i],
                        parameters.Length > i ? parameters[i] : functionProcessor.Undefined);
                }

                functionProcessor.Context.This = This;
                functionReturnObject = functionProcessor.Run(Body);
            }

            return functionReturnObject;
        }
    }
}
