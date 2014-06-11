﻿using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using FluentContract.Internals;
using System.Reflection;

namespace FluentContract
{
    public class ClassMap(Type type, JsonObjectContract defaultContract)
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
                    PropertyName = arg.Key.Name,
                    UnderlyingName = arg.Value.Name,
                    PropertyType = arg.Key.MemberType == MemberTypes.Property ? ((PropertyInfo)arg.Key).PropertyType : ((FieldInfo)arg.Key).FieldType
                });
            }

            return this;
        }
    }
}