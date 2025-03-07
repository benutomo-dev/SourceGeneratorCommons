using System.Diagnostics;
using System.Text;

namespace SourceGeneratorCommons;

/// <summary>
/// 型定義
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
abstract class CsTypeDeclaration : ITypeContainer, IEquatable<CsTypeDeclaration>
{
    public ITypeContainer? Container { get; private set; }

    public string Name { get; }

    public string NameWithGenericParams
    {
        get
        {
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

    public string FullName
    {
        get
        {
            if (_fullName is not null)
                return _fullName;

            if (Container is null)
                _fullName = NameWithGenericParams;
            else
                _fullName = $"{Container.FullName}.{NameWithGenericParams}";

            return _fullName;
        }
    }

    protected bool IsConstructionCompleted { get; private set; }

    private string? _nameWithGenericArgs;

    private string? _fullName;

    protected CsTypeDeclaration(ITypeContainer? container, string name)
    {
        Container = container;
        Name = name;
        IsConstructionCompleted = true;
    }

    protected CsTypeDeclaration(string name, out Action<ITypeContainer?> complete)
    {
        Name = name;

        complete = container =>
        {
            if (IsConstructionCompleted)
                throw new InvalidOperationException();

            Container = container;
            IsConstructionCompleted = true;
        };
    }

    public string MakeStandardHintName()
    {
        var builder = new StringBuilder(256);
        append(builder, this);
        return builder.ToString();

        static void append(StringBuilder builder, ITypeContainer container)
        {
            if (container is CsTypeDeclaration typeDefinitionInfo)
            {
                if (typeDefinitionInfo.Container is not null)
                {
                    append(builder, typeDefinitionInfo.Container);
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
            }
            else
            {
                Debug.Assert(container is NameSpaceInfo);

                builder.Append(container.Name);
            }
        }
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsTypeDeclaration other && Equals(other);

    public virtual bool Equals(CsTypeDeclaration? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (!EqualityComparer<ITypeContainer?>.Default.Equals(Container, other.Container))
            return false;

        if (Name != other.Name)
            return false;

        other._nameWithGenericArgs ??= _nameWithGenericArgs;
        other._fullName ??= _fullName;

        _nameWithGenericArgs ??= other._nameWithGenericArgs;
        _fullName ??= other._fullName;

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        hash.Add(Container);
        hash.Add(Name);
        return hash.ToHashCode();
    }
    #endregion

    private string GetDebuggerDisplay()
    {
        if (IsConstructionCompleted)
            return FullName;
        else
            return $"{Name} (Now constructing...)";
    }
}
