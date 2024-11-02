using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters;

public class AllowAll : IImportFilter
{
    public bool OmitNonApiMembers => false;

    public virtual bool AllowImport(TypeDefinition declaringType, MetadataReader reader) => true;
    public virtual bool AllowImport( MethodDefinition method, MetadataReader reader ) => true;
    public virtual bool AllowImport( FieldDefinition field, MetadataReader reader ) => true;
}
