using System;

namespace RefasmerTestAssembly;

// Post-processed and converted to internal by tests:
public class Class1ToBeMarkedInternal;
public class Class2ToBeMarkedInternal;
public class Class3ToBeMarkedInternal;
public interface IInterface1ToBeMarkedInternal;
public interface IInterface2ToBeMarkedInternal<T>
{
    T Foo();
}

public class PublicClassWithInternalTypeInApi
{
    public void Accept(Class1ToBeMarkedInternal argument) {}
}

public class PublicClassDerivingFromInternal : Class2ToBeMarkedInternal;
public class PublicClassImplementingInternal : IInterface1ToBeMarkedInternal;

public class PublicClassWithInternalInterfaceImpl : IInterface2ToBeMarkedInternal<Class3ToBeMarkedInternal>
{
    Class3ToBeMarkedInternal IInterface2ToBeMarkedInternal<Class3ToBeMarkedInternal>.Foo() => throw new Exception("123");
}