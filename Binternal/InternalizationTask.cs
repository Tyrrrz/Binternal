using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Binternal.Dependencies;
using Binternal.References;
using Binternal.Utils;
using ILRepacking;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using PowerKit.Extensions;

namespace Binternal;

public class InternalizationTask : Task
{
    [Required]
    public ITaskItem[] PackageReferences { get; set; } = [];

    [Required]
    public ITaskItem[] ProjectReferences { get; set; } = [];

    [Required]
    public ITaskItem[] ReferenceAssemblies { get; set; } = [];

    [Required]
    public required string TargetFilePath { get; set; }

    private string TargetDirectoryPath => Path.GetDirectoryName(TargetFilePath) ?? string.Empty;

    private bool Execute(IReadOnlyList<string> internalizedAssemblyFilePaths)
    {
        if (!internalizedAssemblyFilePaths.Any())
        {
            Log.LogMessage("No assemblies to internalize.");
            return true;
        }

        foreach (var filePath in internalizedAssemblyFilePaths)
            Log.LogMessage("Internalizing '{0}'...", filePath);

        // Run ILRepack
        new ILRepack(
            new RepackOptions
            {
                OutputFile = TargetFilePath,
                InputAssemblies = internalizedAssemblyFilePaths.Prepend(TargetFilePath).ToArray(),
                Internalize = true,
                // All directories where the internalized assemblies and their dependencies may be located
                SearchDirectories = internalizedAssemblyFilePaths
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
                    "Failed to remove internalized assembly '{0}' from the output directory.",
                    fileName
                );
                Log.LogWarningFromException(ex, true);
            }
        }

        Log.LogMessage("Internalization successfully completed.");
        return true;
    }

    private bool Execute(DependencyRoot dependencyRoot)
    {
        var nonInternalizedDependencyFilePaths = dependencyRoot
            .Dependencies.Where(d => !d.IsInternalized)
            .SelectMany(d => d.GetAllDependencies().Prepend(d))
            .Select(d => d.AssemblyFilePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var internalizedDependencyFilePaths = dependencyRoot
            .Dependencies.Where(d => d.IsInternalized)
            // Include each rooted dependency and its descendants
            .SelectMany(d => d.GetAllDependencies().Prepend(d))
            .Select(d => d.AssemblyFilePath)
            // Exclude dependencies that are shared with non-internalized dependencies
            .Where(f => !nonInternalizedDependencyFilePaths.Contains(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Execute(internalizedDependencyFilePaths.ToArray());
    }

    private bool Execute(
        IReadOnlyList<ProjectReference> projectReferences,
        IReadOnlyList<PackageReference> packageReferences,
        IReadOnlyList<ReferenceAssembly> referenceAssemblies
    ) => Execute(DependencyRoot.Resolve(projectReferences, packageReferences, referenceAssemblies));

    public override bool Execute()
    {
        Log.LogMessage(
            "Project references: {0}.",
            string.Join(Environment.NewLine, ProjectReferences.Select(r => r.ItemSpec))
        );

        Log.LogMessage(
            "Package references: {0}.",
            string.Join(Environment.NewLine, PackageReferences.Select(r => r.ItemSpec))
        );

        Log.LogMessage(
            "Reference assemblies: {0}.",
            string.Join(Environment.NewLine, ReferenceAssemblies.Select(r => r.ItemSpec))
        );

        Log.LogMessage("Target: '{0}'.", TargetFilePath);

        return Execute(
            ProjectReferences.Select(ProjectReference.TryResolve).WhereNotNull().ToArray(),
            PackageReferences.Select(PackageReference.TryResolve).WhereNotNull().ToArray(),
            ReferenceAssemblies.Select(ReferenceAssembly.TryResolve).WhereNotNull().ToArray()
        );
    }
}
