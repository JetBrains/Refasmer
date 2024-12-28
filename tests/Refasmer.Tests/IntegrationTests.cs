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

    [Test]
    public async Task InternalTypeInPublicApi()
    {
        var assemblyPath = await BuildTestAssemblyWithInternalTypeInPublicApi();
        var resultAssembly = RefasmTestAssembly(assemblyPath, omitNonApiMembers: true);
        await VerifyTypeContents(
            resultAssembly,
            ["RefasmerTestAssembly.PublicClassWithInternalTypeInApi", "RefasmerTestAssembly.ClassToBeMarkedInternal"]);
    }
}
