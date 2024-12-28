namespace RefasmerTestAssembly;

public class PublicClassWithInternalTypeInApi
{
    public void Accept(ClassToBeMarkedInternal argument) {}
}

public class ClassToBeMarkedInternal; // post-processed by tests
