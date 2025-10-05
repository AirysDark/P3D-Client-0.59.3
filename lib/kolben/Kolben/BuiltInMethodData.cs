namespace Kolben
{
    ///<summary>
    /// Contains data describing a built in method.
    /// </summary>
    internal class BuiltInMethodData
    {
        public string Name { get; set; }

        public BuiltInMethodAttribute Attribute { get; set; }

        public BuiltInMethod Delegate { get; set; }
    }
}
