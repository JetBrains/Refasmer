using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private string ToString(StringHandle x) => _reader.GetString(x);
        private string ToString(GuidHandle x) => _reader.GetGuid(x).ToString();
        private string ToString(BlobHandle x) => BitConverter.ToString(_reader.GetBlobBytes(x)).Replace("-", string.Empty);

        private string ToString(AssemblyDefinition x) => $"{{AssemblyDef[{RowId(x):X}]: {ToString(x.Name)} {x.Version}}}";
        private string ToString(ModuleDefinition x) => $"{{ModuleDef[{RowId(x):X}]: {ToString(x.Name)}}}";
        
        private string ToString(AssemblyReferenceHandle x) => ToString(_reader.GetAssemblyReference(x));
        private string ToString(AssemblyReference x) => $"{{AssemblyRef[{RowId(x):X}]: {ToString(x.Name)} {x.Version}}}";

        private string ToString(ModuleReferenceHandle x) => ToString(_reader.GetModuleReference(x));
        private string ToString(ModuleReference x) => $"{{ModuleRef[{RowId(x):X}]: {ToString(x.Name)}}}";
        
        private string ToString(AssemblyFileHandle x) => ToString(_reader.GetAssemblyFile(x));
        private string ToString(AssemblyFile x) => $"{{AssemblyFile[{RowId(x):X}]: {ToString(x.Name)}}}";

        private string ToString(TypeReferenceHandle x) => ToString(_reader.GetTypeReference(x));
        private string ToString(TypeReference x) => $"{{TypeRef[{RowId(x):X}]: {ToString(x.Namespace)}::{ToString(x.Name)}}}";

        private string ToString(MemberReferenceHandle x) => ToString(_reader.GetMemberReference(x));
        private string ToString(MemberReference x) => $"{{MemberRef[{RowId(x):X}]: {ToString(x.Name)} {ToString(x.Signature)}}}";

        private string ToString(TypeDefinitionHandle x) => ToString(_reader.GetTypeDefinition(x));
        private string ToString(TypeDefinition x) => $"{{TypeDef[{RowId(x):X}]: {ToString(x.Namespace)}::{ToString(x.Name)}}}";

        private string ToString(TypeSpecificationHandle x) => ToString(_reader.GetTypeSpecification(x));
        private string ToString(TypeSpecification x) => $"{{TypeSpec[{RowId(x):X}]: {ToString(x.Signature)}}}";
        
        private string ToString(FieldDefinitionHandle x) => ToString(_reader.GetFieldDefinition(x));
        private string ToString(FieldDefinition x) => $"{{FieldDef[{RowId(x):X}]: {ToString(x.Name)}}}";
        
        private string ToString(MethodDefinitionHandle x) => ToString(_reader.GetMethodDefinition(x));
        private string ToString(MethodDefinition x) => $"{{MethodDef[{RowId(x):X}]: {ToString(x.Name)}}}";

        private string ToString(MethodImplementationHandle x) => ToString(_reader.GetMethodImplementation(x));
        private string ToString(MethodImplementation x) => $"{{MethodImpl[{RowId(x):X}]: {ToString(x.MethodBody)} {ToString(x.MethodDeclaration)}}}";

        private string ToString(GenericParameterHandle x) => ToString(_reader.GetGenericParameter(x));
        private string ToString(GenericParameter x) => $"{{GenParam[{RowId(x):X}]: {ToString(x.Name)}}}";

        private string ToString(GenericParameterConstraintHandle x) => ToString(_reader.GetGenericParameterConstraint(x));
        private string ToString(GenericParameterConstraint x) => $"{{GenParamConstr[{RowId(x):X}]: {ToString(x.Parameter)} {ToString(x.Type)}}}";

        private string ToString(ParameterHandle x) => ToString(_reader.GetParameter(x));
        private string ToString(Parameter x) => $"{{Param[{RowId(x):X}]: {ToString(x.Name)}}}";

        private string ToString(InterfaceImplementationHandle x) => ToString(_reader.GetInterfaceImplementation(x));
        private string ToString(InterfaceImplementation x) => $"{{IntImpl[{RowId(x):X}]: {ToString(x.Interface)}}}";
        
        private string ToString(EventDefinitionHandle x) => ToString(_reader.GetEventDefinition(x));
        private string ToString(EventDefinition x) => $"{{EventDef[{RowId(x):X}]: {ToString(x.Name)}}}";

        private string ToString(PropertyDefinitionHandle x) => ToString(_reader.GetPropertyDefinition(x));
        private string ToString(PropertyDefinition x) => $"{{PropertyDef[{RowId(x):X}]: {ToString(x.Name)}}}";

        private string ToString(ExportedTypeHandle x) => ToString(_reader.GetExportedType(x));
        private string ToString(ExportedType x) => $"{{ExpType[{RowId(x):X}]: {ToString(x.Namespace)}::{ToString(x.Name)}}}";

        private string ToString(CustomAttributeHandle x) => ToString(_reader.GetCustomAttribute(x));
        private string ToString(CustomAttribute x) => $"{{CustomAttr[{RowId(x):X}]: {x.DecodeValue()ToString(x.Parent)}}}";

        private string ToString(DeclarativeSecurityAttributeHandle x) => ToString(_reader.GetDeclarativeSecurityAttribute(x));
        private string ToString(DeclarativeSecurityAttribute x) => $"{{DeclSecAttr[{RowId(x):X}]: {ToString(x.PermissionSet)}}}";

        private string ToString(ConstantHandle x) => ToString(_reader.GetConstant(x));
        private string ToString(Constant x) => $"{{Const[{RowId(x):X}]: {ToString(x.Parent)}}}";

        private string ToString(EntityHandle x)
        {
            if (x.IsNil)
                return "Nil";
            
            switch (x.Kind)
            {
                case HandleKind.TypeReference:
                    return ToString((TypeReferenceHandle) x);
                case HandleKind.TypeDefinition:
                    return ToString((TypeDefinitionHandle) x);
                case HandleKind.FieldDefinition:
                    return ToString((FieldDefinitionHandle) x);
                case HandleKind.MethodDefinition:
                    return ToString((MethodDefinitionHandle) x);
                case HandleKind.Parameter:
                    return ToString((ParameterHandle) x);
                case HandleKind.InterfaceImplementation:
                    return ToString((InterfaceImplementationHandle) x);
                case HandleKind.MemberReference:
                    return ToString((MemberReferenceHandle) x);
                case HandleKind.Constant:
                    break;
                case HandleKind.CustomAttribute:
                    return ToString((CustomAttributeHandle) x);
                case HandleKind.DeclarativeSecurityAttribute:
                    return ToString((DeclarativeSecurityAttributeHandle) x);
                case HandleKind.StandaloneSignature:
                    break;
                case HandleKind.EventDefinition:
                    return ToString((EventDefinitionHandle) x);
                case HandleKind.PropertyDefinition:
                    return ToString((PropertyDefinitionHandle) x);
                case HandleKind.MethodImplementation:
                    break;
                case HandleKind.ModuleReference:
                    return ToString((ModuleReferenceHandle) x);
                case HandleKind.TypeSpecification:
                    return ToString((TypeSpecificationHandle) x);
                
                case HandleKind.ModuleDefinition:
                    return ToString(_reader.GetModuleDefinition());
                case HandleKind.AssemblyDefinition:
                    return ToString(_reader.GetAssemblyDefinition());
                
                case HandleKind.AssemblyReference:
                    return ToString((AssemblyReferenceHandle) x);
                case HandleKind.AssemblyFile:
                    return ToString((AssemblyFileHandle) x);
                case HandleKind.ExportedType:
                    return ToString((ExportedTypeHandle) x);
                case HandleKind.ManifestResource:
                    break;
                case HandleKind.GenericParameter:
                    return ToString((GenericParameterHandle) x);
                case HandleKind.MethodSpecification:
                    break;
                case HandleKind.GenericParameterConstraint:
                    return ToString((GenericParameterConstraintHandle) x);
                case HandleKind.Document:
                    break;
                case HandleKind.MethodDebugInformation:
                    break;
                case HandleKind.LocalScope:
                    break;
                case HandleKind.LocalVariable:
                    break;
                case HandleKind.LocalConstant:
                    break;
                case HandleKind.CustomDebugInformation:
                    break;
                case HandleKind.UserString:
                    break;
                case HandleKind.NamespaceDefinition:
                    break;
                case HandleKind.ImportScope:
                    break;
                case HandleKind.Blob:
                    break;
                case HandleKind.Guid:
                    break;
                case HandleKind.String:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            throw new NotImplementedException();
        }

        private static int? RowId(object definition) =>
            (int?)definition.GetType()
                .GetProperty("RowId", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                ?.GetMethod?.Invoke(definition, new object[0])
            ?? (int?)definition.GetType()
                .GetField("_rowId", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                ?.GetValue(definition);
    }
}