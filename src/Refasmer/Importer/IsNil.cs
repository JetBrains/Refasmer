using System;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private bool IsNil(StringHandle x) => x.IsNil;
        private bool IsNil(GuidHandle x) => x.IsNil;
        private bool IsNil(BlobHandle x) => x.IsNil;

        private bool IsNil(AssemblyReferenceHandle x) => x.IsNil;
        private bool IsNil(ModuleReferenceHandle x) => x.IsNil;
        private bool IsNil(AssemblyFileHandle x) => x.IsNil;
        private bool IsNil(TypeReferenceHandle x) => x.IsNil;
        private bool IsNil(MemberReferenceHandle x) => x.IsNil;
        private bool IsNil(TypeDefinitionHandle x) => x.IsNil;
        private bool IsNil(TypeSpecificationHandle x) => x.IsNil;
        private bool IsNil(FieldDefinitionHandle x) => x.IsNil;
        private bool IsNil(MethodDefinitionHandle x) => x.IsNil;
        private bool IsNil(MethodImplementationHandle x) => x.IsNil;
        private bool IsNil(GenericParameterHandle x) => x.IsNil;
        private bool IsNil(GenericParameterConstraintHandle x) => x.IsNil;
        private bool IsNil(ParameterHandle x) => x.IsNil;
        private bool IsNil(InterfaceImplementationHandle x) => x.IsNil;
        private bool IsNil(EventDefinitionHandle x) => x.IsNil;
        private bool IsNil(PropertyDefinitionHandle x) => x.IsNil;
        private bool IsNil(ExportedTypeHandle x) => x.IsNil;
        private bool IsNil(CustomAttributeHandle x) => x.IsNil;
        private bool IsNil(DeclarativeSecurityAttributeHandle x) => x.IsNil;
        private bool IsNil(ConstantHandle x) => x.IsNil;
        private bool IsNil( EntityHandle x ) => x.IsNil;

    }
}