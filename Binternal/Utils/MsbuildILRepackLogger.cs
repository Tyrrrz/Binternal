using ILRepacking;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Binternal.Utils;

internal sealed class MsbuildILRepackLogger(TaskLoggingHelper log) : ILRepacking.ILogger
{
    public bool ShouldLogVerbose { get; set; }

    public void Error(string msg) => log.LogError("{0}", msg);

    public void Warn(string msg) => log.LogWarning("{0}", msg);

    public void Info(string msg) => log.LogMessage(MessageImportance.Normal, "{0}", msg);

    public void Verbose(string msg)
    {
        if (ShouldLogVerbose)
            log.LogMessage(MessageImportance.Low, "{0}", msg);
    }
}
