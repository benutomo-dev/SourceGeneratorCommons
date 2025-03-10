#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;
using System.Text;

namespace SourceGeneratorCommons.CSharp.Declarations;

/// <summary>
/// 型参照
/// </summary>
class CsTypeReference : IEquatable<CsTypeReference>, ILazyConstructionRoot, ILazyConstructionOwner, INullableRefarences
{
    private enum TypeNameEmitMode
    {
        SimpleTypeName,
        ReflectionFullTypeName,
        SourceEmbeddingFullTypeName,
        Cref,
    }

    public CsTypeDeclaration TypeDefinition { get; }

    public EquatableArray<EquatableArray<CsTypeRefWithNullability>> TypeArgs { get; }

    public string InternalReference => _internalReference ??= ToString(TypeNameEmitMode.SimpleTypeName);

    public string GlobalReference => _globalReference ??= ToString(TypeNameEmitMode.SourceEmbeddingFullTypeName);

    public string Cref => _cref ??= ToString(TypeNameEmitMode.Cref);

    string INullableRefarences.NullablePatternInternalReference => _nullableInternalReference ?? $"{InternalReference}?";

    string INullableRefarences.NullablePatternGlobalReference => _nullableGlobalReference ?? $"{GlobalReference}?";

    private Task ConstructionFullCompleted { get; }

    Task ILazyConstructionRoot.ConstructionFullCompleted => ConstructionFullCompleted;

    private string? _cref;
    private string? _globalReference;
    private string? _internalReference;
    private string? _nullableGlobalReference;
    private string? _nullableInternalReference;

    public CsTypeReference(CsTypeDeclaration typeDefinition)
        : this(typeDefinition, EquatableArray<EquatableArray<CsTypeRefWithNullability>>.Empty)
    {
    }

    public CsTypeReference(CsTypeDeclaration typeDefinition, EquatableArray<EquatableArray<CsTypeRefWithNullability>> typeArgs)
    {
        TypeDefinition = typeDefinition ?? throw new ArgumentNullException(nameof(typeDefinition));
        TypeArgs = typeArgs;

        if (typeArgs.Values.IsDefault)
            throw new ArgumentException(null, nameof(typeArgs));

        foreach (var innerTypeArgs in typeArgs.Values)
        {
            if (innerTypeArgs.Values.IsDefault)
                throw new ArgumentException(null, nameof(typeArgs));
        }

        if (GetConstructionFullCompleteFactors(true) is { } factors)
            ConstructionFullCompleted = Task.WhenAll(factors.Select(v => v.SelfConstructionCompleted));
        else
            ConstructionFullCompleted = Task.CompletedTask;
    }

    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        IEnumerable<IConstructionFullCompleteFactor>? factors = null;

        if (!rejectAlreadyCompletedFactor || !((ILazyConstructionRoot)TypeDefinition).ConstructionFullCompleted.IsCompleted)
            factors = [TypeDefinition];

        if (!TypeArgs.Values.IsEmpty)
        {
            foreach (var innerTypeArg in TypeArgs.Values)
            {
                foreach (var typeArg in innerTypeArg.Values)
                {
                    if (typeArg.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor) is { } typeArgFactors)
                    {
                        factors ??= [];
                        factors = factors.Concat(typeArgFactors);
                    }
                }
            }
        }

        return factors;
    }

    public override bool Equals(object? obj) => obj is CsTypeReference other && this.Equals(other);

    public bool Equals(CsTypeReference? other)
    {
        if (other is null)
            return false;

        // CsTypeReferenceは`class A : IEquatable<A>`のような自己参照的な関係にも関わるので
        // 普通にメンバオブジェクトのEqualsを呼び出すと上記のような関係性に含まれる
        // 循環参照でスタックオーバーフローが発生する。
        // 完全な正確性は無いが安全な手法として一番詳細な文字列化結果で同一性を判定する。
        if (GlobalReference != other.GlobalReference)
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        // CsTypeReferenceは`class A : IEquatable<A>`のような自己参照的な関係にも関わるので
        // 普通にメンバオブジェクトのGetHashCodeを呼び出すと上記のような関係性に含まれる
        // 循環参照でスタックオーバーフローが発生する。
        // 完全な正確性は無いが安全な手法として一番詳細な文字列化結果で同一性を判定する。
        hashCode.Add(GlobalReference);
 
        return hashCode.ToHashCode();
    }

    public override string ToString() => InternalReference;

    private string ToString(TypeNameEmitMode mode)
    {
        if (getTypeKeyword(TypeDefinition) is { } typeKeyword)
            return typeKeyword;

        var builder = new StringBuilder();

        append(builder, TypeDefinition, TypeArgs.AsSpan(), mode);

        var value = builder.ToString();

        return value;

        void append(StringBuilder builder, CsTypeDeclaration typeDefinition, ReadOnlySpan<EquatableArray<CsTypeRefWithNullability>> typeArgs, TypeNameEmitMode mode)
        {
            if (typeDefinition is CsArray csArray)
            {
                append(builder, csArray.ElementType.Type.TypeDefinition, csArray.ElementType.Type.TypeArgs.AsSpan(), mode);
                builder.Append('[');
                for (int i = 0; i < csArray.Rank - 1; i++)
                    builder.Append(',');
                builder.Append(']');
            }
            else
            {
                if (mode != TypeNameEmitMode.SimpleTypeName)
                {
                    if (typeDefinition.Container is CsTypeDeclaration containerType)
                    {
                        append(builder, containerType, typeArgs.Slice(0, Math.Max(0, typeArgs.Length - 1)), mode);
                        builder.Append('.');
                    }
                    else if (typeDefinition.Container is not null)
                    {
                        var containerValue = (mode, typeDefinition.Container) switch
                        {
                            // 名前空間はSourceEmbeddingFullTypeNameの時だけ"global::"付きで出力する
                            (not TypeNameEmitMode.SourceEmbeddingFullTypeName, CsNameSpace csNameSpace) => csNameSpace.Name,
                            _ => typeDefinition.Container.FullName,
                        };
                        
                        builder.Append(containerValue);

                        if (!string.IsNullOrWhiteSpace(containerValue))
                            builder.Append('.');
                    }
                }

                builder.Append(typeDefinition.Name);
            }

            var currentTypeArgs = typeArgs.Length > 0 ? typeArgs[^1] : EquatableArray<CsTypeRefWithNullability>.Empty;

            if (currentTypeArgs.Length > 0)
            {
                builder.Append(mode == TypeNameEmitMode.Cref ? '{' : '<');
                for (int i = 0; i < currentTypeArgs.Length; i++)
                {
                    builder.Append(mode == TypeNameEmitMode.Cref ? currentTypeArgs[i].Cref : currentTypeArgs[i].GlobalReference);

                    if (i != currentTypeArgs.Length - 1)
                        builder.Append(',');
                }
                builder.Append(mode == TypeNameEmitMode.Cref ? '}' : '>');
            }
        }

        string? getTypeKeyword(CsTypeDeclaration typeDefinition)
        {
            if (typeDefinition.Container is CsNameSpace { Name: "System" })
            {
                switch (typeDefinition.Name)
                {
                    case "SByte":   return "sbyte";
                    case "Int16":   return "short";
                    case "Int32":   return "int";
                    case "Int64":   return "long";
                    case "Byte":    return "byte";
                    case "UInt16":  return "ushort";
                    case "UInt32":  return "uint";
                    case "UInt64":  return "ulong";
                    case "Single":  return "float";
                    case "Double":  return "double";
                    case "Decimal": return "decimal";
                    case "Char":    return "char";
                    case "Object":  return "object";
                    case "Void":    return "void";
                    default:
                        break;
                };
            }

            return null;
        }
    }
}
