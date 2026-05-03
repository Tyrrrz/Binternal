using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Binline.Utils;
using ILRepacking;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MsbuildTask = Microsoft.Build.Utilities.Task;

namespace Binline;

public class BinlineTask : MsbuildTask
{
    [Required]
    public string TargetAssembly { get; set; } = "";

    [Required]
    public ITaskItem[] PackageReferences { get; set; } = [];

    [Required]
    public ITaskItem[] ReferenceCopyLocalPaths { get; set; } = [];

    private IReadOnlyList<string> ResolveInlinedAssemblies()
    {
        // Collect package IDs that are marked for inlining
        var inlinedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var packageRef in PackageReferences)
        {
            var inlineValue = packageRef.GetMetadata("Inline");
            if (string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase))
                inlinedPackageIds.Add(packageRef.ItemSpec);
        }

        if (inlinedPackageIds.Count == 0)
            return Array.Empty<string>();

        // Find DLLs belonging to the marked packages
        var inlinedAssemblies = new List<string>();
        foreach (var reference in ReferenceCopyLocalPaths)
        {
            var nugetPackageId = reference.GetMetadata("NuGetPackageId");
            if (string.IsNullOrEmpty(nugetPackageId))
                continue;

            if (!inlinedPackageIds.Contains(nugetPackageId))
                continue;

            var path = reference.GetMetadata("FullPath");
            if (string.IsNullOrEmpty(path))
                path = reference.ItemSpec;

            if (!string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!File.Exists(path))
            {
                Log.LogWarning(
                    "Binline: Could not find assembly '{0}' from package '{1}'.",
                    path,
                    nugetPackageId
                );
                continue;
            }

            inlinedAssemblies.Add(path);
        }

        return inlinedAssemblies;
    }

    public override bool Execute()
    {
        var inlinedAssemblies = ResolveInlinedAssemblies();

        if (inlinedAssemblies.Count == 0)
            return true;

        Log.LogMessage(
            MessageImportance.High,
            "Binline: Inlining {0} assembly(-ies) into '{1}'.",
            inlinedAssemblies.Count,
            TargetAssembly
        );

        foreach (var asm in inlinedAssemblies)
            Log.LogMessage(MessageImportance.Normal, "Binline: Inlining '{0}'.", asm);

        var outputDirectory = Path.GetDirectoryName(TargetAssembly) ?? string.Empty;
        var inputAssemblies = new[] { TargetAssembly }.Concat(inlinedAssemblies).ToArray();

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
            Log.LogError("Binline: Failed to inline assemblies: {0}", ex.Message);
            return false;
        }

        // Remove the now-inlined assemblies from the output directory
        foreach (var asmPath in inlinedAssemblies)
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
                    "Binline: Removed inlined assembly '{0}' from output.",
                    asmFileName
                );
            }
            catch (Exception ex)
            {
                Log.LogWarning(
                    "Binline: Could not remove inlined assembly '{0}': {1}",
                    asmFileName,
                    ex.Message
                );
            }
        }

        Log.LogMessage(MessageImportance.High, "Binline: Inlining completed successfully.");
        return true;
    }
}

