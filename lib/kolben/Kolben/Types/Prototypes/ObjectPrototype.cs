using System;

namespace Kolben.Types.Prototypes
{
    internal class ObjectPrototype : Prototype
    {
        public ObjectPrototype() : base("Object") { }

        [BuiltInMethod]
        // ReSharper disable InconsistentNaming
        public static SObject toString(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
            // ReSharper restore InconsistentNaming
        {
            return processor.CreateString(LiteralObjectStr);
        }

        [BuiltInMethod(IsStatic = true, MethodName = "create")]
        public static SObject Create(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            Prototype prototype = null;

            if (parameters.Length > 0)
            {
                var protoParam = parameters[0];
                if (protoParam.TypeOf() == LiteralTypeString)
                {
                    prototype = processor.Context.GetPrototype(((SString)protoParam).Value);
                }
                else if (IsPrototype(protoParam.GetType()))
                {
                    prototype = (Prototype)protoParam;
                }
                else
                {
                    return processor.ErrorHandler.ThrowError(ErrorType.TypeError, ErrorHandler.MessageReferenceNoPrototype, protoParam.TypeOf());
                }
            }

            if (prototype != null)
            {
                var instParams = new SObject[parameters.Length - 1];
                Array.Copy(parameters, 1, instParams, 0, parameters.Length - 1);

                return processor.Context.CreateInstance(prototype, instParams);
            }
            else
            {
                return processor.ErrorHandler.ThrowError(ErrorType.TypeError, ErrorHandler.MessageReferenceNoPrototype, LiteralUndefined);
            }
        }

        [BuiltInMethod(IsStatic = true, MethodName = "addMember")]
        public static SObject AddMember(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            // Parameter #1: (String)Name of the new member
            // [Parameter #2: Default value of the new member ] / Undefined
            // [Parameter #3: Signature config of the new member] / instance member, no special settings

            if (parameters.Length == 0)
                return processor.Undefined;

            Prototype prototype;

            if (IsPrototype(instance.GetType()))
            {
                prototype = (Prototype)instance;
            }
            else
            {
                // The instance will be a prototype instance, so get its prototype from there:
                var protoObj = (SProtoObject)instance;
                prototype = protoObj.Prototype;
            }

            var memberAsString = parameters[0] as SString;
            var memberName = memberAsString != null ? memberAsString.Value : parameters[0].ToString(processor).Value;

            var defaultValue = processor.Undefined;

            if (parameters.Length > 1)
            {
                defaultValue = parameters[1];
            }

            var isReadOnly = false;
            var isStatic = false;
            var isIndexerGet = false;
            var isIndexerSet = false;

            if (parameters.Length > 2)
            {
                var signature = parameters[2];
                var array = signature as SArray;
                if (array != null)
                {
                    foreach (var arrayMember in array.ArrayMembers)
                    {
                        var arrayMemberAsString = arrayMember as SString;
                        if (arrayMemberAsString == null) continue;

                        var signatureMember = arrayMemberAsString.Value;
                        switch (signatureMember)
                        {
                            case "readOnly":
                                isReadOnly = true;
                                break;
                            case "static":
                                isStatic = true;
                                break;
                            case "indexerGet":
                                isIndexerGet = true;
                                break;
                            case "indexerSet":
                                isIndexerSet = true;
                                break;
                        }
                    }
                }
            }

            if ((isIndexerSet || isIndexerGet) && !(defaultValue is SFunction))
                processor.ErrorHandler.ThrowError(ErrorType.TypeError, ErrorHandler.MessageTypeGetterSetterNotAFunction);

            if (!ScriptProcessor.IsValidIdentifier(memberName))
                processor.ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingVarName);

            prototype.AddMember(processor, new PrototypeMember(memberName, defaultValue, isStatic, isReadOnly, isIndexerGet, isIndexerSet));

            return processor.Undefined;
        }
    }
}
