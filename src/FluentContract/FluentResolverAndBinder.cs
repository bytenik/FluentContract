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
    public class FluentResolverAndBinder : SerializationBinder, IContractResolver
    {
        private readonly IDictionary<Type, ClassMap> _infoByType = new Dictionary<Type, ClassMap>();
        private readonly IDictionary<string, ClassMap> _infoByName = new Dictionary<string, ClassMap>();
        private readonly IContractResolver _wrappedResolver;
        private readonly SerializationBinder _wrappedBinder;

        public FluentResolverAndBinder(IContractResolver wrappedResolver = null, SerializationBinder wrappedBinder = null)
        {
            _wrappedResolver = wrappedResolver ?? new DefaultContractResolver();
            _wrappedBinder = wrappedBinder;
        }

        public void RegisterClassMap<T>(Action<ClassMap<T>> initializer)
        {
            var baseContract = ResolveContract(typeof(T)) as JsonObjectContract;
            if (baseContract == null)
                throw new InvalidOperationException("Only classes can be mapped.");

            var cm = new ClassMap<T>(baseContract);
            initializer(cm);

            _infoByType[typeof(T)] = cm;
            if (cm.TypeName != null)
                _infoByName[cm.TypeName] = cm;
        }

        public JsonContract ResolveContract(Type type)
        {
            if (_infoByType.ContainsKey(type))
                return _infoByType[type].JsonContract;

            JsonObjectContract contract;
            var innerContract = _wrappedResolver.ResolveContract(type);
            contract = innerContract as JsonObjectContract;
            if (contract == null) return innerContract;

            foreach (var prop in contract.Properties)
            {
                if (_infoByType.ContainsKey(prop.PropertyType))
                {
                    var info = _infoByType[prop.PropertyType];
                    if (info.TypeName != null)
                        prop.TypeNameHandling = TypeNameHandling.All;
                }
            }

            return contract;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_wrappedResolver != null, "Wrapped contract resolver cannot be null.");
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (_infoByType.ContainsKey(serializedType) && _infoByType[serializedType].TypeName != null)
            {
                assemblyName = null;
                typeName = _infoByType[serializedType].TypeName;
            }
            else if (_wrappedBinder != null)
                _wrappedBinder.BindToName(serializedType, out assemblyName, out typeName);
            else
                base.BindToName(serializedType, out assemblyName, out typeName);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            if (_infoByName.ContainsKey(typeName))
                return _infoByName[typeName].Type;
            else if (_wrappedBinder != null)
                return _wrappedBinder.BindToType(assemblyName, typeName);
            else
                return null;
        }
    }
}
