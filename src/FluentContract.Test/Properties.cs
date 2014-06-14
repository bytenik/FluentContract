using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FluentContract.Test
{
    public class Properties
    {
        class Obj
        {
            public string String { get; set; }
        }

        [Fact]
        public void Can_Find_Member_When_Parent_Renames()
        {
            var wrapped = new CamelCasePropertyNamesContractResolver();
            var mappings = new FluentMappings(wrapped);
            mappings.MapClass<Obj>(cm => cm.MapMember(x => x.String, x => x.SetConverter(null)));
            var contract = (JsonObjectContract)mappings.ContractResolver.ResolveContract(typeof(Obj));
            Assert.Equal(1, contract.Properties.Count); // ensures that we didn't make our own second property by accident
        }
    }
}
