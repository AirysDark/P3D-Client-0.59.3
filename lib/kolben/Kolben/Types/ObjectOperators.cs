using System;

namespace Kolben.Types
{
    /// <summary>
    /// Implements methods to use operators on <see cref="SObject"/> instances.
    /// </summary>
    internal class ObjectOperators
    {
        private static Tuple<double, double> GetNumericOperatorParameters(ScriptProcessor processor, SObject left, SObject right)
        {
            var numberLeftAsNumber = left as SNumber;
            var numLeft = numberLeftAsNumber?.Value ?? left.ToNumber(processor).Value;

            var numberRightAsNumber = right as SNumber;
            var numRight = numberRightAsNumber?.Value ?? right.ToNumber(processor).Value;

            return new Tuple<double, double>(numLeft, numRight);
        }

        private static Tuple<bool, bool> GetBooleanicOperatorParameters(ScriptProcessor processor, SObject left, SObject right)
        {
            var leftAsBool = left as SBool;
            var boolLeft = leftAsBool?.Value ?? left.ToBool(processor).Value;

            var rightAsBool = right as SBool;
            var boolRight = rightAsBool?.Value ?? right.ToBool(processor).Value;

            return new Tuple<bool, bool>(boolLeft, boolRight);
        }

        internal static string AddOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            if (left is SString || right is SString)
            {
                var leftAsString = left as SString;
                var strLeft = leftAsString != null ? leftAsString.Value : left.ToString(processor).Value;

                var rightAsString = right as SString;
                var strRight = rightAsString != null ? rightAsString.Value : right.ToString(processor).Value;

                return "\"" + strLeft + strRight + "\"";
            }

            var numbers = GetNumericOperatorParameters(processor, left, right);

            return SNumber.ConvertToScriptString(numbers.Item1 + numbers.Item2);
        }

        internal static string SubtractOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var numbers = GetNumericOperatorParameters(processor, left, right);

            return SNumber.ConvertToScriptString(numbers.Item1 - numbers.Item2);
        }

        internal static string MultiplyOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var numbers = GetNumericOperatorParameters(processor, left, right);

            return SNumber.ConvertToScriptString(numbers.Item1 * numbers.Item2);
        }

        /// <summary>
        /// Multiplies an object with -1.
        /// </summary>
        internal static SObject NegateNumber(ScriptProcessor processor, SObject obj)
        {
            var sNumber = obj as SNumber;
            var number = sNumber?.Value ?? obj.ToNumber(processor).Value;

            return processor.CreateNumber(number * -1);
        }

        internal static string DivideOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var numbers = GetNumericOperatorParameters(processor, left, right);

            if (Math.Abs(numbers.Item2) < double.Epsilon) // Catch division by 0 by returning infinity (wtf -> what a terrible feature).
                return SObject.LiteralInfinity;

            return SNumber.ConvertToScriptString(numbers.Item1 / numbers.Item2);
        }

        internal static string ModulusOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var numbers = GetNumericOperatorParameters(processor, left, right);

            if (Math.Abs(numbers.Item2) < double.Epsilon) // Because, when we divide by 0, we get Infinity, but when we do (x % 0), we get NaN. Great.
                return SObject.LiteralNan;

            return SNumber.ConvertToScriptString(numbers.Item1 % numbers.Item2);
        }

        internal static string ExponentOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var numbers = GetNumericOperatorParameters(processor, left, right);

            return SNumber.ConvertToScriptString(Math.Pow(numbers.Item1, numbers.Item2));
        }

        internal static string NotOperator(ScriptProcessor processor, SObject obj)
        {
            return SBool.ConvertToScriptString(!obj.ToBool(processor).Value);
        }

        internal static string IncrementOperator(ScriptProcessor processor, SObject obj)
        {
            // Only variables can be incremented:

            var variable = obj as SVariable;
            if (variable != null)
            {
                var svar = variable;
                svar.Data = processor.CreateNumber(svar.Data.ToNumber(processor).Value + 1D);
                return svar.Identifier;
            }
            processor.ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxInvalidIncrement);
            return "";
        }

        internal static string DecrementOperator(ScriptProcessor processor, SObject obj)
        {
            // Only variables can be decremented:

            var variable = obj as SVariable;
            if (variable != null)
            {
                var svar = variable;
                svar.Data = processor.CreateNumber(svar.Data.ToNumber(processor).Value - 1D);
                return svar.Identifier;
            }

            processor.ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxInvalidDecrement);
            return "";
        }

        internal static string EqualsOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            return SBool.ConvertToScriptString(ObjectComparer.LooseEquals(processor, left, right));
        }

        internal static string NotEqualsOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            return SBool.ConvertToScriptString(!ObjectComparer.LooseEquals(processor, left, right));
        }

        internal static string TypeEqualsOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            return SBool.ConvertToScriptString(ObjectComparer.StrictEquals(processor, left, right));
        }

        internal static string TypeNotEqualsOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            return SBool.ConvertToScriptString(!ObjectComparer.StrictEquals(processor, left, right));
        }

        internal static string SmallerOrEqualsOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var numbers = GetNumericOperatorParameters(processor, left, right);

            return SBool.ConvertToScriptString(numbers.Item1 <= numbers.Item2);
        }

        internal static string LargerOrEqualsOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var numbers = GetNumericOperatorParameters(processor, left, right);

            return SBool.ConvertToScriptString(numbers.Item1 >= numbers.Item2);
        }

        internal static string SmallerOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var numbers = GetNumericOperatorParameters(processor, left, right);

            return SBool.ConvertToScriptString(numbers.Item1 < numbers.Item2);
        }

        internal static string LargerOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var numbers = GetNumericOperatorParameters(processor, left, right);

            return SBool.ConvertToScriptString(numbers.Item1 > numbers.Item2);
        }

        internal static string OrOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var bools = GetBooleanicOperatorParameters(processor, left, right);

            return SBool.ConvertToScriptString(bools.Item1 || bools.Item2);
        }

        internal static string AndOperator(ScriptProcessor processor, SObject left, SObject right)
        {
            var bools = GetBooleanicOperatorParameters(processor, left, right);

            return SBool.ConvertToScriptString(bools.Item1 && bools.Item2);
        }
    }
}
