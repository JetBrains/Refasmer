using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters;

/// <summary>Base type for a filter that doesn't pass all types.</summary>
/// <param name="omitNonApiTypes">Whether the non-API types should be hidden when possible.</param>
public abstract class PartialTypeFilterBase(bool omitNonApiTypes) : IImportFilter
{
    public bool RequiresPreprocessing => omitNonApiTypes;

    public void PreprocessAssembly(MetadataReader assembly)
    {
    }

    public bool RequireImport(TypeDefinition type, MetadataReader reader)
    {
        return false;
    }

    public bool AllowImport(TypeDefinition type, MetadataReader reader, CachedAttributeChecker attributeChecker)
    {
        var isCompilerGenerated = attributeChecker.HasAttribute(reader, type, FullNames.CompilerGenerated);
        return !isCompilerGenerated;
    }
    
    public abstract bool AllowImport(MethodDefinition method, MetadataReader reader);
    public abstract bool AllowImport(FieldDefinition field, MetadataReader reader);

    public bool ProcessValueTypeFields()
    {
        return false;
    }
}