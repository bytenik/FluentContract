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
    public class Inheritance
    {
        interface IInterface { }

        class Container
        {
            public Parent Object { get; set; }
        }

        class InterfaceContainer
        {
            public IInterface Object { get; set; }
        }

        class CollectionContainer
        {
            public Parent[] Object { get; set; }
        }

        abstract class Parent
        {
            public string ParentString { get; set; }
        }

        class Child : Parent, IInterface
        {
            public string ChildString { get; set; }
        }

        [Fact]
        public void Discriminator_Should_Be_Written_For_Child()
        {
            var mappings = new FluentMappings();
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            mappings.MapClass<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.MapClass<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new Container() { Object = new Child() }, sett);
            var linq = JsonConvert.DeserializeObject<JObject>(json);
            Assert.Equal("Child", linq.$Object["$type"]);
        }

        [Fact]
        public void Discriminator_Should_Be_Written_For_Child_When_Container_Is_Mapped()
        {
            var mappings = new FluentMappings();
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            mappings.MapClass<Container>(cm => { });
            mappings.MapClass<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.MapClass<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new Container() { Object = new Child() }, sett);
            var linq = JsonConvert.DeserializeObject<JObject>(json);
            Assert.Equal("Child", linq.$Object["$type"]);
        }

        [Fact]
        public void Discriminator_Enables_Child_Deserialization_Into_Parent_Reference()
        {
            var mappings = new FluentMappings();
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            mappings.MapClass<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.MapClass<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new Container() { Object = new Child() { ChildString = "Test" } }, sett);
            var rt = JsonConvert.DeserializeObject<Container>(json, sett);
            Assert.IsType<Child>(rt.Object);
            Assert.Equal("Test", ((Child)rt.Object).ChildString);
        }

        [Fact]
        public void Discriminator_Enables_Child_Deserialization_Into_Interface_Reference()
        {
            var mappings = new FluentMappings();
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            mappings.MapClass<IInterface>(cm => cm.SetDiscriminator("Interface"));
            mappings.MapClass<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new InterfaceContainer() { Object = new Child() { ChildString = "Test" } }, sett);
            Console.WriteLine(json);
            var rt = JsonConvert.DeserializeObject<InterfaceContainer>(json, sett);
            Assert.IsType<Child>(rt.Object);
            Assert.Equal("Test", ((Child)rt.Object).ChildString);
        }

        [Fact]
        public void Missing_Discriminator_Breaks_Child_Deserialization()
        {
            var mappings = new FluentMappings();
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            mappings.MapClass<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.MapClass<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new Container() { Object = new Child() { ChildString = "Test" } }, sett);

            // kill the child map
            mappings = new FluentMappings();
            sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            mappings.MapClass<Parent>(cm => cm.SetDiscriminator("Parent"));

            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Container>(json, sett));
        }

        [Fact]
        public void Discriminator_Should_Be_Written_For_Collection_Contained_In_Type()
        {
            var mappings = new FluentMappings();
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            mappings.MapClass<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.MapClass<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new CollectionContainer() { Object = new Parent[] { new Child(), new Child() } }, sett);
            Console.WriteLine(json);
            var linq = JsonConvert.DeserializeObject<JObject>(json);
            Assert.Equal("Child", linq.$Object[0]["$type"]);
        }

        [Fact]
        public void Discriminator_Should_Be_Written_For_Array()
        {
            var mappings = new FluentMappings();
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            mappings.MapClass<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.MapClass<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject(new Parent[] { new Child(), new Child() }, sett);
            Console.WriteLine(json);
            var linq = JsonConvert.DeserializeObject<JArray>(json);
            Assert.Equal("Child", linq[0]["$type"]);
        }

        [Fact]
        public void Discriminator_Should_Be_Written_For_Set()
        {
            var mappings = new FluentMappings();
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            mappings.MapClass<Parent>(cm => cm.SetDiscriminator("Parent"));
            mappings.MapClass<Child>(cm => cm.SetDiscriminator("Child"));
            var json = JsonConvert.SerializeObject((ISet<Parent>)new HashSet<Parent> { new Child(), new Child() }, sett);
            Console.WriteLine(json);
            var linq = JsonConvert.DeserializeObject<JArray>(json);
            Assert.Equal("Child", linq[0]["$type"]);
        }
    }
}
