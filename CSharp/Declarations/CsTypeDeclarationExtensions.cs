#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif

namespace SourceGeneratorCommons.CSharp.Declarations;

internal static class CsTypeDeclarationExtensions
{
    public static bool Is(this CsTypeDeclaration typeDeclaration, CsSpecialType specialType)
    {
        if (typeDeclaration is not { Container: CsNameSpace { IsSystem: true } })
            return false;

        var expectedTypeName = specialType switch
        {
            CsSpecialType.Object => nameof(Object),
            CsSpecialType.Byte => nameof(Byte),
            CsSpecialType.SByte => nameof(SByte),
            CsSpecialType.Short => nameof(Int16),
            CsSpecialType.Int => nameof(Int32),
            CsSpecialType.Long => nameof(Int64),
            CsSpecialType.Float => nameof(Single),
            CsSpecialType.Double => nameof(Double),
            CsSpecialType.UShort => nameof(UInt16),
            CsSpecialType.UInt => nameof(UInt32),
            CsSpecialType.ULong => nameof(UInt64),
            CsSpecialType.Char => nameof(Char),
            CsSpecialType.String => nameof(String),
            CsSpecialType.Guid => nameof(Guid),
            CsSpecialType.Decimal => nameof(Decimal),
            CsSpecialType.Nullable => nameof(Nullable),
            _ => throw new ArgumentException(null, nameof(specialType)),
        };

        return typeDeclaration.Name == expectedTypeName;
    }
}
