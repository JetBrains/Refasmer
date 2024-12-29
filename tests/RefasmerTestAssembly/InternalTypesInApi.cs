using System;

namespace RefasmerTestAssembly;

// Types suffixed by "ToBeMarkedInternal" are post-processed and converted to internal by tests.
public class Class1ToBeMarkedInternal;

public class PublicClassWithInternalTypeInApi
{
    public void Accept(Class1ToBeMarkedInternal argument) {}
}

public class Class2ToBeMarkedInternal;
public class PublicClassDerivingFromInternal : Class2ToBeMarkedInternal;

public interface IInterface1ToBeMarkedInternal;
public class PublicClassImplementingInternal : IInterface1ToBeMarkedInternal;

public interface IInterface2ToBeMarkedInternal<T>
{
    T Foo();
}
public class Class3ToBeMarkedInternal;
public class PublicClassWithInternalInterfaceImpl : IInterface2ToBeMarkedInternal<Class3ToBeMarkedInternal>
{
    Class3ToBeMarkedInternal IInterface2ToBeMarkedInternal<Class3ToBeMarkedInternal>.Foo() => throw new Exception("123");
}

internal class InternalClass3;
internal interface IInterface3
{
    void Accept(InternalClass3 arg);
}
public class PublicClassWithInternalTypeInExplicitImpl : IInterface3
{
    void IInterface3.Accept(InternalClass3 arg) {}
}
