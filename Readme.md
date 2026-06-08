# Binternal

[![Status](https://img.shields.io/badge/status-active-47c219.svg)](https://github.com/Tyrrrz/.github/blob/prime/docs/project-status.md)
[![Made in Ukraine](https://img.shields.io/badge/made_in-ukraine-ffd700.svg?labelColor=0057b7)](https://tyrrrz.me/ukraine)
[![Build](https://img.shields.io/github/actions/workflow/status/Tyrrrz/Binternal/main.yml?branch=prime)](https://github.com/Tyrrrz/Binternal/actions)
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

**Binternal** is an MSBuild extension that lets you _internalize_ package and project references by merging them into the output assembly as internal APIs.
This effectively allows you to treat any dependency as a private implementation detail, which can be useful to reduce the exposed surface area of your package or when targeting execution scenarios where proper dependency resolution is not available.

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
> This will make sure they are not passed as transitive dependencies to downstream consumers, which is important if you're building a NuGet package.

When the above project is built, `CliWrap.dll`, along with all of its own dependencies, will be merged into the output assembly.
Public members exposed by this package will be converted into internal members, preventing them from being accessed by outside callers.
