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
        private readonly IDictionary<Type, ContractInformation> _infoByType = new Dictionary<Type, ContractInformation>();
        private readonly IDictionary<string, ContractInformation> _infoByName = new Dictionary<string, ContractInformation>();
        private readonly IContractResolver _wrappedResolver;
        private readonly SerializationBinder _wrappedBinder;

        public FluentResolverAndBinder(IContractResolver wrappedResolver, SerializationBinder wrappedBinder = null)
        {
            Contract.Requires<ArgumentNullException>(wrappedResolver != null);

            _wrappedResolver = wrappedResolver;
            _wrappedBinder = wrappedBinder;
        }

        public FluentResolverAndBinder(SerializationBinder wrappedBinder = null)
            : this(new DefaultContractResolver(), wrappedBinder)
        {
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
            Contract.Invariant(_wrappedResolver != null, "Inner contract resolver cannot be null.");
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
