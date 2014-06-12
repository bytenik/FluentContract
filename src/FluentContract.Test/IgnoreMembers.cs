using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FluentContract.Test
{
    public class IgnoreMembers
    {
        class Obj
        {
            public string String { get; set; }
        }

        [Fact]
        public void Can_Ignore_Property()
        {
            var mappings = new FluentMappings();
            mappings.RegisterClassMap<Obj>(cm => cm.UnmapMember(x => x.String));
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };

            var obj = new Obj { String = "Test" };
            var json = JsonConvert.SerializeObject(obj, sett);
            var jobj = JsonConvert.DeserializeObject<JObject>(json);
            Assert.Empty(jobj.Properties());
        }
    }
}
