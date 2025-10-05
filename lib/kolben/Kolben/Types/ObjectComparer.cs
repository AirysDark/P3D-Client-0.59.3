using System;

namespace Kolben.Types
{
    internal static class ObjectComparer
    {
        /// <summary>
        /// Compares two objects for equality, respecting their typings.
        /// </summary>
        /// <remarks>Used by the === and !== equality operators.</remarks>
        public static bool StrictEquals(ScriptProcessor processor, SObject left, SObject right)
        {
            left = SObject.Unbox(left);
            right = SObject.Unbox(right);

            if (left.TypeOf() != right.TypeOf())
            {
                return false;
            }
            // If they are undefined or null, return true:
            if (left.TypeOf() == SObject.LiteralUndefined || left.TypeOf() == SObject.LiteralNull)
            {
                return true;
            }
            // Both are numbers:
            if (left.TypeOf() == SObject.LiteralTypeNumber)
            {
                var numLeft = ((SNumber)left).Value;
                var numRight = ((SNumber)right).Value;

                return Math.Abs(numLeft - numRight) < double.Epsilon;
            }
            // Both are string:
            if (left.TypeOf() == SObject.LiteralTypeString)
            {
                var strLeft = ((SString)left).Value;
                var strRight = ((SString)right).Value;

                return strLeft == strRight;
            }
            // Both are bool:
            if (left.TypeOf() == SObject.LiteralTypeBool)
            {
                var boolLeft = ((SBool)left).Value;
                var boolRight = ((SBool)right).Value;

                return boolLeft == boolRight;
            }
            return ReferenceEquals(left, right);
        }

        /// <summary>
        /// Compares two objects for equality, converting types if needed.
        /// </summary>
        /// <remarks>Used by the == and != equality operators.</remarks>
        public static bool LooseEquals(ScriptProcessor processor, SObject left, SObject right)
        {
            left = SObject.Unbox(left);
            right = SObject.Unbox(right);

            // both types are the same:
            if (left.TypeOf() == right.TypeOf())
            {
                // If they are undefined or null, return true:
                if (left.TypeOf() == SObject.LiteralUndefined || left.TypeOf() == SObject.LiteralNull)
                {
                    return true;
                }
                // Both are numbers:
                if (left.TypeOf() == SObject.LiteralTypeNumber)
                {
                    var numLeft = ((SNumber)left).Value;
                    var numRight = ((SNumber)right).Value;

                    return Math.Abs(numLeft - numRight) < double.Epsilon;
                }
                // Both are string:
                if (left.TypeOf() == SObject.LiteralTypeString)
                {
                    var strLeft = ((SString)left).Value;
                    var strRight = ((SString)right).Value;

                    return strLeft == strRight;
                }
                // Both are bool:
                if (left.TypeOf() == SObject.LiteralTypeBool)
                {
                    var boolLeft = ((SBool)left).Value;
                    var boolRight = ((SBool)right).Value;

                    return boolLeft == boolRight;
                }
                return ReferenceEquals(left, right);
            }
            // null & undefined
            if (left.TypeOf() == SObject.LiteralNull && right.TypeOf() == SObject.LiteralUndefined ||
                left.TypeOf() == SObject.LiteralUndefined && right.TypeOf() == SObject.LiteralNull)
            {
                return true;
            }
            // When one is a number and another is a string, convert the string to a number and compare:
            if (left.TypeOf() == SObject.LiteralTypeString && right.TypeOf() == SObject.LiteralTypeNumber)
            {
                var numLeft = left.ToNumber(processor).Value;
                var numRight = ((SNumber)right).Value;

                return Math.Abs(numLeft - numRight) < double.Epsilon;
            }
            if (left.TypeOf() == SObject.LiteralTypeNumber && right.TypeOf() == SObject.LiteralTypeString)
            {
                var numRight = right.ToNumber(processor).Value;
                var numLeft = ((SNumber)left).Value;

                return Math.Abs(numLeft - numRight) < double.Epsilon;
            }
            if (left.TypeOf() == SObject.LiteralTypeBool)
            {
                return LooseEquals(processor, left.ToNumber(processor), right);
            }
            if (right.TypeOf() == SObject.LiteralTypeBool)
            {
                return LooseEquals(processor, left, right.ToNumber(processor));
            }

            return false;
        }
    }
}
