namespace Kolben
{
    /// <summary>
    /// A string escaper searching an expression from right to left.
    /// </summary>
    internal class RightToLeftStringEscapeHelper : StringEscapeHelper
    {

        /// <summary>
        /// Creates a new instance of the <see cref="RightToLeftStringEscapeHelper"/> class, setting it to an index within the expression and ignoring every char before that index.
        /// </summary>
        internal RightToLeftStringEscapeHelper(string expression, int startIndex, bool ignoreStart) : base(expression)
        {
            if (ignoreStart)
            {
                Index = startIndex;
            }
            else
            {
                if (HasStringsValue)
                {
                    while (startIndex < Index)
                    {
                        InternalCheckNext();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RightToLeftStringEscapeHelper"/> class, setting searching up to a specific index in the expression.
        /// </summary>
        internal RightToLeftStringEscapeHelper(string expression, int startIndex) : this(expression, startIndex, false) { }

        internal override void CheckStartAt(int startIndex)
        {
            if (!HasStringsValue) return;

            if (startIndex > Index)
            {
                Index = Expression.Length - 1;
                CheckStartAt(startIndex);
            }
            else
            {
                while (startIndex < Index)
                {
                    CheckNext();
                }
                CheckNext();
            }
        }

        private void InternalCheckNext()
        {
            var t = Expression[Index];

            if (EndOfString)
            {
                EndOfString = false;
                IsStringValue = false;
            }

            if (t == StringDelimiterSingle || t == StringDelimiterDouble)
            {
                if (IsStringValue && t == StartChar)
                {
                    var cIndex = Index - 1;
                    var isEscaped = false;

                    while (cIndex >= 0 && Expression[cIndex] == '\\')
                    {
                        isEscaped = !isEscaped;
                        cIndex--;
                    }

                    if (!isEscaped)
                    {
                        EndOfString = true;
                    }
                }
                else if (!IsStringValue)
                {
                    IsStringValue = true;
                    StartChar = t;
                }
            }

            Index--;

        }

        protected override void CheckNext()
        {
            InternalCheckNext();
        }
    }
}