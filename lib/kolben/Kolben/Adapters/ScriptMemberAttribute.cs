using System;

namespace Kolben.Adapters
{
    public abstract class ScriptMemberAttribute : Attribute
    {
        /// <summary>
        /// If this is set, the value of this property will be used as the variable name.
        /// </summary>
        public string VariableName { get; set; }
    }
}
