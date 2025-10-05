using System.Collections.Generic;

namespace Kolben
{
    /// <summary>
    /// The object buffer assigns Ids to object instances and stores them so they can be retrieved by their Ids.
    /// </summary>
    internal static class ObjectBuffer
    {
        internal const string ObjPrefix = "§";

        private static readonly object SyncRoot = new object();

        private static readonly List<object> Buffer = new List<object>();

        internal static int GetObjectId(object obj)
        {
            int objId;

            lock (SyncRoot)
            {
                if (!Buffer.Contains(obj))
                    Buffer.Add(obj);

                objId = Buffer.IndexOf(obj);
            }

            return objId;
        }

        internal static bool HasObject(int id)
        {
            bool result;

            lock (SyncRoot)
            {
                result = Buffer.Count > id;
            }

            return result;
        }

        internal static object GetObject(int id)
        {
            object result;

            lock (SyncRoot)
            {
                result = Buffer[id];
            }

            return result;
        }
    }
}
