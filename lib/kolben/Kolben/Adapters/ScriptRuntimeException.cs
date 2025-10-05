using System;
using Kolben.Types;
using Kolben.Types.Prototypes;

namespace Kolben.Adapters
{
    /// <summary>
    /// An exception thrown by the scripting runtime environment.
    /// </summary>
    public class ScriptRuntimeException : Exception
    {
        internal SError ErrorObject { get; }

        /// <summary>
        /// The type of error that occurred.
        /// </summary>
        public string Type => GetStringMember(ErrorObject, ErrorPrototype.MemberNameType);

        /// <summary>
        /// The line in the source of the script the error occurred on.
        /// </summary>
        public int Line => (int)GetNumberMember(ErrorObject, ErrorPrototype.MemberNameLine);

        internal ScriptRuntimeException(SError errorObject)
            : base(GetStringMember(errorObject, ErrorPrototype.MemberNameMessage))
        {
            ErrorObject = errorObject;
        }

        private static string GetStringMember(SProtoObject obj, string member)
        {
            var memberString = SObject.Unbox(obj.Members[member]) as SString;
            return memberString != null ? memberString.Value : "";
        }

        private static double GetNumberMember(SProtoObject obj, string member)
        {
            var memberNumber = SObject.Unbox(obj.Members[member]) as SNumber;
            return memberNumber?.Value ?? -1;
        }
    }
}
