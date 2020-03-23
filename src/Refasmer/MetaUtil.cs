using System;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Xml;
using System.Xml.Schema;

namespace JetBrains.Refasmer
{
    public static class MetaUtil
    {
        public static string ToString(this MetadataReader reader, StringHandle x) => reader.GetString(x);
        public static string ToString(this MetadataReader reader, GuidHandle x) => reader.GetGuid(x).ToString();
        public static string ToString(this MetadataReader reader, BlobHandle x) => BitConverter.ToString(reader.GetBlobBytes(x)).Replace("-", string.Empty);

        public static string ToString(this MetadataReader reader, AssemblyDefinition x) => $"{{AssemblyDef[{RowId(x):X}]: {reader.ToString(x.Name)} {x.Version}}}";
        public static string ToString(this MetadataReader reader, ModuleDefinition x) => $"{{ModuleDef[{RowId(x):X}]: {reader.ToString(x.Name)}}}";
        
        public static string ToString(this MetadataReader reader, AssemblyReferenceHandle x) => ToString(reader, reader.GetAssemblyReference(x));
        public static string ToString(this MetadataReader reader, AssemblyReference x) => $"{{AssemblyRef[{RowId(x):X}]: {reader.ToString(x.Name)} {x.Version}}}";

        public static string ToString(this MetadataReader reader, ModuleReferenceHandle x) => ToString(reader, reader.GetModuleReference(x));
        public static string ToString(this MetadataReader reader, ModuleReference x) => $"{{ModuleRef[{RowId(x):X}]: {reader.ToString(x.Name)}}}";
        
        public static string ToString(this MetadataReader reader, AssemblyFileHandle x) => ToString(reader, reader.GetAssemblyFile(x));
        public static string ToString(this MetadataReader reader, AssemblyFile x) => $"{{AssemblyFile[{RowId(x):X}]: {reader.ToString(x.Name)}}}";

        public static string ToString(this MetadataReader reader, TypeReferenceHandle x) => ToString(reader, reader.GetTypeReference(x));
        public static string ToString(this MetadataReader reader, TypeReference x) => $"{{TypeRef[{RowId(x):X}]: {reader.ToString(x.Namespace)}::{reader.ToString(x.Name)}}}";

        public static string ToString(this MetadataReader reader, MemberReferenceHandle x) => ToString(reader, reader.GetMemberReference(x));
        public static string ToString(this MetadataReader reader, MemberReference x) => $"{{MemberRef[{RowId(x):X}]: {reader.ToString(x.Name)} {reader.ToString(x.Signature)}}}";

        public static string ToString(this MetadataReader reader, TypeDefinitionHandle x) => ToString(reader, reader.GetTypeDefinition(x));
        public static string ToString(this MetadataReader reader, TypeDefinition x) => $"{{TypeDef[{RowId(x):X}]: {reader.ToString(x.Namespace)}::{reader.ToString(x.Name)}}}";

        public static string ToString(this MetadataReader reader, TypeSpecificationHandle x) => ToString(reader, reader.GetTypeSpecification(x));
        public static string ToString(this MetadataReader reader, TypeSpecification x) => $"{{TypeSpec[{RowId(x):X}]: {reader.ToString(x.Signature)}}}";
        
        public static string ToString(this MetadataReader reader, FieldDefinitionHandle x) => ToString(reader, reader.GetFieldDefinition(x));
        public static string ToString(this MetadataReader reader, FieldDefinition x) => $"{{FieldDef[{RowId(x):X}]: {reader.ToString(x.Name)}}}";
        
        public static string ToString(this MetadataReader reader, MethodDefinitionHandle x) => ToString(reader, reader.GetMethodDefinition(x));
        public static string ToString(this MetadataReader reader, MethodDefinition x) => $"{{MethodDef[{RowId(x):X}]: {reader.ToString(x.Name)}}}";

        public static string ToString(this MetadataReader reader, MethodImplementationHandle x) => ToString(reader, reader.GetMethodImplementation(x));
        public static string ToString(this MetadataReader reader, MethodImplementation x) => $"{{MethodImpl[{RowId(x):X}]: {reader.ToString(x.MethodBody)} {reader.ToString(x.MethodDeclaration)}}}";

        public static string ToString(this MetadataReader reader, GenericParameterHandle x) => ToString(reader, reader.GetGenericParameter(x));
        public static string ToString(this MetadataReader reader, GenericParameter x) => $"{{GenParam[{RowId(x):X}]: {reader.ToString(x.Name)}}}";

        public static string ToString(this MetadataReader reader, GenericParameterConstraintHandle x) => ToString(reader, reader.GetGenericParameterConstraint(x));
        public static string ToString(this MetadataReader reader, GenericParameterConstraint x) => $"{{GenParamConstr[{RowId(x):X}]: {reader.ToString(x.Parameter)} {reader.ToString(x.Type)}}}";

        public static string ToString(this MetadataReader reader, ParameterHandle x) => ToString(reader, reader.GetParameter(x));
        public static string ToString(this MetadataReader reader, Parameter x) => $"{{Param[{RowId(x):X}]: {reader.ToString(x.Name)}}}";

        public static string ToString(this MetadataReader reader, InterfaceImplementationHandle x) => ToString(reader, reader.GetInterfaceImplementation(x));
        public static string ToString(this MetadataReader reader, InterfaceImplementation x) => $"{{IntImpl[{RowId(x):X}]: {reader.ToString(x.Interface)}}}";
        
        public static string ToString(this MetadataReader reader, EventDefinitionHandle x) => ToString(reader, reader.GetEventDefinition(x));
        public static string ToString(this MetadataReader reader, EventDefinition x) => $"{{EventDef[{RowId(x):X}]: {reader.ToString(x.Name)}}}";

        public static string ToString(this MetadataReader reader, PropertyDefinitionHandle x) => ToString(reader, reader.GetPropertyDefinition(x));
        public static string ToString(this MetadataReader reader, PropertyDefinition x) => $"{{PropertyDef[{RowId(x):X}]: {reader.ToString(x.Name)}}}";

        public static string ToString(this MetadataReader reader, ExportedTypeHandle x) => ToString(reader, reader.GetExportedType(x));
        public static string ToString(this MetadataReader reader, ExportedType x) => $"{{ExpType[{RowId(x):X}]: {reader.ToString(x.Namespace)}::{reader.ToString(x.Name)}}}";

        public static string ToString(this MetadataReader reader, CustomAttributeHandle x) => ToString(reader, reader.GetCustomAttribute(x));
        public static string ToString(this MetadataReader reader, CustomAttribute x) => $"{{CustomAttr[{RowId(x):X}]: {reader.ToString(x.Parent)}}}";

        public static string ToString(this MetadataReader reader, DeclarativeSecurityAttributeHandle x) => ToString(reader, reader.GetDeclarativeSecurityAttribute(x));
        public static string ToString(this MetadataReader reader, DeclarativeSecurityAttribute x) => $"{{DeclSecAttr[{RowId(x):X}]: {reader.ToString(x.PermissionSet)}}}";

        public static string ToString(this MetadataReader reader, EntityHandle x)
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

        public static int? RowId(object definition) =>
            (int?)definition.GetType()
                .GetProperty("RowId", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                ?.GetMethod?.Invoke(definition, new object[0])
            ?? (int?)definition.GetType()
                .GetField("_rowId", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                ?.GetValue(definition);

        public static bool IsNil( object handle ) => 
            (bool) handle.GetType().GetProperty("IsNil")?.GetMethod?.Invoke(handle, new object[0]);
    }
}