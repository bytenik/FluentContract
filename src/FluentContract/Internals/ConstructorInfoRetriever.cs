using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FluentContract.Internals
{
    /// <summary>
    /// A helper class used to create and compile delegates for creator maps.
    /// </summary>
    /// <remarks>
    /// Taken and modified from https://github.com/mongodb/mongo-csharp-driver/blob/master/src/MongoDB.Bson/Serialization/CreatorMapDelegateCompiler.cs,
    /// which is licensed under Apache License v2.0.
    /// </remarks>
    class ConstructorInfoRetriever : ExpressionVisitor
    {
        private Type _class;
        private ParameterExpression _prototypeParameter;
        private ConstructorInfo _constructorInfo;
        private Dictionary<ParameterInfo, MemberInfo> _parameters;

        private MemberInfo _lastMemberAccess;

        public ConstructorInfo GetConstructorInfo<TClass>(Expression<Func<TClass, TClass>> creatorLambda, out Dictionary<ParameterInfo, MemberInfo> arguments)
        {
            _class = typeof(TClass); // not creatorLambda.Type in case lambda returns a subtype of TClass
            _prototypeParameter = creatorLambda.Parameters[0];
            _parameters = new Dictionary<ParameterInfo, MemberInfo>();
            var body = Visit(creatorLambda.Body);

            arguments = _parameters;
            return _constructorInfo;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Constructor.DeclaringType == _class)
            {
                _constructorInfo = node.Constructor;
                _parameters = node.Constructor.GetParameters().Select((pi, i) =>
                {
                    var arg = node.Arguments[i];

                    if (arg.NodeType != ExpressionType.MemberAccess)
                        throw new InvalidOperationException("Constructor argument " + i + " is not a member reference against the prototype parameter.");

                    VisitMember((MemberExpression)arg);

                    return new { pi, mi = _lastMemberAccess };
                })
                .ToDictionary(x => x.pi, x => x.mi);
            }

            return base.VisitNew(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == _prototypeParameter)
            {
                _lastMemberAccess = node.Member;

                return node;
            }
            else
                throw new InvalidOperationException("The only operations allowed are accessing a field or property on the prototype parameter.");
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _prototypeParameter)
                throw new InvalidOperationException("The only operations allowed on the prototype parameter are accessing a field or property.");

            return base.VisitParameter(node);
        }
    }
}
