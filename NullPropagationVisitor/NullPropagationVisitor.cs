using System;
using System.Linq.Expressions;
using static NullPropagationVisitor.HelperMethods;

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
            return Common(propertyAccess.Expression, caller =>
            {
                return MakeNullable(new ExpressionReplacerVisitor(propertyAccess.Expression,
                IsNullableStruct(propertyAccess.Expression) ? caller : RemoveNullable(caller)).Visit(propertyAccess));
            });
        }

        protected override Expression VisitMethodCall(MethodCallExpression propertyAccess)
        {
            if (propertyAccess.Object == null)
                return base.VisitMethodCall(propertyAccess);

            return Common(propertyAccess.Object, caller =>
            {
                return MakeNullable(new ExpressionReplacerVisitor(propertyAccess.Object,
                IsNullableStruct(propertyAccess.Object) ? caller : RemoveNullable(caller)).Visit(propertyAccess));
            });
        }

        protected virtual Expression VisitConvert(UnaryExpression propertyAccess)
        {
            if (propertyAccess.NodeType != ExpressionType.Convert)
                throw new InvalidOperationException("invalid call");

            return Common(propertyAccess.Operand, caller =>
            {
                return Expression.Convert(RemoveNullable(caller), propertyAccess.Type);
            });
        }

        private BlockExpression Common(Expression instance, Func<Expression, Expression> callback)
        {
            var safe = _recursive ? base.Visit(instance) : instance;
            var caller = Expression.Variable(safe.Type, "caller");
            var assign = Expression.Assign(caller, safe);
            var acess = MakeNullable(callback(caller));

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
