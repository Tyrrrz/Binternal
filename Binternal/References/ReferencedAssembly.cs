using System;
using System.IO;
using System.Reflection;
using Binternal.Utils.Extensions;
using Microsoft.Build.Framework;
using PowerKit.Extensions;

namespace Binternal.References;

internal partial record ReferencedAssembly(
    string FilePath,
    string Name,
    string? ProjectFilePath,
    string? PackageId
);

internal partial record ReferencedAssembly : IEquatable<ReferencedAssembly>
{
    public virtual bool Equals(ReferencedAssembly? other)
    {
        if (other is null)
            return false;

        return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() =>
        HashCode.Combine(
            FilePath?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0,
            Name?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0
        );
}

internal partial record ReferencedAssembly
{
    public static ReferencedAssembly? TryResolve(ITaskItem taskItem)
    {
        var filePath = taskItem.GetMetadata("FullPath").NullIfWhiteSpace() ?? taskItem.ItemSpec;
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var name = AssemblyName.TryGetAssemblyName(filePath)?.Name;
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var projectFilePath = (
            taskItem.GetMetadata("MSBuildSourceProjectFile").NullIfWhiteSpace()
            ?? taskItem.GetMetadata("OriginalProjectReferenceItemSpec").NullIfWhiteSpace()
            ?? taskItem.GetMetadata("ProjectReferenceOriginalItemSpec").NullIfWhiteSpace()
        )?.Pipe(Path.GetFullPath);

        var packageId = taskItem.GetMetadata("NuGetPackageId").NullIfWhiteSpace();

        return new ReferencedAssembly(filePath, name, projectFilePath, packageId);
    }
}
