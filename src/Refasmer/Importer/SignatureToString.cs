using System;
using System.Reflection.Metadata;
using System.Text;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private string TypeSignatureToString( BlobHandle srcHandle )
        {
            var blobReader = _reader.GetBlobReader(srcHandle);
            var stringBuilder = new StringBuilder();

            TypeSignatureToString(ref blobReader, stringBuilder);
            return stringBuilder.ToString();
        }

        private string SignatureWithHeaderToString( BlobHandle srcHandle )
        {
            var blobReader = _reader.GetBlobReader(srcHandle);
            var stringBuilder = new StringBuilder();
    
            var header = blobReader.ReadSignatureHeader();

            stringBuilder.Append($"{header.Kind} ");
            switch (header.Kind)
            {
                case SignatureKind.Method:
                case SignatureKind.Property:
                    MethodSignatureToString(header, ref blobReader, stringBuilder);
                    break;
                case SignatureKind.Field:
                    FieldSignatureToString(header, ref blobReader, stringBuilder);
                    break;
                case SignatureKind.LocalVariables:
                    LocalSignatureToString(header, ref blobReader, stringBuilder);
                    break;
                case SignatureKind.MethodSpecification:
                    MethodSpecSignatureToString(header, ref blobReader, stringBuilder);
                    break;
                default:
                    throw new BadImageFormatException();
            }

            return stringBuilder.ToString();
        }

        private void MethodSignatureToString( SignatureHeader header, ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            if (header.IsGeneric)
            {
                var genericParameterCount = blobReader.ReadCompressedInteger();
            }

            var parameterCount = blobReader.ReadCompressedInteger();

            // Return type
            TypeSignatureToString(ref blobReader, stringBuilder);

            stringBuilder.Append("( ");
            
            for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
                TypeSignatureToString(ref blobReader, stringBuilder);

            stringBuilder.Append(" )");
        }

        private void FieldSignatureToString( SignatureHeader header, ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            TypeSignatureToString(ref blobReader, stringBuilder);
        }

        private void LocalSignatureToString( SignatureHeader header, ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            TypeSequenceSignatureToString(ref blobReader, stringBuilder);
        }

        private void MethodSpecSignatureToString( SignatureHeader header, ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            TypeSequenceSignatureToString(ref blobReader, stringBuilder);
        }

        private void TypeSignatureToString( ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            var typeCode = blobReader.ReadCompressedInteger();
            
            if (typeCode != (int)SignatureTypeKind.Class && typeCode != (int)SignatureTypeKind.ValueType)
                stringBuilder.Append($"{(SignatureTypeCode) typeCode} ");

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
                    TypeSignatureToString(ref blobReader, stringBuilder);
                    break;

                case (int)SignatureTypeCode.FunctionPointer:
                    var header = blobReader.ReadSignatureHeader();
                    MethodSignatureToString(header, ref blobReader, stringBuilder);
                    break;

                case (int)SignatureTypeCode.Array:
                    ArrayTypeSignatureToString(ref blobReader, stringBuilder);
                    break;

                case (int)SignatureTypeCode.RequiredModifier:
                case (int)SignatureTypeCode.OptionalModifier:
                    ModifiedTypeSignatureToString(ref blobReader, stringBuilder);
                    break;

                case (int)SignatureTypeCode.GenericTypeInstance:
                    GenericTypeInstanceSignatureToString(ref blobReader, stringBuilder);
                    break;

                case (int)SignatureTypeCode.GenericTypeParameter:
                case (int)SignatureTypeCode.GenericMethodParameter:
                    var index = blobReader.ReadCompressedInteger();
                    stringBuilder.Append($"{index} ");
                    break;

                case (int)SignatureTypeKind.Class:
                case (int)SignatureTypeKind.ValueType:
                    TypeHandleSignatureToString(ref blobReader, stringBuilder);
                    break;

                default:
                    throw new BadImageFormatException();
            }
        }

        private void ArrayTypeSignatureToString( ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            // Element type
            TypeSignatureToString(ref blobReader, stringBuilder);
            
            var rank = blobReader.ReadCompressedInteger();
            
            var sizesCount = blobReader.ReadCompressedInteger();
            
            for (var i = 0; i < sizesCount; i++)
                blobReader.ReadCompressedInteger();

            var lowerBoundsCount = blobReader.ReadCompressedInteger();
            
            for (var i = 0; i < lowerBoundsCount; i++)
                blobReader.ReadCompressedSignedInteger();
        }
        
        private void ModifiedTypeSignatureToString( ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            TypeHandleSignatureToString(ref blobReader, stringBuilder);
            TypeSignatureToString(ref blobReader, stringBuilder);
        }

        private void TypeHandleSignatureToString( ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            var srcHandle = blobReader.ReadTypeHandle();
            stringBuilder.Append($"{ToString(srcHandle)} ");
        }

        private void GenericTypeInstanceSignatureToString( ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            TypeSignatureToString(ref blobReader, stringBuilder);
            TypeSequenceSignatureToString(ref blobReader, stringBuilder);
        }

        private void TypeSequenceSignatureToString( ref BlobReader blobReader, StringBuilder stringBuilder )
        {
            var count = blobReader.ReadCompressedInteger();
            
            for (var i = 0; i < count; i++)
                TypeSignatureToString(ref blobReader, stringBuilder);
        }
    }
}