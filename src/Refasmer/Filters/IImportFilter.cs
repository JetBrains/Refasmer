using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters
{
    public interface IImportFilter
    {
        /// <summary>
        /// Whether the <see cref="PreprocessAssembly"/> should be called before actual filtering happens.
        /// </summary>
        bool RequiresPreprocessing { get; }
        void PreprocessAssembly(MetadataReader assembly);
        
        bool AllowImport(TypeDefinition type, MetadataReader reader, CachedAttributeChecker attributeChecker);
        bool AllowImport( MethodDefinition method, MetadataReader reader );
        bool AllowImport( FieldDefinition field, MetadataReader reader );

        /// <summary>Allows this filter to completely substitute the processing of a value type field block.</summary>
        /// <returns>
        /// Whether the replacement happened. If <c>true</c>, then the fields won't be processed outside.
        /// </returns>
        bool ProcessValueTypeFields();
    }
}