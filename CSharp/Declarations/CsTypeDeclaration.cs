#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SourceGeneratorCommons.CSharp.Declarations;

/// <summary>
/// 型定義
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
abstract class CsTypeDeclaration : ITypeContainer, IEquatable<CsTypeDeclaration>
{
    public ITypeContainer? Container { get; private set; }

    public abstract bool IsValueType { get; }

    public virtual bool IsReferenceType => !IsValueType;

    public abstract int Arity { get; }

    public abstract EquatableArray<CsTypeParameterDeclaration> GenericTypeParams { get; }

    public virtual bool CanInherit => false;

    public string Name { get; }

    public string NameWithGenericParams
    {
        get
        {
            ThrowIfInitializeNotFullCompleted();

            if (_nameWithGenericArgs is not null)
                return _nameWithGenericArgs;

            if (this is CsGenericDefinableTypeDeclaration { GenericTypeParams: { IsDefaultOrEmpty: false } genericTypeParams })
            {
                _nameWithGenericArgs = $"{Name}<{string.Join(",", genericTypeParams.Values.Select(v => v.Name))}>";
            }
            else
            {
                _nameWithGenericArgs = Name;
            }

            return _nameWithGenericArgs;
        }
    }

    public string FullNameWithNameSpaceAlias
    {
        get
        {
            ThrowIfInitializeNotFullCompleted();

            if (_fullName is not null)
                return _fullName;

            if (Container is null)
                _fullName = NameWithGenericParams;
            else
                _fullName = $"{Container.FullNameWithNameSpaceAlias}.{NameWithGenericParams}";

            return _fullName;
        }
    }

    public bool IsDefinedUnderSystemNameSpace => Container?.IsDefinedUnderSystemNameSpace ?? false;

    private string? _nameWithGenericArgs;

    private string? _fullName;

    private Task ConstructionFullCompleted { get; }

    Task ILazyConstructionRoot.ConstructionFullCompleted => ConstructionFullCompleted;


    protected Task SelfConstructionCompleted => _selfConstructionCompletedSource?.Task ?? Task.CompletedTask;

    Task IConstructionFullCompleteFactor.SelfConstructionCompleted => SelfConstructionCompleted;

    private TaskCompletionSource? _selfConstructionCompletedSource;

    protected static bool RejectAlreadyCompletedFactor { get; }

#if DEBUG
    public ImmutableArray<IConstructionFullCompleteFactor> ConstructionFullCompleteFactors { get; private set; }
#endif

    static CsTypeDeclaration()
    {
#if DEBUG
        RejectAlreadyCompletedFactor = false;
#else
        RejectAlreadyCompletedFactor = true;
#endif
    }

    protected CsTypeDeclaration(ITypeContainer? container, string name)
    {
        Container = container;
        Name = name;

        ConstructionFullCompleted = container?.ConstructionFullCompleted ?? Task.CompletedTask;
    }

    protected CsTypeDeclaration(string name, out Action<ITypeContainer?, IEnumerable<IConstructionFullCompleteFactor>?> complete)
    {
        Name = name;

        var constructionFullCompletedSource = new TaskCompletionSource();
        ConstructionFullCompleted = constructionFullCompletedSource.Task;

        _selfConstructionCompletedSource = new TaskCompletionSource();

        complete = (container, constructionFullCompleteFactors) =>
        {
            DebugSGen.Assert(!_selfConstructionCompletedSource.Task.IsCompleted);

            if (_selfConstructionCompletedSource.Task.IsCompleted)
                throw new InvalidOperationException();

            Container = container;

            _selfConstructionCompletedSource.SetResult();

            if (container is not null && constructionFullCompleteFactors is not null)
                constructionFullCompleteFactors = constructionFullCompleteFactors.Concat([container]);
            else if (container is not null)
                constructionFullCompleteFactors = [container];

#if DEBUG
            ConstructionFullCompleteFactors = constructionFullCompleteFactors?.Distinct(ReferenceEqualityComparer<IConstructionFullCompleteFactor>.Default).ToImmutableArray() ?? ImmutableArray<IConstructionFullCompleteFactor>.Empty;
#endif

            if (constructionFullCompleteFactors is null)
            {
                constructionFullCompletedSource.SetResult();
            }
            else
            {
                var waitTargets = constructionFullCompleteFactors
                    .Where(v => !v.SelfConstructionCompleted.IsCompleted)
                    .Select(v =>
                    {
                        // 自分自身は既に完了状態に移行しているはずなので、
                        // 再帰的な参照があった場合でも既に除外されているはず
                        DebugSGen.Assert(!ReferenceEquals(this, v));
                        return v;
                    })
                    .Distinct(ReferenceEqualityComparer<IConstructionFullCompleteFactor>.Default)
                    .Select(v => v.SelfConstructionCompleted)
                    .ToArray();

                if (waitTargets.Length > 0)
                    Task.WhenAll(waitTargets).ContinueWith(_ => constructionFullCompletedSource.SetResult());
                else
                    constructionFullCompletedSource.SetResult();
            }
        };
    }

    protected abstract CsTypeDeclaration Clone();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfInitializeNotFullCompleted()
    {
        if (!ConstructionFullCompleted.IsCompleted)
            throwInvalidOperationException();

        static void throwInvalidOperationException() => throw new InvalidOperationException();
    }

    public string MakeStandardHintName()
    {
        ThrowIfInitializeNotFullCompleted();

        var builder = new StringBuilder(256);
        append(builder, this);
        return builder.ToString();

        static bool append(StringBuilder builder, ITypeContainer container)
        {
            if (container is CsTypeDeclaration typeDefinitionInfo)
            {
                if (typeDefinitionInfo.Container is not null)
                {
                    if (append(builder, typeDefinitionInfo.Container))
                        builder.Append('.');
                }

                builder.Append(typeDefinitionInfo.Name);

                if (typeDefinitionInfo is CsGenericDefinableTypeDeclaration { GenericTypeParams: { IsDefaultOrEmpty: false } genericTypeParams })
                {
                    foreach (var genericArgument in genericTypeParams.Values)
                    {
                        builder.Append('_');
                        builder.Append(genericArgument.Name);
                    }
                }

                return true;
            }
            else
            {
                DebugSGen.Assert(container is CsNameSpace);

                if (!string.IsNullOrWhiteSpace(container.Name))
                {
                    builder.Append(container.Name);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    public override string ToString() => $"{GetType().Name}{{{NameWithGenericParams}}}";

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsTypeDeclaration other && Equals(other);

    public virtual bool Equals(CsTypeDeclaration? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        ThrowIfInitializeNotFullCompleted();

        if (Name != other.Name)
            return false;

        if (!EqualityComparer<ITypeContainer?>.Default.Equals(Container, other.Container))
            return false;

        other._nameWithGenericArgs ??= _nameWithGenericArgs;
        other._fullName ??= _fullName;

        _nameWithGenericArgs ??= other._nameWithGenericArgs;
        _fullName ??= other._fullName;

        return true;
    }

    public override int GetHashCode()
    {
        ThrowIfInitializeNotFullCompleted();

        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        hash.Add(Container);
        hash.Add(Name);
        return hash.ToHashCode();
    }
    #endregion

    private string GetDebuggerDisplay()
    {
        if (ConstructionFullCompleted.IsCompleted)
            return FullNameWithNameSpaceAlias;
        else
            return $"{Name} (Now constructing...)";
    }
}
