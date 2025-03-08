namespace SourceGeneratorCommons;

class NameSpaceInfo : ITypeContainer, IEquatable<NameSpaceInfo>
{
    public string Name { get; }

    public string FullName => Name;

    public NameSpaceInfo(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public Task ConstructionFullCompleted => Task.CompletedTask;

    public Task SelfConstructionCompleted => Task.CompletedTask;

    public override bool Equals(object? obj)
    {
        return Equals(obj as NameSpaceInfo);
    }

    public bool Equals(NameSpaceInfo? other)
    {
        return other is not null &&
               Name == other.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name);
    }
}
