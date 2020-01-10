using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Newtonsoft.Json;

namespace JetBrains.Refasmer
{
    public class AssemblyDumper : LoggerBase
    {
        public AssemblyDumper(LoggerBase loggerBase) : base(loggerBase)
        {
        }

        private static List<string> CustomAttributesToStings(IEnumerable<CustomAttribute> customAttributes) =>
            customAttributes?.Select(a => a.AttributeType.FullName).OrderBy(s => s).ToList();

        private static List<string> TypeAttributes(TypeDefinition t)
        {
            var result = new List<string>();
            if (t.IsNotPublic) result.Add("IsNotPublic");
            if (t.IsPublic) result.Add("IsPublic");
            if (t.IsNestedPublic) result.Add("IsNestedPublic");
            if (t.IsNestedPrivate) result.Add("IsNestedPrivate");
            if (t.IsNestedFamily) result.Add("IsNestedFamily");
            if (t.IsNestedAssembly) result.Add("IsNestedAssembly");
            if (t.IsNestedFamilyAndAssembly) result.Add("IsNestedFamilyAndAssembly");
            if (t.IsNestedFamilyOrAssembly) result.Add("IsNestedFamilyOrAssembly");
            if (t.IsAutoLayout) result.Add("IsAutoLayout");
            if (t.IsSequentialLayout) result.Add("IsSequentialLayout");
            if (t.IsExplicitLayout) result.Add("IsExplicitLayout");
            if (t.IsClass) result.Add("IsClass");
            if (t.IsInterface) result.Add("IsInterface");
            if (t.IsAbstract) result.Add("IsAbstract");
            if (t.IsSealed) result.Add("IsSealed");
            if (t.IsSpecialName) result.Add("IsSpecialName");
            if (t.IsImport) result.Add("IsImport");
            if (t.IsSerializable) result.Add("IsSerializable");
            if (t.IsWindowsRuntime) result.Add("IsWindowsRuntime");
            if (t.IsAnsiClass) result.Add("IsAnsiClass");
            if (t.IsUnicodeClass) result.Add("IsUnicodeClass");
            if (t.IsAutoClass) result.Add("IsAutoClass");
            if (t.IsBeforeFieldInit) result.Add("IsBeforeFieldInit");
            if (t.IsRuntimeSpecialName) result.Add("IsRuntimeSpecialName");
            if (t.HasSecurity) result.Add("HasSecurity");
            return result;
        }

        private static List<string> MethodAttributes(MethodDefinition m)
        {
            var result = new List<string>();
            if (m.IsCompilerControlled) result.Add("IsCompilerControlled");
            if (m.IsPrivate) result.Add("IsPrivate");
            if (m.IsFamilyAndAssembly) result.Add("IsFamilyAndAssembly");
            if (m.IsAssembly) result.Add("IsAssembly");
            if (m.IsFamily) result.Add("IsFamily");
            if (m.IsFamilyOrAssembly) result.Add("IsFamilyOrAssembly");
            if (m.IsPublic) result.Add("IsPublic");
            if (m.IsStatic) result.Add("IsStatic");
            if (m.IsFinal) result.Add("IsFinal");
            if (m.IsVirtual) result.Add("IsVirtual");
            if (m.IsHideBySig) result.Add("IsHideBySig");
            if (m.IsReuseSlot) result.Add("IsReuseSlot");
            if (m.IsNewSlot) result.Add("IsNewSlot");
            if (m.IsCheckAccessOnOverride) result.Add("IsCheckAccessOnOverride");
            if (m.IsAbstract) result.Add("IsAbstract");
            if (m.IsSpecialName) result.Add("IsSpecialName");
            if (m.IsPInvokeImpl) result.Add("IsPInvokeImpl");
            if (m.IsUnmanagedExport) result.Add("IsUnmanagedExport");
            if (m.IsRuntimeSpecialName) result.Add("IsRuntimeSpecialName");
            if (m.HasSecurity) result.Add("HasSecurity");
            if (m.IsIL) result.Add("IsIL");
            if (m.IsNative) result.Add("IsNative");
            if (m.IsRuntime) result.Add("IsRuntime");
            if (m.IsUnmanaged) result.Add("IsUnmanaged");
            if (m.IsManaged) result.Add("IsManaged");
            if (m.IsForwardRef) result.Add("IsForwardRef");
            if (m.IsPreserveSig) result.Add("IsPreserveSig");
            if (m.IsInternalCall) result.Add("IsInternalCall");
            if (m.IsSynchronized) result.Add("IsSynchronized");
            return result;
        }

        private static List<string> FieldAttributes(FieldDefinition f)
        {
            var result = new List<string>();
            if (f.IsCompilerControlled) result.Add("IsCompilerControlled");
            if (f.IsPrivate) result.Add("IsPrivate");
            if (f.IsFamilyAndAssembly) result.Add("IsFamilyAndAssembly");
            if (f.IsAssembly) result.Add("IsAssembly");
            if (f.IsFamily) result.Add("IsFamily");
            if (f.IsFamilyOrAssembly) result.Add("IsFamilyOrAssembly");
            if (f.IsPublic) result.Add("IsPublic");
            if (f.IsStatic) result.Add("IsStatic");
            if (f.IsInitOnly) result.Add("IsInitOnly");
            if (f.IsLiteral) result.Add("IsLiteral");
            if (f.IsNotSerialized) result.Add("IsNotSerialized");
            if (f.IsSpecialName) result.Add("IsSpecialName");
            if (f.IsPInvokeImpl) result.Add("IsPInvokeImpl");
            if (f.IsRuntimeSpecialName) result.Add("IsRuntimeSpecialName");
            if (f.HasDefault) result.Add("HasDefault");
            return result;
        }
        
        public void DumpAssembly(AssemblyDefinition assembly, TextWriter writer)
        {
            var dump = new
            {
                assembly.FullName,
                MainModuleName = assembly.MainModule.Name,
                
                CustomAttributes = CustomAttributesToStings(assembly.CustomAttributes),
                
                Modules = assembly.Modules.Select(a => new 
                {
                    a.Name, a.Mvid,
                    
                    References = a.AssemblyReferences
                        .Select(ar => ar.FullName)
                        .OrderBy(s => s)
                        .Distinct()
                        .ToList(),

                    Resources = a.Resources
                        .Select(r => new {r.ResourceType, r.Name})
                        .OrderBy(r => r.Name)
                        .ToList(),
                    
                    CustomAttributes = CustomAttributesToStings(a.CustomAttributes),

                    Types = a.Types
                        .FlattenTree(t => t.NestedTypes)
                        .Select(t => new
                        {
                            t.FullName, t.Attributes,
                            CustomAttributes = CustomAttributesToStings(t.CustomAttributes),
                            Flags = TypeAttributes(t),

                            Methods = t.Methods?.Select(m => new
                            {
                                m.FullName, m.Attributes,
                                CustomAttributes = CustomAttributesToStings(m.CustomAttributes),
                                Flags = MethodAttributes(m),
                                BodySize = m.Body?.Instructions?.Count,
                            }).OrderBy(m => m.FullName).ToList(),

                            Fields = t.Fields?.Select(f => new
                            {
                                f.FullName, f.Attributes,
                                CustomAttributes = CustomAttributesToStings(f.CustomAttributes),
                                Flags = FieldAttributes(f),
                                DeclaringTypeName = f.DeclaringType.FullName,
                            }).OrderBy(f => f.FullName).ToList(),

                            Properties = t.Properties?.Select(p => new
                            {
                                p.FullName, p.Attributes,
                                CustomAttributes = CustomAttributesToStings(p.CustomAttributes),
                                DeclaringType = p.DeclaringType.FullName,
                                GetMethodName = p.GetMethod?.FullName,
                                SetMethodName = p.SetMethod?.FullName,
                                OtherMethodNames = p.OtherMethods?.Select(m => m.FullName).OrderBy(s => s).ToList(),
                            }).OrderBy(p => p.FullName).ToList(),

                        }).OrderBy(t => t.FullName).ToList(),
                }).OrderBy(a => a.Name).ToList(),
            };

            var serializer = new JsonSerializer {Formatting = Formatting.Indented};
            serializer.Serialize(writer, dump);
        }
    }
}