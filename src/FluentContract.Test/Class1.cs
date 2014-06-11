using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentContract.Test
{
    public class TestInheritance
    {
        class Container
        {
            public Parent Object { get; set; }
        }

        class Parent
        {
            public string ParentString { get; set; }
        }

        class Child : Parent
        {
            public string ChildString { get; set; }
        }

        [Fact]
        public void Discriminator_Should_Be_Written_For_Child()
        {
            var mappings = new FluentResolverAndBinder();
            var sett = new JsonSerializerSettings { ContractResolver = mappings, Binder = mappings };
            mappings.RegisterClassMap<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.RegisterClassMap<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new Container() { Object = new Child() }, sett);
            var linq = JsonConvert.DeserializeObject<JObject>(json);
            Assert.Equal("Child", linq.$Object["$type"]);
        }

        [Fact]
        public void Discriminator_Should_Be_Written_For_Parent()
        {
            var mappings = new FluentResolverAndBinder();
            var sett = new JsonSerializerSettings { ContractResolver = mappings, Binder = mappings };
            mappings.RegisterClassMap<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.RegisterClassMap<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new Container() { Object = new Parent() }, sett);
            var linq = JsonConvert.DeserializeObject<JObject>(json);
            Assert.Equal("Parent", linq.$Object["$type"]);
        }

        [Fact]
        public void Discriminator_Enables_Child_Deserialization_Into_Parent_Reference()
        {
            var mappings = new FluentResolverAndBinder();
            var sett = new JsonSerializerSettings { ContractResolver = mappings, Binder = mappings };
            mappings.RegisterClassMap<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.RegisterClassMap<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new Container() { Object = new Child() { ChildString = "Test" } }, sett);
            var rt = JsonConvert.DeserializeObject<Container>(json, sett);
            Assert.IsType<Child>(rt.Object);
            Assert.Equal("Test", ((Child)rt.Object).ChildString);
        }

        [Fact]
        public void Missing_Discriminator_Breaks_Child_Deserialization()
        {
            var mappings = new FluentResolverAndBinder();
            var sett = new JsonSerializerSettings { ContractResolver = mappings, Binder = mappings };
            mappings.RegisterClassMap<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.RegisterClassMap<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new Container() { Object = new Child() { ChildString = "Test" } }, sett);

            // kill the child map
            mappings = new FluentResolverAndBinder();
            sett = new JsonSerializerSettings { ContractResolver = mappings, Binder = mappings };
            mappings.RegisterClassMap<Parent>(cm => cm.SetDiscriminator("Parent"));

            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Container>(json, sett));
        }
    }
}
