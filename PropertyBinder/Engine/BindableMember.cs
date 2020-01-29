using System;
using System.Linq.Expressions;
using System.Reflection;

namespace PropertyBinder.Engine
{
    internal sealed class BindableMember
    {
        private readonly Func<Type, Delegate> _createSelector;

        public BindableMember(PropertyInfo property)
        {
            Name = string.Intern(property.Name);
            _createSelector = t => CreatePropertySelector(t, property);
            CanSubscribe = !property.IsDefined(typeof(ImmutableAttribute));
        }

        public BindableMember(FieldInfo field)
        {
            Name = string.Intern(field.Name);
            _createSelector = t => CreateMemberSelector(t, field);
        }

        public BindableMember(string index)
        {
            Name = string.Intern(index);
            _createSelector = t => CreateIndexerSelector(t, index);
            CanSubscribe = true;
        }

        public string Name { get; }

        public bool CanSubscribe { get; private set; }

        public Delegate CreateSelector(Type parentType)
        {
            return _createSelector(parentType);
        }

        private static Delegate CreateMemberSelector(Type parentType, MemberInfo member)
        {
            var parameter = Expression.Parameter(parentType);
            return Binder.ExpressionCompiler.Compile(Expression.Lambda(Expression.MakeMemberAccess(parameter, member), parameter));
        }

        private static Delegate CreatePropertySelector(Type parentType, PropertyInfo property)
        {
            if (parentType.IsValueType)
            {
                return CreateMemberSelector(parentType, property);
            }

            return property.GetGetMethod(true).CreateDelegate(typeof(Func<,>).MakeGenericType(parentType, property.PropertyType));
        }

        private static Delegate CreateIndexerSelector(Type parentType, string index)
        {
            var parameter = Expression.Parameter(parentType);
            return Binder.ExpressionCompiler.Compile(Expression.Lambda(
                Expression.Call(
                    parameter,
                    parentType.GetMethod("get_Item"),
                    Expression.Constant(index)),
                parameter));
        }
    }
}