using System.Text;
using Mono.Cecil;

namespace JetBrains.Refasmer.Tests;

public static class Printer
{
    public static void PrintType(TypeDefinition type, StringBuilder printout)
    {
        printout.AppendLine($"type: {type.FullName}");
        if (type.HasFields)
        {
            printout.AppendLine("fields:");
            foreach (var field in type.Fields)
            {
                printout.AppendLine($"- {field.Name}: {field.FieldType}");
            }
        }

        if (type.HasMethods)
        {
            printout.AppendLine("methods:");
            foreach (var method in type.Methods)
            {
                printout.Append($"- {method.Name}(");
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
                foreach (var instruction in method.Body.Instructions)
                {
                    printout.AppendLine($"  - {instruction}");
                }
            }
        }
    }
}
