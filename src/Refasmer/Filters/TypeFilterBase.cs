using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters;

public abstract class TypeFilterBase(bool omitNonApiTypes) : IImportFilter
{
    public bool RequiresPreprocessing => omitNonApiTypes;

    public void PreprocessAssembly(MetadataReader assembly)
    {
    }

    public bool RequireImport(TypeDefinition type, MetadataReader reader)
    {
        return false;
    }

    public abstract bool AllowImport(TypeDefinition type, MetadataReader reader);
    public abstract bool AllowImport(MethodDefinition method, MetadataReader reader);
    public abstract bool AllowImport(FieldDefinition field, MetadataReader reader);

    public bool ProcessValueTypeFields()
    {
        return false;
    }
}