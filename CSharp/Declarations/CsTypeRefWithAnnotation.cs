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
internal struct CsTypeRefWithAnnotation : IEquatable<CsTypeRefWithAnnotation>, ILazyConstructionOwner
{
    public CsTypeRef Type { get; }

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
                DebugSGen.Assert(CanSetNullableAnnotation(Type.TypeDefinition));
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
                DebugSGen.Assert(CanSetNullableAnnotation(Type.TypeDefinition));
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

    public string SimpleCref => Type.SimpleCref;

    public string Cref => Type.Cref;


    /// <summary>
    /// <see cref="CsTypeRefWithAnnotation"/>のコンストラクタ。
    /// </summary>
    /// <param name="type">型</param>
    /// <param name="isNullableIfRefereceType">
    /// 参照型に対するnull許容性。trueの場合、参照型を文字列で表現した場合に`object?`のように末尾に?が付くようになる。
    /// 値型に対してこのパラメータの設定は無効。値型の<see cref="IsNullable"/>は常に<see langword="false"/>となる。
    /// </param>
    public CsTypeRefWithAnnotation(CsTypeRef type, bool isNullableIfRefereceType)
    {
        Type = type;

        if (isNullableIfRefereceType)
        {
            if (CanSetNullableAnnotation(type.TypeDefinition))
                IsNullable = true;
        }
    }

    public static bool CanSetNullableAnnotation(CsTypeDeclaration typeDeclaration)
    {
        // 制約のない型パラメータなどは参照型であると同時に値型ともなる
        // 参照型になる可能性があればIsNullableの設定が可能と判定する
        return typeDeclaration.IsReferenceType || !typeDeclaration.IsValueType; ;
    }


    /// <remarks>
    /// このメソッドではTなどの型パラメータは元の型制約に関係なく常に参照型とみなす扱いとなる。
    /// </remarks>
    public CsTypeRefWithAnnotation ToNullableIfReferenceType()
    {
        return new CsTypeRefWithAnnotation(Type, isNullableIfRefereceType: true);
    }

    public CsTypeRefWithAnnotation ToDisnullable()
    {
        return new CsTypeRefWithAnnotation(Type, isNullableIfRefereceType: false);
    }

    public CsTypeRefWithAnnotation WithTypeArgs(EquatableArray<EquatableArray<CsTypeRefWithAnnotation>> typeArgs)
    {
        return new CsTypeRefWithAnnotation(Type.WithTypeArgs(typeArgs), isNullableIfRefereceType: IsNullable);
    }

    public CsTypeRefWithAnnotation WithTypeRedirection(IReadOnlyDictionary<CsTypeRef, CsTypeRef> typeRedirectDictionary)
    {
        if (typeRedirectDictionary.TryGetValue(Type, out var remapedType))
            return remapedType.WithAnnotation(IsNullable);

        var remapedTypeArgsBuilder = ImmutableArray.CreateBuilder<EquatableArray<CsTypeRefWithAnnotation>>(Type.TypeArgs.Length);

        foreach (var innerTypeArgs in Type.TypeArgs.Values)
        {
            var remapedInnerTypeArgsBuilder = ImmutableArray.CreateBuilder<CsTypeRefWithAnnotation>(innerTypeArgs.Length);

            foreach (var typeArg in innerTypeArgs.Values)
                remapedInnerTypeArgsBuilder.Add(typeArg.WithTypeRedirection(typeRedirectDictionary));

            remapedTypeArgsBuilder.Add(remapedInnerTypeArgsBuilder.MoveToImmutable());
        }

        var remapedTypeArgs = remapedTypeArgsBuilder.MoveToImmutable().ToEquatableArray();

        if (Type.TypeArgs.Equals(remapedTypeArgs))
            return this;

        return Type
            .WithTypeArgs(remapedTypeArgs)
            .WithAnnotation(IsNullable);
    }

    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        return Type.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor);
    }

    public override bool Equals(object? obj) => obj is CsTypeRefWithAnnotation other && this.Equals(other);

    public bool Equals(CsTypeRefWithAnnotation other)
    {
        if (IsNullable != other.IsNullable)
            return false;

        if (!EqualityComparer< CsTypeRef>.Default.Equals(Type, other.Type))
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
