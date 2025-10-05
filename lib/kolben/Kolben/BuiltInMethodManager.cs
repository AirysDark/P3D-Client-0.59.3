using Kolben.Types;
using Kolben.Types.Prototypes;
using System;
using System.Collections.Generic;
using System.Reflection;
using Kolben.Adapters;

namespace Kolben
{
    /// <summary>
    /// The delegate for hardcoded methods.
    /// </summary>
    /// <param name="instance">The calling instance.</param>
    /// <param name="processor">The script processor this call originates from.</param>
    /// <param name="This">The contextual "this" object.</param>
    /// <param name="parameters">Parameters for this method call.</param>
    public delegate SObject BuiltInMethod(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters);

    /// <summary>
    /// The delegate for a hardcoded method, with .Net native object types.
    /// </summary>
    /// <param name="parameters">Parameters for this method call.</param>
    public delegate object DotNetBuiltInMethod(object This, ScriptObjectLink objLink, object[] parameters);

    /// <summary>
    /// Searches for and creates built in method delegates.
    /// </summary>
    internal static class BuiltInMethodManager
    {
        /// <summary>
        /// Returns a list of methods with: Methodname, Method Attribute and Method Delegate.
        /// </summary>
        internal static List<BuiltInMethodData> GetMethods(Type t)
        {
            var list = new List<BuiltInMethodData>();
            var isPrototype = Prototype.IsPrototype(t);

            var methods = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<BuiltInMethodAttribute>(false);

                // Only proceed if the method has the correct attribute
                if (attribute != null && (!attribute.IsStatic || isPrototype))
                {
                    var usedMethodName = method.Name;

                    if (!string.IsNullOrEmpty(attribute.MethodName))
                        usedMethodName = attribute.MethodName;

                    list.Add(new BuiltInMethodData
                    {
                        Name = usedMethodName,
                        Attribute = attribute,
                        Delegate = (BuiltInMethod) Delegate.CreateDelegate(typeof(BuiltInMethod), method)
                    });
                }
            }

            return list;
        }
    }

    /// <summary>
    /// An attribute added to methods to mark them as built in methods of <see cref="Prototype"/>s.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class BuiltInMethodAttribute : Attribute
    {
        /// <summary>
        /// If this is set, the value of this property will be used as identifier instead of the method name of the method of this attribute.
        /// </summary>
        public string MethodName { get; set; }

        public bool IsStatic { get; set; }

        public bool IsIndexerGet { get; set; }

        public bool IsIndexerSet { get; set; }

        public FunctionUsageType FunctionType { get; set; }
    }
}
