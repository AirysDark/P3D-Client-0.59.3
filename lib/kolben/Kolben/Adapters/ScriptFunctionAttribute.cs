using System;

namespace Kolben.Adapters
{
    /// <summary>
    /// An attribute to add to methods that should get added as variables in adapted script objects.
    /// 
    /// The signature of the method has to conform to <see cref="BuiltInMethod"/>.
    /// 
    /// This can also be applied to string fields, creating source functions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    public class ScriptFunctionAttribute : ScriptMemberAttribute
    {
        public ScriptFunctionAttribute(ScriptFunctionType functionType)
        {
            FunctionType = functionType;
        }

        public bool IsStatic { get; set; }

        public ScriptFunctionType FunctionType { get; set; }
    }
}
