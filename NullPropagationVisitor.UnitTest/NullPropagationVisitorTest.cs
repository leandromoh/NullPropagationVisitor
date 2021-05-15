using System;
using System.Linq.Expressions;
using Xunit;
using FluentAssertions;

namespace NullPropagationVisitor.UnitTest
{
    public class NullPropagationVisitorTest
    {
        public struct Foo
        {
            public int Number;

            public string GetNullWhenIs2() => Number == 2 ? null : Number.ToString();
        }

        private static T Self<T>(T s) => s;

        private readonly NullPropagationVisitor visitor;
        private readonly NullPropagationVisitor visitorNonRecursive;

        public NullPropagationVisitorTest()
        {
            visitor = new NullPropagationVisitor(recursive: true);
            visitorNonRecursive = new NullPropagationVisitor(recursive: false);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData('A', 'A')]
        void Single_member_property_of_nullable_struct(char? input, char? expected)
        {
            // Arrange

            Expression<Func<char?, char>> ex = x => x.Value;

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<char?, char?>>).Compile();

            // Assert 

            func(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(2, null)]
        [InlineData(3, 1)]
        [InlineData(33, 2)]
        void When_non_recursive_but_ends_nullable_should_visit_ok(int input, int? expected)
        {
            // Arrange

            var instance = new Foo { Number = input };
            Expression<Func<Foo, int>> ex = x => x.GetNullWhenIs2().Length;

            // Act

            var func = (visitorNonRecursive.Visit(ex) as Expression<Func<Foo, int?>>).Compile();

            // Assert 

            func(instance).Should().Be(expected);
        }

        [Fact]
        void When_non_recursive_but_starts_non_nullable_should_throw()
        {
            // Arrange

            Expression<Func<Foo, string>> ex = x => x.GetNullWhenIs2();

            // Act

            Action act = () => visitorNonRecursive.Visit(ex);

            // Assert

            act.Should().Throw<InvalidOperationException>().WithMessage("Can not apply operand on type Foo. Only nullable are allowed.");
        }

        [Fact]
        void When_recursive_but_starts_non_nullable_should_throw()
        {
            // Arrange

            Expression<Func<Foo, string>> ex = x => x.GetNullWhenIs2();

            // Act

            Action act = () => visitor.Visit(ex);

            // Assert

            act.Should().Throw<InvalidOperationException>().WithMessage("Can not apply operand on type Foo. Only nullable are allowed.");
        }

        [Fact]
        void When_recursive_and_find_non_nullable_in_the_most_left_should_throw()
        {
            // Arrange

            Expression<Func<Foo, int>> ex = x => x.GetNullWhenIs2().Length;

            // Act

            Action act = () => visitor.Visit(ex);

            // Assert

            act.Should().Throw<InvalidOperationException>().WithMessage("Can not apply operand on type Foo. Only nullable are allowed.");
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData('A', "A")]
        void Single_member_method_of_nullable_struct(char? input, string expected)
        {
            // Arrange

            Expression<Func<char?, string>> ex = x => x.Value.ToString();

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<char?, string>>).Compile();

            // Assert 

            func(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("bar", 3)]
        void Single_member_property_expression(string input, int? expected)
        {
            // Arrange

            Expression<Func<string, int>> ex = str => str.Length;

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<string, int?>>).Compile();

            // Assert 

            func(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("bar", 3)]
        void Single_member_property_expression_with_implicit_convertion_non_nullable(string input, int? expected)
        {
            // Arrange

            Expression<Func<string, double>> ex = str => str.Length;

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<string, double?>>).Compile();

            // Assert 

            func(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("bar", 3)]
        void Single_member_property_expression_with_implicit_convertion_to_nullable(string input, int? expected)
        {
            // Arrange

            Expression<Func<string, double?>> ex = str => str.Length;

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<string, double?>>).Compile();

            // Assert 

            func(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("bar", 3)]
        void Single_member_property_expression_with_explicit_and_implicit_convertion_non_nullable(string input, int? expected)
        {
            // Arrange

            Expression<Func<string, double>> ex = str => (float)str.Length;

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<string, double?>>).Compile();

            // Assert 

            func(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("bar", 3)]
        void Should_not_evaluate_left_side_more_than_once(string input, int? expected)
        {
            // Arrange

            var calls = 0;
            Func<string, string> counter = (string str) =>
            {
                calls++;
                return str;
            };


            Expression<Func<string, int>> ex = str => counter(str).Length;

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<string, int?>>).Compile();

            // Assert 

            func(input).Should().Be(expected);
            calls.Should().Be(1);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("bar", 3)]
        void Single_static_method_call_with_member_expression(string input, int? expected)
        {
            // Arrange

            Expression<Func<string, int>> ex = str => Self(str).Length;

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<string, int?>>).Compile();

            // Assert 

            func(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("bar", '3')]
        [InlineData("foo", 'X')]
        [InlineData("1234", '4')]
        void Complex_chain_null_propagation_inside_ternary(string input, char? expected)
        {
            // Arrange

            Expression<Func<string, char?>> ex = str => str == "foo" ? 'X' : Self(str).Length.ToString()[0];

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<string, char?>>).Compile();

            // Assert

            func(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData('A', "A")]
        void Complex_chain_null_propagation(char? input, string expected)
        {
            // Arrange

            Expression<Func<char?, string>> ex = str => Self(str).Value.ToString()[0].ToString();

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<char?, string>>).Compile();

            // Assert 

            func(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("bar", '3')]
        [InlineData("foo", 'X')]
        [InlineData("1234", '4')]
        void Complex_chain_null_propagation_inside_ternary_with_conversion(string input, char? expected)
        {
            // Arrange

            Expression<Func<string, char?>> ex = s => s == "foo" ? 'X' : ((double)Self(s).Length).ToString()[0];

            // Act

            var func = (visitor.Visit(ex) as Expression<Func<string, char?>>).Compile();

            // Assert

            func(input).Should().Be(expected);
        }
    }
}
