using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FluentContract
{
    public class FluentMappings
    {
        private readonly IDictionary<Type, ClassMap> _infoByType = new Dictionary<Type, ClassMap>();
        private readonly IDictionary<Type, JsonContract> _registeredContracts = new Dictionary<Type, JsonContract>();
        private readonly IDictionary<string, ClassMap> _infoByName = new Dictionary<string, ClassMap>();
        private readonly ISet<Type> _requiresDiscriminator = new HashSet<Type>();

        private readonly IContractResolver _wrappedResolver;
        private readonly SerializationBinder _wrappedBinder;

        private readonly FluentSerializationBinder _binder;
        public SerializationBinder Binder { get { return _binder; } }

        private readonly FluentContractResolver _contractResolver;
        public IContractResolver ContractResolver { get { return _contractResolver; } }

        public FluentMappings(IContractResolver wrappedResolver = null, SerializationBinder wrappedBinder = null)
        {
            _wrappedResolver = wrappedResolver ?? new DefaultContractResolver();
            _wrappedBinder = wrappedBinder;

            _binder = new FluentSerializationBinder(this);
            _contractResolver = new FluentContractResolver(this);
        }

        public void RegisterContract(Type type, JsonContract contract)
        {
            _registeredContracts[type] = contract;
        }

        public void MapClass<T>(Action<ClassMap<T>> classMapInitializer)
        {
            var baseContract = ContractResolver.ResolveContract(typeof(T)) as JsonObjectContract;
            if (baseContract == null)
                throw new InvalidOperationException("Only classes can be mapped.");

            var cm = new ClassMap<T>(baseContract);
            classMapInitializer(cm);

            _infoByType[typeof(T)] = cm;
            if (cm.TypeName != null)
                _infoByName[cm.TypeName] = cm;
        }

        private class FluentSerializationBinder : SerializationBinder
        {
            public FluentSerializationBinder(FluentMappings mappings)
            {
                Mappings = mappings;
            }

            public FluentMappings Mappings { get; }

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                if (Mappings._infoByType.ContainsKey(serializedType) && Mappings._infoByType[serializedType].TypeName != null)
                {
                    assemblyName = null;
                    typeName = Mappings._infoByType[serializedType].TypeName;
                }
                else if (Mappings._wrappedBinder != null)
                    Mappings._wrappedBinder.BindToName(serializedType, out assemblyName, out typeName);
                else
                    base.BindToName(serializedType, out assemblyName, out typeName);
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                if (Mappings._infoByName.ContainsKey(typeName))
                    return Mappings._infoByName[typeName].Type;
                else if (Mappings._wrappedBinder != null)
                    return Mappings._wrappedBinder.BindToType(assemblyName, typeName);
                else
                    return null;
            }
        }

        private class FluentContractResolver : IContractResolver
        {
            public FluentContractResolver(FluentMappings mappings)
            {
                Mappings = mappings;
            }

            public FluentMappings Mappings { get; }

            public JsonContract ResolveContract(Type type)
            {
                Contract.Requires<ArgumentNullException>(type != null);

                if (Mappings._registeredContracts.ContainsKey(type))
                    return Mappings._registeredContracts[type];

                JsonContract innerContract;
                if (Mappings._infoByType.ContainsKey(type))
                    innerContract = Mappings._infoByType[type].JsonContract;
                else
                    innerContract = Mappings._wrappedResolver.ResolveContract(type);

                if (innerContract is JsonArrayContract)
                {
                    var contract = (JsonArrayContract)innerContract;

                    if (Mappings._infoByType.ContainsKey(contract.CollectionItemType))
                    {
                        var info = Mappings._infoByType[contract.CollectionItemType];
                        if (info.TypeName != null && contract.ItemTypeNameHandling == null)
                            contract.ItemTypeNameHandling = TypeNameHandling.All;
                    }

                    return contract;
                }
                else if (innerContract is JsonObjectContract)
                {
                    var contract = (JsonObjectContract)innerContract;

                    if (contract.Properties != null)
                    {
                        foreach (var prop in contract.Properties)
                        {
                            if (Mappings._infoByType.ContainsKey(prop.PropertyType))
                            {
                                var info = Mappings._infoByType[prop.PropertyType];
                                if (info.TypeName != null && prop.TypeNameHandling == null)
                                    prop.TypeNameHandling = TypeNameHandling.All;
                            }
                        }
                    }

                    if (contract.CreatorParameters != null)
                    {
                        foreach (var prop in contract.CreatorParameters)
                        {
                            if (Mappings._infoByType.ContainsKey(prop.PropertyType))
                            {
                                var info = Mappings._infoByType[prop.PropertyType];
                                if (info.TypeName != null && prop.TypeNameHandling == null)
                                    prop.TypeNameHandling = TypeNameHandling.All;
                            }
                        }
                    }

                    return contract;
                }
                else
                    return innerContract;
            }
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_wrappedResolver != null, "Wrapped contract resolver cannot be null.");
        }
    }
}
