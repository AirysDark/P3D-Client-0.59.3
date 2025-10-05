using System;
using Kolben.Types.Prototypes;
using System.Collections.Generic;
using System.Text;

namespace Kolben.Types
{
    /// <summary>
    /// An object that is an instance of a <see cref="Prototypes.Prototype"/>.
    /// </summary>
    public class SProtoObject : SObject
    {
        internal const string PropertyGetPrefix = "_get_";
        internal const string PropertySetPrefix = "_set_";
        protected internal const string MemberNamePrototype = "prototype";
        protected internal const string MemberNameSuper = "super";

        /// <summary>
        /// Determines if this object actually was instantiated through a prototype.
        /// </summary>
        internal bool IsProtoInstance { get; set; }

        internal Dictionary<string, SVariable> Members { get; } = new Dictionary<string, SVariable>();

        internal SFunction IndexerGetFunction { get; set; }
        internal SFunction IndexerSetFunction { get; set; }

        // keeps references to objects that should be copied back when translated between script and .net object.
        internal Dictionary<string, object> ReferenceContainer { get; set; }

        internal void AddMember(SVariable member)
        {
            Members.Add(member.Identifier, member);
        }

        internal void AddMember(string idenfifier, SObject data)
        {
            Members.Add(idenfifier, new SVariable(idenfifier, data));
        }

        internal void SetMember(string identifier, SObject data)
        {
            Members[identifier].Data = data;
        }

        internal SProtoObject SuperClass
        {
            get
            {
                if (Members.ContainsKey(MemberNameSuper))
                {
                    return (SProtoObject)Members[MemberNameSuper].Data;
                }
                return null;
            }
        }

        internal Prototype Prototype
        {
            get
            {
                if (Members.ContainsKey(MemberNamePrototype))
                {
                    return (Prototype)Members[MemberNamePrototype].Data;
                }
                return null;
            }
        }

        internal override bool HasMember(ScriptProcessor processor, string memberName)
        {
            if (Members.ContainsKey(memberName))
            {
                return true;
            }
            else if (Prototype != null && Prototype.HasMember(processor, memberName))
            {
                return true;
            }
            else if (SuperClass != null)
            {
                return SuperClass.HasMember(processor, memberName);
            }
            return false;
        }

        internal override SObject GetMember(ScriptProcessor processor, SObject accessor, bool isIndexer)
        {
            if (isIndexer && accessor.TypeOf() == LiteralTypeNumber)
            {
                if (Prototype.GetIndexerGetFunction() != IndexerGetFunction)
                    IndexerGetFunction = Prototype.GetIndexerGetFunction();

                return IndexerGetFunction != null ? IndexerGetFunction.Call(processor, this, this, new[] { accessor }) : processor.Undefined;
            }

            var accessorAsString = accessor as SString;
            var memberName = accessorAsString != null ? accessorAsString.Value : accessor.ToString(processor).Value;

            if (Members.ContainsKey(PropertyGetPrefix + memberName)) // getter property
            {
                return ((SFunction)Members[PropertyGetPrefix + memberName].Data).Call(processor, this, this, new SObject[] { });
            }
            if (Members.ContainsKey(memberName))
            {
                return Members[memberName];
            }
            if (Prototype != null && Prototype.HasMember(processor, memberName))
            {
                return Prototype.GetMember(processor, accessor, isIndexer);
            }
            if (SuperClass != null)
            {
                return SuperClass.GetMember(processor, accessor, isIndexer);
            }

            return processor.Undefined;
        }

        internal override void SetMember(ScriptProcessor processor, SObject accessor, bool isIndexer, SObject value)
        {
            if (isIndexer)
            {
                if (Prototype.GetIndexerSetFunction() != IndexerSetFunction)
                    IndexerSetFunction = Prototype.GetIndexerSetFunction();
            }

            if (isIndexer && accessor.TypeOf() == LiteralTypeNumber && IndexerSetFunction != null)
            {
                IndexerSetFunction.Call(processor, this, this, new[] { accessor, value });
            }
            else
            {
                var accessorAsString = accessor as SString;
                var memberName = accessorAsString != null ? accessorAsString.Value : accessor.ToString(processor).Value;

                if (Members.ContainsKey(PropertySetPrefix + memberName)) // setter property
                {
                    ((SFunction)Members[PropertySetPrefix + memberName].Data).Call(processor, this, this, new[] { value });
                }
                if (Members.ContainsKey(memberName))
                {
                    Members[memberName].Data = value;
                }
                else if (Prototype != null && Prototype.HasMember(processor, memberName) && !Prototype.IsStaticMember(memberName))
                {
                    // This is the case when new members got added to the prototype, and we haven't copied them over to the instance yet.
                    // So we do that now, and then set the value of that member:
                    AddMember(memberName, value);
                }
                else
                {
                    SuperClass?.SetMember(processor, accessor, isIndexer, value);
                }
            }
        }

        internal override SObject ExecuteMethod(ScriptProcessor processor, string methodName, SObject caller, SObject This, SObject[] parameters)
        {
            if (Members.ContainsKey(methodName))
            {
                if (Members[methodName].Data.TypeOf() == LiteralTypeFunction)
                {
                    return ((SFunction)Members[methodName].Data).Call(processor, caller, this, parameters);
                }
                else
                {
                    return processor.ErrorHandler.ThrowError(ErrorType.TypeError, ErrorHandler.MessageTypeNotAFunction, methodName);
                }
            }
            else if (Prototype != null && Prototype.HasMember(processor, methodName))
            {
                return Prototype.ExecuteMethod(processor, methodName, caller, This, parameters);
            }
            else if (SuperClass != null)
            {
                return SuperClass.ExecuteMethod(processor, methodName, caller, This, parameters);
            }
            else
            {
                return processor.ErrorHandler.ThrowError(ErrorType.TypeError, ErrorHandler.MessageTypeNotAFunction, methodName);
            }
        }

        internal override string ToScriptObject()
        {
            return ObjectBuffer.ObjPrefix + ObjectBuffer.GetObjectId(this);
        }

        private const string FormatSourceMember = "{0}:{1}";

        internal override string ToScriptSource()
        {
            var source = new StringBuilder();

            foreach (var member in Members)
            {
                if (member.Key != MemberNamePrototype && member.Key != MemberNameSuper) // Do not include super class or prototype in the string representation
                {
                    if (source.Length > 0)
                        source.Append(",");

                    var data = Unbox(member.Value);

                    source.Append(ReferenceEquals(data, this)
                        ? string.Format(FormatSourceMember, member.Key, "{}")
                        : string.Format(FormatSourceMember, member.Key, data.ToScriptSource()));
                }
            }

            source.Insert(0, "{");
            source.Append("}");

            return source.ToString();
        }

        internal override double SizeOf()
        {
            return Members.Count;
        }

        internal override SString ToString(ScriptProcessor processor)
        {
            return processor.CreateString(LiteralObjectStr);
        }

        /// <summary>
        /// Parses an anonymous object.
        /// </summary>
        internal static SProtoObject Parse(ScriptProcessor processor, string source)
        {
            var prototype = new Prototype(LiteralObject);

            source = source.Trim();

            // Format: { member1 : content1, member2 : content2 }

            // Remove "{" and "}":
            source = source.Remove(source.Length - 1, 1).Remove(0, 1).Trim();

            if (source.Length == 0)
                return prototype.CreateInstance(processor, null, false);

            var index = 0;

            while (index < source.Length)
            {
                var nextSeperatorIndex = source.IndexOf(",", index, StringComparison.Ordinal);
                if (nextSeperatorIndex == -1)
                {
                    nextSeperatorIndex = source.Length;
                }
                else
                {
                    //Let's find the correct ",":

                    nextSeperatorIndex = index;

                    var depth = 0;
                    StringEscapeHelper escaper = new LeftToRightStringEscapeHelper(source, nextSeperatorIndex, true);
                    var foundSeperator = false;

                    while (!foundSeperator && nextSeperatorIndex < source.Length)
                    {
                        var t = source[nextSeperatorIndex];

                        escaper.CheckStartAt(nextSeperatorIndex);

                        if (!escaper.IsString)
                        {
                            if (t == '{' || t == '[' || t == '(')
                            {
                                depth++;
                            }
                            else if (t == '}' || t == ']' || t == ')')
                            {
                                depth--;
                            }
                            else if (t == ',' && depth == 0)
                            {
                                foundSeperator = true;
                                nextSeperatorIndex--; // it adds one to the index at the end of the while loop, but we need the index where we stopped searching, so we subtract 1 here to accommodate for that.
                            }
                        }

                        nextSeperatorIndex++;
                    }
                }

                var member = source.Substring(index, nextSeperatorIndex - index);

                string identifier;
                SObject contentObj;
                if (member.Contains(":"))
                {
                    identifier = member.Remove(member.IndexOf(":", StringComparison.Ordinal)).Trim();
                    var content = member.Remove(0, member.IndexOf(":", StringComparison.Ordinal) + 1);

                    contentObj = processor.ExecuteStatement(new ScriptStatement(content));
                }
                else
                {
                    identifier = member.Trim();
                    contentObj = processor.Undefined;
                }

                if (!ScriptProcessor.IsValidIdentifier(identifier))
                    processor.ErrorHandler.ThrowError(ErrorType.SyntaxError, ErrorHandler.MessageSyntaxMissingVarName);

                prototype.AddMember(processor, new PrototypeMember(identifier, contentObj));

                index = nextSeperatorIndex + 1;
            }

            return prototype.CreateInstance(processor, null, false);
        }
    }
}
