using Kolben.Adapters;
using System.Threading.Tasks;

namespace Kolben.Types
{
    /// <summary>
    /// Represents a link to an API of another application imported via "using" statement.
    /// </summary>
    internal class SApiUsing : SObject
    {
        /// <summary>
        /// The name of the API class.
        /// </summary>
        public string ApiClass { get; }

        /// <summary>
        /// The source of the API class.
        /// </summary>
        public string ModuleName { get; }

        public SApiUsing(string apiClass, string moduleName)
        {
            ApiClass = apiClass;
            ModuleName = moduleName;
        }

        internal override void SetMember(ScriptProcessor processor, SObject accessor, bool isIndexer, SObject value)
        {
            if (processor.Context.HasCallback(CallbackType.SetMember))
            {
                var callback = (DSetMember)processor.Context.GetCallback(CallbackType.SetMember);
                var task = Task.Factory.StartNew(() => callback(processor, ModuleName, accessor, isIndexer, value));
                task.Wait();
            }
            else
            {
                processor.ErrorHandler.ThrowError(ErrorType.ApiError, ErrorHandler.MessageApiNotSupported);
            }
        }

        internal override SObject ExecuteMethod(ScriptProcessor processor, string methodName, SObject caller, SObject This, SObject[] parameters)
        {
            if (processor.Context.HasCallback(CallbackType.ExecuteMethod))
            {
                var callback = (DExecuteMethod)processor.Context.GetCallback(CallbackType.ExecuteMethod);
                var task = Task<SObject>.Factory.StartNew(() => callback(processor, ModuleName, methodName, parameters));
                task.Wait();

                return task.Result;
            }
            else
            {
                processor.ErrorHandler.ThrowError(ErrorType.ApiError, ErrorHandler.MessageApiNotSupported);
                return processor.Undefined;
            }
        }

        internal override SObject GetMember(ScriptProcessor processor, SObject accessor, bool isIndexer)
        {
            if (processor.Context.HasCallback(CallbackType.GetMember))
            {
                var callback = (DGetMember)processor.Context.GetCallback(CallbackType.GetMember);
                var task = Task<SObject>.Factory.StartNew(() => callback(processor, ModuleName, accessor, isIndexer));
                task.Wait();

                return task.Result;
            }
            else
            {
                processor.ErrorHandler.ThrowError(ErrorType.ApiError, ErrorHandler.MessageApiNotSupported);
                return processor.Undefined;
            }
        }

        internal override bool HasMember(ScriptProcessor processor, string memberName)
        {
            if (processor.Context.HasCallback(CallbackType.HasMember))
            {
                var callback = (DHasMember)processor.Context.GetCallback(CallbackType.HasMember);
                var task = Task<bool>.Factory.StartNew(() => callback(processor, ModuleName, memberName));
                task.Wait();

                return task.Result;
            }
            else
            {
                // If no API callback function is added to check for member existence, then we just assume that no member exists and return false.
                return false;
            }
        }

        internal override string ToScriptObject()
        {
            return ApiClass;
        }

        internal override string ToScriptSource()
        {
            return ApiClass;
        }

        internal override double SizeOf()
        {
            return ApiClass.Length;
        }
    }
}
