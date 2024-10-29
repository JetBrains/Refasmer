using System;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters
{
    public class AllowAll : IImportFilter
    {
        public bool RequiresPreprocessing => false;
        public void PreprocessAssembly(MetadataReader assembly) =>
            throw new InvalidOperationException();

        public virtual bool AllowImport(TypeDefinition declaringType, MetadataReader reader,
            CachedAttributeChecker attributeChecker) => true;
        public virtual bool AllowImport( MethodDefinition method, MetadataReader reader ) => true;
        public virtual bool AllowImport( FieldDefinition field, MetadataReader reader ) => true;

        public bool ProcessValueTypeFields() => false;
    }
}