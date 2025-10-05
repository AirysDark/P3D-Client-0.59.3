using Kolben.Types;
using System;
using Kolben.Types.Prototypes;

namespace Kolben.Adapters
{
    /// <summary>
    /// Enables <see cref="ScriptContext"/> manipulations for Variables and Prototypes.
    /// </summary>
    public static class ScriptContextManipulator
    {
        /// <summary>
        /// Adds a new variable or overwrites one with the same name.
        /// </summary>
        public static void AddVariable(ScriptProcessor processor, string identifier, object data)
        {
            AddVariable(processor, identifier, ScriptInAdapter.Translate(processor, data));
        }

        /// <summary>
        /// Adds a new variable or overwrites one with the same name.
        /// </summary>
        public static void AddVariable(ScriptProcessor processor, string identifier, SObject data)
        {
            processor.Context.AddVariable(identifier, data);
        }

        /// <summary>
        /// Creates a <see cref="Prototype"/> from a .net <see cref="Type"/> and adds it to the context.
        /// </summary>
        /// <param name="processor">Reference to a processor instance.</param>
        /// <param name="t">The type from which to create the prototype. The name of the type will be used for the Prototype's name.</param>
        public static void AddPrototype(ScriptProcessor processor, Type t)
        {
            processor.Context.AddPrototype(ScriptInAdapter.TranslatePrototype(processor, t));
        }

        /// <summary>
        /// Returns the content of a variable, or Undefined, if the variable does not exist.
        /// </summary>
        public static SObject GetVariable(ScriptProcessor processor, string identifier)
        {
            if (processor.Context.IsVariable(identifier))
                return processor.Context.GetVariable(identifier).Data;
            else
                return processor.Undefined;
        }

        /// <summary>
        /// Returns the translated content of a variable.
        /// </summary>
        public static object GetVariableTranslated(ScriptProcessor processor, string identifier)
        {
            return ScriptOutAdapter.Translate(GetVariable(processor, identifier));
        }

        /// <summary>
        /// Returns if the context has a specific variable.
        /// </summary>
        public static bool HasVariable(ScriptProcessor processor, string identifier)
        {
            return processor.Context.IsVariable(identifier);
        }

        /// <summary>
        /// Sets the callback for checking if an API class has a member.
        /// </summary>
        public static void SetCallbackHasMember(ScriptProcessor processor, DHasMember callback)
        {
            processor.Context.AddCallback(CallbackType.HasMember, callback);
        }

        /// <summary>
        /// Sets the callback for getting a member of an API class.
        /// </summary>
        public static void SetCallbackGetMember(ScriptProcessor processor, DGetMember callback)
        {
            processor.Context.AddCallback(CallbackType.GetMember, callback);
        }

        /// <summary>
        /// Sets the callback for setting a member of an API class.
        /// </summary>
        public static void SetCallbackSetMember(ScriptProcessor processor, DSetMember callback)
        {
            processor.Context.AddCallback(CallbackType.SetMember, callback);
        }

        /// <summary>
        /// Sets the callback for executing a method of an API class.
        /// </summary>
        public static void SetCallbackExecuteMethod(ScriptProcessor processor, DExecuteMethod callback)
        {
            processor.Context.AddCallback(CallbackType.ExecuteMethod, callback);
        }

        /// <summary>
        /// Sets the callback for getting the content of a script file.
        /// </summary>
        public static void SetCallbackScriptPipeline(ScriptProcessor processor, DScriptPipeline callback)
        {
            processor.Context.AddCallback(CallbackType.ScriptPipeline, callback);
        }

        /// <summary>
        /// Returns if the script processor has thrown a script runtime exception.
        /// </summary>
        public static bool ThrownRuntimeError(ScriptProcessor processor)
        {
            return processor.ErrorHandler.ThrownError;
        }
    }
}
