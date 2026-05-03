using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILRepacking;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MsbuildTask = Microsoft.Build.Utilities.Task;

namespace Binline;

internal sealed class MsbuildILRepackLogger : ILRepacking.ILogger
{
    private readonly TaskLoggingHelper _log;

    public MsbuildILRepackLogger(TaskLoggingHelper log) => _log = log;

    public bool ShouldLogVerbose { get; set; }

    public void Error(string msg) => _log.LogError("{0}", msg);

    public void Warn(string msg) => _log.LogWarning("{0}", msg);

    public void Info(string msg) => _log.LogMessage(MessageImportance.Normal, "{0}", msg);

    public void Verbose(string msg)
    {
        if (ShouldLogVerbose)
            _log.LogMessage(MessageImportance.Low, "{0}", msg);
    }
}

public class BinlineTask : MsbuildTask
{
    [Required]
    public string TargetAssembly { get; set; } = "";

    [Required]
    public ITaskItem[] PackageReferences { get; set; } = [];

    [Required]
    public ITaskItem[] ReferenceCopyLocalPaths { get; set; } = [];

    public string? OutputDirectory { get; set; }

    private IReadOnlyList<string> ResolveBinlineAssemblies()
    {
        // Collect package IDs that are marked for inlining
        var binlinePackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var packageRef in PackageReferences)
        {
            var binlineValue = packageRef.GetMetadata("Binline");
            if (string.Equals(binlineValue, "true", StringComparison.OrdinalIgnoreCase))
                binlinePackageIds.Add(packageRef.ItemSpec);
        }

        if (binlinePackageIds.Count == 0)
            return Array.Empty<string>();

        // Find DLLs belonging to the marked packages
        var binlineAssemblies = new List<string>();
        foreach (var reference in ReferenceCopyLocalPaths)
        {
            var nugetPackageId = reference.GetMetadata("NuGetPackageId");
            if (string.IsNullOrEmpty(nugetPackageId))
                continue;

            if (!binlinePackageIds.Contains(nugetPackageId))
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

            binlineAssemblies.Add(path);
        }

        return binlineAssemblies;
    }

    public override bool Execute()
    {
        var binlineAssemblies = ResolveBinlineAssemblies();

        if (binlineAssemblies.Count == 0)
            return true;

        Log.LogMessage(
            MessageImportance.High,
            "Binline: Inlining {0} assembly(-ies) into '{1}'.",
            binlineAssemblies.Count,
            TargetAssembly
        );

        foreach (var asm in binlineAssemblies)
            Log.LogMessage(MessageImportance.Normal, "Binline: Inlining '{0}'.", asm);

        var inputAssemblies = new[] { TargetAssembly }.Concat(binlineAssemblies).ToArray();
        var searchDirectories = new[] { Path.GetDirectoryName(TargetAssembly) ?? string.Empty };

        var options = new RepackOptions
        {
            OutputFile = TargetAssembly,
            InputAssemblies = inputAssemblies,
            Internalize = true,
            SearchDirectories = searchDirectories,
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
        if (!string.IsNullOrEmpty(OutputDirectory))
        {
            foreach (var asmPath in binlineAssemblies)
            {
                var asmFileName = Path.GetFileName(asmPath);
                var outputPath = Path.Combine(OutputDirectory, asmFileName);

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
        }

        Log.LogMessage(MessageImportance.High, "Binline: Inlining completed successfully.");
        return true;
    }
}
