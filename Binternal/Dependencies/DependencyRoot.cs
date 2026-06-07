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
        ReferenceAssembly referenceAssembly,
        IReadOnlyList<ReferenceAssembly> candidateReferenceAssemblies,
        ISet<ReferenceAssembly>? visitedReferenceAssemblies = null
    )
    {
        visitedReferenceAssemblies ??= new HashSet<ReferenceAssembly>();
        visitedReferenceAssemblies.Add(referenceAssembly);

        var dependencyAssemblyNames = AssemblyName
            .TryGetReferencedAssemblyNames(referenceAssembly.FilePath)
            ?.Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (dependencyAssemblyNames is null)
            yield break;

        foreach (var candidateReferenceAssembly in candidateReferenceAssemblies)
        {
            if (!dependencyAssemblyNames.Contains(candidateReferenceAssembly.Name))
                continue;

            if (visitedReferenceAssemblies.Contains(candidateReferenceAssembly))
                continue;

            var nestedDependencies = FindDependencies(
                    candidateReferenceAssembly,
                    candidateReferenceAssemblies,
                    visitedReferenceAssemblies
                )
                .ToArray();

            yield return new Dependency(
                candidateReferenceAssembly.Name,
                candidateReferenceAssembly.FilePath,
                nestedDependencies
            );
        }
    }

    private static IReadOnlyList<RootedDependency> ResolveDependencies(
        IReadOnlyList<ProjectReference> projectReferences,
        IReadOnlyList<PackageReference> packageReferences,
        IReadOnlyList<ReferenceAssembly> referenceAssemblies
    )
    {
        var dependencies = new HashSet<RootedDependency>();

        foreach (var referenceAssembly in referenceAssemblies)
        {
            var projectReference = projectReferences.FirstOrDefault(p =>
                string.Equals(
                    p.FilePath,
                    referenceAssembly.ProjectFilePath,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            var packageReference = packageReferences.FirstOrDefault(p =>
                string.Equals(p.Id, referenceAssembly.PackageId, StringComparison.OrdinalIgnoreCase)
            );

            if (projectReference is null && packageReference is null)
                continue;

            var isInternalized =
                projectReference?.IsInternalized == true
                || packageReference?.IsInternalized == true;

            var dependency = new RootedDependency(
                referenceAssembly.Name,
                referenceAssembly.FilePath,
                isInternalized,
                FindDependencies(referenceAssembly, referenceAssemblies).ToArray()
            );

            dependencies.Add(dependency);
        }

        return dependencies.ToArray();
    }

    public static DependencyRoot Resolve(
        IReadOnlyList<ProjectReference> projectReferences,
        IReadOnlyList<PackageReference> packageReferences,
        IReadOnlyList<ReferenceAssembly> referenceAssemblies
    ) => new(ResolveDependencies(projectReferences, packageReferences, referenceAssemblies));
}
