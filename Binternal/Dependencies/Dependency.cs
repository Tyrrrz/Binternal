using System;
using System.Collections.Generic;
using System.Linq;

namespace Binternal.Dependencies;

internal partial record Dependency(
    string AssemblyName,
    string AssemblyFilePath,
    IReadOnlyList<IDependency> Dependencies
) : IDependency
{
    public IReadOnlyList<IDependency> GetAllDependencies()
    {
        var allDependencies = new HashSet<IDependency>();

        void AddDependencies(IEnumerable<IDependency> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (allDependencies.Add(dependency))
                    AddDependencies(dependency.Dependencies);
            }
        }

        AddDependencies(Dependencies);

        return allDependencies.ToArray();
    }
}

internal partial record Dependency : IEquatable<Dependency>
{
    public virtual bool Equals(Dependency? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return string.Equals(AssemblyName, other.AssemblyName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                AssemblyFilePath,
                other.AssemblyFilePath,
                StringComparison.OrdinalIgnoreCase
            )
            && Dependencies.SequenceEqual(other.Dependencies);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        hashCode.Add(AssemblyName, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(AssemblyFilePath, StringComparer.OrdinalIgnoreCase);

        foreach (var dependency in Dependencies)
            hashCode.Add(dependency);

        return hashCode.ToHashCode();
    }
}
