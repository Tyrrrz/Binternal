# Binternal

[![Status](https://img.shields.io/badge/status-active-47c219.svg)](https://github.com/Tyrrrz/.github/blob/prime/docs/project-status.md)
[![Build](https://img.shields.io/github/actions/workflow/status/Tyrrrz/Binternal/main.yml?branch=prime)](https://github.com/Tyrrrz/Binternal/actions)
[![Version](https://img.shields.io/nuget/v/Binternal.svg)](https://nuget.org/packages/Binternal)
[![Downloads](https://img.shields.io/nuget/dt/Binternal.svg)](https://nuget.org/packages/Binternal)
[![Discord](https://img.shields.io/discord/869237470565392384?label=discord)](https://discord.gg/2SUWKFnHSm)

<table>
    <tr>
        <td width="99999" align="center">Development of this project is entirely funded by the community. <b><a href="https://tyrrrz.me/donate">Consider donating to support!</a></b></td>
    </tr>
</table>

<p align="center">
    <img src="favicon.png" alt="Icon" />
</p>

> **Made in 🇺🇦 Ukraine — [despite Russia's best efforts](https://tyrrrz.me/ukraine)**.
> Using this project or its source code comes with the implicit agreement that you condemn Russia's war of aggression against Ukraine, recognize Russia as a terrorist state, and support Ukraine's territorial integrity.

**Binternal** is an MSBuild extension that lets you _internalize_ package and project references by merging them into the output assembly as internal APIs.
This effectively allows you to treat any dependency as a private implementation detail, which can be useful to reduce the exposed surface area of your package or when targeting execution scenarios where proper dependency resolution is not available.

## Install

- 📦 [NuGet](https://nuget.org/packages/Binternal): `dotnet add package Binternal`

## Usage

In order to internalize a dependency, mark its corresponding `PackageReference` or `ProjectReference` with the `Internalize` attribute set to `true`:

```xml
<ItemGroup>
  <!-- Add the Binternal package to enable support for the Internalize attribute -->
  <PackageReference Include="Binternal" PrivateAssets="all" />

  <!-- This package will be merged into the output assembly -->
  <PackageReference Include="CliWrap" Internalize="true" PrivateAssets="all" />
</ItemGroup>
```

> [!NOTE]
> Consider also adding the `PrivateAssets="all"` attribute to references that are being internalized.
> This will make sure they are not passed as transitive dependencies downstream when your assembly is consumed through a NuGet package.

When the above project is built, `CliWrap.dll`, along with all of its own dependencies, will be merged into the output assembly.
Public members exposed by this package will be converted into internal members, preventing them from being accessed by outside callers.
