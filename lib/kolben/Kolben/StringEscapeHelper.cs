using System.Linq;
using System.Text;

namespace Kolben
{
    /// <summary>
    /// A base class for classes to implement searches for string delimiters and wrappers.
    /// </summary>
    internal abstract class StringEscapeHelper
    {
        protected internal const char StringDelimiterSingle = '\'';
        protected internal const char StringDelimiterDouble = '\"';

        protected bool IsStringValue;
        protected string Expression;
        protected int Index;
        protected char StartChar;
        protected bool EndOfString;
        protected bool HasStringsValue;

        protected internal StringEscapeHelper(string expression)
        {
            Expression = expression;
            HasStringsValue = HasStrings(expression);
        }

        /// <summary>
        /// Returns if the <see cref="StringEscapeHelper"/> is currently positioned within a string.
        /// </summary>
        internal bool IsString => IsStringValue;

        /// <summary>
        /// Jumps to the given index, ignoring all chars in between the current index and the new one.
        /// </summary>
        internal void JumpTo(int index)
        {
            Index = index;
        }

        /// <summary>
        /// Checks for string delimiters at the given index.
        /// </summary>
        internal abstract void CheckStartAt(int startIndex);
        protected abstract void CheckNext();

        protected static bool HasStrings(string expression)
        {
            return expression.Contains(StringDelimiterSingle) || expression.Contains(StringDelimiterDouble);
        }

        /// <summary>
        /// Returns if an expression contains a value, disregarding content within string literals.
        /// </summary>
        internal static bool ContainsWithoutStrings(string expression, string value)
        {
            if (!expression.Contains(value))
            {
                return false;
            }
            else
            {
                if (!HasStrings(expression))
                {
                    return true;
                }
                else
                {
                    var index = 0;
                    var escaped = false;
                    var isString = false;
                    var startChar = StringDelimiterSingle;

                    var sb = new StringBuilder();

                    while (index < expression.Length)
                    {
                        var t = expression[index];

                        if (t == StringDelimiterSingle || t == StringDelimiterDouble)
                        {
                            if (!isString)
                            {
                                isString = true;
                                startChar = t;

                                // Append the starting delimiter:
                                sb.Append(t);
                            }
                            else
                            {
                                if (!escaped && t == startChar)
                                {
                                    isString = false;
                                }
                                else
                                {
                                    escaped = false;
                                }
                            }
                        }
                        else if (t == '\\')
                        {
                            escaped = !escaped;
                        }
                        else
                        {
                            if (escaped)
                                escaped = false;
                        }

                        if (!isString)
                            sb.Append(t);

                        index++;
                    }

                    return sb.ToString().Contains(value);
                }
            }
        }
    }
}
