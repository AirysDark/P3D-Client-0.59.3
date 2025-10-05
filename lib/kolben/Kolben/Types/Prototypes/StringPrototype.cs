using System;
using System.Linq;

namespace Kolben.Types.Prototypes
{
    internal class StringPrototype : Prototype
    {
        public StringPrototype() : base("String")
        {
            Constructor = new PrototypeMember("constructor", new SFunction(ConstructorImpl));
        }

        protected override SProtoObject CreateBaseObject()
        {
            return new SString();
        }

        private static SObject ConstructorImpl(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var obj = (SString)instance;

            var parameterAsString = parameters[0] as SString;
            if (parameterAsString != null)
            {
                obj.Value = parameterAsString.Value;
                obj.Escaped = parameterAsString.Escaped;
            }
            else
            {
                obj.Value = parameters[0].ToString(processor).Value;
            }

            return obj;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.PropertyGetter, MethodName = "length")]
        public static SObject Length(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            return processor.CreateNumber(((SString) instance).Value.Length);
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.PropertyGetter, IsStatic = true, MethodName = "empty")]
        public static SObject Empty(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            return processor.CreateString("");
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "charAt")]
        public static SObject CharAt(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var str = ((SString) instance).Value;

            if (str == "")
                return processor.CreateString("");

            if (TypeContract.Ensure(parameters, typeof(SNumber)))
            {
                var index = (int)((SNumber) parameters[0]).Value;

                if (index < 0 || index >= str.Length)
                    return processor.CreateString("");

                return processor.CreateString(str[index].ToString());
            }
            else
            {
                return processor.CreateString(str[0].ToString());
            }
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "concat")]
        public static SObject Concat(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length > 0)
            {
                var str = (SString) instance;

                string result = str.Value;
                foreach (var param in parameters)
                {
                    string concatStr = param.ToString(processor).Value;
                    result += concatStr;
                }

                return processor.CreateString(result);
            }

            return processor.CreateString(((SString) instance).Value);
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "includes")]
        public static SObject Includes(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (parameters.Length >= 1)
            {
                var str = (SString) instance;
                var includes = parameters[0].ToString(processor);

                return processor.CreateBool(str.Value.Contains(includes.Value));
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "endsWith")]
        public static SObject EndsWith(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (TypeContract.Ensure(parameters, typeof(SString)))
            {
                var str = (SString) instance;
                var includes = (SString) parameters[0];

                if (includes.Value == "")
                    return processor.CreateBool(str.Value == "");

                return processor.CreateBool(str.Value.EndsWith(includes.Value));
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "startsWith")]
        public static SObject StartsWith(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (TypeContract.Ensure(parameters, typeof(SString)))
            {
                var str = (SString) instance;
                var includes = (SString) parameters[0];

                if (includes.Value == "")
                    return processor.CreateBool(str.Value == "");

                return processor.CreateBool(str.Value.StartsWith(includes.Value));
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "indexOf")]
        public static SObject IndexOf(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (TypeContract.Ensure(parameters, typeof(SString)))
            {
                var str = (SString) instance;
                var search = (SString) parameters[0];

                if (!str.Value.Contains(search.Value) || search.Value == "")
                    return processor.CreateNumber(-1);

                return processor.CreateNumber(str.Value.IndexOf(search.Value, StringComparison.Ordinal));
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "lastIndexOf")]
        public static SObject LastIndexOf(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (TypeContract.Ensure(parameters, typeof(SString)))
            {
                var str = (SString) instance;
                var search = (SString) parameters[0];

                if (!str.Value.Contains(search.Value) || search.Value == "")
                    return processor.CreateNumber(-1);

                return processor.CreateNumber(str.Value.LastIndexOf(search.Value, StringComparison.Ordinal));
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "padEnd")]
        public static SObject PadEnd(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var str = ((SString) instance).Value;
            int totalLength = 0;
            var padStr = " ";

            if (TypeContract.Ensure(parameters, new[] { typeof(SNumber), typeof(SString) }))
            {
                totalLength = (int)((SNumber) parameters[0]).Value;
                padStr = ((SString) parameters[1]).Value;
            }
            if (TypeContract.Ensure(parameters, typeof(SNumber)))
            {
                totalLength = (int)((SNumber) parameters[0]).Value;
            }

            if (padStr == "" || totalLength <= str.Length)
                return processor.CreateString(str);

            while (str.Length < totalLength)
                str += padStr;

            if (str.Length > totalLength)
                str = str.Remove(totalLength);

            return processor.CreateString(str);
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "padStart")]
        public static SObject PadStart(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var str = ((SString) instance).Value;
            int totalLength = 0;
            var padStr = " ";
            string newPadded = "";

            if (TypeContract.Ensure(parameters, new[] { typeof(SNumber), typeof(SString) }))
            {
                totalLength = (int)((SNumber) parameters[0]).Value;
                padStr = ((SString) parameters[1]).Value;
            }
            if (TypeContract.Ensure(parameters, typeof(SNumber)))
            {
                totalLength = (int)((SNumber) parameters[0]).Value;
            }

            if (padStr == "" || totalLength <= str.Length)
                return processor.CreateString(str);

            while (newPadded.Length + str.Length < totalLength)
                newPadded += padStr;

            if (newPadded.Length + str.Length > totalLength)
                newPadded = newPadded.Remove(totalLength - str.Length);

            str = newPadded + str;

            return processor.CreateString(str);
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "repeat")]
        public static SObject Repeat(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (TypeContract.Ensure(parameters, typeof(SNumber)))
            {
                var repeatStr = ((SString) instance).Value;
                var times = (int)((SNumber) parameters[0]).Value;
                var str = "";

                for (int i = 0; i < times; i++)
                    str += repeatStr;

                return processor.CreateString(str);
            }

            return processor.CreateString(((SString) instance).Value);
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "replace")]
        public static SObject Replace(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (TypeContract.Ensure(parameters, new[] { typeof(SString), typeof(SString) }))
            {
                var str = ((SString) instance).Value;
                var replace = ((SString) parameters[0]).Value;
                var with = ((SString) parameters[1]).Value;

                return processor.CreateString(string.IsNullOrWhiteSpace(replace) ? str : str.Replace(replace, with));
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "slice")]
        public static SObject Slice(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (TypeContract.Ensure(parameters, new[] { typeof(SNumber), typeof(SNumber) }))
            {
                var str = ((SString) instance).Value;
                var sliceBegin = (int)((SNumber) parameters[0]).Value;
                var sliceEnd = (int)((SNumber) parameters[1]).Value;

                if (sliceBegin < 0)
                    sliceBegin += str.Length;
                if (sliceBegin < 0)
                    sliceBegin = 0;
                if (sliceBegin > str.Length)
                    return processor.CreateString("");

                if (sliceEnd < 0)
                    sliceEnd += str.Length;
                if (sliceEnd < 0)
                    sliceEnd = 0;
                if (sliceEnd > str.Length)
                    sliceEnd = str.Length;

                return processor.CreateString(str.Substring(sliceBegin, sliceEnd - sliceBegin));
            }
            if (TypeContract.Ensure(parameters, typeof(SNumber)))
            {
                var str = ((SString) instance).Value;
                var sliceBegin = (int)((SNumber) parameters[0]).Value;

                if (sliceBegin < 0)
                    sliceBegin += str.Length;
                if (sliceBegin < 0)
                    sliceBegin = 0;
                if (sliceBegin > str.Length)
                    return processor.CreateString("");

                return processor.CreateString(str.Substring(sliceBegin));
            }

            return processor.Undefined;
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "split")]
        public static SObject Split(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            int limit = -1;
            string[] delimiters;
            string str = ((SString) instance).Value;

            if (TypeContract.Ensure(parameters, new[] { typeof(SArray), typeof(SNumber) }))
            {
                delimiters = ((SArray) parameters[0]).ArrayMembers.Select(m => m.ToString(processor).Value).ToArray();
                limit = (int)((SNumber) parameters[1]).Value;
            }
            else if (TypeContract.Ensure(parameters, typeof(SArray)))
            {
                delimiters = ((SArray) parameters[0]).ArrayMembers.Select(m => m.ToString(processor).Value).ToArray();
            }
            else if (TypeContract.Ensure(parameters, new[] { typeof(SString), typeof(SNumber) }))
            {
                delimiters = new[] { ((SString) parameters[0]).Value };
                limit = (int)((SNumber) parameters[1]).Value;
            }
            else if (TypeContract.Ensure(parameters, typeof(SString)))
            {
                delimiters = new[] { ((SString) parameters[0]).Value };
            }
            else
            {
                return processor.CreateArray(new SObject[] { processor.CreateString(str) });
            }

            var split = str.Split(delimiters, StringSplitOptions.None);
            if (limit >= 0 && split.Length > limit)
            {
                var result = new string[limit];
                Array.Copy(split, result, limit);
                split = result;
            }

            return processor.CreateArray(split.Select(processor.CreateString).ToArray<SObject>());
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "toLower")]
        public static SObject ToLower(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            string str = ((SString) instance).Value;
            return processor.CreateString(str.ToLower());
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "toUpper")]
        public static SObject ToUpper(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            string str = ((SString) instance).Value;
            return processor.CreateString(str.ToUpper());
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "trim")]
        public static SObject Trim(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            string str = ((SString) instance).Value;
            string[] trimChars = { " " };

            if (TypeContract.Ensure(parameters, typeof(SArray)))
            {
                trimChars = ((SArray) parameters[0]).ArrayMembers.Select(m => m.ToString(processor).Value).ToArray();
            }
            else if (TypeContract.Ensure(parameters, typeof(SString)))
            {
                trimChars = new[] { ((SString) parameters[0]).Value };
            }

            foreach (var trimChar in trimChars)
            {
                while (str.StartsWith(trimChar))
                    str = str.Remove(0, trimChar.Length);
                while (str.EndsWith(trimChar))
                    str = str.Remove(str.Length - trimChar.Length);
            }

            return processor.CreateString(str);
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "trimStart")]
        public static SObject TrimStart(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            string str = ((SString) instance).Value;
            string[] trimChars = { " " };

            if (TypeContract.Ensure(parameters, typeof(SArray)))
            {
                trimChars = ((SArray) parameters[0]).ArrayMembers.Select(m => m.ToString(processor).Value).ToArray();
            }
            else if (TypeContract.Ensure(parameters, typeof(SString)))
            {
                trimChars = new[] { ((SString) parameters[0]).Value };
            }

            foreach (var trimChar in trimChars)
            {
                while (str.StartsWith(trimChar))
                    str = str.Remove(0, trimChar.Length);
            }

            return processor.CreateString(str);
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "trimEnd")]
        public static SObject TrimEnd(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            string str = ((SString) instance).Value;
            string[] trimChars = { " " };

            if (TypeContract.Ensure(parameters, typeof(SArray)))
            {
                trimChars = ((SArray) parameters[0]).ArrayMembers.Select(m => m.ToString(processor).Value).ToArray();
            }
            else if (TypeContract.Ensure(parameters, typeof(SString)))
            {
                trimChars = new[] { ((SString) parameters[0]).Value };
            }

            foreach (var trimChar in trimChars)
            {
                while (str.EndsWith(trimChar))
                    str = str.Remove(str.Length - trimChar.Length);
            }

            return processor.CreateString(str);
        }

        [BuiltInMethod(FunctionType = FunctionUsageType.Default, MethodName = "remove")]
        public static SObject Remove(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            if (TypeContract.Ensure(parameters, new[] { typeof(SNumber), typeof(SNumber) }))
            {
                var str = ((SString) instance).Value;
                var start = (int)((SNumber) parameters[0]).Value;
                var length = (int)((SNumber) parameters[1]).Value;

                if (start < 0)
                    start = 0;
                if (start > str.Length)
                    return processor.CreateString(str);

                if (length <= 0)
                    return processor.CreateString(str);
                if (length + start > str.Length)
                    length = str.Length - start;

                return processor.CreateString(str.Remove(start, length));
            }
            if (TypeContract.Ensure(parameters, typeof(SNumber)))
            {
                var str = ((SString) instance).Value;
                var start = (int)((SNumber) parameters[0]).Value;
                
                if (start < 0)
                    start = 0;
                if (start > str.Length)
                    return processor.CreateString(str);

                return processor.CreateString(str.Remove(start));
            }

            return processor.Undefined;
        }
    }
}
