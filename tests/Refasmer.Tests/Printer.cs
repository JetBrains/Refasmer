using System.Text;
using Mono.Cecil;

namespace JetBrains.Refasmer.Tests;

public static class Printer
{
    public static void PrintType(TypeDefinition type, StringBuilder printout, string indent = "")
    {
        var access = GetAccessString(type);
        printout.AppendLine($"{indent}{access} type: {type.FullName}");
        if (type.HasFields)
        {
            printout.AppendLine($"{indent}fields:");
            foreach (var field in type.Fields)
            {
                printout.AppendLine($"{indent}- {field.Name}: {field.FieldType}");
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

                printout.AppendLine($"{indent}): {method.ReturnType}:");
                foreach (var instruction in method.Body.Instructions)
                {
                    printout.AppendLine($"{indent}  - {instruction}");
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
}
