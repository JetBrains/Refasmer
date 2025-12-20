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
    [TestCase("RefasmerTestAssembly.InternalEnumType")]
    [TestCase("RefasmerTestAssembly.EnumType")]
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
    [TestCase("RefasmerTestAssembly.CustomEnumerable")]
    [TestCase("RefasmerTestAssembly.EnumType")]
    [TestCase("RefasmerTestAssembly.InternalEnumType")]
    [TestCase("RefasmerTestAssembly.InternalInterfaceImplBug")]
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
    [TestCase("PublicClassImplementingInternal", "IInterface1ToBeMarkedInternal")]
    [TestCase("PublicClassWithInternalInterfaceImpl", "Class3ToBeMarkedInternal,IInterface2ToBeMarkedInternal`1")]
    [TestCase("PublicClassWithInternalTypeInExplicitImpl", "IInterface3")]
    public async Task InternalTypeInPublicApi(string mainClassName, string auxiliaryClassNames)
    {
        var assemblyPath = await BuildTestAssemblyWithInternalTypeInPublicApi();
        var resultAssembly = RefasmTestAssembly(assemblyPath, omitNonApiMembers: true);

        var fullMainClassName = $"RefasmerTestAssembly.{mainClassName}";
        var fullAuxiliaryClassNames = auxiliaryClassNames.Split(',').Select(x => $"RefasmerTestAssembly.{x}");

        await VerifyTypeContents(
            resultAssembly,
            [fullMainClassName, ..fullAuxiliaryClassNames],
            assertTypeExists: false,
            parameters: [mainClassName]);
    }

    [Test]
    public async Task InterfaceImplementations()
    {
        var assemblyPath = await BuildTestAssemblyWithInternalTypeInPublicApi();
        var resultAssembly = RefasmTestAssembly(assemblyPath, omitNonApiMembers: true);

        await VerifyTypeContents(
            resultAssembly,
            ["RefasmerTestAssembly.InterfaceImplementations", "RefasmerTestAssembly.IWithStaticMethods`1"]);
    }
}
