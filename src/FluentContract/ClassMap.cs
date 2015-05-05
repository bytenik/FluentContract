using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
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
        public ClassMap(Type type, JsonObjectContract defaultContract)
        {
            Type = type;
            JsonContract = defaultContract;
        }

        public Type Type { get; }

        protected internal JsonObjectContract JsonContract { get; }

        public string TypeName { get; set; }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Type != null);
        }
    }

    public class ClassMap<T> : ClassMap
    {
        public ClassMap(JsonObjectContract defaultContract)
            : base(typeof(T), defaultContract)
        {
        }

        public ClassMap<T> SetDiscriminator(string typeName)
        {
            TypeName = typeName;
            return this;
        }

        public ClassMap<T> MapCreator(Func<T> creator)
        {
            JsonContract.DefaultCreator = () => creator();
            return this;
        }

        public ClassMap<T> MapCreator(Expression<Func<T, T>> creatorLambda)
        {
            var ctorir = new ConstructorInfoRetriever();
            Dictionary<ParameterInfo, MemberInfo> args;
            var ctor = ctorir.GetConstructorInfo(creatorLambda, out args);

            JsonContract.OverrideCreator = x => ctor.Invoke(x);
            JsonContract.CreatorParameters.Clear();

            foreach (var arg in args)
            {
                JsonContract.CreatorParameters.Add(new JsonProperty
                {
                    PropertyName = arg.Value.Name,
                    UnderlyingName = arg.Key.Name,
                    PropertyType = arg.Value.MemberType == MemberTypes.Property ? ((PropertyInfo)arg.Value).PropertyType : ((FieldInfo)arg.Value).FieldType
                });
            }

            return this;
        }

        public ClassMap<T> SetConverter(JsonConverter converter)
        {
            JsonContract.Converter = converter;
            return this;
        }

        private Type GetMemberType(MemberInfo member)
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

        private JsonProperty ExpressionToProperty(Expression<Func<T, object>> member)
        {
            var mi = member.GetMemberInfo();

            var jprop = JsonContract.Properties.SingleOrDefault(x => x.UnderlyingName == mi.Name);
            if (jprop == null)
            {
                jprop = new JsonProperty
                {
                    UnderlyingName = mi.Name,
                    DeclaringType = Type,
                    PropertyType = GetMemberType(mi)
                };
                JsonContract.Properties.Add(jprop);
            }

            return jprop;
        }

        private JsonProperty MapMemberInternal(Expression<Func<T, object>> member)
        {
            var mi = member.GetMemberInfo();
            var jprop = ExpressionToProperty(member);
            jprop.Ignored = false;
            jprop.Readable = mi is FieldInfo || ((mi is PropertyInfo) && ((PropertyInfo)mi).CanRead);
            jprop.Writable = mi is FieldInfo || ((mi is PropertyInfo) && ((PropertyInfo)mi).CanWrite);
            if (jprop.ValueProvider == null) jprop.ValueProvider = new DynamicValueProvider(mi);

            return jprop;
        }

        public ClassMap<T> UnmapAll()
        {
            JsonContract.Properties.Clear();
            return this;
        }

        public ClassMap<T> MapMember(Expression<Func<T, object>> member)
        {
            MapMemberInternal(member);
            return this;
        }

        public ClassMap<T> MapMember(Expression<Func<T, object>> member, Action<MemberMap> memberMapInitializer)
        {
            var jprop = MapMemberInternal(member);
            memberMapInitializer(new MemberMap(jprop));

            return this;
        }

        public ClassMap<T> UnmapMember(Expression<Func<T, object>> member)
        {
            var jprop = ExpressionToProperty(member);
            jprop.Ignored = true;

            return this;
        }
    }
}
