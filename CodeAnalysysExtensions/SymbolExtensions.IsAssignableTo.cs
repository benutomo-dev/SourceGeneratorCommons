using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceGeneratorCommons;

internal static partial class SymbolExtensions
{
    internal static bool IsAssignableTo(this IFieldSymbol? fieldSymbol, ITypeSymbol? assignTargetTypeSymbol, Compilation compilation) => IsAssignableTo(fieldSymbol?.Type, assignTargetTypeSymbol, compilation);

    internal static bool IsAssignableTo(this IPropertySymbol? propertySymbol, ITypeSymbol? assignTargetTypeSymbol, Compilation compilation) => IsAssignableTo(propertySymbol?.Type, assignTargetTypeSymbol, compilation);

    internal static bool IsAssignableTo(this ILocalSymbol? localSymbol, ITypeSymbol? assignTargetTypeSymbol, Compilation compilation) => IsAssignableTo(localSymbol?.Type, assignTargetTypeSymbol, compilation);

    internal static bool IsAssignableTo(this ITypeSymbol? typeSymbol, ITypeSymbol? assignTargetTypeSymbol, Compilation compilation)
    {
        if (typeSymbol is null) return false;

        if (assignTargetTypeSymbol is null) return false;

        var comparer = SymbolEqualityComparer.Default;

        if (comparer.Equals(typeSymbol, assignTargetTypeSymbol)) return true;

        var conversion = compilation.ClassifyConversion(typeSymbol, assignTargetTypeSymbol);

        return conversion.IsImplicit && conversion.Exists;
    }
}
