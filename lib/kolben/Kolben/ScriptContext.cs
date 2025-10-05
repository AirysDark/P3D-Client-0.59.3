using Kolben.Adapters;
using Kolben.Types;
using Kolben.Types.Prototypes;
using System;
using System.Collections.Generic;

namespace Kolben
{
    /// <summary>
    /// Adds various context elements to a <see cref="ScriptProcessor"/> class instance.
    /// </summary>
    internal class ScriptContext
    {
        /// <summary>
        /// The parent context to this context. This context takes priority over its parent, if identifiers overlap.
        /// </summary>
        internal ScriptContext Parent { get; }
        /// <summary>
        /// The object that gets returned when the script references "this".
        /// </summary>
        internal SObject This { get; set; }
        /// <summary>
        /// Stores references to async tasks started by the script processor of this context.
        /// </summary>
        internal List<string> AsyncTasks { get; } = new List<string>();

        private readonly Dictionary<CallbackType, Delegate> _apiCallbacks = new Dictionary<CallbackType, Delegate>();

        private readonly Dictionary<string, SApiUsing> _apiUsings = new Dictionary<string, SApiUsing>();
        private readonly Dictionary<string, SVariable> _variables = new Dictionary<string, SVariable>();
        private readonly Dictionary<string, Prototype> _prototypes = new Dictionary<string, Prototype>();
        private readonly ScriptProcessor _processor;

        internal ScriptContext(ScriptProcessor processor, ScriptContext parent)
        {
            _processor = processor;
            Parent = parent;

            This = parent != null ? parent.This : new GlobalContextObject(this);
        }

        internal void Initialize()
        {
            if (Parent == null)
            {
                AddVariable(SObject.LiteralUndefined, SUndefined.Factory(), true);
                AddVariable(SObject.LiteralNull, SNull.Factory(), true);

                AddPrototype(new ObjectPrototype());
                AddPrototype(new BooleanPrototype());
                AddPrototype(new NumberPrototype());
                AddPrototype(new StringPrototype());
                AddPrototype(new ArrayPrototype());
                AddPrototype(new ErrorPrototype(_processor));

                GlobalFunctions.GetFunctions()
                    .ForEach(AddVariable);
            }
        }

        internal void AddCallback(CallbackType callbackType, Delegate callback)
        {
            // Adds or replaces a delegate in the delegate list.

            if (_apiCallbacks.ContainsKey(callbackType))
                _apiCallbacks.Add(callbackType, callback);
            else
                _apiCallbacks[callbackType] = callback;
        }

        internal bool HasCallback(CallbackType callbackType)
        {
            if (_apiCallbacks.ContainsKey(callbackType))
            {
                return true;
            }
            else
            {
                return Parent != null && Parent.HasCallback(callbackType);
            }
        }

        internal Delegate GetCallback(CallbackType callbackType)
        {
            if (_apiCallbacks.ContainsKey(callbackType))
            {
                return _apiCallbacks[callbackType];
            }
            else
            {
                return Parent?.GetCallback(callbackType);
            }
        }

        /// <summary>
        /// Returns if this context has a variable with a given identifier defined.
        /// </summary>
        internal bool IsVariable(string identifier)
        {
            if (_variables.ContainsKey(identifier))
            {
                return true;
            }
            else
            {
                return Parent != null && Parent.IsVariable(identifier);
            }
        }

        internal SVariable GetVariable(string identifier)
        {
            if (_variables.ContainsKey(identifier))
            {
                return _variables[identifier];
            }
            else
            {
                return Parent?.GetVariable(identifier);
            }
        }

        /// <summary>
        /// Adds a variable to the context.
        /// </summary>
        internal void AddVariable(string identifier, SObject data)
        {
            AddVariable(new SVariable(identifier, data));
        }

        /// <summary>
        /// Adds a variable to the context and sets the readonly property.
        /// </summary>
        internal void AddVariable(string identifier, SObject data, bool isReadOnly)
        {
            AddVariable(new SVariable(identifier, data, isReadOnly));
        }

        /// <summary>
        /// Adds a variable to the context.
        /// </summary>
        internal void AddVariable(SVariable variable)
        {
            if (_variables.ContainsKey(variable.Identifier))
            {
                _variables[variable.Identifier] = variable;
            }
            else
            {
                _variables.Add(variable.Identifier, variable);
            }
        }

        internal bool IsApiUsing(string identifier)
        {
            if (_apiUsings.ContainsKey(identifier))
            {
                return true;
            }
            else
            {
                if (Parent != null)
                {
                    return Parent.IsApiUsing(identifier);
                }
            }
            return false;
        }

        internal SApiUsing GetApiUsing(string identifier)
        {
            if (_apiUsings.ContainsKey(identifier))
            {
                return _apiUsings[identifier];
            }
            else
            {
                return Parent?.GetApiUsing(identifier);
            }
        }

        internal void AddApiUsing(SApiUsing apiUsing)
        {
            if (!_apiUsings.ContainsKey(apiUsing.ApiClass))
                _apiUsings.Add(apiUsing.ApiClass, apiUsing);
        }

        internal bool IsPrototype(string identifier)
        {
            if (_prototypes.ContainsKey(identifier))
            {
                return true;
            }
            else
            {
                if (Parent != null)
                {
                    return Parent.IsPrototype(identifier);
                }
            }
            return false;
        }

        internal Prototype GetPrototype(string identifier)
        {
            if (_prototypes.ContainsKey(identifier))
            {
                return _prototypes[identifier];
            }
            else
            {
                return Parent?.GetPrototype(identifier);
            }
        }

        internal void AddPrototype(Prototype prototype)
        {
            if (_prototypes.ContainsKey(prototype.Name))
                _prototypes[prototype.Name] = prototype;
            else
                _prototypes.Add(prototype.Name, prototype);
        }

        /// <summary>
        /// Creates an instance with the given prototype name.
        /// </summary>
        internal SObject CreateInstance(string prototypeName, SObject[] parameters)
        {
            return CreateInstance(GetPrototype(prototypeName), parameters);
        }

        /// <summary>
        /// Creates an instance of the given prototype.
        /// </summary>
        internal SObject CreateInstance(Prototype prototype, SObject[] parameters)
        {
            if (!prototype.IsAbstract)
                return prototype.CreateInstance(_processor, parameters, true);
            else
                return _processor.ErrorHandler.ThrowError(ErrorType.TypeError, ErrorHandler.MessageTypeAbstractNoInstance);
        }

        /// <summary>
        /// Creates an instance from a "new" operator.
        /// </summary>
        internal SObject CreateInstance(string exp)
        {
            exp = exp.Remove(0, "new ".Length).Trim();

            var prototypeName = exp.Remove(exp.IndexOf("(", StringComparison.Ordinal));
            var prototype = GetPrototype(prototypeName);

            if (prototype == null)
                _processor.ErrorHandler.ThrowError(ErrorType.ReferenceError, ErrorHandler.MessageReferenceNotDefined, prototypeName);

            var argCode = exp.Remove(0, exp.IndexOf("(", StringComparison.Ordinal) + 1);
            argCode = argCode.Remove(argCode.Length - 1, 1);

            var parameters = _processor.ParseParameters(argCode);

            return CreateInstance(prototypeName, parameters);
        }
    }
}
