using System;
using System.Collections.Generic;

namespace Binternal.Dependencies;

internal partial record RootedDependency(
    string AssemblyName,
    string AssemblyFilePath,
    bool IsInternalized,
    IReadOnlyList<IDependency> Dependencies
) : Dependency(AssemblyName, AssemblyFilePath, Dependencies);

internal partial record RootedDependency : IEquatable<RootedDependency>
{
    public virtual bool Equals(RootedDependency? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return base.Equals(other) && IsInternalized == other.IsInternalized;
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), IsInternalized);
}
