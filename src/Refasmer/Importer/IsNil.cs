using System;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private static bool IsNil(StringHandle x) => x.IsNil;
        private static bool IsNil(GuidHandle x) => x.IsNil;
        private static bool IsNil(BlobHandle x) => x.IsNil;

        private static bool IsNil(AssemblyReferenceHandle x) => x.IsNil;
        private static bool IsNil(ModuleReferenceHandle x) => x.IsNil;
        private static bool IsNil(AssemblyFileHandle x) => x.IsNil;
        private static bool IsNil(TypeReferenceHandle x) => x.IsNil;
        private static bool IsNil(MemberReferenceHandle x) => x.IsNil;
        private static bool IsNil(TypeDefinitionHandle x) => x.IsNil;
        private static bool IsNil(TypeSpecificationHandle x) => x.IsNil;
        private static bool IsNil(FieldDefinitionHandle x) => x.IsNil;
        private static bool IsNil(MethodDefinitionHandle x) => x.IsNil;
        private static bool IsNil(MethodImplementationHandle x) => x.IsNil;
        private static bool IsNil(GenericParameterHandle x) => x.IsNil;
        private static bool IsNil(GenericParameterConstraintHandle x) => x.IsNil;
        private static bool IsNil(ParameterHandle x) => x.IsNil;
        private static bool IsNil(InterfaceImplementationHandle x) => x.IsNil;
        private static bool IsNil(EventDefinitionHandle x) => x.IsNil;
        private static bool IsNil(PropertyDefinitionHandle x) => x.IsNil;
        private static bool IsNil(ExportedTypeHandle x) => x.IsNil;
        private static bool IsNil(CustomAttributeHandle x) => x.IsNil;
        private static bool IsNil(DeclarativeSecurityAttributeHandle x) => x.IsNil;
        private static bool IsNil(ConstantHandle x) => x.IsNil;
        private static bool IsNil( EntityHandle x ) => x.IsNil;

    }
}