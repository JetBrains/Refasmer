using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace JetBrains.Refasmer
{
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

        private BlobHandle ImportTypeSignature( BlobHandle srcHandle )
        {
            try
            {
                var blobReader = _reader.GetBlobReader(srcHandle);
                var blobBuilder = new BlobBuilder(blobReader.Length);

                ImportTypeSignature(ref blobReader, blobBuilder);
                return _builder.GetOrAddBlob(blobBuilder);
            }
            catch (UnknownTypeInSignature e)
            {
                if (e.Handle.Kind != HandleKind.TypeDefinition)
                    throw;

                var typeDef = _reader.GetTypeDefinition((TypeDefinitionHandle) e.Handle);

                if (Filter?.AllowImport(typeDef, _reader) == true)
                    throw;

            }
            return default;
        }

        private BlobHandle ImportSignatureWithHeader( BlobHandle srcHandle )
        {
            try
            {
                var blobReader = _reader.GetBlobReader(srcHandle);
                var blobBuilder = new BlobBuilder(blobReader.Length);
                
                var header = blobReader.ReadSignatureHeader();
                blobBuilder.WriteByte(header.RawValue);

                switch (header.Kind)
                {
                    case SignatureKind.Method:
                    case SignatureKind.Property:
                        ImportMethodSignature(header, ref blobReader, blobBuilder);
                        break;
                    case SignatureKind.Field:
                        ImportFieldSignature(header, ref blobReader, blobBuilder);
                        break;
                    case SignatureKind.LocalVariables:
                        ImportLocalSignature(header, ref blobReader, blobBuilder);
                        break;
                    case SignatureKind.MethodSpecification:
                        ImportMethodSpecSignature(header, ref blobReader, blobBuilder);
                        break;
                    default:
                        throw new BadImageFormatException();
                }
                return _builder.GetOrAddBlob(blobBuilder);
            }
            catch (UnknownTypeInSignature e)
            {
                if (e.Handle.Kind != HandleKind.TypeDefinition)
                    throw;

                var typeDef = _reader.GetTypeDefinition((TypeDefinitionHandle) e.Handle);

                if (Filter?.AllowImport(typeDef, _reader) == true)
                    throw;

            }
            return default;

        }

        private void ImportMethodSignature( SignatureHeader header, ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            if (header.IsGeneric)
            {
                var genericParameterCount = blobReader.ReadCompressedInteger();
                blobBuilder.WriteCompressedInteger(genericParameterCount);
            }

            var parameterCount = blobReader.ReadCompressedInteger();
            blobBuilder.WriteCompressedInteger(parameterCount);

            // Return type
            ImportTypeSignature(ref blobReader, blobBuilder);

            for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
                ImportTypeSignature(ref blobReader, blobBuilder);
        }

        private void ImportFieldSignature( SignatureHeader header, ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            ImportTypeSignature(ref blobReader, blobBuilder);
        }

        private void ImportLocalSignature( SignatureHeader header, ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            ImportTypeSequenceSignature(ref blobReader, blobBuilder);
        }

        private void ImportMethodSpecSignature( SignatureHeader header, ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            ImportTypeSequenceSignature(ref blobReader, blobBuilder);
        }

        private void ImportTypeSignature( ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            var typeCode = blobReader.ReadCompressedInteger();
            blobBuilder.WriteCompressedInteger(typeCode);
            
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
                    ImportTypeSignature(ref blobReader, blobBuilder);
                    break;

                case (int)SignatureTypeCode.FunctionPointer:
                    var header = blobReader.ReadSignatureHeader();
                    ImportMethodSignature(header, ref blobReader, blobBuilder);
                    break;

                case (int)SignatureTypeCode.Array:
                    ImportArrayTypeSignature(ref blobReader, blobBuilder);
                    break;

                case (int)SignatureTypeCode.RequiredModifier:
                case (int)SignatureTypeCode.OptionalModifier:
                    ImportModifiedTypeSignature(ref blobReader, blobBuilder);
                    break;

                case (int)SignatureTypeCode.GenericTypeInstance:
                    ImportGenericTypeInstanceSignature(ref blobReader, blobBuilder);
                    break;

                case (int)SignatureTypeCode.GenericTypeParameter:
                case (int)SignatureTypeCode.GenericMethodParameter:
                    var index = blobReader.ReadCompressedInteger();
                    blobBuilder.WriteCompressedInteger(index);
                    break;

                case (int)SignatureTypeKind.Class:
                case (int)SignatureTypeKind.ValueType:
                    ImportTypeHandleSignature(ref blobReader, blobBuilder);
                    break;

                default:
                    throw new BadImageFormatException();
            }
        }

        private void ImportArrayTypeSignature( ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            // Element type
            ImportTypeSignature(ref blobReader, blobBuilder);
            
            var rank = blobReader.ReadCompressedInteger();
            blobBuilder.WriteCompressedInteger(rank);
            
            var sizesCount = blobReader.ReadCompressedInteger();
            blobBuilder.WriteCompressedInteger(sizesCount);
            
            for (var i = 0; i < sizesCount; i++)
                blobBuilder.WriteCompressedInteger(blobReader.ReadCompressedInteger());

            var lowerBoundsCount = blobReader.ReadCompressedInteger();
            blobBuilder.WriteCompressedInteger(lowerBoundsCount);
            
            for (var i = 0; i < lowerBoundsCount; i++)
                blobBuilder.WriteCompressedSignedInteger(blobReader.ReadCompressedSignedInteger());
        }
        
        private void ImportModifiedTypeSignature( ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            ImportTypeHandleSignature(ref blobReader, blobBuilder);
            ImportTypeSignature(ref blobReader, blobBuilder);
        }

        private void ImportTypeHandleSignature( ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            var srcHandle = blobReader.ReadTypeHandle();
            var dstHandle = Import(srcHandle);
            if (dstHandle.IsNil)
                throw new UnknownTypeInSignature(srcHandle, $"Unknown type in signature: {ToString(srcHandle)}"); 
            
            blobBuilder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(dstHandle));
        }

        private void ImportGenericTypeInstanceSignature( ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            ImportTypeSignature(ref blobReader, blobBuilder);
            ImportTypeSequenceSignature(ref blobReader, blobBuilder);
        }

        private void ImportTypeSequenceSignature( ref BlobReader blobReader, BlobBuilder blobBuilder )
        {
            var count = blobReader.ReadCompressedInteger();
            blobBuilder.WriteCompressedInteger(count);

            for (var i = 0; i < count; i++)
                ImportTypeSignature(ref blobReader, blobBuilder);
        }
    }
}