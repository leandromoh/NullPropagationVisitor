using System;
using System.Linq.Expressions;

namespace NullPropagationVisitor
{
    public class NullPropagationVisitor : ExpressionVisitor
    {
        private readonly bool _recursive;

        public NullPropagationVisitor(bool recursive)
        {
            _recursive = recursive;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Expression.Lambda(Visit(node.Body), node.Parameters);
        }

        protected override Expression VisitUnary(UnaryExpression propertyAccess)
        {
            if (propertyAccess.NodeType == ExpressionType.Convert && !IsNullable(propertyAccess))
            {
                var safe = Visit(propertyAccess.Operand);
                var caller = Expression.Variable(safe.Type, "caller");
                var assign = Expression.Assign(caller, safe);

                var tipofinal = MakeNullable(propertyAccess.Type);
                var d = Expression.Convert(RemoveNullable(caller), propertyAccess.Type);
                var e = Expression.Convert(d, tipofinal);

                var result = Expression.Condition(
                        test: Expression.Equal(caller, Expression.Constant(null)),
                        ifTrue: Expression.Constant(null, tipofinal),
                        ifFalse: e);

                var block = Expression.Block(
                    variables: new[]
                    {
                            caller,
                    },
                    expressions: new Expression[]
                    {
                            assign,
                            result,
                    });

                return block;
            }

            if (propertyAccess.Operand is MemberExpression mem)
                return VisitMember(mem);

            if (propertyAccess.Operand is MethodCallExpression met)
                return VisitMethodCall(met);

            if (propertyAccess.Operand is ConditionalExpression cond)
                return Expression.Condition(
                        test: cond.Test,
                        ifTrue: MakeNullable(Visit(cond.IfTrue)),
                        ifFalse: MakeNullable(Visit(cond.IfFalse)));


            return base.VisitUnary(propertyAccess);
        }

        protected override Expression VisitMember(MemberExpression propertyAccess)
        {
            return Common(propertyAccess.Expression, propertyAccess);
        }

        protected override Expression VisitMethodCall(MethodCallExpression propertyAccess)
        {
            if (propertyAccess.Object == null)
                return base.VisitMethodCall(propertyAccess);

            return Common(propertyAccess.Object, propertyAccess);
        }

        private BlockExpression Common(Expression instance, Expression propertyAccess)
        {
            var safe = _recursive ? base.Visit(instance) : instance;
            var caller = Expression.Variable(safe.Type, "caller");
            var assign = Expression.Assign(caller, safe);
            var acess = MakeNullable(new ExpressionReplacerVisitor(instance,
                IsNullableStruct(instance) ? caller : RemoveNullable(caller)).Visit(propertyAccess));
            var ternary = Expression.Condition(
                        test: Expression.Equal(caller, Expression.Constant(null)),
                        ifTrue: Expression.Constant(null, acess.Type),
                        ifFalse: acess);

            return Expression.Block(
                    type: acess.Type,
                    variables: new[]
                    {
                            caller,
                    },
                    expressions: new Expression[]
                    {
                            assign,
                            ternary,
                    });
        }

        private static Expression MakeNullable(Expression ex)
        {
            if (IsNullable(ex))
                return ex;

            return Expression.Convert(ex, MakeNullable(ex.Type));
        }

        private static Type MakeNullable(Type type)
        {
            if (IsNullable(type))
                return type;

            return typeof(Nullable<>).MakeGenericType(type);
        }

        private static bool IsNullable(Expression ex)
        {
            return IsNullable(ex.Type);
        }

        private static bool IsNullable(Type type)
        {
            return !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);
        }

        private static bool IsNullableStruct(Expression ex)
        {
            return ex.Type.IsValueType && (Nullable.GetUnderlyingType(ex.Type) != null);
        }

        private static Expression RemoveNullable(Expression ex)
        {
            if (IsNullableStruct(ex))
                return Expression.Convert(ex, ex.Type.GenericTypeArguments[0]);

            return ex;
        }
    }
}
