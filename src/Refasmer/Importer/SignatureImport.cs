using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using JetBrains.Refasmer.Importer;

namespace JetBrains.Refasmer;

public partial class MetadataImporter
{
    private class UnknownTypeInSignature : Exception
    {
        public readonly EntityHandle Handle;

        public UnknownTypeInSignature( EntityHandle handle, string message )
            : base(message)
        {
            Handle = handle;
        }
    }

    private class ImportingVisitor(
        MetadataReader metadataReader,
        MetadataImporter metadataImporter) : ISignatureVisitor<BlobHandle>
    {
        private BlobBuilder? _blobBuilder;
        private BlobBuilder BlobBuilder =>
            _blobBuilder ?? throw new InvalidOperationException("Blob builder is not defined yet.");
        public void VisitReader(BlobReader reader)
        {
            _blobBuilder = new BlobBuilder(reader.Length);
        }

        public void WriteByte(byte @byte) => BlobBuilder.WriteByte(@byte);
        public void WriteCompressedInteger(int integer) => BlobBuilder.WriteCompressedInteger(integer);
        public void WriteCompressedSignedInteger(int integer) => BlobBuilder.WriteCompressedSignedInteger(integer);

        public void VisitTypeHandle(EntityHandle srcHandle)
        {
            var dstHandle = metadataImporter.Import(srcHandle);
            if (dstHandle.IsNil)
                throw new UnknownTypeInSignature(srcHandle, $"Unknown type in signature: {metadataReader.ToString(srcHandle)}");

            WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(dstHandle));
        }

        public BlobHandle GetResult() => metadataImporter._builder.GetOrAddBlob(BlobBuilder);
    }

    private T AcceptTypeSignature<T>(BlobHandle srcHandle, ISignatureVisitor<T> visitor)
    {
        var blobReader = _reader.GetBlobReader(srcHandle);
        visitor.VisitReader(blobReader);

        AcceptTypeSignature(ref blobReader, visitor);
        return visitor.GetResult();
    }

    private BlobHandle ImportTypeSignature(BlobHandle signature)
    {
        var visitor = new ImportingVisitor(_reader, this);
        return AcceptTypeSignature(signature, visitor);
    }

    private void AcceptFieldSignature<T>(FieldDefinition field, ISignatureVisitor<T> visitor)
    {
        AcceptSignatureWithHeader(field.Signature, visitor);
    }

    private void AcceptMethodSignature<T>(MethodDefinition method, ISignatureVisitor<T> visitor)
    {
        AcceptSignatureWithHeader(method.Signature, visitor);
    }

    private T AcceptSignatureWithHeader<T>(BlobHandle srcHandle, ISignatureVisitor<T> visitor)
    {
        var blobReader = _reader.GetBlobReader(srcHandle);
        visitor.VisitReader(blobReader);
            
        var header = blobReader.ReadSignatureHeader();
        visitor.WriteByte(header.RawValue);

        switch (header.Kind)
        {
            case SignatureKind.Method:
            case SignatureKind.Property:
                AcceptMethodSignature(header, ref blobReader, visitor);
                break;
            case SignatureKind.Field:
                AcceptFieldSignature(ref blobReader, visitor);
                break;
            case SignatureKind.LocalVariables:
                AcceptLocalSignature(ref blobReader, visitor);
                break;
            case SignatureKind.MethodSpecification:
                AcceptMethodSpecSignature(ref blobReader, visitor);
                break;
            default:
                throw new BadImageFormatException();
        }
        return visitor.GetResult();
    }

    private BlobHandle ImportSignatureWithHeader(BlobHandle signature)
    {
        var visitor = new ImportingVisitor(_reader, this);
        return AcceptSignatureWithHeader(signature, visitor);
    }

    private void AcceptMethodSignature<T>(
        SignatureHeader header,
        ref BlobReader blobReader,
        ISignatureVisitor<T> visitor)
    {
        if (header.IsGeneric)
        {
            var genericParameterCount = blobReader.ReadCompressedInteger();
            visitor.WriteCompressedInteger(genericParameterCount);
        }

        var parameterCount = blobReader.ReadCompressedInteger();
        visitor.WriteCompressedInteger(parameterCount);

        // Return type
        AcceptTypeSignature(ref blobReader, visitor);

        for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            AcceptTypeSignature(ref blobReader, visitor);
    }

    private void AcceptFieldSignature<T>(ref BlobReader blobReader, ISignatureVisitor<T> visitor)
    {
        AcceptTypeSignature(ref blobReader, visitor);
    }

    private void AcceptLocalSignature<T>(ref BlobReader blobReader, ISignatureVisitor<T> visitor)
    {
        AcceptTypeSequenceSignature(ref blobReader, visitor);
    }

    private void AcceptMethodSpecSignature<T>(ref BlobReader blobReader, ISignatureVisitor<T> visitor)
    {
        AcceptTypeSequenceSignature(ref blobReader, visitor);
    }

    private void AcceptTypeSignature<T>(ref BlobReader blobReader, ISignatureVisitor<T> visitor)
    {
        var typeCode = blobReader.ReadCompressedInteger();
        visitor.WriteCompressedInteger(typeCode);
            
        switch (typeCode)
        {
            case (int)SignatureTypeCode.Boolean:
            case (int)SignatureTypeCode.Char:
            case (int)SignatureTypeCode.SByte:
            case (int)SignatureTypeCode.Byte:
            case (int)SignatureTypeCode.Int16:
            case (int)SignatureTypeCode.UInt16:
            case (int)SignatureTypeCode.Int32:
            case (int)SignatureTypeCode.UInt32:
            case (int)SignatureTypeCode.Int64:
            case (int)SignatureTypeCode.UInt64:
            case (int)SignatureTypeCode.Single:
            case (int)SignatureTypeCode.Double:
            case (int)SignatureTypeCode.IntPtr:
            case (int)SignatureTypeCode.UIntPtr:
            case (int)SignatureTypeCode.Object:
            case (int)SignatureTypeCode.String:
            case (int)SignatureTypeCode.Void:
            case (int)SignatureTypeCode.TypedReference:

            case (int)SignatureTypeCode.Sentinel:
                break;

            case (int)SignatureTypeCode.Pointer:
            case (int)SignatureTypeCode.ByReference:
            case (int)SignatureTypeCode.Pinned:
            case (int)SignatureTypeCode.SZArray:
                AcceptTypeSignature(ref blobReader, visitor);
                break;

            case (int)SignatureTypeCode.FunctionPointer:
                var header = blobReader.ReadSignatureHeader();
                visitor.WriteByte(header.RawValue);
                AcceptMethodSignature(header, ref blobReader, visitor);
                break;

            case (int)SignatureTypeCode.Array:
                AcceptArrayTypeSignature(ref blobReader, visitor);
                break;

            case (int)SignatureTypeCode.RequiredModifier:
            case (int)SignatureTypeCode.OptionalModifier:
                AcceptModifiedTypeSignature(ref blobReader, visitor);
                break;

            case (int)SignatureTypeCode.GenericTypeInstance:
                AcceptGenericTypeInstanceSignature(ref blobReader, visitor);
                break;

            case (int)SignatureTypeCode.GenericTypeParameter:
            case (int)SignatureTypeCode.GenericMethodParameter:
                var index = blobReader.ReadCompressedInteger();
                visitor.WriteCompressedInteger(index);
                break;

            case (int)SignatureTypeKind.Class:
            case (int)SignatureTypeKind.ValueType:
                AcceptTypeHandleSignature(ref blobReader, visitor);
                break;

            default:
                throw new BadImageFormatException();
        }
    }

    private void AcceptArrayTypeSignature<T>(ref BlobReader blobReader, ISignatureVisitor<T> visitor)
    {
        // Element type
        AcceptTypeSignature(ref blobReader, visitor);
            
        var rank = blobReader.ReadCompressedInteger();
        visitor.WriteCompressedInteger(rank);
            
        var sizesCount = blobReader.ReadCompressedInteger();
        visitor.WriteCompressedInteger(sizesCount);
            
        for (var i = 0; i < sizesCount; i++)
            visitor.WriteCompressedInteger(blobReader.ReadCompressedInteger());

        var lowerBoundsCount = blobReader.ReadCompressedInteger();
        visitor.WriteCompressedInteger(lowerBoundsCount);
            
        for (var i = 0; i < lowerBoundsCount; i++)
            visitor.WriteCompressedSignedInteger(blobReader.ReadCompressedSignedInteger());
    }
        
    private void AcceptModifiedTypeSignature<T>(ref BlobReader blobReader, ISignatureVisitor<T> visitor)
    {
        AcceptTypeHandleSignature(ref blobReader, visitor);
        AcceptTypeSignature(ref blobReader, visitor);
    }

    private void AcceptTypeHandleSignature<T>(ref BlobReader blobReader, ISignatureVisitor<T> visitor)
    {
        var srcHandle = blobReader.ReadTypeHandle();
        visitor.VisitTypeHandle(srcHandle);
    }

    private void AcceptGenericTypeInstanceSignature<T>(ref BlobReader blobReader, ISignatureVisitor<T> visitor)
    {
        AcceptTypeSignature(ref blobReader, visitor);
        AcceptTypeSequenceSignature(ref blobReader, visitor);
    }

    private void AcceptTypeSequenceSignature<T>(ref BlobReader blobReader, ISignatureVisitor<T> visitor)
    {
        var count = blobReader.ReadCompressedInteger();
        visitor.WriteCompressedInteger(count);

        for (var i = 0; i < count; i++)
            AcceptTypeSignature(ref blobReader, visitor);
    }
}