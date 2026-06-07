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
    public ITaskItem[] ReferencedAssemblies { get; set; } = [];

    [Required]
    public required string TargetFilePath { get; set; }

    private string TargetDirectoryPath => Path.GetDirectoryName(TargetFilePath) ?? string.Empty;

    private bool Execute(
        IReadOnlyList<string> internalizedAssemblyFilePaths,
        IReadOnlyList<string> searchDirectoryPaths
    )
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
                SearchDirectories = searchDirectoryPaths,
                // Some merged dependencies ship identical linker resource names (e.g. ILLink.Substitutions.xml).
                // Keep them instead of emitting repeated duplicate-resource warnings.
                AllowDuplicateResources = true,
                // Some merged dependencies may contain identical type definitions (e.g. polyfills).
                // Keep them instead of emitting repeated duplicate-type warnings.
                AllowAllDuplicateTypes = true,
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

        var searchDirectoryPaths = dependencyRoot
            .Dependencies.SelectMany(d => d.GetAllDependencies().Prepend(d))
            .Select(d => Path.GetDirectoryName(d.AssemblyFilePath) ?? string.Empty)
            .WhereNotNullOrWhiteSpace()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Execute(internalizedDependencyFilePaths.ToArray(), searchDirectoryPaths.ToArray());
    }

    private bool Execute(
        IReadOnlyList<ProjectReference> projectReferences,
        IReadOnlyList<PackageReference> packageReferences,
        IReadOnlyList<ReferencedAssembly> referencedAssemblies
    ) =>
        Execute(DependencyRoot.Resolve(projectReferences, packageReferences, referencedAssemblies));

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
            string.Join(Environment.NewLine, ReferencedAssemblies.Select(r => r.ItemSpec))
        );

        Log.LogMessage("Target: '{0}'.", TargetFilePath);

        return Execute(
            ProjectReferences.Select(ProjectReference.TryResolve).WhereNotNull().ToArray(),
            PackageReferences.Select(PackageReference.TryResolve).WhereNotNull().ToArray(),
            ReferencedAssemblies.Select(ReferencedAssembly.TryResolve).WhereNotNull().ToArray()
        );
    }
}
