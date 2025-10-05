namespace Kolben.Types
{
    /// <summary>
    /// Represents the "null" literal as object.
    /// </summary>
    internal class SNull : SObject
    {
        private SNull() { }

        /// <summary>
        /// Creates the null object.
        /// </summary>
        internal static SNull Factory()
        {
            return new SNull();
        }

        internal override string ToScriptObject()
        {
            return LiteralNull;
        }

        internal override string ToScriptSource()
        {
            return LiteralNull;
        }

        internal override SString ToString(ScriptProcessor processor)
        {
            return processor.CreateString(LiteralNull);
        }

        internal override SBool ToBool(ScriptProcessor processor)
        {
            return processor.CreateBool(false);
        }

        internal override SNumber ToNumber(ScriptProcessor processor)
        {
            return processor.CreateNumber(0);
        }

        internal override string TypeOf()
        {
            return LiteralNull;
        }

        internal override double SizeOf()
        {
            return 0;
        }
    }
}
