# Binternal

[![Status](https://img.shields.io/badge/status-active-47c219.svg)](https://github.com/Tyrrrz/.github/blob/prime/docs/project-status.md)
[![Made in Ukraine](https://img.shields.io/badge/made_in-ukraine-ffd700.svg?labelColor=0057b7)](https://tyrrrz.me/ukraine)
[![Build](https://img.shields.io/github/actions/workflow/status/Tyrrrz/Binternal/main.yml?branch=master)](https://github.com/Tyrrrz/Binternal/actions)
[![Version](https://img.shields.io/nuget/v/Binternal.svg)](https://nuget.org/packages/Binternal)
[![Downloads](https://img.shields.io/nuget/dt/Binternal.svg)](https://nuget.org/packages/Binternal)
[![Discord](https://img.shields.io/discord/869237470565392384?label=discord)](https://discord.gg/2SUWKFnHSm)
[![Fuck Russia](https://img.shields.io/badge/fuck-russia-e4181c.svg?labelColor=000000)](https://twitter.com/tyrrrz/status/1495972128977571848)

<table>
    <tr>
        <td width="99999" align="center">Development of this project is entirely funded by the community. <b><a href="https://tyrrrz.me/donate">Consider donating to support!</a></b></td>
    </tr>
</table>

<p align="center">
    <img src="favicon.png" alt="Icon" />
</p>

**Binternal** is an MSBuild extension that internalizes package and project references by merging their types directly into your assembly as internal APIs.

## Terms of use<sup>[[?]](https://github.com/Tyrrrz/.github/blob/prime/docs/why-so-political.md)</sup>

By using this project or its source code, for any purpose and in any shape or form, you grant your **implicit agreement** to all the following statements:

- You **condemn Russia and its military aggression against Ukraine**
- You **recognize that Russia is an occupant that unlawfully invaded a sovereign state**
- You **support Ukraine's territorial integrity, including its claims over temporarily occupied territories of Crimea and Donbas**
- You **reject false narratives perpetuated by Russian state propaganda**

To learn more about the war and how you can help, [click here](https://tyrrrz.me/ukraine). Glory to Ukraine! 🇺🇦

## Install

- 📦 [NuGet](https://nuget.org/packages/Binternal): `dotnet add package Binternal`

## Usage

Mark a `PackageReference` or `ProjectReference` with `Internalize="true"` to merge it into your assembly as internal types:

```xml
<ItemGroup>
  <PackageReference Include="JsonExtensions" Version="1.1.0" Internalize="true" />
</ItemGroup>
```

After building, the marked dependency's types are merged into your output assembly with **internal** visibility, and the original DLL is removed from the output directory.
When packaging a library, internalized references are automatically excluded from the generated nuspec, so consumers of your package will not see them as dependencies.

### Project references

`ProjectReference` items can be internalized in the same way:

```xml
<ItemGroup>
  <ProjectReference Include="..\MyUtils\MyUtils.csproj" Internalize="true" />
</ItemGroup>
```

### How it works

Binternal hooks into the MSBuild `AfterBuild` target and uses [ILRepack](https://github.com/gluck/il-repack) to merge the marked assemblies into your output assembly, converting all public types from the merged assemblies to internal.

### Use cases

- Distributing a library that depends on small utility packages without exposing them to consumers
- Keeping a single-file output for tools or libraries
- Embedding utility libraries (e.g. `JsonExtensions`, `PolyShim`) inside your assembly
