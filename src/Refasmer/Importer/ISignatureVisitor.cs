using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Importer;

internal interface ISignatureVisitor<out T>
{
    void VisitReader(BlobReader reader);

    void WriteByte(byte @byte);
    void WriteCompressedInteger(int integer);
    void WriteCompressedSignedInteger(int integer);

    void VisitTypeHandle(EntityHandle srcHandle);

    T GetResult();
}
