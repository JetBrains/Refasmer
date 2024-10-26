using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters
{
    public interface IImportFilter
    {
        bool RequiresPreprocessing { get; }
        void Preprocess();
        
        bool AllowImport( TypeDefinition type, MetadataReader reader );
        bool AllowImport( MethodDefinition method, MetadataReader reader );
        bool AllowImport( FieldDefinition field, MetadataReader reader );

        // TODO: others on demand
    }
}