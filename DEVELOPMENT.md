# Development and Release Process

Stable updates contain completed, tested Workbench changes. Explicitly authorized tester previews follow the exception below.

## Owner-authorized tester preview exception — 2026-10-03

The owner explicitly authorized publishing the current editor for external testing,
bug reports and suggestions before interactive acceptance and independent audit
are complete. Preview branches may publish a Release-buildable source snapshot
with updated guide/changelog and explicit known unverified workflows. This is a
tester preview, not a completed stable update. Never bundle private assets, maps,
credentials, generated output or local handoffs. Stable/main promotion retains
the behavior-testing requirements below. No server deployment is authorized by
publishing this editor. See docs/TESTING_PREVIEW.md for this snapshot's gates.

## Local workflow

1. Make a focused change.
2. Update `docs/USER_GUIDE.md` when user-facing behavior changes.
3. Build the Release configuration:

   ```powershell
   dotnet build AstoniaMapWorkbench.csproj --configuration Release
   ```

4. Test the changed workflow in the Workbench. For map/rendering changes, record the map line, area, coordinates or sprite IDs, and the expected result.
5. Run `git diff --check` and confirm no `bin/`, `obj/`, credentials, maps, runtime logs, or private client/server assets are staged.
6. Commit a focused change and push it to `main` only after steps 1-5 pass.

The copy of `docs/USER_GUIDE.md` on GitHub is the canonical public guide. Do not leave user-facing documentation only in a local checkout or in the AU server repository.

## What counts as tested

Compilation alone is not enough for a user-facing editor change. A completed update must have:

- a successful Release build;
- a behavior check for the changed control or renderer;
- documentation updated when the workflow changed;
- no known blocker left unstated.

Visual/gameplay validation remains separate from compilation. A map editor change can be technically built while still needing owner testing in the client/server runtime.

## Push commands

From this repository:

```powershell
dotnet build AstoniaMapWorkbench.csproj --configuration Release
git diff --check
git status
git add .
git commit -m "map: describe the completed change"
git push origin main
```

Do not push unfinished experiments. Keep them local or in a branch until the behavior is tested and documented.

## Server 3.5 lane

The Workbench currently loads, edits, and saves the shared core `.map` directives used by Server 3.0S and 3.5S, and can generate a map-flag profile from either checkout. Full 3.5 compatibility is still unverified until its client assets, template differences, and runtime behavior have been tested. Keep those claims separate when documenting or releasing changes.