using RefasmerTestAssembly;

namespace RefasmerTestAssembly
{
    public enum EnumType
    {
        A = 0,
        B = 1,
        C = 4,
        D = 8
    }

    internal enum InternalEnumType
    {
        A = 0,
        B = 1,
        C = 4,
        D = 8
    }
}

namespace System.Diagnostics.CodeAnalysis
{
#pragma warning disable CS9113 // Parameter is unread.
    // This type is needed to pin the internal enum, so that it's always imported. This mimics relation between an enum
    // DynamicallyAccessedMemberTypes and DynamicallyAccessedMembersAttribute.
    internal class MySomethingAttribute(InternalEnumType value) : Attribute;
#pragma warning restore CS9113 // Parameter is unread.
}
