namespace RefasmerTestAssembly;

// Post-processed and converted to internal by tests:
public class Class1ToBeMarkedInternal;
public class Class2ToBeMarkedInternal;
public class InterfaceToBeMarkedInternal;

public class PublicClassWithInternalTypeInApi
{
    public void Accept(Class1ToBeMarkedInternal argument) {}
}

public class PublicClassDerivingFromInternal : Class2ToBeMarkedInternal;
public class PublicClassImplementingInternal : InterfaceToBeMarkedInternal;
