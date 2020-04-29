using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using JetBrains.Refasmer.Filters;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter : LoggerBase
    {
        private readonly MetadataReader _reader;
        private readonly MetadataBuilder _builder;

        private readonly IImportFilter _filter;
        
        public MetadataImporter( MetadataReader reader, MetadataBuilder builder, LoggerBase logger, IImportFilter filter = null) : base(logger)
        {
            _reader = reader;
            _builder = builder;
            _filter = filter;
        }


        public static byte[] MakeRefasm(MetadataReader metaReader, PEReader peReader, IImportFilter filter, LoggerBase logger )
        {
            var metaBuilder = new MetadataBuilder();

            var importer = new MetadataImporter(metaReader, metaBuilder, logger, filter);
            importer.Import();
            
            logger.Debug?.Invoke($"Building reference assembly");
            
            var metaRootBuilder = new MetadataRootBuilder(metaBuilder, metaReader.MetadataVersion, true);

            var peHeaderBuilder = new PEHeaderBuilder(
                peReader.PEHeaders.CoffHeader.Machine, 
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
            
            var ilStream = new BlobBuilder();
            var peBuilder = new ManagedPEBuilder(peHeaderBuilder, metaRootBuilder, ilStream);
            var blobBuilder = new BlobBuilder();
            peBuilder.Serialize(blobBuilder);

            return blobBuilder.ToArray();
        }

        public static void MakeRefasm( string inputPath, string outputPath, IImportFilter filter, LoggerBase logger )
        {
            logger.Trace?.Invoke("Reading assembly");
            var peReader = new PEReader(new FileStream(inputPath, FileMode.Open, FileAccess.Read)); 
            var metaReader = peReader.GetMetadataReader();

            if (!metaReader.IsAssembly)
                throw new Exception("File format is not supported"); 
            
            var result = MakeRefasm(metaReader, peReader, filter, logger);

            logger.Debug?.Invoke($"Writing result to {outputPath}");
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            File.WriteAllBytes(outputPath, result);
        }
     }
}