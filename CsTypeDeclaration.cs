using System.Diagnostics;
using System.Text;

namespace SourceGeneratorCommons;

/// <summary>
/// 型定義
/// </summary>
abstract record class CsTypeDeclaration(
    ITypeContainer? Container,
    string Name
    )
    : ITypeContainer
{
    private string? _nameWithGenericArgs;

    private string? _fullName;

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



    public virtual bool Equals(CsTypeDeclaration? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (!base.Equals(other))
            return false;

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
}
