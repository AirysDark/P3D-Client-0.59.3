using System;

namespace Kolben.Adapters
{
    /// <summary>
    /// An attribute to add to fields that should get added as variables in adapted script objects.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ScriptVariableAttribute : ScriptMemberAttribute
    { }
}
