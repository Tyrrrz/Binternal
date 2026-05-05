using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Binternal.Utils;
using ILRepacking;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MsbuildTask = Microsoft.Build.Utilities.Task;

namespace Binternal;

public class BinternalTask : MsbuildTask
{
    [Required]
    public string TargetAssembly { get; set; } = "";

    [Required]
    public ITaskItem[] PackageReferences { get; set; } = [];

    public ITaskItem[] ProjectReferences { get; set; } = [];

    [Required]
    public ITaskItem[] ReferenceCopyLocalPaths { get; set; } = [];

    private IReadOnlyList<string> ResolveInternalizedAssemblies()
    {
        // Collect package IDs that are marked for internalization
        var internalizedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var packageRef in PackageReferences)
        {
            var internalizeValue = packageRef.GetMetadata("Internalize");
            if (string.Equals(internalizeValue, "true", StringComparison.OrdinalIgnoreCase))
                internalizedPackageIds.Add(packageRef.ItemSpec);
        }

        // Collect expected output DLL names for project references marked for internalization
        var internalizedProjectDllNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var projectRef in ProjectReferences)
        {
            var internalizeValue = projectRef.GetMetadata("Internalize");
            if (!string.Equals(internalizeValue, "true", StringComparison.OrdinalIgnoreCase))
                continue;

            // The output assembly name matches the project file name (without extension)
            var projectFileName = Path.GetFileNameWithoutExtension(projectRef.ItemSpec);
            if (!string.IsNullOrEmpty(projectFileName))
                internalizedProjectDllNames.Add(projectFileName + ".dll");
        }

        if (internalizedPackageIds.Count == 0 && internalizedProjectDllNames.Count == 0)
            return Array.Empty<string>();

        // Find DLLs belonging to the marked packages or project references
        var internalizedAssemblies = new List<string>();
        foreach (var reference in ReferenceCopyLocalPaths)
        {
            string? sourceLabel;
            var path = reference.GetMetadata("FullPath");
            if (string.IsNullOrEmpty(path))
                path = reference.ItemSpec;

            if (!string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase))
                continue;

            var nugetPackageId = reference.GetMetadata("NuGetPackageId");
            if (!string.IsNullOrEmpty(nugetPackageId) && internalizedPackageIds.Contains(nugetPackageId))
            {
                sourceLabel = $"package '{nugetPackageId}'";
            }
            else if (string.IsNullOrEmpty(nugetPackageId) && internalizedProjectDllNames.Contains(Path.GetFileName(path)))
            {
                // Note: matching by DLL filename assumes the project's AssemblyName matches its filename.
                // Projects with a custom <AssemblyName> must not use this feature.
                sourceLabel = $"project reference '{Path.GetFileNameWithoutExtension(path)}'";
            }
            else
            {
                continue;
            }

            if (!File.Exists(path))
            {
                Log.LogWarning(
                    "Binternal: Could not find assembly '{0}' from {1}.",
                    path,
                    sourceLabel
                );
                continue;
            }

            internalizedAssemblies.Add(path);
        }

        return internalizedAssemblies;
    }

    public override bool Execute()
    {
        var internalizedAssemblies = ResolveInternalizedAssemblies();

        if (internalizedAssemblies.Count == 0)
            return true;

        Log.LogMessage(
            MessageImportance.High,
            "Binternal: Internalizing {0} assembly(-ies) into '{1}'.",
            internalizedAssemblies.Count,
            TargetAssembly
        );

        foreach (var asm in internalizedAssemblies)
            Log.LogMessage(MessageImportance.Normal, "Binternal: Internalizing '{0}'.", asm);

        var outputDirectory = Path.GetDirectoryName(TargetAssembly) ?? string.Empty;
        var inputAssemblies = new[] { TargetAssembly }.Concat(internalizedAssemblies).ToArray();

        var options = new RepackOptions
        {
            OutputFile = TargetAssembly,
            InputAssemblies = inputAssemblies,
            Internalize = true,
            SearchDirectories = new[] { outputDirectory },
            // Disables ILRepack's built-in console logging; output is handled via MsbuildILRepackLogger
            Log = false,
        };

        try
        {
            var logger = new MsbuildILRepackLogger(Log);
            var repack = new ILRepack(options, logger);
            repack.Repack();
        }
        catch (Exception ex)
        {
            Log.LogError("Binternal: Failed to internalize assemblies: {0}", ex.Message);
            return false;
        }

        // Remove the now-internalized assemblies from the output directory
        foreach (var asmPath in internalizedAssemblies)
        {
            var asmFileName = Path.GetFileName(asmPath);
            var outputPath = Path.Combine(outputDirectory, asmFileName);

            if (!File.Exists(outputPath))
                continue;

            // Don't delete if it's the same file as the source (edge case)
            if (string.Equals(
                    Path.GetFullPath(outputPath),
                    Path.GetFullPath(asmPath),
                    StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                File.Delete(outputPath);
                Log.LogMessage(
                    MessageImportance.Normal,
                    "Binternal: Removed internalized assembly '{0}' from output.",
                    asmFileName
                );
            }
            catch (Exception ex)
            {
                Log.LogWarning(
                    "Binternal: Could not remove internalized assembly '{0}': {1}",
                    asmFileName,
                    ex.Message
                );
            }
        }

        Log.LogMessage(MessageImportance.High, "Binternal: Internalization completed successfully.");
        return true;
    }
}

