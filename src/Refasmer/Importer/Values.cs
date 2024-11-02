using System.Reflection.Metadata;

namespace JetBrains.Refasmer;

public partial class MetadataImporter
{
    private StringHandle ImportValue( StringHandle src ) => _builder.GetOrAddString(_reader.GetString(src));
    private GuidHandle ImportValue( GuidHandle src ) => _builder.GetOrAddGuid(_reader.GetGuid(src));
    private BlobHandle ImportValue( BlobHandle src ) => _builder.GetOrAddBlob(_reader.GetBlobContent(src));

        
}