#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif

namespace SourceGeneratorCommons.CSharp.Declarations;

internal static class CsTypeDeclarationExtensions
{
    public static bool Is(this CsTypeDeclaration typeDeclaration, CsSpecialType specialType)
    {
        if (typeDeclaration is not { Container: CsNameSpace { IsDefinedUnderSystemNameSpace: true } })
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
            CsSpecialType.NullableT => nameof(Nullable),
            CsSpecialType.Task => nameof(Task),
            CsSpecialType.TaskT => nameof(Task<int>),
            CsSpecialType.ValueTask => nameof(ValueTask),
            CsSpecialType.ValueTaskT => nameof(ValueTask<int>),
            _ => throw new ArgumentException(null, nameof(specialType)),
        };

        if (typeDeclaration.Name == expectedTypeName)
        {
            switch (specialType)
            {
                case CsSpecialType.NullableT:
                case CsSpecialType.TaskT:
                case CsSpecialType.ValueTaskT:
                    return typeDeclaration.GenericTypeParams.Length == 1;
                case CsSpecialType.Task:
                case CsSpecialType.ValueTask:
                    return !typeDeclaration.IsGenericType;
                default:
                    return true;
            }
        }

        return false;
    }
}
