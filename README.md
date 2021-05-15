# NullPropagationVisitor

[Expression tree](https://docs.microsoft.com/dotnet/csharp/programming-guide/concepts/expression-trees/) is an amazing feature of C#, however it is only support a [limited subset of C# features](https://github.com/dotnet/csharplang/discussions/158). 

While there is no native support to null propagation operator `a.?b`, this library can do the job as an [expression visitor](https://stackoverflow.com/questions/41432852/why-would-i-want-to-use-an-expressionvisitor).

```c#
Expression<Func<string, int>> ex = str => str.Length;
```
become
```c#
(string str) =>
{
    string caller = str;

    return (caller == null) ? null : (int?)caller.Length;
}
```
