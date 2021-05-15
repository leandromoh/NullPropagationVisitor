# NullPropagationVisitor

[Expression tree](https://docs.microsoft.com/dotnet/csharp/programming-guide/concepts/expression-trees/) is an amazing feature of C#, however it is only support a [limited subset of C# features](https://github.com/dotnet/csharplang/discussions/158).  
While there is no native support to null propagation operator `a.?b`, this library can do the job as an [expression visitor](https://stackoverflow.com/questions/41432852/why-would-i-want-to-use-an-expressionvisitor).  
Just call `Visit` method of the visitor and you are done!

As well the `?.` operator, the visitor evaluates its left-hand operand no more than once.  
You can use projects like [ReadableExpressions](https://github.com/agileobjects/ReadableExpressions) to check the expression returned by visitor.

## Examples of use

Check some examples [here](NullPropagationVisitor.UnitTest/NullPropagationVisitorTest.cs)

## Transformation

It works from the basic cenarios:

```c#
Expression<Func<string, int>> ex = str => str.Length;
```
which become something like
```c#
(string str) =>
{
    string caller = str;

    return (caller == null) ? null : (int?)caller.Length;
}
```

To the complex ones
```c#
Expression<Func<string, char>> ex = str => str == "foo" ? 'X' : Self(str).Length.ToString()[0];
```
which become something like
```c#
(string str) => (str == "foo")
    ? (char?)'X'
    : {
        string caller =
        {
            int? caller =
            {
                string caller = Program.Self(str);

                return (caller == null) ? null : (int?)caller.Length;
            };

            return (caller == null) ? null : ((int)caller).ToString();
        };

        return (caller == null) ? null : (char?)caller[0];
    }

```
