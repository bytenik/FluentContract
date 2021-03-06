﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FluentContract.Test
{
    public class Constructors
    {
        class Custom
        {
            public string String { get; private set; }
            public int Integer { get; private set; }

            public Custom(int? x)
            {
                String = x.ToString();
                Integer = x.Value;
            }

            public Custom(string y)
            {
                String = y;
            }
        }

        [Fact]
        public void Class_Wont_Construct_Without_Customization()
        {
            var inst = new Custom(5);
            var json = JsonConvert.SerializeObject(inst);
            Console.WriteLine(json);
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Custom>(json));
        }

        [Fact]
        public void Class_Should_Construct_Via_Lambda()
        {
            var mappings = new FluentMappings();
            mappings.MapClass<Custom>(x => x.MapCreator(() => new Custom(3)));

            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            var inst = new Custom(5);
            var json = JsonConvert.SerializeObject(inst, sett);
            Console.WriteLine(json);
            var newinst = JsonConvert.DeserializeObject<Custom>(json, sett);
            Assert.NotEqual(inst.String, newinst.String);
        }

        [Fact]
        public void Class_Constructs_With_Custom_Constructor_With_Implied_Cast()
        {
            var mappings = new FluentMappings();
            mappings.MapClass<Custom>(x => x.MapCreator(t => new Custom(t.Integer)));

            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            var inst = new Custom(5);
            var json = JsonConvert.SerializeObject(inst, sett);
            Console.WriteLine(json);
            var newinst = JsonConvert.DeserializeObject<Custom>(json, sett);
            Assert.Equal(inst.String, newinst.String);
        }

        [Fact]
        public void Class_Constructs_With_Custom_Constructor()
        {
            var mappings = new FluentMappings();
            mappings.MapClass<Custom>(x => x.MapCreator(t => new Custom(t.String)));

            var sett = new JsonSerializerSettings { ContractResolver = mappings.ContractResolver, Binder = mappings.Binder };
            var inst = new Custom(5);
            var json = JsonConvert.SerializeObject(inst, sett);
            Console.WriteLine(json);
            var newinst = JsonConvert.DeserializeObject<Custom>(json, sett);
            Assert.Equal(inst.String, newinst.String);
        }
    }
}
