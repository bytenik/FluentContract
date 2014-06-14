using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FluentContract.Test
{
    public class BasicResolve
    {
        [Fact]
        public void Can_Resolve_Null()
        {
            var mappings = new FluentMappings();
            mappings.ContractResolver.ResolveContract(null);
        }
    }
}
