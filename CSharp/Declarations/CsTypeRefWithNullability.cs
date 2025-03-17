#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;
using System.Collections.Immutable;

namespace SourceGeneratorCommons.CSharp.Declarations;

/// <summary>
/// null許容性付き型参照
/// </summary>
internal struct CsTypeRefWithNullability : IEquatable<CsTypeRefWithNullability>, ILazyConstructionOwner
{
    public CsTypeReference Type { get; }

    /// <summary>
    /// 参照型に対するnull許容性。trueの場合、参照型を文字列で表現した場合に`object?`のように末尾に?が付くようになる。
    /// </summary>
    /// <remarks>
    /// 値型に対しては常にfalse。`int?`のような値型は<see cref="Type"/>に`System.Nullable&lt;int&gt;`のような型が設定されることで表現される。
    /// </remarks>
    public bool IsNullable { get; }

    public string InternalReference
    {
        get
        {
            if (IsNullable)
            {
                DebugSGen.Assert(Type.TypeDefinition.IsReferenceType);
                return ((INullableRefarences)Type).NullablePatternInternalReference;
            }

            if (Type.TypeDefinition.Is(CsSpecialType.NullableT))
            {
                DebugSGen.Assert(!Type.TypeArgs.IsDefaultOrEmpty);
                DebugSGen.Assert(!Type.TypeArgs[0].IsDefaultOrEmpty && Type.TypeArgs[0].Length == 1);

                return ((INullableRefarences)Type.TypeArgs[0][0].Type).NullablePatternInternalReference;
            }

            return Type.InternalReference;
        }
    }

    public string GlobalReference
    {
        get
        {
            if (IsNullable)
            {
                DebugSGen.Assert(!Type.TypeDefinition.IsValueType);
                return ((INullableRefarences)Type).NullablePatternGlobalReference;
            }

            if (Type.TypeDefinition.Is(CsSpecialType.NullableT))
            {
                DebugSGen.Assert(!Type.TypeArgs.IsDefaultOrEmpty);
                DebugSGen.Assert(!Type.TypeArgs[0].IsDefaultOrEmpty && Type.TypeArgs[0].Length == 1);

                return ((INullableRefarences)Type.TypeArgs[0][0].Type).NullablePatternGlobalReference;
            }

            return Type.GlobalReference;
        }
    }

    public string Cref => Type.Cref;


    /// <summary>
    /// <see cref="CsTypeRefWithNullability"/>のコンストラクタ。
    /// </summary>
    /// <param name="type">型</param>
    /// <param name="isNullableIfRefereceType">
    /// 参照型に対するnull許容性。trueの場合、参照型を文字列で表現した場合に`object?`のように末尾に?が付くようになる。
    /// 値型に対してこのパラメータの設定は無効。値型の<see cref="IsNullable"/>は常に<see langword="false"/>となる。
    /// </param>
    public CsTypeRefWithNullability(CsTypeReference type, bool isNullableIfRefereceType)
    {
        Type = type;

        if (isNullableIfRefereceType && !type.TypeDefinition.IsValueType)
            IsNullable = true;
    }

    public CsTypeRefWithNullability ToNullableIfReferenceType()
    {
        return new CsTypeRefWithNullability(Type, isNullableIfRefereceType: true);
    }

    public CsTypeRefWithNullability ToDisnullable()
    {
        return new CsTypeRefWithNullability(Type, isNullableIfRefereceType: false);
    }

    public CsTypeRefWithNullability WithTypeArgs(EquatableArray<EquatableArray<CsTypeRefWithNullability>> typeArgs)
    {
        return new CsTypeRefWithNullability(Type.WithTypeArgs(typeArgs), isNullableIfRefereceType: IsNullable);
    }

    public CsTypeRefWithNullability WithTypeRedirection(IReadOnlyDictionary<CsTypeReference, CsTypeReference> typeRedirectDictionary)
    {
        if (typeRedirectDictionary.TryGetValue(Type, out var remapedType))
            return remapedType.WithNullability(IsNullable);

        var remapedTypeArgsBuilder = ImmutableArray.CreateBuilder<EquatableArray<CsTypeRefWithNullability>>(Type.TypeArgs.Length);

        foreach (var innerTypeArgs in Type.TypeArgs.Values)
        {
            var remapedInnerTypeArgsBuilder = ImmutableArray.CreateBuilder<CsTypeRefWithNullability>(innerTypeArgs.Length);

            foreach (var typeArg in innerTypeArgs.Values)
                remapedInnerTypeArgsBuilder.Add(typeArg.WithTypeRedirection(typeRedirectDictionary));

            remapedTypeArgsBuilder.Add(remapedInnerTypeArgsBuilder.MoveToImmutable());
        }

        var remapedTypeArgs = remapedTypeArgsBuilder.MoveToImmutable().ToEquatableArray();

        if (Type.TypeArgs.Equals(remapedTypeArgs))
            return this;

        return Type
            .WithTypeArgs(remapedTypeArgs)
            .WithNullability(IsNullable);
    }

    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        return Type.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor);
    }

    public override bool Equals(object? obj) => obj is CsTypeRefWithNullability other && this.Equals(other);

    public bool Equals(CsTypeRefWithNullability other)
    {
        if (IsNullable != other.IsNullable)
            return false;

        if (!EqualityComparer< CsTypeReference>.Default.Equals(Type, other.Type))
            return false;

        return true;
    }
    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        hashCode.Add(IsNullable);
        hashCode.Add(Type);

        return hashCode.ToHashCode();
    }
    public override string ToString() => InternalReference;
}
