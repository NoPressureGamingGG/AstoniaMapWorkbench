# Astonia Map Workbench

**Tester preview — 2026-10-13.** This branch contains work awaiting broader interactive acceptance and independent audit. Use copied/staged maps, report issues through GitHub Issues, and read [preview test scope](docs/TESTING_PREVIEW.md). The Server 3.5S core map format is tested; other 3.5 runtime and asset behavior is not certified.

A local Windows map editor and review tool for Astonia-compatible Server 3 map data.

The Workbench is designed to be useful without access to a live server. It opens staged `.map` files, reads nearby `.chr`/`.itm` templates, previews compatible client art, validates authoring data, and saves reviewable workspace outputs.

## Status

- **Server 3.0S:** active development target.
- **Server 3.5S:** map loading/editing for the shared core `.map` directives and checkout-specific map-flag profiles are supported. Client-art rendering, template differences, and runtime behavior are not fully verified.
- **Live server changes:** never performed automatically.

The current editor can be tested from the Release build after running:

```powershell
dotnet build AstoniaMapWorkbench.csproj --configuration Release
```

Output: `bin/Release/net9.0-windows/AstoniaMapWorkbench.exe`

For testers, use the GitHub Actions artifact or a GitHub Release marked `win-x64`. Those builds are self-contained and include the .NET runtime; testers do not need to install .NET 9 separately. Unzip the package and launch `AstoniaMapWorkbench.exe`.

## Safety boundary

The Workbench does not connect to an area server, upload maps, restart processes, or modify a canonical Server checkout automatically. Review the generated map diff, promote it through the server project's normal workflow, restart the affected area, and test in staging.

## Feedback

Please use GitHub Issues for bugs, rendering differences, feature requests, and usability feedback. Include:

- Workbench version or commit;
- Windows version and monitor layout if the issue is visual/UI;
- target line: Server 3 or Server 3.5;
- map path relative to the server checkout and area ID;
- sprite IDs, template names, or coordinates involved;
- screenshots and the Validation output when relevant;
- exact reproduction steps.

Do not attach credentials, database dumps, runtime logs containing private data, or entire proprietary server/client checkouts.

## Compatibility roadmap

Server 3.0S and 3.5S map files use the same core `.map` directives in their current server sources, and the Workbench loads those maps and generates profiles from either checkout layout. Broader 3.5 compatibility still requires verifying its client assets, templates, and runtime-specific behavior before those features are advertised as supported.

The [user guide](docs/USER_GUIDE.md) in this repository is the canonical, publicly updated Workbench guide. Every user-facing control or workflow change must update it in the same commit as the code.