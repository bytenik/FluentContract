using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FluentContract.Internals
{
    static class ReflectionHelper
    {
        public static MemberInfo GetMemberInfo<TObject>(this Expression<Func<TObject, object>> expression)
        {
            var member = expression.Body as MemberExpression;
            if (member != null)
                return member.Member;

            throw new ArgumentException("expression");
        }
    }
}
