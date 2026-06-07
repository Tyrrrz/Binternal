using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Binternal.References;
using Binternal.Utils.Extensions;

namespace Binternal.Dependencies;

internal record DependencyRoot(IReadOnlyList<RootedDependency> Dependencies)
{
    private static IEnumerable<IDependency> FindDependencies(
        ReferencedAssembly referencedAssembly,
        IReadOnlyList<ReferencedAssembly> candidateReferencedAssemblies,
        ISet<ReferencedAssembly>? visitedReferencedAssemblies = null
    )
    {
        visitedReferencedAssemblies ??= new HashSet<ReferencedAssembly>();
        visitedReferencedAssemblies.Add(referencedAssembly);

        var dependencyAssemblyNames = AssemblyName
            .TryGetReferencedAssemblyNames(referencedAssembly.FilePath)
            ?.Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (dependencyAssemblyNames is null)
            yield break;

        foreach (var candidateReferencedAssembly in candidateReferencedAssemblies)
        {
            if (!dependencyAssemblyNames.Contains(candidateReferencedAssembly.Name))
                continue;

            if (visitedReferencedAssemblies.Contains(candidateReferencedAssembly))
                continue;

            var nestedDependencies = FindDependencies(
                    candidateReferencedAssembly,
                    candidateReferencedAssemblies,
                    visitedReferencedAssemblies
                )
                .ToArray();

            yield return new Dependency(
                candidateReferencedAssembly.Name,
                candidateReferencedAssembly.FilePath,
                nestedDependencies
            );
        }
    }

    private static IReadOnlyList<RootedDependency> ResolveDependencies(
        IReadOnlyList<ProjectReference> projectReferences,
        IReadOnlyList<PackageReference> packageReferences,
        IReadOnlyList<ReferencedAssembly> referencedAssemblies
    )
    {
        var dependencies = new HashSet<RootedDependency>();

        foreach (var referencedAssembly in referencedAssemblies)
        {
            var projectReference = projectReferences.FirstOrDefault(p =>
                string.Equals(
                    p.FilePath,
                    referencedAssembly.ProjectFilePath,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            var packageReference = packageReferences.FirstOrDefault(p =>
                string.Equals(
                    p.Id,
                    referencedAssembly.PackageId,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if (projectReference is null && packageReference is null)
                continue;

            var isInternalized =
                projectReference?.IsInternalized == true
                || packageReference?.IsInternalized == true;

            var dependency = new RootedDependency(
                referencedAssembly.Name,
                referencedAssembly.FilePath,
                isInternalized,
                FindDependencies(referencedAssembly, referencedAssemblies).ToArray()
            );

            dependencies.Add(dependency);
        }

        return dependencies.ToArray();
    }

    public static DependencyRoot Resolve(
        IReadOnlyList<ProjectReference> projectReferences,
        IReadOnlyList<PackageReference> packageReferences,
        IReadOnlyList<ReferencedAssembly> referencedAssemblies
    ) => new(ResolveDependencies(projectReferences, packageReferences, referencedAssemblies));
}
