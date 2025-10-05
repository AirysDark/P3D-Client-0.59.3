using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Kolben.Types
{
    /// <summary>
    /// Represents an array : a collection of objects. Not type-safe.
    /// </summary>
    internal class SArray : SProtoObject
    {
        private const string RegexEmptyArray = @"\[\s*\]";

        /// <summary>
        /// The members of this array.
        /// </summary>
        public SObject[] ArrayMembers { get; set; }

        /// <summary>
        /// Parses a string as an array.
        /// </summary>
        internal new static SObject Parse(ScriptProcessor processor, string exp)
        {
            // Format: [item1, item2, ... itemn]

            if (Regex.IsMatch(exp, RegexEmptyArray)) return processor.CreateArray(0);

            exp = exp.Remove(exp.Length - 1, 1).Remove(0, 1).Trim(); // Remove [ and ].

            var elements = new List<SObject>();

            var elementStart = 0;
            var index = 0;
            var depth = 0;
            StringEscapeHelper escaper = new LeftToRightStringEscapeHelper(exp, 0);
            string element;

            while (index < exp.Length)
            {
                var t = exp[index];
                escaper.CheckStartAt(index);

                if (!escaper.IsString)
                {
                    if (t == '{' || t == '[' || t == '(')
                    {
                        depth++;
                    }
                    else if (t == '}' || t == ']' || t == ')')
                    {
                        depth--;
                    }
                    else if (t == ',' && depth == 0)
                    {
                        element = exp.Substring(elementStart, index - elementStart);

                        elements.Add(string.IsNullOrWhiteSpace(element)
                            ? processor.Undefined
                            : processor.ExecuteStatement(new ScriptStatement(element)));

                        elementStart = index + 1;
                    }
                }

                index++;
            }

            element = exp.Substring(elementStart, index - elementStart);

            elements.Add(string.IsNullOrWhiteSpace(element)
                ? processor.Undefined
                : processor.ExecuteStatement(new ScriptStatement(element)));

            return processor.CreateArray(elements.ToArray());
        }
        
        internal override string ToScriptSource()
        {
            var source = new StringBuilder();

            foreach (var arrItem in ArrayMembers)
            {
                if (source.Length > 0)
                    source.Append(",");

                if (!ReferenceEquals(Unbox(arrItem), this))
                {
                    source.Append(arrItem.ToScriptSource());
                }
            }

            return source.ToString();
        }

        internal override double SizeOf()
        {
            return ArrayMembers.Length;
        }

        internal override SString ToString(ScriptProcessor processor)
        {
            return processor.CreateString(ToScriptSource());
        }
    }
}
