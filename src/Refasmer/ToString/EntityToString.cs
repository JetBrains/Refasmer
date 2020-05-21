using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace JetBrains.Refasmer
{
    public static class EntityToString
    {
        public static string ToString( this MetadataReader reader, StringHandle x) => reader.GetString(x);
        public static string ToString( this MetadataReader reader, GuidHandle x) => reader.GetGuid(x).ToString();
        public static string ToString( this MetadataReader reader, BlobHandle x) => BitConverter.ToString(reader.GetBlobBytes(x)).Replace("-", string.Empty);

        public static string ToString( this MetadataReader reader, AssemblyDefinition x) => $"{{AssemblyDef[{RowId(x):X}]: {reader.ToString( x.Name)} {x.Version}}}";
        public static string ToString( this MetadataReader reader, ModuleDefinition x) => $"{{ModuleDef[{RowId(x):X}]: {reader.ToString( x.Name)}}}";
        
        public static string ToString( this MetadataReader reader, AssemblyReferenceHandle x)  => reader.ToString( reader.GetAssemblyReference(x));
        public static string ToString( this MetadataReader reader, AssemblyReference x) => $"{{AssemblyRef[{RowId(x):X}]: {reader.ToString( x.Name)} {x.Version}}}";

        public static string ToString( this MetadataReader reader, ModuleReferenceHandle x)  => reader.ToString( reader.GetModuleReference(x));
        public static string ToString( this MetadataReader reader, ModuleReference x) => $"{{ModuleRef[{RowId(x):X}]: {reader.ToString( x.Name)}}}";
        
        public static string ToString( this MetadataReader reader, AssemblyFileHandle x)  => reader.ToString( reader.GetAssemblyFile(x));
        public static string ToString( this MetadataReader reader, AssemblyFile x) => $"{{AssemblyFile[{RowId(x):X}]: {reader.ToString( x.Name)}}}";

        public static string ToString( this MetadataReader reader, TypeReferenceHandle x)  => reader.ToString( reader.GetTypeReference(x));
        public static string ToString( this MetadataReader reader, TypeReference x) => $"{{TypeRef[{RowId(x):X}]: {reader.ToString( x.Namespace)}::{reader.ToString( x.Name)}}}";

        public static string ToString( this MetadataReader reader, MemberReferenceHandle x)  => reader.ToString( reader.GetMemberReference(x));
        public static string ToString( this MetadataReader reader, MemberReference x) => $"{{MemberRef[{RowId(x):X}]: {reader.ToString( x.Parent)} {reader.ToString( x.Name)} {reader.SignatureWithHeaderToString(x.Signature)}}}";

        public static string ToString( this MetadataReader reader, TypeDefinitionHandle x)  => reader.ToString( reader.GetTypeDefinition(x));
        public static string ToString( this MetadataReader reader, TypeDefinition x) => $"{{TypeDef[{RowId(x):X}]: {reader.ToString( x.Namespace)}::{reader.ToString( x.Name)}}}";

        public static string ToString( this MetadataReader reader, TypeSpecificationHandle x)  => reader.ToString( reader.GetTypeSpecification(x));
        public static string ToString( this MetadataReader reader, TypeSpecification x) => $"{{TypeSpec[{RowId(x):X}]: {reader.TypeSignatureToString(x.Signature)}}}";
        
        public static string ToString( this MetadataReader reader, FieldDefinitionHandle x)  => reader.ToString( reader.GetFieldDefinition(x));
        public static string ToString( this MetadataReader reader, FieldDefinition x) => $"{{FieldDef[{RowId(x):X}]: {reader.ToString( x.Name)}}}";
        
        public static string ToString( this MetadataReader reader, MethodDefinitionHandle x)  => reader.ToString( reader.GetMethodDefinition(x));
        public static string ToString( this MetadataReader reader, MethodDefinition x) => $"{{MethodDef[{RowId(x):X}]: {reader.ToString( x.Name)}}}";

        public static string ToString( this MetadataReader reader, MethodImplementationHandle x)  => reader.ToString( reader.GetMethodImplementation(x));
        public static string ToString( this MetadataReader reader, MethodImplementation x) => $"{{MethodImpl[{RowId(x):X}]: {reader.ToString( x.MethodBody)} {reader.ToString( x.MethodDeclaration)}}}";

        public static string ToString( this MetadataReader reader, GenericParameterHandle x)  => reader.ToString( reader.GetGenericParameter(x));
        public static string ToString( this MetadataReader reader, GenericParameter x) => $"{{GenParam[{RowId(x):X}]: {reader.ToString( x.Name)}}}";

        public static string ToString( this MetadataReader reader, GenericParameterConstraintHandle x)  => reader.ToString( reader.GetGenericParameterConstraint(x));
        public static string ToString( this MetadataReader reader, GenericParameterConstraint x) => $"{{GenParamConstr[{RowId(x):X}]: {reader.ToString( x.Parameter)} {reader.ToString( x.Type)}}}";

        public static string ToString( this MetadataReader reader, ParameterHandle x)  => reader.ToString( reader.GetParameter(x));
        public static string ToString( this MetadataReader reader, Parameter x) => $"{{Param[{RowId(x):X}]: {reader.ToString( x.Name)}}}";

        public static string ToString( this MetadataReader reader, InterfaceImplementationHandle x)  => reader.ToString( reader.GetInterfaceImplementation(x));
        public static string ToString( this MetadataReader reader, InterfaceImplementation x) => $"{{IntImpl[{RowId(x):X}]: {reader.ToString( x.Interface)}}}";
        
        public static string ToString( this MetadataReader reader, EventDefinitionHandle x)  => reader.ToString( reader.GetEventDefinition(x));
        public static string ToString( this MetadataReader reader, EventDefinition x) => $"{{EventDef[{RowId(x):X}]: {reader.ToString( x.Name)}}}";

        public static string ToString( this MetadataReader reader, PropertyDefinitionHandle x)  => reader.ToString( reader.GetPropertyDefinition(x));
        public static string ToString( this MetadataReader reader, PropertyDefinition x) => $"{{PropertyDef[{RowId(x):X}]: {reader.ToString( x.Name)}}}";

        public static string ToString( this MetadataReader reader, ExportedTypeHandle x)  => reader.ToString( reader.GetExportedType(x));
        public static string ToString( this MetadataReader reader, ExportedType x) => $"{{ExpType[{RowId(x):X}]: {reader.ToString( x.Namespace)}::{reader.ToString( x.Name)} {reader.ToString( x.Implementation)}[{x.GetTypeDefinitionId()}]}}";

        public static string ToString( this MetadataReader reader, CustomAttributeHandle x)  => reader.ToString( reader.GetCustomAttribute(x));
        public static string ToString( this MetadataReader reader, CustomAttribute x) => $"{{CustomAttr[{RowId(x):X}]: {reader.ToString( reader.GetCustomAttrClass(x))} {reader.ToString( x.Parent)}}}";

        public static string ToString( this MetadataReader reader, DeclarativeSecurityAttributeHandle x)  => reader.ToString( reader.GetDeclarativeSecurityAttribute(x));
        public static string ToString( this MetadataReader reader, DeclarativeSecurityAttribute x) => $"{{DeclSecAttr[{RowId(x):X}]: {reader.ToString( x.PermissionSet)}}}";

        public static string ToString( this MetadataReader reader, ConstantHandle x)  => reader.ToString( reader.GetConstant(x));
        public static string ToString( this MetadataReader reader, Constant x) => $"{{Const[{RowId(x):X}]: {reader.ToString( x.Parent)}}}";

        public static string ToString( this MetadataReader reader, EntityHandle x)
        {
            if (x.IsNil)
                return "Nil";
            
            switch (x.Kind)
            {
                case HandleKind.TypeReference:
                    return reader.ToString((TypeReferenceHandle) x);
                case HandleKind.TypeDefinition:
                    return reader.ToString((TypeDefinitionHandle) x);
                case HandleKind.FieldDefinition:
                    return reader.ToString((FieldDefinitionHandle) x);
                case HandleKind.MethodDefinition:
                    return reader.ToString((MethodDefinitionHandle) x);
                case HandleKind.Parameter:
                    return reader.ToString((ParameterHandle) x);
                case HandleKind.InterfaceImplementation:
                    return reader.ToString((InterfaceImplementationHandle) x);
                case HandleKind.MemberReference:
                    return reader.ToString((MemberReferenceHandle) x);
                case HandleKind.Constant:
                    break;
                case HandleKind.CustomAttribute:
                    return reader.ToString((CustomAttributeHandle) x);
                case HandleKind.DeclarativeSecurityAttribute:
                    return reader.ToString((DeclarativeSecurityAttributeHandle) x);
                case HandleKind.StandaloneSignature:
                    break;
                case HandleKind.EventDefinition:
                    return reader.ToString((EventDefinitionHandle) x);
                case HandleKind.PropertyDefinition:
                    return reader.ToString((PropertyDefinitionHandle) x);
                case HandleKind.MethodImplementation:
                    break;
                case HandleKind.ModuleReference:
                    return reader.ToString((ModuleReferenceHandle) x);
                case HandleKind.TypeSpecification:
                    return reader.ToString((TypeSpecificationHandle) x);
                
                case HandleKind.ModuleDefinition:
                    return reader.ToString(reader.GetModuleDefinition());
                case HandleKind.AssemblyDefinition:
                    return reader.ToString(reader.GetAssemblyDefinition());
                
                case HandleKind.AssemblyReference:
                    return reader.ToString((AssemblyReferenceHandle) x);
                case HandleKind.AssemblyFile:
                    return reader.ToString((AssemblyFileHandle) x);
                case HandleKind.ExportedType:
                    return reader.ToString((ExportedTypeHandle) x);
                case HandleKind.ManifestResource:
                    break;
                case HandleKind.GenericParameter:
                    return reader.ToString((GenericParameterHandle) x);
                case HandleKind.MethodSpecification:
                    break;
                case HandleKind.GenericParameterConstraint:
                    return reader.ToString((GenericParameterConstraintHandle) x);
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

        private static readonly Func<object, int?> RowId = MetaUtil.RowId;
    }
}