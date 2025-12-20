namespace RefasmerTestAssembly;

// See #53 and #54.
public class ExplicitImplOfInternalInterface : IInternalInterface<string>
{
    void IInternalInterface<string>.Method(string x, Internal2 y)
    {
    }
}

internal class Internal2{}

internal interface IInternalInterface<T>
{
    void Method(T x, Internal2 y);
}
