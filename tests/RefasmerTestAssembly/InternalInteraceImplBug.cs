namespace RefasmerTestAssembly;

public class Class1 : IInternalInterace<string>
{
    void IInternalInterace<string>.Method(string x, Internal2 y)
    {
    }
}

internal class Internal2{}

internal interface IInternalInterace<T>
{
    void Method(T x, Internal2 y);
}
