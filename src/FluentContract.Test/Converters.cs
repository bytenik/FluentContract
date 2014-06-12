using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FluentContract.Test
{
    public class Converters
    {
        class TestConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public const string ReadRepl = "Read Replacement";
            public const string WriteRepl = "Write Replacement";

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return ReadRepl;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(WriteRepl);
            }
        }

        class Obj
        {
            public string String { get; set; }
        }

        [Fact]
        public void Custom_Converter_Should_Write_Entire_Type()
        {
            var mappings = new FluentMappings();
            mappings.RegisterClassMap<Obj>(cm => cm.SetConverter(new TestConverter()));
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };

            var obj = new Obj { String = "Hello World" };
            var json = JsonConvert.SerializeObject(obj, sett);
            var str = JsonConvert.DeserializeObject<string>(json); // no sett
            Assert.Equal(TestConverter.WriteRepl, str);
        }

        [Fact]
        public void Custom_Converter_Should_Write_Single_Property()
        {
            var mappings = new FluentMappings();
            mappings.RegisterClassMap<Obj>(cm => cm.MapMember(mm => mm.String, mm => mm.SetConverter(new TestConverter())));
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };

            var obj = new Obj { String = "Hello World" };
            var json = JsonConvert.SerializeObject(obj, sett);
            Console.WriteLine(json);
            Assert.Contains(TestConverter.WriteRepl, json);
            Assert.DoesNotContain("Hello World", json);
        }

        [Fact]
        public void Custom_Converter_Should_Read_Single_Property()
        {
            var mappings = new FluentMappings();
            mappings.RegisterClassMap<Obj>(cm => cm.MapMember(mm => mm.String, mm => mm.SetConverter(new TestConverter())));
            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };

            var json = "{ \"String\": \"Hello World\" }";
            var obj = JsonConvert.DeserializeObject<Obj>(json, sett);
            Assert.Equal(TestConverter.ReadRepl, obj.String);
        }
    }
}
