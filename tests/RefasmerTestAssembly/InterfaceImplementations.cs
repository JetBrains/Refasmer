using System;

namespace RefasmerTestAssembly;

public interface IWithStaticMethods<TSelf> where TSelf : IWithStaticMethods<TSelf>
{
    static abstract bool operator ==(TSelf a, TSelf b);
    static abstract bool operator !=(TSelf a, TSelf b);
}
public class InterfaceImplementations : IWithStaticMethods<InterfaceImplementations>
{
    public override bool Equals(object? obj) => throw new NotSupportedException();

    public override int GetHashCode() => throw new NotSupportedException();

    public static bool operator ==(InterfaceImplementations a, InterfaceImplementations b) => throw new NotSupportedException();

    public static bool operator !=(InterfaceImplementations a, InterfaceImplementations b) => throw new NotSupportedException();
}
