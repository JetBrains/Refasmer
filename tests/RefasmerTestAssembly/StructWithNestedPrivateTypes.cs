namespace RefasmerTestAssembly;

public struct StructWithNestedPrivateTypes
{
    private struct NestedPrivateStruct
    {
        private int Field;
    }
    
    private NestedPrivateStruct PrivateField;
}