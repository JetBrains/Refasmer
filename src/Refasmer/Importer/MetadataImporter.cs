using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using JetBrains.Refasmer.Filters;

namespace JetBrains.Refasmer;

public partial class MetadataImporter : LoggerBase
{
    private readonly MetadataReader _reader;
    private readonly MetadataBuilder _builder;
    private readonly BlobBuilder _ilStream;

    public IImportFilter? Filter;

    public bool MakeMock;
    public bool OmitReferenceAssemblyAttr;
        
    public MetadataImporter( MetadataReader reader, MetadataBuilder builder, BlobBuilder ilStream, LoggerBase logger) : base(logger)
    {
        _reader = reader;
        _builder = builder;
        _ilStream = ilStream;
    }

    /// <summary>Produces a reference assembly for the passed input.</summary>
    /// <param name="metaReader">Input assembly's metadata reader.</param>
    /// <param name="peReader">Input file's PE structure reader.</param>
    /// <param name="logger">Logger to write the process information to.</param>
    /// <param name="filter">
    /// Filter to apply to the assembly members. If <c>null</c> then will be auto-detected from the assembly
    /// contents: for an assembly that has a <see cref="InternalsVisibleToAttribute"/> applied to it, use
    /// <see cref="AllowPublicAndInternals"/>, otherwise use <see cref="AllowPublic"/>.
    /// </param>
    /// <param name="omitNonApiMembers">
    ///     <para>Omit private members and types not participating in the public API (will preserve the empty vs
    ///     non-empty struct semantics, but might affect the <c>unmanaged</c> struct constraint).</para>
    ///
    ///     <para>Mandatory if the <paramref name="filter"/> is not passed. Ignored otherwise.</para>
    /// </param>
    /// <param name="makeMock">
    /// Whether to make a mock assembly instead of a reference assembly. A mock assembly throws
    /// <see cref="NotImplementedException"/> in each imported method, while a reference assembly follows the
    /// reference assembly specification.
    /// </param>
    /// <param name="omitReferenceAssemblyAttr">
    /// Whether to omit the reference assembly attribute in the generated assembly.
    /// </param>
    /// <returns>Bytes of the generated assembly.</returns>
    public static byte[] MakeRefasm(
        MetadataReader metaReader,
        PEReader peReader,
        LoggerBase logger,
        IImportFilter? filter,
        bool? omitNonApiMembers,
        bool makeMock = false,
        bool omitReferenceAssemblyAttr = false)
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
            importer.Filter = new AllowPublicAndInternals(
                omitNonApiMembers ?? throw new Exception(
                    $"{nameof(omitNonApiMembers)} should be specified for the current filter mode."));
            logger.Info?.Invoke("InternalsVisibleTo attributes found, using AllowPublicAndInternals entity filter");
        }
        else
        {
            importer.Filter = new AllowPublic(
                omitNonApiMembers ?? throw new Exception(
                    $"{nameof(omitNonApiMembers)} should be specified for the current filter mode."));
            logger.Info?.Invoke("Using AllowPublic entity filter");
        }
            

        var mvidBlob = importer.Import();
            
        logger.Debug?.Invoke($"Building {(makeMock ? "mockup" : "reference")} assembly");
            
        var metaRootBuilder = new MetadataRootBuilder(metaBuilder, metaReader.MetadataVersion, true);
            
        var peHeaderBuilder = new PEHeaderBuilder(
            Machine.I386, // override machine to force AnyCPU assembly 
            peReader.PEHeaders.PEHeader!.SectionAlignment,
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
                    hasher.AppendData(segment.Array!, segment.Offset, segment.Count);
                    
                return BlobContentId.FromHash(hasher.GetHashAndReset());
            });
            
        var blobBuilder = new BlobBuilder();
        var contentId = peBuilder.Serialize(blobBuilder);
            
        mvidBlob.CreateWriter().WriteGuid(contentId.Guid);

        return blobBuilder.ToArray();
    }

    /// <summary>Produces a reference assembly for the passed input.</summary>
    /// <param name="inputPath">Path to the input assembly.</param>
    /// <param name="outputPath">Path to the output assembly.</param>
    /// <param name="logger">Logger to write the process information to.</param>
    /// <param name="omitNonApiMembers">
    ///     <para>Omit private members and types not participating in the public API (will preserve the empty vs
    ///     non-empty struct semantics, but might affect the <c>unmanaged</c> struct constraint).</para>
    ///
    ///     <para>Mandatory if the <paramref name="filter"/> is not passed. Ignored otherwise.</para>
    /// </param>
    /// <param name="filter">
    /// Filter to apply to the assembly members. If <c>null</c> then will be auto-detected from the assembly
    /// contents: for an assembly that has a <see cref="InternalsVisibleToAttribute"/> applied to it, use
    /// <see cref="AllowPublicAndInternals"/>, otherwise use <see cref="AllowPublic"/>.
    /// </param>
    /// <param name="makeMock">
    /// Whether to make a mock assembly instead of a reference assembly. A mock assembly throws
    /// <see cref="NotImplementedException"/> in each imported method, while a reference assembly follows the
    /// reference assembly specification.
    /// </param>
    /// <returns>Bytes of the generated assembly.</returns>
    public static void MakeRefasm(
        string inputPath,
        string outputPath,
        LoggerBase logger,
        bool? omitNonApiMembers,
        IImportFilter? filter = null,
        bool makeMock = false)
    {
        logger.Debug?.Invoke($"Reading assembly {inputPath}");
        var peReader = new PEReader(new FileStream(inputPath, FileMode.Open, FileAccess.Read)); 
        var metaReader = peReader.GetMetadataReader();

        if (!metaReader.IsAssembly)
            throw new Exception("File format is not supported"); 
            
        var result = MakeRefasm(metaReader, peReader, logger, filter, omitNonApiMembers, makeMock);

        logger.Debug?.Invoke($"Writing result to {outputPath}");
        if (File.Exists(outputPath))
            File.Delete(outputPath);

        File.WriteAllBytes(outputPath, result);
    }
}