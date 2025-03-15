#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;
using System.Collections.Immutable;
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

    private ImmutableArray<string> EqualsSignature
    {
        get
        {
            if (!_equalsSignature.IsDefault)
                return _equalsSignature;

            var count = 0;
            countLength(TypeDefinition, TypeArgs.AsSpan(), ref count, out _);

            var builder = ImmutableArray.CreateBuilder<string>(count);

            fill(builder, TypeDefinition, TypeArgs.AsSpan(), out _);

            return builder.MoveToImmutable();

            static void countLength(CsTypeDeclaration type, ReadOnlySpan<EquatableArray<CsTypeRefWithNullability>> typeArgs, ref int count, out CsNameSpace? nameSpace)
            {
                count += 1;

                nameSpace = null;

                if (type.Container is CsTypeDeclaration containerType)
                    countLength(containerType, typeArgs.Length > 0 ? typeArgs.Slice(1) : [], ref count, out nameSpace);
                else if (type.Container is CsNameSpace)
                    nameSpace = (CsNameSpace)type.Container;

                if (typeArgs.Length > 0 && typeArgs[0].Length > 0)
                {
                    count += 2;
                    foreach (var typeArg in typeArgs[0].AsSpan())
                        countLength(typeArg.Type.TypeDefinition, typeArg.Type.TypeArgs.AsSpan(), ref count, out _);
                }

                if (nameSpace is not null)
                    count++;
            }

            static void fill(ImmutableArray<string>.Builder builder, CsTypeDeclaration type, ReadOnlySpan<EquatableArray<CsTypeRefWithNullability>> typeArgs, out CsNameSpace? nameSpace)
            {
                nameSpace = null;

                if (type.Container is CsTypeDeclaration containerType)
                    fill(builder, containerType, typeArgs.Length > 0 ? typeArgs.Slice(1) : [], out nameSpace);
                else if (type.Container is CsNameSpace)
                    nameSpace = (CsNameSpace)type.Container;
                
                builder.Add(type.Name);

                if (typeArgs.Length > 0 && typeArgs[0].Length > 0)
                {
                    builder.Add("<");
                    foreach (var typeArg in typeArgs[0].AsSpan())
                        fill(builder, typeArg.Type.TypeDefinition, typeArg.Type.TypeArgs.AsSpan(), out _);
                    builder.Add(">");
                }

                if (nameSpace is not null)
                    builder.Add(nameSpace.Name);
            }
        }
    }

    private ImmutableArray<string> _equalsSignature;

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

    public CsTypeRefWithNullability WithNullability(bool isNullableIfRefereceType)
    {
        return new CsTypeRefWithNullability(this, isNullableIfRefereceType);
    }

    public CsTypeReference WithTypeDefinition(CsTypeDeclaration typeDefinition)
    {
        ValidateRecursiveTypeArgsArity(typeDefinition, TypeArgs.AsSpan());

        return new CsTypeReference(typeDefinition, TypeArgs);

    }

    public CsTypeReference WithTypeArgs(EquatableArray<EquatableArray<CsTypeRefWithNullability>> typeArgs)
    {
        ValidateRecursiveTypeArgsArity(TypeDefinition, typeArgs.AsSpan());

        return new CsTypeReference(TypeDefinition, typeArgs);
    }

    static void ValidateRecursiveTypeArgsArity(CsTypeDeclaration typeDeclaration, ReadOnlySpan<EquatableArray<CsTypeRefWithNullability>> typeArgs)
    {
        if (typeDeclaration.IsGenericType)
        {
            if (typeArgs.Length == 0)
                throw new ArgumentException($"{typeDeclaration.Name}に対応する型引数の数が一致しません。", nameof(typeArgs));

            if (typeDeclaration.GenericTypeParams.Length != typeArgs[0].Length)
                throw new ArgumentException($"{typeDeclaration.Name}に対応する型引数の数が一致しません。", nameof(typeArgs));
        }
        else
        {
            if (typeArgs.Length != 0 && !typeArgs[0].IsDefaultOrEmpty)
                throw new ArgumentException($"{typeDeclaration.Name}に対応する型引数の数が一致しません。", nameof(typeArgs));
        }


        if (typeDeclaration.Container is CsTypeDeclaration containerTypeDeclaration)
        {
            if (typeArgs.Length <= 1)
                throw new ArgumentException($"{containerTypeDeclaration.Name}に対応する型引数が含まれていません。", nameof(typeArgs));

            ValidateRecursiveTypeArgsArity(containerTypeDeclaration, typeArgs.Slice(1));
        }
        else
        {
            if (typeArgs.Length > 1)
                throw new ArgumentException($"対応する型のない型引数が含まれています。", nameof(typeArgs));
        }
    }

    public override bool Equals(object? obj) => obj is CsTypeReference other && this.Equals(other);

    public bool Equals(CsTypeReference? other)
    {
        if (other is null)
            return false;

        // CsTypeReferenceは`class A : IEquatable<A>`のような自己参照的な関係にも関わるので
        // 普通にメンバオブジェクトのEqualsを呼び出すと上記のような関係性に含まれる
        // 循環参照でスタックオーバーフローが発生する。
        // 安全かつ高速な手法として一番詳細な型名(※)の連結で同一性を判定する。
        // ※ Systemの型は全てIntern化されるので参照比較のみで判別可能
        if (!EqualsSignature.AsSpan().SequenceEqual(other.EqualsSignature.AsSpan()))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        // CsTypeReferenceは`class A : IEquatable<A>`のような自己参照的な関係にも関わるので
        // 普通にメンバオブジェクトのGetHashCodeを呼び出すと上記のような関係性に含まれる
        // 循環参照でスタックオーバーフローが発生する。
        // 安全かつ高速な手法として一番詳細な型名(※)の連結で同一性を判定する。
        // ※ Systemの型は全てIntern化されるので参照比較のみで判別可能
        foreach (var value in EqualsSignature.AsSpan())
            hashCode.Add(value);
 
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
                            _ => typeDefinition.Container.FullNameWithNameSpaceAlias,
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
                    case "Boolean": return "bool";
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
                    case "String":  return "string";
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
