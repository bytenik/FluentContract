using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentContract
{
    public class ContractInformation(Type type)
    {
        public Type Type { get; set; } = type;
        public JsonObjectContract JsonContract { get; set; }
        public string TypeName { get; set; }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Type != null);
        }
    }
}
