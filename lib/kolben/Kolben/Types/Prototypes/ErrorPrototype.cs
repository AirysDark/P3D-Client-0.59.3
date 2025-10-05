namespace Kolben.Types.Prototypes
{
    internal class ErrorPrototype : Prototype
    {
        internal const string MemberNameMessage = "message";
        internal const string MemberNameType = "type";
        internal const string MemberNameLine = "line";

        public ErrorPrototype(ScriptProcessor processor) : base("Error")
        {
            Constructor = new PrototypeMember(ClassMethodCtor, new SFunction(ConstructorImpl));

            AddMember(processor, new PrototypeMember(MemberNameMessage, processor.Undefined));
            AddMember(processor, new PrototypeMember(MemberNameType, processor.Undefined));
            AddMember(processor, new PrototypeMember(MemberNameLine, processor.Undefined));
        }

        protected override SProtoObject CreateBaseObject()
        {
            return new SError();
        }

        private static SObject ConstructorImpl(ScriptProcessor processor, SObject instance, SObject This, SObject[] parameters)
        {
            var obj = (SError)instance;

            if (parameters.Length > 0)
            {
                var stringParameter = parameters[0] as SString;
                var message = stringParameter ?? parameters[0].ToString(processor);

                obj.Members[MemberNameMessage].Data = message;
            }

            if (parameters.Length > 1)
            {
                var stringParameter = parameters[1] as SString;
                var errorType = stringParameter ?? parameters[1].ToString(processor);

                obj.Members[MemberNameType].Data = errorType;
            }
            else
            {
                obj.Members[MemberNameType].Data = processor.CreateString("UserError");
            }

            if (parameters.Length > 2)
            {
                var line = parameters[2] as SNumber;
                var errorLine = line ?? parameters[2].ToNumber(processor);

                obj.Members[MemberNameLine].Data = errorLine;
            }
            else
            {
                obj.Members[MemberNameLine].Data = processor.CreateNumber(-1);
            }

            return obj;
        }
    }
}
