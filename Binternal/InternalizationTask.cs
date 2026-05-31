using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Binternal.Utils;
using ILRepacking;
using Microsoft.Build.Framework;
using PowerKit.Extensions;
using MsbuildTask = Microsoft.Build.Utilities.Task;

namespace Binternal;

public class InternalizationTask : MsbuildTask
{
    [Required]
    public ITaskItem[] PackageReferences { get; set; } = [];

    [Required]
    public ITaskItem[] ProjectReferences { get; set; } = [];

    [Required]
    public ITaskItem[] ReferenceLocals { get; set; } = [];

    [Required]
    public required string TargetFilePath { get; set; }

    private string TargetDirectoryPath => Path.GetDirectoryName(TargetFilePath) ?? string.Empty;

    private IReadOnlyList<string> GetInternalizedPackageIds() =>
        PackageReferences
            .Where(r =>
                string.Equals(
                    r.GetMetadata("Internalize"),
                    "true",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Select(r => r.ItemSpec)
            .WhereNotNullOrWhiteSpace()
            .ToArray();

    private IReadOnlyList<string> GetInternalizedProjectNames() =>
        ProjectReferences
            .Where(r =>
                string.Equals(
                    r.GetMetadata("Internalize"),
                    "true",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Select(r => r.ItemSpec)
            .Select(Path.GetFileNameWithoutExtension)
            .WhereNotNullOrWhiteSpace()
            .ToArray();

    private IReadOnlyList<string> GetInternalizedPackageAssemblyFilePaths()
    {
        var packageIds = GetInternalizedPackageIds().ToHashSet(StringComparer.OrdinalIgnoreCase);

        return ReferenceLocals
            .Where(r =>
                r.GetMetadata("NuGetPackageId")?.NullIfWhiteSpace() is { } packageId
                && packageIds.Contains(packageId)
            )
            .Select(r => r.GetMetadata("FullPath")?.NullIfWhiteSpace() ?? r.ItemSpec)
            .WhereNotNullOrWhiteSpace()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<string> GetInternalizedProjectAssemblyFilePaths()
    {
        var projectNames = GetInternalizedProjectNames()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return ReferenceLocals
            .Where(r => r.GetMetadata("NuGetPackageId").NullIfWhiteSpace() is null)
            .Select(r => r.GetMetadata("FullPath")?.NullIfWhiteSpace() ?? r.ItemSpec)
            .WhereNotNullOrWhiteSpace()
            .Where(p => projectNames.Contains(Path.GetFileNameWithoutExtension(p)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<string> GetInternalizedAssemblyFilePaths() =>
        GetInternalizedPackageAssemblyFilePaths()
            .Concat(GetInternalizedProjectAssemblyFilePaths())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public override bool Execute()
    {
        Log.LogMessage(
            "Package references: {0}.",
            string.Join(", ", PackageReferences.Select(r => r.ItemSpec))
        );
        Log.LogMessage(
            "Project references: {0}.",
            string.Join(", ", ProjectReferences.Select(r => r.ItemSpec))
        );
        Log.LogMessage(
            "Reference locals: {0}.",
            string.Join(", ", ReferenceLocals.Select(r => r.ItemSpec))
        );
        Log.LogMessage("Target: '{0}'.", TargetFilePath);

        var internalizedAssemblyFilePaths = GetInternalizedAssemblyFilePaths();
        if (!internalizedAssemblyFilePaths.Any())
            return true;

        Log.LogMessage(
            "Internalizing {0} assembly(-ies) into '{1}'.",
            internalizedAssemblyFilePaths.Count,
            TargetFilePath
        );

        foreach (var assembly in internalizedAssemblyFilePaths)
            Log.LogMessage("Internalizing '{0}'.", assembly);

        // Run ILRepack
        try
        {
            new ILRepack(
                new RepackOptions
                {
                    OutputFile = TargetFilePath,
                    InputAssemblies = internalizedAssemblyFilePaths
                        .Prepend(TargetFilePath)
                        .ToArray(),
                    Internalize = true,
                    // All directories where the internalized assemblies and their dependencies may be located
                    SearchDirectories = ReferenceLocals
                        .Select(r => r.GetMetadata("FullPath")?.NullIfWhiteSpace() ?? r.ItemSpec)
                        .WhereNotNullOrWhiteSpace()
                        .Select(Path.GetDirectoryName)
                        .WhereNotNullOrWhiteSpace()
                        .Prepend(TargetDirectoryPath)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    // Disable ILRepack's built-in console logging since we provide a custom logger below
                    Log = false,
                },
                new ILRepackToMSBuildLogger(Log)
            ).Repack();
        }
        catch (Exception ex)
        {
            Log.LogError("Failed to internalize assemblies.");
            Log.LogErrorFromException(ex, true);
            return false;
        }

        // Remove the now-internalized assemblies from the output directory
        foreach (var filePath in internalizedAssemblyFilePaths)
        {
            var fileName = Path.GetFileName(filePath);
            var outputFilePath = Path.Combine(TargetDirectoryPath, fileName);

            if (!File.Exists(outputFilePath))
                continue;

            // Don't delete if it's the same file as the source (edge case)
            if (
                string.Equals(
                    Path.GetFullPath(filePath),
                    Path.GetFullPath(outputFilePath),
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }

            try
            {
                File.Delete(outputFilePath);
                Log.LogMessage(
                    "Removed internalized assembly '{0}' from the output directory.",
                    fileName
                );
            }
            catch (Exception ex)
            {
                Log.LogWarning(
                    "Failed to remove internalized assembly '{0}' from the output directory: {1}",
                    fileName,
                    ex.Message
                );
            }
        }

        Log.LogMessage("Internalization successfully completed.");
        return true;
    }
}
