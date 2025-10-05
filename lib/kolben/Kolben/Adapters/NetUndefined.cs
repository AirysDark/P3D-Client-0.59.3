namespace Kolben.Adapters
{
    /// <summary>
    /// .Net equivalent of the undefined value.
    /// </summary>
    public class NetUndefined
    {
        private NetUndefined() { }

        private static NetUndefined _instance;

        public static NetUndefined Instance => _instance ?? (_instance = new NetUndefined());
    }
}
