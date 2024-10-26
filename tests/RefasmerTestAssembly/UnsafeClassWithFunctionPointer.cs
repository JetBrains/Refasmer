namespace RefasmerTestAssembly;

public unsafe class UnsafeClassWithFunctionPointer
{
    public void MethodWithFunctionPointer(delegate*<void> functionPointer) { }
}