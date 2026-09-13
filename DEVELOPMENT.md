# Development and Release Process

The public repository contains only completed, tested Workbench updates.

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

Server 3.5 compatibility work stays in this repository under a separate adapter/profile lane. It must not be called supported until representative 3.5 maps open, validate, save, and are tested in the 3.5 runtime.