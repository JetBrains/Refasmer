using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private EntityHandle _notImplementedStringCtor;

        
        private static bool CheckNotImplementedCtorSignature( BlobReader blobReader )
        {
            var header = blobReader.ReadSignatureHeader();

            if (header.Kind != SignatureKind.Method)
                return false;
            if (!header.IsInstance)
                return false;
            if (header.IsGeneric)
                return false;

            var parameterCount = blobReader.ReadCompressedInteger();
            if (parameterCount != 1)
                return false;

            // return type
            var typeCode = blobReader.ReadCompressedInteger();
            if (typeCode != (int)SignatureTypeCode.Void)
                return false;
            
            // first param
            typeCode = blobReader.ReadCompressedInteger();
            if (typeCode != (int)SignatureTypeCode.String)
                return false;
            
            return true;
        }
        
        private EntityHandle FindOrCreateNotImplementedStringCtor()
        {
            var ctorHandle = FindMethod(FullNames.NotImplementedException, ".ctor", CheckNotImplementedCtorSignature);
            
            if (!IsNil(ctorHandle))
            {
                Trace?.Invoke($"Found NotImplementedException constructor {_reader.ToString(ctorHandle)}");                
                return Import(ctorHandle);
            }

            var runtimeRef = FindOrCreateRuntimeReference();

            var notImplExceptionTypeRef = _builder.AddTypeReference(runtimeRef, _builder.GetOrAddString("System"), 
                _builder.GetOrAddString("NotImplementedException"));

            var ctor = new BlobBuilder();

            new BlobEncoder(ctor).MethodSignature(isInstanceMethod: true).Parameters(1, t => t.Void(),
                p => { p.AddParameter().Type().String(); });

            var ctorBlob = _builder.GetOrAddBlob(ctor);

            ctorHandle = _builder.AddMemberReference(notImplExceptionTypeRef, _builder.GetOrAddString(".ctor"), ctorBlob);
            Trace?.Invoke($"Created NotImplementedException constructor reference {RowId(ctorHandle)}");

            return ctorHandle;
        }
        
        private int MakeMockBody( MethodDefinitionHandle methodDefHandle )
        {
            if (_notImplementedStringCtor.IsNil)
                _notImplementedStringCtor = FindOrCreateNotImplementedStringCtor();
            
            var methodDef = _reader.GetMethodDefinition(methodDefHandle);

            var fqn = $"{_reader.GetFullname(_reader.GetMethodClass(methodDefHandle))}.{_reader.GetString(methodDef.Name)}";
            
            var ilBuilder = new BlobBuilder();
            var il = new InstructionEncoder(ilBuilder);
            il.LoadString(_builder.GetOrAddUserString($"Method {fqn} not implemented in mock library"));
            il.OpCode(ILOpCode.Newobj);
            il.Token(_notImplementedStringCtor);
            il.OpCode(ILOpCode.Throw);
 
            var methodBodyStream = new MethodBodyStreamEncoder(_ilStream);
            return methodBodyStream.AddMethodBody(il);
        }
    }
}