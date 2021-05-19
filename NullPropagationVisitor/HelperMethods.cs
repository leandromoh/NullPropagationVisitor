using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NullPropagationVisitor
{
    internal static class HelperMethods
    {
        private static bool IsValueType(this Type type)
        {
#if !NETSTANDARD1_1
            return type.IsValueType;
#else
            return type.GetTypeInfo().IsValueType;
#endif
        }

#if NETSTANDARD1_1
        public static bool IsAssignableFrom(this Type to, Type from)
        {
            var _to = to.GetTypeInfo();
            var _from = from.GetTypeInfo();

            return _to.IsAssignableFrom(_from);
        }
#endif

        public static Expression MakeNullable(Expression ex)
        {
            if (IsNullable(ex))
                return ex;

            return Expression.Convert(ex, MakeNullable(ex.Type));
        }

        public static Type MakeNullable(Type type)
        {
            if (IsNullable(type))
                return type;

            return typeof(Nullable<>).MakeGenericType(type);
        }

        public static bool IsNullable(Expression ex)
        {
            return IsNullable(ex.Type);
        }

        public static bool IsNullable(Type type)
        {
            return !type.IsValueType() || (Nullable.GetUnderlyingType(type) != null);
        }

        public static bool IsNullableStruct(Expression ex)
        {
            return ex.Type.IsValueType() && (Nullable.GetUnderlyingType(ex.Type) != null);
        }

        public static bool IsReferenceType(Expression ex)
        {
            return !ex.Type.IsValueType();
        }

        public static Expression RemoveNullable(Expression ex)
        {
            if (IsNullableStruct(ex))
                return Expression.Convert(ex, ex.Type.GenericTypeArguments[0]);

            return ex;
        }
    }
}
