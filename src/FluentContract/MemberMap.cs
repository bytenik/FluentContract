﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentContract
{
    public class MemberMap
    {
        public MemberMap(JsonProperty jsonProperty)
        {
            _jsonProperty = jsonProperty;
        }

        private readonly JsonProperty _jsonProperty;

        public MemberMap SetConverter(JsonConverter converter)
        {
            _jsonProperty.Converter = converter;
            _jsonProperty.MemberConverter = converter;
            return this;
        }

        public MemberMap SetName(string name)
        {
            _jsonProperty.PropertyName = name;
            return this;
        }
    }
}
