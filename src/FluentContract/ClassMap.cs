using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using FluentContract.Internals;
using System.Reflection;
using Newtonsoft.Json;

namespace FluentContract
{
    public abstract class ClassMap
    {
        public ClassMap(Type type)
        {
            Type = type;
        }
        
        public Type Type { get; }

        protected readonly IList<Action<JsonObjectContract>> Steps = new List<Action<JsonObjectContract>>();

        public bool Inheritable { get; set; }

        public JsonObjectContract TransformContract(JsonObjectContract baseContract)
        {
            foreach (var step in Steps)
                step(baseContract);
            return baseContract;
        }

        public string TypeName { get; set; }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Type != null);
        }
    }

    public class ClassMap<T> : ClassMap
    {
        public ClassMap()
            : base(typeof(T))
        {
        }

        public ClassMap<T> SetInheritable(bool inheritable = true)
        {
            Inheritable = inheritable;
            return this;
        }

        public ClassMap<T> SetDiscriminator(string typeName)
        {
            TypeName = typeName;
            return this;
        }
        
        public ClassMap<T> MapCreator(Func<T> creator)
        {
            Steps.Add(c => c.DefaultCreator = () => creator());
            return this;
        }

        public ClassMap<T> MapCreator(Expression<Func<T, T>> creatorLambda)
        {
            var ctorir = new ConstructorInfoRetriever();
            Dictionary<ParameterInfo, MemberInfo> args;
            var ctor = ctorir.GetConstructorInfo(creatorLambda, out args);

            Steps.Add(c =>
            {
                c.OverrideCreator = x => ctor.Invoke(x);
                c.CreatorParameters.Clear();

                foreach (var arg in args)
                {
                    c.CreatorParameters.Add(new JsonProperty
                    {
                        PropertyName = arg.Value.Name,
                        UnderlyingName = arg.Key.Name,
                        PropertyType =
                            arg.Value.MemberType == MemberTypes.Property
                                ? ((PropertyInfo) arg.Value).PropertyType
                                : ((FieldInfo) arg.Value).FieldType
                    });
                }
            });

            return this;
        }

        public ClassMap<T> ModifyContract(Action<JsonObjectContract> modifier)
        {
            Steps.Add(modifier);
            return this;
        }

        public ClassMap<T> SetConverter(JsonConverter converter)
        {
            Steps.Add(c => c.Converter = converter);
            return this;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo)
                return ((PropertyInfo)member).PropertyType;
            else if (member is FieldInfo)
                return ((FieldInfo)member).FieldType;
            else if (member is MethodInfo)
                return ((MethodInfo)member).ReturnType;
            else
                throw new NotSupportedException("Only the type of properties, fields, and the return type of methods can be determined.");
        }

        public JsonProperty MemberToProperty(MemberInfo memberInfo, JsonObjectContract contract)
        {
            var jprop = contract.Properties.SingleOrDefault(x => x.UnderlyingName == memberInfo.Name);
            if (jprop == null)
            {
                jprop = new JsonProperty
                {
                    UnderlyingName = memberInfo.Name,
                    DeclaringType = typeof(T),
                    PropertyType = GetMemberType(memberInfo)
                };
                contract.Properties.Add(jprop);
            }

            return jprop;
        }

        public JsonProperty ExpressionToProperty(Expression<Func<T, object>> member, JsonObjectContract contract)
        {
            var mi = member.GetMemberInfo();
            return MemberToProperty(mi, contract);
        }

        public MemberInfo PropertyToMember(JsonProperty property)
        {
            return property.DeclaringType.GetMember(property.UnderlyingName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
        }

        private JsonProperty MapMemberInternal(Expression<Func<T, object>> member, JsonObjectContract contract)
        {
            var mi = member.GetMemberInfo();

            var jprop = ExpressionToProperty(member, contract);
            jprop.Ignored = false;
            jprop.Readable = mi is FieldInfo || ((mi is PropertyInfo) && ((PropertyInfo)mi).CanRead);
            jprop.Writable = mi is FieldInfo || ((mi is PropertyInfo) && ((PropertyInfo)mi).CanRead);
            if (jprop.ValueProvider == null) jprop.ValueProvider = new DynamicValueProvider(mi);

            return jprop;
        }

        public ClassMap<T> UnmapAll()
        {
            Steps.Add(c => c.Properties.Clear());
            return this;
        }

        public ClassMap<T> MapMember(Expression<Func<T, object>> member)
        {
            Steps.Add(c => MapMemberInternal(member, c));
            return this;
        }

        public ClassMap<T> MapMember(Expression<Func<T, object>> member, Action<MemberMap> memberMapInitializer)
        {
            Steps.Add(c =>
            {
                var jprop = MapMemberInternal(member, c);
                memberMapInitializer(new MemberMap(jprop));
            });

            return this;
        }

        public ClassMap<T> UnmapMember(Expression<Func<T, object>> member)
        {
            Steps.Add(c =>
            {
                var jprop = ExpressionToProperty(member, c);
                jprop.Ignored = true;
            });

            return this;
        }
    }
}