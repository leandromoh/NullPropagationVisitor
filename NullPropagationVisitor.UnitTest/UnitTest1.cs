using System;
using System.Linq.Expressions;
using Xunit;

namespace NullPropagationVisitor.UnitTest
{
    public class UnitTest1
    {
        private static string Foo(string s) => s;

        private readonly NullPropagationVisitor visitor;

        public UnitTest1()
        {
            visitor = new NullPropagationVisitor(recursive: true);
        }

        [Fact]
        void Test1()
        {
            Expression<Func<string, char?>> f = s => s == "foo" ? 'X' : Foo(s).Length.ToString()[0];

            var fBody = (Expression<Func<string, char?>>)visitor.Visit(f);

            var fFunc = fBody.Compile();

            Console.WriteLine(fFunc(null) == null);
            Console.WriteLine(fFunc("bar") == '3');
            Console.WriteLine(fFunc("foo") == 'X');
        }

        [Fact]
        void Test2()
        {
            Expression<Func<string, int>> y = s => s.Length;

            var yBody = visitor.Visit(y.Body);
            var yFunc = Expression.Lambda<Func<string, int?>>(
                                        body: yBody,
                                        parameters: y.Parameters)
                                .Compile();

            Console.WriteLine(yFunc(null) == null);
            Console.WriteLine(yFunc("bar") == 3);
        }

        [Fact]
        void Test3()
        {
            Expression<Func<char?, string>> y = s => s.Value.ToString()[0].ToString();

            var yBody = (Expression<Func<char?, string>>)visitor.Visit(y);
            var yFunc = yBody.Compile();

            Console.WriteLine(yFunc(null) == null);
            Console.WriteLine(yFunc('A') == "A");
        }

        [Fact]
        void Test4()
        {
            Expression<Func<string, double>> y = str => str.Length;

            var yBody = (Expression<Func<string, double?>>)visitor.Visit(y);
            var yFunc = yBody.Compile();

            Console.WriteLine(yFunc(null) == null);
            Console.WriteLine(yFunc("bar") == 3);
        }

        [Fact]
        void Test4_0()
        {
            Expression<Func<string, double?>> y = str => str.Length;

            var yBody = (Expression<Func<string, double?>>)visitor.Visit(y);
            var yFunc = yBody.Compile();

            Console.WriteLine(yFunc(null) == null);
            Console.WriteLine(yFunc("bar") == 3);
        }

        [Fact]
        void Test5()
        {
            Expression<Func<string, char?>> f = s => s == "foo" ? 'X' : ((double)Foo(s).Length).ToString()[0];

            var fBody = (Expression<Func<string, char?>>)visitor.Visit(f);

            var fFunc = fBody.Compile();

            Console.WriteLine(fFunc(null) == null);
            Console.WriteLine(fFunc("bar") == '3');
            Console.WriteLine(fFunc("foo") == 'X');
        }

        [Fact]
        void Test6()
        {
            Expression<Func<string, double>> y = str => (float)str.Length;

            var yBody = (Expression<Func<string, double?>>)visitor.Visit(y);
            var yFunc = yBody.Compile();

            Console.WriteLine(yFunc(null) == null);
            Console.WriteLine(yFunc("bar") == 3);
        }
    }
}
