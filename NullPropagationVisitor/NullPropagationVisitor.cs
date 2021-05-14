using System;
using static NullPropagationVisitor.HelperMethods;
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
            if (propertyAccess.NodeType == ExpressionType.Convert)
                return VisitConvert(propertyAccess);

            if (propertyAccess.Operand is MemberExpression member)
                return VisitMember(member);

            if (propertyAccess.Operand is MethodCallExpression method)
                return VisitMethodCall(method);

            if (propertyAccess.Operand is ConditionalExpression condition)
                return VisitConditional(condition);

            return base.VisitUnary(propertyAccess);
        }

        protected override Expression VisitConditional(ConditionalExpression cond)
        {
            return Expression.Condition(
                        test: cond.Test,
                        ifTrue: MakeNullable(Visit(cond.IfTrue)),
                        ifFalse: MakeNullable(Visit(cond.IfFalse)));
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

        protected virtual Expression VisitConvert(UnaryExpression propertyAccess)
        {
            if (propertyAccess.NodeType != ExpressionType.Convert)
                throw new InvalidOperationException("invalid call");

            var safe = Visit(propertyAccess.Operand);
            var caller = Expression.Variable(safe.Type, "caller");
            var assign = Expression.Assign(caller, safe);

            var tipofinal = MakeNullable(propertyAccess.Type);
            var d = Expression.Convert(RemoveNullable(caller), propertyAccess.Type);
            var e = Expression.Convert(d, tipofinal);

            var cond = Expression.Condition(
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
                            cond,
                        });

            return block;
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
    }
}
