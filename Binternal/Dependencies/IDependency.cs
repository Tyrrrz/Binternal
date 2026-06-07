using System.Collections.Generic;

namespace Binternal.Dependencies;

internal interface IDependency : IAssemblyInfo
{
    IReadOnlyList<IDependency> Dependencies { get; }

    IReadOnlyList<IDependency> GetAllDependencies();
}
