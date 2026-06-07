using System;
using Microsoft.Build.Framework;

namespace Binternal.References;

internal partial record PackageReference(string Id, bool IsInternalized) : IReference;

internal partial record PackageReference : IEquatable<PackageReference>
{
    public virtual bool Equals(PackageReference? other)
    {
        if (other is null)
            return false;

        return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() => Id?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
}

internal partial record PackageReference
{
    public static PackageReference? TryResolve(ITaskItem taskItem)
    {
        var id = taskItem.ItemSpec;
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var isInternalized = string.Equals(
            taskItem.GetMetadata("Internalize"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );

        return new PackageReference(id, isInternalized);
    }
}
