# Binline

**Binline** is an MSBuild extension that allows you to inline package and project references as internal dependencies — merging their types directly into your assembly as internal APIs.

## Install

- 📦 [NuGet](https://nuget.org/packages/Binline): `dotnet add package Binline`

## Usage

Mark a `PackageReference` or `ProjectReference` with `<Binline>true</Binline>` to inline it into your assembly:

```xml
<ItemGroup>
  <!-- Inline JsonExtensions — its types become internal to your assembly -->
  <PackageReference Include="JsonExtensions" Version="1.1.0">
    <Binline>true</Binline>
  </PackageReference>
</ItemGroup>
```

After building, the marked dependency's types are merged into your output assembly as **internal** types, and the original DLL is removed from the output directory. This lets you ship a single self-contained assembly without worrying about DLL conflicts.

### How it works

Binline hooks into the MSBuild `AfterBuild` target. It uses [ILRepack](https://github.com/gluck/il-repack) to merge the marked assemblies into your output assembly, with all public types from the merged assemblies converted to internal. The merged DLLs are then removed from the output directory.

### Use cases

- Distributing a library that has small private dependencies without exposing them (avoid DLL hell)
- Keeping a single-file output for tools or libraries
- Embedding utility libraries (e.g. `JsonExtensions`, `PolyShim`) inside your assembly
