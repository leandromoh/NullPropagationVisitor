using System.Linq.Expressions;

namespace NullPropagationVisitor
{
    internal class ExpressionReplacerVisitor : ExpressionVisitor
    {
        private readonly Expression _oldEx;
        private readonly Expression _newEx;

        internal ExpressionReplacerVisitor(Expression oldEx, Expression newEx)
        {
            _oldEx = oldEx;
            _newEx = newEx;
        }

        public override Expression Visit(Expression node)
        {
            if (node == _oldEx)
                return _newEx;

            return base.Visit(node);
        }
    }
}
