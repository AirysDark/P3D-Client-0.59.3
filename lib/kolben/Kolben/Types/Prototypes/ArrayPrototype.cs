using System;
using System.Collections.Generic;
using System.Linq;

namespace Kolben.Types.Prototypes
{
    internal class ArrayPrototype : Prototype
    {
        private const string ErrorSingleMultipleElements = "The sequence contains multiple matching elements.";

        public ArrayPrototype() : base("Array")
        {
            Constructor = new PrototypeMember("Constructor", new SFunction(ConstructorImpl));
        }

        protected override SProtoObject CreateBaseObject()
        {
            return new SArray();
        }

        private static SObject ConstructorImpl(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var arr = (SArray)instance;

            var length = (int)((SNumber)parameters[0]).Value;
            arr.ArrayMembers = new SObject[length];

            Array.Copy(parameters, 1, arr.ArrayMembers, 0, parameters.Length - 1);
            return arr;
        }

        [BuiltInMethod(IsIndexerSet = true, MethodName = "IndexerSet")]
        public static SObject IndexerSet(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var arr = (SArray)instance;

            if (parameters.Length >= 2)
            {
                var accessor = (int)parameters[0].ToNumber(processor).Value;

                if (accessor >= 0)
                {
                    if (accessor < arr.ArrayMembers.Length)
                    {
                        arr.ArrayMembers[accessor] = parameters[1];
                    }
                    else
                    {
                        var arrMembers = arr.ArrayMembers;
                        Array.Resize(ref arrMembers, accessor + 1);
                        arrMembers[accessor] = parameters[1];
                        arr.ArrayMembers = arrMembers;
                    }
                }
            }

            return processor.Undefined;
        }

        [BuiltInMethod(IsIndexerGet = true, MethodName = "indexerGet")]
        public static SObject IndexerGet(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var arr = (SArray)instance;

            if (parameters.Length >= 1)
            {
                var accessor = (int)parameters[0].ToNumber(processor).Value;

                if (accessor >= 0 && accessor < arr.ArrayMembers.Length)
                {
                    return arr.ArrayMembers[accessor] ??
                            processor.Undefined;
                }
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.PropertyGetter, MethodName = "length")]
        public static SObject Length(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var arr = (SArray)instance;
            return processor.CreateNumber(arr.ArrayMembers.Length);
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "includes")]
        public static SObject Includes(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 1)
            {
                var arr = (SArray)instance;
                var compare = parameters[0];

                return processor.CreateBool(arr.ArrayMembers.Any(m => ObjectComparer.LooseEquals(processor, m, compare)));
            }

            if (parameters.Length >= 2)
            {
                var arr = (SArray)instance;
                var compare = parameters[0];
                var comparer = (SFunction)Unbox(parameters[1]);

                return processor.CreateBool(arr.ArrayMembers.Any(m => ((SBool)comparer.Call(processor, This, This, new[] { m, compare })).Value));
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "any")]
        public static SObject Any(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
            {
                var arr = (SArray)instance;
                return processor.CreateBool(arr.ArrayMembers.Length > 0);
            }

            if (parameters.Length >= 1)
            {
                var arr = (SArray)instance;
                var comparer = (SFunction)Unbox(parameters[0]);

                return processor.CreateBool(arr.ArrayMembers.Any(m => ((SBool)comparer.Call(processor, This, This, new[] { m })).Value));
            }
            
            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "where")]
        public static SObject Where(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length >= 1)
            {
                var arr = (SArray)instance;
                var comparer = (SFunction)Unbox(parameters[0]);

                var results = arr.ArrayMembers.Where((m, i) => ((SBool)comparer.Call(processor, This, This, new[] {m, processor.CreateNumber(i)})).Value);
                return processor.CreateArray(results.ToArray());
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "first")]
        public static SObject First(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
            {
                var arr = (SArray)instance;

                if (arr.ArrayMembers.Length == 0)
                    return processor.Undefined;
                else
                    return arr.ArrayMembers[0];
            }

            if (parameters.Length >= 1)
            {
                var arr = (SArray)instance;
                var comparer = (SFunction)Unbox(parameters[0]);

                var result = arr.ArrayMembers.FirstOrDefault(m => ((SBool)comparer.Call(processor, This, This, new[] { m })).Value);
                return result ?? processor.Undefined;
            }

            return processor.Undefined;
        }
        
        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "last")]
        public static SObject Last(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
            {
                var arr = (SArray)instance;

                if (arr.ArrayMembers.Length == 0)
                    return processor.Undefined;
                else
                    return arr.ArrayMembers.Last();
            }

            if (parameters.Length >= 1)
            {
                var arr = (SArray)instance;
                var comparer = (SFunction)Unbox(parameters[0]);

                var result = arr.ArrayMembers.LastOrDefault(m => ((SBool)comparer.Call(processor, This, This, new[] { m })).Value);
                return result ?? processor.Undefined;
            }

            return processor.Undefined;
        }
        
        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "select")]
        public static SObject Select(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length >= 1)
            {
                var arr = (SArray)instance;
                var comparer = (SFunction)Unbox(parameters[0]);

                var result = arr.ArrayMembers.Select(m => comparer.Call(processor, This, This, new[] { m }));
                return processor.CreateArray(result.ToArray());
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "single")]
        public static SObject Single(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
            {
                var arr = (SArray)instance;

                if (arr.ArrayMembers.Length == 1)
                    return arr.ArrayMembers[0];
                else
                    return processor.ErrorHandler.ThrowError(ErrorType.UserError, ErrorSingleMultipleElements);
            }

            if (parameters.Length >= 1)
            {
                var arr = (SArray)instance;
                var comparer = (SFunction)Unbox(parameters[0]);

                var result = arr.ArrayMembers.Where(m => ((SBool)comparer.Call(processor, This, This, new[] { m })).Value).ToArray();

                return result.Length == 1 ? result.ElementAt(0) : processor.ErrorHandler.ThrowError(ErrorType.UserError, ErrorSingleMultipleElements);
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "count")]
        public static SObject Count(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length == 0)
            {
                var arr = (SArray)instance;
                return processor.CreateNumber(arr.ArrayMembers.Length);
            }

            if (parameters.Length >= 1)
            {
                var arr = (SArray)instance;
                var comparer = (SFunction)Unbox(parameters[0]);

                var result = arr.ArrayMembers.Count(m => ((SBool)comparer.Call(processor, This, This, new[] { m })).Value);
                return processor.CreateNumber(result);
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "all")]
        public static SObject All(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length >= 1)
            {
                var arr = (SArray)instance;
                var comparer = (SFunction)Unbox(parameters[0]);
                
                var result = arr.ArrayMembers.All(m => ((SBool)comparer.Call(processor, This, This, new[] { m })).Value);
                return processor.CreateBool(result);
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "push")]
        public static SObject Push(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length > 0)
            {
                var arr = (SArray) instance;
                var addItems = new List<SObject>(arr.ArrayMembers);
                foreach (var param in parameters)
                {
                    var unboxed = Unbox(param);
                    if (unboxed is SArray)
                        addItems.AddRange((unboxed as SArray).ArrayMembers);
                    else
                        addItems.Add(unboxed);
                }
                
                arr.ArrayMembers = addItems.ToArray();
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "pop")]
        public static SObject Pop(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var arr = (SArray)instance;
    
            if (arr.ArrayMembers.Length > 0)
            {
                var newItems = new SObject[arr.ArrayMembers.Length - 1];
                var returnItem = arr.ArrayMembers.Last();
                Array.Copy(arr.ArrayMembers, newItems, arr.ArrayMembers.Length - 1);
                arr.ArrayMembers = newItems;

                return returnItem;
            }

            return processor.Undefined;
        }
    }
}
