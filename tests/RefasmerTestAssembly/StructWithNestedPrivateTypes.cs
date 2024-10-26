namespace RefasmerTestAssembly;

public struct StructWithNestedPrivateTypes
{
    private struct NestedPrivateStruct
    {
        private int Field;
    }
    
    private NestedPrivateStruct PrivateField;
    
    private struct UnusedPrivateStruct
    {
        private int Field;
    }

    public struct UnusedPublicStruct
    {
        private int Field;
    }
}