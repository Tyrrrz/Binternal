using System;
using System.IO;
using Microsoft.Build.Framework;
using PowerKit.Extensions;

namespace Binternal.References;

internal partial record ProjectReference(string FilePath, bool IsInternalized) : IReference
{
    public string Id => FilePath;
}

internal partial record ProjectReference : IEquatable<ProjectReference>
{
    public virtual bool Equals(ProjectReference? other)
    {
        if (other is null)
            return false;

        return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() =>
        FilePath?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
}

internal partial record ProjectReference
{
    public static ProjectReference? TryResolve(ITaskItem taskItem)
    {
        var filePath = taskItem.GetMetadata("FullPath")?.Pipe(Path.GetFullPath);
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var isInternalized = string.Equals(
            taskItem.GetMetadata("Internalize"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );

        return new ProjectReference(filePath, isInternalized);
    }
}
