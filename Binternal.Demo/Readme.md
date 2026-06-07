# Binternal Demo

This demo project showcases the following internalization scenario:

- Package A: `Microsoft.Extensions.Http.Polly` (direct reference, internalized)
- Package B: `Microsoft.Extensions.DependencyInjection.Abstractions` (direct reference, not internalized)

Package A also transitively depends on Package B through the `Microsoft.Extensions.*` dependency graph.

Expected output behavior after build:

- Package A assemblies are internalized and removed from the output directory.
- Package B assembly remains as a standalone output dependency.
