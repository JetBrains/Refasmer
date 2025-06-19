using System.Text;
using Mono.Cecil;

namespace JetBrains.Refasmer.Tests;

public static class Printer
{
    public static void PrintType(TypeDefinition type, StringBuilder printout, string indent = "")
    {
        var access = GetAccessString(type);
        var typeKind = GetTypeKindString(type);
        printout.AppendLine($"{indent}{access} {typeKind}: {type.FullName}");

        var baseType = type.BaseType;
        if (baseType != null && baseType.FullName != "System.Object" && baseType.FullName != "System.ValueType")
        {
            printout.AppendLine($"{indent}  - base type: {baseType.FullName}");
        }

        if (type.HasInterfaces)
        {
            foreach (var @interface in type.Interfaces)
            {
                printout.AppendLine($"{indent}  - interface impl: {@interface.InterfaceType.FullName}");
            }
        }

        if (type.HasFields)
        {
            printout.AppendLine($"{indent}fields:");
            foreach (var field in type.Fields)
            {
                printout.AppendLine($"{indent}- {GetAccessString(field)} {field.Name}: {field.FieldType}");
            }
        }

        if (type.HasMethods)
        {
            printout.AppendLine($"{indent}methods:");
            foreach (var method in type.Methods)
            {
                printout.Append($"{indent}- {method.Name}(");
                var parameters = method.Parameters;
                for (var i = 0; i < parameters.Count; i++)
                {
                    printout.Append($"{parameters[i].ParameterType} {parameters[i].Name}");
                    if (i < parameters.Count - 1)
                    {
                        printout.Append(", ");
                    }
                }

                printout.AppendLine($"): {method.ReturnType}:");
                if (method.IsAbstract)
                    printout.AppendLine($"{indent}  - <abstract>");
                else
                {
                    foreach (var instruction in method.Body.Instructions)
                    {
                        printout.AppendLine($"{indent}  - {instruction}");
                    }
                }
            }
        }

        if (type.HasNestedTypes)
        {
            printout.AppendLine($"{indent}types:");
            foreach (var nestedType in type.NestedTypes)
            {
                PrintType(nestedType, printout, indent + "  ");
            }
        }
    }
    
    private static string GetAccessString(TypeDefinition type)
    {
        if (type.IsPublic) return "public";
        if (type.IsNestedFamily) return "protected";
        if (type.IsNestedPrivate) return "private";
        if (type.IsNestedAssembly) return "internal";
        if (type.IsNestedFamilyOrAssembly) return "protected internal";
        if (type.IsNestedFamilyAndAssembly) return "private protected";
        return "internal";
    }

    private static string GetAccessString(FieldDefinition field)
    {
        if (field.IsPublic) return "public";
        if (field.IsFamily) return "protected";
        if (field.IsPrivate) return "private";
        if (field.IsAssembly) return "internal";
        if (field.IsFamilyOrAssembly) return "protected internal";
        if (field.IsFamilyAndAssembly) return "private protected";
        throw new Exception($"Unknown field accessibility for field {field}.");
    }

    private static string GetTypeKindString(TypeDefinition typeDefinition)
    {
        var result = new StringBuilder();
        if (typeDefinition.IsInterface) result.Append("interface");
        else if (typeDefinition.IsAbstract) result.Append("abstract ");
        if (typeDefinition.IsValueType) result.Append("struct");
        else if (typeDefinition.IsClass) result.Append("class");
        return result.ToString();
    }
}
