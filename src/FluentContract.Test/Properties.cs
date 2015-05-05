using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace FluentContract.Test
{
    public class Properties
    {
        private string _string;

        class Obj
        {
            [JsonProperty]
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

        [Fact]
        public void Can_Roundtrip_Private_Field()
        {
            var wrapped = new CamelCasePropertyNamesContractResolver();
            var mappings = new FluentMappings(wrapped);
            mappings.MapClass<Properties>(cm =>
            {
                cm.UnmapAll();
                cm.MapMember(x => x._string, x => x.SetName("String"));
            });
            var contract = (JsonObjectContract)mappings.ContractResolver.ResolveContract(typeof(Properties));
            Assert.Equal(1, contract.Properties.Count); // ensures that we didn't make our own second property by accident
            Assert.Equal(nameof(_string), contract.Properties[0].UnderlyingName);

            _string = "Testing 1-2-3";
            var sett = new JsonSerializerSettings {ContractResolver = mappings.ContractResolver};
            var json = JsonConvert.SerializeObject(this, sett);
            var deser = JsonConvert.DeserializeObject<Properties>(json, sett);
            Assert.Equal(_string, deser._string);
        }

        [Fact]
        public void Can_Roundtrip_Public_Property()
        {
            var wrapped = new CamelCasePropertyNamesContractResolver();
            var mappings = new FluentMappings(wrapped);
            mappings.MapClass<Obj>(cm =>
            {
                cm.UnmapAll();
                cm.MapMember(x => x.String, x => x.SetName("string"));
            });

            var obj = new Obj() {String = "testing 1-2-3"};
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver };

            var json = JsonConvert.SerializeObject(obj, sett);
            var deser = JsonConvert.DeserializeObject<Obj>(json, sett);
            Assert.Equal(obj.String, deser.String);
        }
    }
}
