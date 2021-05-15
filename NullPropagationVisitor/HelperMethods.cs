using System;
using System.Linq.Expressions;

namespace NullPropagationVisitor
{
    public static class HelperMethods
    {
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
            return !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);
        }

        public static bool IsNullableStruct(Expression ex)
        {
            return ex.Type.IsValueType && (Nullable.GetUnderlyingType(ex.Type) != null);
        }

        public static bool IsReferenceType(Expression ex)
        {
            return !ex.Type.IsValueType;
        }

        public static Expression RemoveNullable(Expression ex)
        {
            if (IsNullableStruct(ex))
                return Expression.Convert(ex, ex.Type.GenericTypeArguments[0]);

            return ex;
        }
    }
}
