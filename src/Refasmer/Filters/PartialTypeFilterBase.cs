using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters;

/// <summary>Base type for a filter that doesn't pass all types.</summary>
/// <param name="omitNonApiMembers">Whether the non-API types should be hidden when possible.</param>
public abstract class PartialTypeFilterBase(bool omitNonApiMembers) : IImportFilter
{
    public bool OmitNonApiMembers => omitNonApiMembers;
    
    protected readonly CachedAttributeChecker AttributeCache = new();
    
    public virtual bool AllowImport(TypeDefinition type, MetadataReader reader)
    {
        if (type.Attributes.HasFlag(TypeAttributes.Public)) return true;
        var isCompilerGenerated = AttributeCache.HasAttribute(reader, type, FullNames.CompilerGenerated);
        return !isCompilerGenerated;
    }
    
    public abstract bool AllowImport(MethodDefinition method, MetadataReader reader);
    public abstract bool AllowImport(FieldDefinition field, MetadataReader reader);
}
