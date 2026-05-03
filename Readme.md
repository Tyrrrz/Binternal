# Binternal

**Binternal** is an MSBuild extension that allows you to internalize package and project references — merging their types directly into your assembly as internal APIs.

## Install

- 📦 [NuGet](https://nuget.org/packages/Binternal): `dotnet add package Binternal`

## Usage

Mark a `PackageReference` or `ProjectReference` with `Internalize="true"` to internalize it into your assembly:

```xml
<ItemGroup>
  <!-- Internalize JsonExtensions — its types become internal to your assembly -->
  <PackageReference Include="JsonExtensions" Version="1.1.0" Internalize="true" />
</ItemGroup>
```

After building, the marked dependency's types are merged into your output assembly as **internal** types, and the original DLL is removed from the output directory. This lets you ship a single self-contained assembly without worrying about DLL conflicts.

### How it works

Binternal hooks into the MSBuild `AfterBuild` target. It uses [ILRepack](https://github.com/gluck/il-repack) to merge the marked assemblies into your output assembly, with all public types from the merged assemblies converted to internal. The merged DLLs are then removed from the output directory.

### Use cases

- Distributing a library that has small private dependencies without exposing them (avoid DLL hell)
- Keeping a single-file output for tools or libraries
- Embedding utility libraries (e.g. `JsonExtensions`, `PolyShim`) inside your assembly
