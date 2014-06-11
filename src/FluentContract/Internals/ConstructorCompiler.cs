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
        private Dictionary<MemberInfo, ParameterExpression> _parameters;

        public ConstructorInfo GetConstructorInfo<TClass>(Expression<Func<TClass, TClass>> creatorLambda, out Dictionary<MemberInfo, ParameterExpression> arguments)
        {
            // transform c => expression (where c is the prototype parameter)
            // to (p1, p2, ...) => expression' where expression' is expression with every c.X replaced by p#

            _class = typeof(TClass); // not creatorLambda.Type in case lambda returns a subtype of TClass
            _prototypeParameter = creatorLambda.Parameters[0];
            _parameters = new Dictionary<MemberInfo, ParameterExpression>();
            var body = Visit(creatorLambda.Body);

            arguments = _parameters;
            return _constructorInfo;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Constructor.DeclaringType == _class)
                _constructorInfo = node.Constructor;

            return base.VisitNew(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == _prototypeParameter)
            {
                var memberInfo = node.Member;

                ParameterExpression parameter;
                if (!_parameters.TryGetValue(memberInfo, out parameter))
                {
                    var parameterName = string.Format("_p{0}_", _parameters.Count + 1); // avoid naming conflicts with body
                    parameter = Expression.Parameter(node.Type, parameterName);
                    _parameters.Add(memberInfo, parameter);
                }

                return parameter;
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _prototypeParameter)
                throw new InvalidOperationException("The only operations allowed on the prototype parameter are accessing a field or property.");

            return base.VisitParameter(node);
        }
    }
}
