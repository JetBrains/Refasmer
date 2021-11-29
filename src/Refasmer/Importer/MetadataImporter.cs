using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using JetBrains.Refasmer.Filters;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter : LoggerBase
    {
        private readonly MetadataReader _reader;
        private readonly MetadataBuilder _builder;
        private readonly BlobBuilder _ilStream;

        public IImportFilter Filter;

        public bool MakeMock;
        public bool OmitReferenceAssemblyAttr;
        
        public MetadataImporter( MetadataReader reader, MetadataBuilder builder, BlobBuilder ilStream, LoggerBase logger) : base(logger)
        {
            _reader = reader;
            _builder = builder;
            _ilStream = ilStream;
        }


        public static byte[] MakeRefasm(MetadataReader metaReader, PEReader peReader, LoggerBase logger, IImportFilter filter = null, 
            bool makeMock = false, bool omitReferenceAssemblyAttr = false )
        {
            var metaBuilder = new MetadataBuilder();
            var ilStream = new BlobBuilder();
            ilStream.Align(4);

            var importer = new MetadataImporter(metaReader, metaBuilder, ilStream, logger)
            {
                MakeMock = makeMock,
                OmitReferenceAssemblyAttr = omitReferenceAssemblyAttr
            };

            if (filter != null)
            {
                importer.Filter = filter;
                logger.Info?.Invoke("Using custom entity filter");
            }
            else if (importer.IsInternalsVisible())
            {
                importer.Filter = new AllowPublicAndInternals();
                logger.Info?.Invoke("InternalsVisibleTo attributes found, using AllowPublicAndInternals entity filter");
            }
            else
            {
                importer.Filter = new AllowPublic();
                logger.Info?.Invoke("Using AllowPublic entity filter");
            }
            

            var mvidBlob = importer.Import();
            
            logger.Debug?.Invoke($"Building {(makeMock ? "mockup" : "reference")} assembly");
            
            var metaRootBuilder = new MetadataRootBuilder(metaBuilder, metaReader.MetadataVersion, true);
            
            var peHeaderBuilder = new PEHeaderBuilder(
                Machine.I386, // override machine to force AnyCPU assembly 
                peReader.PEHeaders.PEHeader.SectionAlignment,
                peReader.PEHeaders.PEHeader.FileAlignment,
                peReader.PEHeaders.PEHeader.ImageBase,
                peReader.PEHeaders.PEHeader.MajorLinkerVersion,
                peReader.PEHeaders.PEHeader.MinorLinkerVersion,
                peReader.PEHeaders.PEHeader.MajorOperatingSystemVersion,
                peReader.PEHeaders.PEHeader.MinorOperatingSystemVersion,
                peReader.PEHeaders.PEHeader.MajorImageVersion,
                peReader.PEHeaders.PEHeader.MinorImageVersion,
                peReader.PEHeaders.PEHeader.MajorSubsystemVersion,
                peReader.PEHeaders.PEHeader.MinorSubsystemVersion,
                peReader.PEHeaders.PEHeader.Subsystem,
                peReader.PEHeaders.PEHeader.DllCharacteristics,
                peReader.PEHeaders.CoffHeader.Characteristics,
                peReader.PEHeaders.PEHeader.SizeOfStackReserve,
                peReader.PEHeaders.PEHeader.SizeOfStackCommit,
                peReader.PEHeaders.PEHeader.SizeOfHeapReserve,
                peReader.PEHeaders.PEHeader.SizeOfHeapCommit
                );

            var peBuilder = new ManagedPEBuilder(peHeaderBuilder, metaRootBuilder, ilStream, 
                deterministicIdProvider: blobs =>
                {
                    var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256) ?? throw new Exception("Cannot create hasher");
                    
                    foreach (var segment in blobs.Select(b => b.GetBytes()))
                        hasher.AppendData(segment.Array, segment.Offset, segment.Count);
                    
                    return BlobContentId.FromHash(hasher.GetHashAndReset());
                });
            
            var blobBuilder = new BlobBuilder();
            var contentId = peBuilder.Serialize(blobBuilder);
            
            mvidBlob.CreateWriter().WriteGuid(contentId.Guid);

            return blobBuilder.ToArray();
        }

        public static void MakeRefasm( string inputPath, string outputPath, LoggerBase logger, IImportFilter filter = null, bool makeMock = false )
        {
            logger.Debug?.Invoke($"Reading assembly {inputPath}");
            var peReader = new PEReader(new FileStream(inputPath, FileMode.Open, FileAccess.Read)); 
            var metaReader = peReader.GetMetadataReader();

            if (!metaReader.IsAssembly)
                throw new Exception("File format is not supported"); 
            
            var result = MakeRefasm(metaReader, peReader, logger, filter, makeMock);

            logger.Debug?.Invoke($"Writing result to {outputPath}");
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            File.WriteAllBytes(outputPath, result);
        }
     }
}