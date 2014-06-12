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
    public abstract class ClassMap(Type type, JsonObjectContract defaultContract)
    {
        public Type Type { get; } = type;

        protected internal JsonObjectContract JsonContract { get; } = defaultContract;

        public string TypeName { get; set; }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Type != null);
        }
    }

    public class ClassMap<T>(JsonObjectContract defaultContract)
        : ClassMap(typeof(T), defaultContract)
    {
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
            var ctor = ctorir.GetConstructorInfo(creatorLambda, out var args);

            JsonContract.OverrideConstructor = ctor;
            JsonContract.ParametrizedConstructor = null;
            JsonContract.ConstructorParameters.Clear();

            foreach (var arg in args)
            {
                JsonContract.ConstructorParameters.Add(new JsonProperty
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

        private JsonProperty ExpressionToProperty(Expression<Func<T, object>> member)
        {
            var mi = member.GetMemberInfo();

            var jprop = JsonContract.Properties.SingleOrDefault(x => x.PropertyName == mi.Name);
            if (jprop == null)
            {
                jprop = new JsonProperty { PropertyName = mi.Name, DeclaringType = Type };
                JsonContract.Properties.Add(jprop);
            }

            return jprop;
        }

        public ClassMap<T> MapMember(Expression<Func<T, object>> member, Action<MemberMap> memberMapInitializer)
        {
            var jprop = ExpressionToProperty(member);
            jprop.Ignored = false;
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
