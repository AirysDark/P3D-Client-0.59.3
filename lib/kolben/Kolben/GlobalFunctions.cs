using Kolben.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
// ReSharper disable CanBeReplacedWithTryCastAndCheckForNull

namespace Kolben
{
    /// <summary>
    /// A class containing all globally accessible script functions.
    /// </summary>
    internal static class GlobalFunctions
    {
        /// <summary>
        /// Creates an array of script functions built from the static BuiltInMethods of this class.
        /// </summary>
        internal static List<SVariable> GetFunctions() =>
            BuiltInMethodManager.GetMethods(typeof(GlobalFunctions))
                .Select(m => new SVariable(m.Name, new SFunction(m.Delegate), true))
                .ToList();

        /// <summary>
        /// Mirrors the eval() function of JavaScript.
        /// </summary>
        [BuiltInMethod(MethodName = "eval")]
        public static SObject Eval(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
                return processor.Undefined;

            var parameterAsString = parameters[0] as SString;
            var code = parameterAsString != null ? parameterAsString.Value : parameters[0].ToString(processor).Value;

            var evalProcessor = new ScriptProcessor(processor.Context);
            return evalProcessor.Run(code);
        }

        [BuiltInMethod(MethodName = "sizeof")]
        public static SObject SizeOf(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters) =>
            parameters.Length == 0 ? processor.Undefined : processor.CreateNumber(parameters[0].SizeOf());

        [BuiltInMethod(MethodName = "typeof")]
        public static SObject TypeOf(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters) =>
            parameters.Length == 0 ? processor.Undefined : processor.CreateString(parameters[0].TypeOf());

        /// <summary>
        /// This is not like the C# nameof operator - it rather returns the actual typed name of the object, instead of just "object".
        /// </summary>
        [BuiltInMethod(MethodName = "nameof")]
        public static SObject NameOf(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
                return processor.Undefined;

            var parameterAsString = parameters[0] as SProtoObject;
            if (parameterAsString != null)
            {
                return processor.CreateString(parameterAsString.IsProtoInstance ? parameterAsString.Prototype.Name : parameterAsString.TypeOf());
            }
            return processor.CreateString(parameters[0].TypeOf());
        }

        /// <summary>
        /// Converts a primitive object (SString, SNumber, SBool) into their prototype counterparts.
        /// </summary>
        [BuiltInMethod(MethodName = "toComplex")]
        public static SObject ToComplex(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
                return processor.Undefined;

            if (parameters[0] is SString)
                return processor.Context.CreateInstance("String", new[] { parameters[0] });
            if (parameters[0] is SNumber)
                return processor.Context.CreateInstance("Number", new[] { parameters[0] });
            if (parameters[0] is SBool)
                return processor.Context.CreateInstance("Boolean", new[] { parameters[0] });
            return parameters[0]; // returns the input object, if no conversion was conducted.
        }

        /// <summary>
        /// Converts a complex object (SString, SNumber, SBool) back into their primitive counterparts.
        /// </summary>
        [BuiltInMethod(MethodName = "toPrimitive")]
        public static SObject ToPrimitive(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0) return processor.Undefined;
            if (parameters[0] is SString) return processor.CreateString(((SString)parameters[0]).Value);
            if (parameters[0] is SNumber) return processor.CreateNumber(((SNumber)parameters[0]).Value);
            if (parameters[0] is SBool) return processor.CreateBool(((SBool)parameters[0]).Value);
            return parameters[0]; // returns the input object, if no conversion was conducted.
        }

        [BuiltInMethod(MethodName = "isNaN")]
        public static SObject IsNaN(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
                return processor.Undefined;

            double dbl;
            if (parameters[0] is SNumber)
                dbl = ((SNumber)parameters[0]).Value;
            else
                dbl = parameters[0].ToNumber(processor).Value;

            return processor.CreateBool(double.IsNaN(dbl));
        }

        [BuiltInMethod(MethodName = "isFinite")]
        public static SObject IsFinite(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
                return processor.Undefined;

            var parameterAsNumber = parameters[0] as SNumber;
            var dbl = parameterAsNumber?.Value ?? parameters[0].ToNumber(processor).Value;

            return processor.CreateBool(!(double.IsNaN(dbl) || double.IsInfinity(dbl)));
        }

        [BuiltInMethod(MethodName = "sync")]
        public static SObject DoSync(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            string[] tasks = processor.Context.Parent.AsyncTasks.ToArray();
            if (parameters.Length >= 1)
            {
                var param = SObject.Unbox(parameters[0]);
                if (param is SString)
                    tasks = new[] { (param as SString).Value };
                else if (param is SArray)
                {
                    tasks = (param as SArray).ArrayMembers.Select(m => m.ToString(processor).Value).ToArray();
                }
            }

            Console.WriteLine($"Sync tasks: ({string.Join(",", tasks)})");

            SpinWait.SpinUntil(() => tasks.All(t => !processor.Context.Parent.AsyncTasks.Contains(t)));
            return processor.Undefined;
        }
    }
}
