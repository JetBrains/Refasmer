using System;
using System.Reflection.Metadata;
using System.Text;

namespace JetBrains.Refasmer;

public static class SignatureToString
{
    public static string TypeSignatureToString( this MetadataReader reader, BlobHandle srcHandle )
    {
        var blobReader = reader.GetBlobReader(srcHandle);
        var stringBuilder = new StringBuilder();

        TypeSignatureToString(reader, ref blobReader, stringBuilder);
        return stringBuilder.ToString();
    }

    public static string SignatureWithHeaderToString( this MetadataReader reader, BlobHandle srcHandle )
    {
        var blobReader = reader.GetBlobReader(srcHandle);
        var stringBuilder = new StringBuilder();
    
        var header = blobReader.ReadSignatureHeader();

        stringBuilder.Append($"{header.Kind} ");
        switch (header.Kind)
        {
            case SignatureKind.Method:
            case SignatureKind.Property:
                MethodSignatureToString(reader, header, ref blobReader, stringBuilder);
                break;
            case SignatureKind.Field:
                FieldSignatureToString(reader, ref blobReader, stringBuilder);
                break;
            case SignatureKind.LocalVariables:
                LocalSignatureToString(reader, ref blobReader, stringBuilder);
                break;
            case SignatureKind.MethodSpecification:
                MethodSpecSignatureToString(reader, ref blobReader, stringBuilder);
                break;
            default:
                throw new BadImageFormatException();
        }

        return stringBuilder.ToString();
    }

    private static void MethodSignatureToString( MetadataReader reader, SignatureHeader header, ref BlobReader blobReader, StringBuilder stringBuilder )
    {
        if (header.IsGeneric)
        {
            //genericParameterCount
            blobReader.ReadCompressedInteger();
        }

        var parameterCount = blobReader.ReadCompressedInteger();

        // Return type
        TypeSignatureToString(reader, ref blobReader, stringBuilder);

        stringBuilder.Append("( ");
            
        for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            TypeSignatureToString(reader, ref blobReader, stringBuilder);

        stringBuilder.Append(" )");
    }

    private static void FieldSignatureToString( MetadataReader reader, ref BlobReader blobReader, StringBuilder stringBuilder )
    {
        TypeSignatureToString(reader, ref blobReader, stringBuilder);
    }

    private static void LocalSignatureToString( MetadataReader reader, ref BlobReader blobReader, StringBuilder stringBuilder )
    {
        TypeSequenceSignatureToString(reader, ref blobReader, stringBuilder);
    }

    private static void MethodSpecSignatureToString( MetadataReader reader, ref BlobReader blobReader, StringBuilder stringBuilder )
    {
        TypeSequenceSignatureToString(reader, ref blobReader, stringBuilder);
    }

    private static void TypeSignatureToString( MetadataReader reader, ref BlobReader blobReader, StringBuilder stringBuilder )
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
                TypeSignatureToString(reader, ref blobReader, stringBuilder);
                break;

            case (int)SignatureTypeCode.FunctionPointer:
                var header = blobReader.ReadSignatureHeader();
                MethodSignatureToString(reader, header, ref blobReader, stringBuilder);
                break;

            case (int)SignatureTypeCode.Array:
                ArrayTypeSignatureToString(reader, ref blobReader, stringBuilder);
                break;

            case (int)SignatureTypeCode.RequiredModifier:
            case (int)SignatureTypeCode.OptionalModifier:
                ModifiedTypeSignatureToString(reader, ref blobReader, stringBuilder);
                break;

            case (int)SignatureTypeCode.GenericTypeInstance:
                GenericTypeInstanceSignatureToString(reader, ref blobReader, stringBuilder);
                break;

            case (int)SignatureTypeCode.GenericTypeParameter:
            case (int)SignatureTypeCode.GenericMethodParameter:
                var index = blobReader.ReadCompressedInteger();
                stringBuilder.Append($"{index} ");
                break;

            case (int)SignatureTypeKind.Class:
            case (int)SignatureTypeKind.ValueType:
                TypeHandleSignatureToString(reader, ref blobReader, stringBuilder);
                break;

            default:
                throw new BadImageFormatException();
        }
    }

    private static void ArrayTypeSignatureToString( MetadataReader reader, ref BlobReader blobReader, StringBuilder stringBuilder )
    {
        // Element type
        TypeSignatureToString(reader, ref blobReader, stringBuilder);
            
        // Rank
        blobReader.ReadCompressedInteger();
            
        var sizesCount = blobReader.ReadCompressedInteger();
            
        for (var i = 0; i < sizesCount; i++)
            blobReader.ReadCompressedInteger();

        var lowerBoundsCount = blobReader.ReadCompressedInteger();
            
        for (var i = 0; i < lowerBoundsCount; i++)
            blobReader.ReadCompressedSignedInteger();
    }
        
    private static void ModifiedTypeSignatureToString( MetadataReader reader, ref BlobReader blobReader, StringBuilder stringBuilder )
    {
        TypeHandleSignatureToString(reader, ref blobReader, stringBuilder);
        TypeSignatureToString(reader, ref blobReader, stringBuilder);
    }

    private static void TypeHandleSignatureToString( MetadataReader reader, ref BlobReader blobReader, StringBuilder stringBuilder )
    {
        var srcHandle = blobReader.ReadTypeHandle();
        stringBuilder.Append($"{reader.ToString(srcHandle)} ");
    }

    private static void GenericTypeInstanceSignatureToString( MetadataReader reader, ref BlobReader blobReader, StringBuilder stringBuilder )
    {
        TypeSignatureToString(reader, ref blobReader, stringBuilder);
        TypeSequenceSignatureToString(reader, ref blobReader, stringBuilder);
    }

    private static void TypeSequenceSignatureToString( MetadataReader reader, ref BlobReader blobReader, StringBuilder stringBuilder )
    {
        var count = blobReader.ReadCompressedInteger();
            
        for (var i = 0; i < count; i++)
            TypeSignatureToString(reader, ref blobReader, stringBuilder);
    }
}