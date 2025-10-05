namespace Kolben.Types
{
    internal class SBool : SProtoObject
    {
        /// <summary>
        /// Converts the value to the script representation.
        /// </summary>
        internal static string ConvertToScriptString(bool value)
        {
            if (value)
                return LiteralBoolTrue;
            else
                return LiteralBoolFalse;
        }

        /// <summary>
        /// The value of this instance.
        /// </summary>
        internal bool Value { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="SBool"/> class without setting a default value.
        /// </summary>
        internal SBool() { }

        private SBool(bool value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates an instance of the <see cref="SBool"/> class and sets an initial value.
        /// </summary>
        internal static SBool Factory(bool value)
        {
            return new SBool(value);
        }

        internal override string ToScriptObject()
        {
            if (Prototype == null)
                return ToScriptSource();
            else
                return base.ToScriptObject();
        }

        internal override string ToScriptSource()
        {
            return ConvertToScriptString(Value);
        }

        internal override SString ToString(ScriptProcessor processor)
        {
            return processor.CreateString(ConvertToScriptString(Value));
        }

        internal override SBool ToBool(ScriptProcessor processor)
        {
            return processor.CreateBool(Value);
        }

        internal override SNumber ToNumber(ScriptProcessor processor)
        {
            if (Value)
                return processor.CreateNumber(1);
            else
                return processor.CreateNumber(0);
        }

        internal override string TypeOf()
        {
            if (Prototype == null)
                return LiteralTypeBool;
            else
                return base.TypeOf();
        }

        internal override double SizeOf()
        {
            return 1;
        }
    }
}
