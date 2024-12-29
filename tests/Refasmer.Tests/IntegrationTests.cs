namespace JetBrains.Refasmer.Tests;

public class IntegrationTests : IntegrationTestBase
{
    [TestCase("RefasmerTestAssembly.PublicClassWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.PublicStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.UnsafeClassWithFunctionPointer")]
    [TestCase("RefasmerTestAssembly.StructWithNestedPrivateTypes")]
    [TestCase("RefasmerTestAssembly.BlittableGraph")]
    [TestCase("RefasmerTestAssembly.BlittableStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.NonBlittableStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.NonBlittableGraph")]
    public async Task CheckRefasmedType(string typeName)
    {
        var assemblyPath = await BuildTestAssembly();
        var resultAssembly = RefasmTestAssembly(assemblyPath);
        await VerifyTypeContent(resultAssembly, typeName);
    }
    
    [TestCase("RefasmerTestAssembly.PublicClassWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.PublicStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.UnsafeClassWithFunctionPointer")]
    [TestCase("RefasmerTestAssembly.StructWithNestedPrivateTypes")]
    [TestCase("RefasmerTestAssembly.BlittableGraph")]
    [TestCase("RefasmerTestAssembly.BlittableStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.NonBlittableStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.NonBlittableGraph")]
    [TestCase("RefasmerTestAssembly.EmptyStructWithStaticMember")]
    [TestCase("RefasmerTestAssembly.NonEmptyStructWithStaticMember")]
    public async Task CheckRefasmedTypeOmitNonApi(string typeName)
    {
        var assemblyPath = await BuildTestAssembly();
        var resultAssembly = RefasmTestAssembly(assemblyPath, omitNonApiMembers: true);
        await VerifyTypeContent(resultAssembly, typeName);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CheckCompilerGeneratedClasses(bool omitNonApi)
    {
        var assemblyPath = await BuildTestAssemblyWithInternalTypeInPublicApi();
        var resultAssembly = RefasmTestAssembly(assemblyPath, omitNonApiMembers: omitNonApi);
        await VerifyTypeContents(
            resultAssembly,
            ["RefasmerTestAssembly.CompilerGeneratedPublicClass", "RefasmerTestAssembly.CompilerGeneratedInternalClass"],
            assertTypeExists: false,
            parameters: [omitNonApi]);
    }

    [TestCase("PublicClassWithInternalTypeInApi", "Class1ToBeMarkedInternal")]
    [TestCase("PublicClassDerivingFromInternal", "Class2ToBeMarkedInternal")]
    [TestCase("PublicClassImplementingInternal", "InterfaceToBeMarkedInternal")]
    public async Task InternalTypeInPublicApi(string mainClassName, string auxiliaryClassName)
    {
        var assemblyPath = await BuildTestAssemblyWithInternalTypeInPublicApi();
        var resultAssembly = RefasmTestAssembly(assemblyPath, omitNonApiMembers: true);

        var fullMainClassName = $"RefasmerTestAssembly.{mainClassName}";
        var fullAuxiliaryClassName = $"RefasmerTestAssembly.{auxiliaryClassName}";

        await VerifyTypeContents(
            resultAssembly,
            [fullMainClassName, fullAuxiliaryClassName],
            parameters: [mainClassName]);
    }
}
