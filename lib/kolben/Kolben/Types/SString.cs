using System;

namespace Kolben.Types
{
    internal class SString : SProtoObject
    {
        internal const string StringLengthPropertyName = "length";

        private const string StringNormalFormat = "\"{0}\"";
        private const string StringUnescapedFormat = "@\"{0}\"";

        private static string Unescape(string val)
        {
            if (val.Contains("\\"))
            {
                var searchOffset = 0;

                while (val.IndexOf("\\", searchOffset, StringComparison.Ordinal) > -1)
                {
                    var cIndex = val.IndexOf("\\", StringComparison.Ordinal);

                    if (cIndex < val.Length - 1) //When the \ is not the last character:
                    {
                        var escapeSequenceChar = val[cIndex + 1];
                        string insert;

                        switch (escapeSequenceChar)
                        {
                            case '0':
                                insert = ((char)0).ToString();
                                break;
                            case '\'':
                                insert = "\'";
                                break;
                            case '\"':
                                insert = "\"";
                                break;
                            case '\\':
                                insert = "\\";
                                break;
                            case 'n':
                                insert = "\n";
                                break;
                            case 'r':
                                insert = "\r";
                                break;
                            case 'v':
                                insert = "\v";
                                break;
                            case 't':
                                insert = "\t";
                                break;
                            case 'b':
                                insert = "\b";
                                break;
                            case 'f':
                                insert = "\f";
                                break;

                            default:
                                insert = "";
                                break;
                        }

                        val = val.Remove(cIndex) + insert + val.Remove(0, cIndex + 2);
                    }

                    searchOffset = cIndex + 1;
                }
            }
            return val;
        }

        private static string Interpolate(ScriptProcessor processor, string val)
        {
            // string interpolation
            // replaces the following pattern: {variable}.
            // {something} has to written as {{something}}.
            // string interpolation can be turned on by putting a $ in front of the string literal.

            if (val.Contains("{") && val.Contains("}"))
            {
                var searchOffset = 0;

                while (val.IndexOf("{", searchOffset, StringComparison.Ordinal) > -1 && val.IndexOf("}", searchOffset, StringComparison.Ordinal) > -1)
                {
                    var startIndex = val.IndexOf("{", searchOffset, StringComparison.Ordinal);

                    if (startIndex < val.Length - 2)
                    {
                        if (val[startIndex + 1] == '{')
                        {
                            val = val.Remove(startIndex + 1) + val.Remove(0, startIndex + 2);

                            // find closing bracket and remove it:
                            var level = 1;
                            var i = startIndex + 1;
                            var foundClosing = false;
                            while (!foundClosing && i < val.Length)
                            {
                                var token = val[i];

                                switch (token)
                                {
                                    case '{':
                                        level++;
                                        break;
                                    case '}':
                                        level--;
                                        if (level == 0)
                                        {
                                            val = val.Remove(i) + val.Remove(0, i + 1);
                                            foundClosing = true;
                                        }
                                        break;
                                }

                                i++;
                            }

                            searchOffset = startIndex + 1;
                        }
                        else
                        {
                            var endIndex = val.IndexOf("}", startIndex, StringComparison.Ordinal);
                            if (endIndex > -1)
                            {
                                var interpolationSequence = val.Substring(startIndex, endIndex - startIndex + 1);
                                interpolationSequence = processor.ExecuteStatement(new ScriptStatement(interpolationSequence.Remove(interpolationSequence.Length - 1, 1).Remove(0, 1))).ToString(processor).Value;

                                val = val.Remove(startIndex) + interpolationSequence + val.Remove(0, endIndex + 1);
                                searchOffset = startIndex + interpolationSequence.Length;
                            }
                            else
                            {
                                searchOffset = startIndex + 1;
                            }
                        }
                    }
                }
            }

            return val;
        }

        /// <summary>
        /// The value of this instance.
        /// </summary>
        internal string Value { get; set; }

        /// <summary>
        /// If this instance has escaped characters or not. If not, the script representation will have an "@" in front of the " or '.
        /// </summary>
        internal bool Escaped { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="SString"/> class without setting a default value.
        /// </summary>
        internal SString() { }

        private SString(ScriptProcessor processor, string value, bool escaped, bool interpolate)
        {
            Escaped = escaped;

            Value = escaped ? Unescape(value) : value;

            if (interpolate) Value = Interpolate(processor, Value);
        }

        /// <summary>
        /// Creates an instance of the <see cref="SString"/> class and sets an initial value.
        /// </summary>
        internal static SString Factory(ScriptProcessor processor, string value, bool escaped, bool interpolate)
        {
            return new SString(processor, value, escaped, interpolate);
        }

        internal override string ToScriptObject()
        {
            return Prototype == null ? ToScriptSource() : base.ToScriptObject();
        }

        internal override string ToScriptSource()
        {
            return string.Format(Escaped ? StringNormalFormat : StringUnescapedFormat, Value);
        }

        internal override SString ToString(ScriptProcessor processor)
        {
            return processor.CreateString(Value, Escaped, false);
        }

        internal override SBool ToBool(ScriptProcessor processor)
        {
            return processor.CreateBool(Value != "");
        }

        internal override SNumber ToNumber(ScriptProcessor processor)
        {
            if (Value.Trim() == "")
            {
                return processor.CreateNumber(0);
            }
            double dblResult;
            return processor.CreateNumber(double.TryParse(Value, out dblResult) ? dblResult : double.NaN);
        }

        internal override string TypeOf()
        {
            return Prototype == null ? LiteralTypeString : base.TypeOf();
        }

        internal override double SizeOf()
        {
            return Value.Length;
        }
    }
}
