namespace Kolben
{
    /// <summary>
    /// A string escaper searching an expression from left to right.
    /// </summary>
    internal class LeftToRightStringEscapeHelper : StringEscapeHelper
    {
        private bool _isEscaped;

        /// <summary>
        /// Creates a new instance of the <see cref="LeftToRightStringEscapeHelper"/> class, setting it to an index within the expression and ignoring every char in front of that index.
        /// </summary>
        internal LeftToRightStringEscapeHelper(string expression, int startIndex, bool ignoreStart) : base(expression)
        {
            if (ignoreStart)
            {
                Index = startIndex;
            }
            else
            {
                if (HasStringsValue)
                {
                    while (startIndex > Index)
                    {
                        CheckNext();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="LeftToRightStringEscapeHelper"/> class, setting searching up to a specific index in the expression.
        /// </summary>
        internal LeftToRightStringEscapeHelper(string expression, int startIndex) : this(expression, startIndex, false) { }

        internal override void CheckStartAt(int startIndex)
        {
            if (HasStringsValue)
            {
                if (startIndex < Index)
                {
                    Index = 0;
                    CheckStartAt(startIndex);
                }
                else
                {
                    while (startIndex > Index)
                    {
                        CheckNext();
                    }
                    CheckNext();
                }
            }
        }

        protected sealed override void CheckNext()
        {
            var t = Expression[Index];

            if (EndOfString)
            {
                EndOfString = false;
                IsStringValue = false;
            }

            if (t == StringDelimiterSingle || t == StringDelimiterDouble)
            {
                if (!IsStringValue)
                {
                    IsStringValue = true;
                    StartChar = t;
                }
                else
                {
                    if (!_isEscaped && t == StartChar)
                    {
                        EndOfString = true;
                    }
                    else
                    {
                        _isEscaped = false;
                    }
                }
            }
            else if (t == '\\')
            {
                _isEscaped = !_isEscaped;
            }
            else
            {
                if (_isEscaped)
                    _isEscaped = false;
            }

            Index++;
        }
    }
}