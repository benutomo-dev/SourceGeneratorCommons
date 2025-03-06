using System.Diagnostics;
using System.Text;

namespace SourceGeneratorCommons;

/// <summary>
/// 型定義
/// </summary>
record class CsTypeDeclaration(
    ITypeContainer? Container,
    string Name,
    TypeCategory TypeCategory,
    EquatableArray<GenericTypeParam> GenericTypeParams = default,
    CsTypeReference? BaseType = null,
    EquatableArray<CsTypeReference> Interfaces = default,
    CsAccessibility Accessibility = CsAccessibility.Default,
    bool IsStatic = false,
    bool IsReadOnly = false,
    bool IsRef = false,
    ClassModifier ClassModifier = ClassModifier.Default
    )
    : ITypeContainer, IEquatable<CsTypeDeclaration>
{
    public string NameWithGenericParams
    {
        get
        {
            if (_nameWithGenericArgs is not null)
                return _nameWithGenericArgs;

            if (GenericTypeParams.IsDefaultOrEmpty)
            {
                _nameWithGenericArgs = Name;
            }
            else
            {
                _nameWithGenericArgs = $"{Name}<{string.Join(",", GenericTypeParams.Values.Select(v => v.Name))}>";
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

    public string? _nameWithGenericArgs;

    public string? _fullName;

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

                if (typeDefinitionInfo.GenericTypeParams.Length > 0)
                {
                    foreach (var genericArgument in typeDefinitionInfo.GenericTypeParams.Values)
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

        var equalsResult = true
               && EqualityComparer<ITypeContainer?>.Default.Equals(Container, other.Container)
               && Name == other.Name
               && TypeCategory == other.TypeCategory
               && Accessibility == other.Accessibility
               && IsStatic == other.IsStatic
               && IsReadOnly == other.IsReadOnly
               && IsRef == other.IsRef
               && ClassModifier == other.ClassModifier
               && BaseType == other.BaseType
               && EqualityComparer<EquatableArray<GenericTypeParam>>.Default.Equals(GenericTypeParams, other.GenericTypeParams)
               && EqualityComparer<EquatableArray<CsTypeReference>>.Default.Equals(Interfaces, other.Interfaces);

        if (equalsResult)
        {
            other._nameWithGenericArgs ??= _nameWithGenericArgs;
            other._fullName ??= _fullName;

            _nameWithGenericArgs ??= other._nameWithGenericArgs;
            _fullName ??= other._fullName;
        }

        return equalsResult;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Container);
        hash.Add(Name);
        hash.Add(BaseType);
        hash.Add(TypeCategory);
        hash.Add(Accessibility);
        hash.Add(IsStatic);
        hash.Add(IsReadOnly);
        hash.Add(IsRef);
        hash.Add(ClassModifier);
        hash.Add(GenericTypeParams);
        hash.Add(Interfaces);
        return hash.ToHashCode();
    }
}
