# Publishing

The Workbench is intentionally separate from the Astonia Uncharted server repository. Publish this directory as its own GitHub repository and link it from AU documentation.

## One-time publication

Install and authenticate GitHub CLI, then run from this directory:

```powershell
gh auth login
gh repo create AstoniaMapWorkbench --public --description "Astonia map editor and review tool for Server 3, with a shared Server 3.5 compatibility lane" --source . --remote origin --push
```

This creates the repository under the authenticated GitHub account. No Astonia credentials or server checkout access are required by the editor repository.

## Future fixes

For each fix:

```powershell
dotnet build AstoniaMapWorkbench.csproj --configuration Release
git add .
git commit -m "map: describe the focused fix"
git push
```

GitHub Actions then builds every push and pull request. Open an Issue for feedback and reference the issue in the commit or pull request when applicable.

The repository now follows the completed-and-tested rule described in [DEVELOPMENT.md](DEVELOPMENT.md). Push only after the Release build and the relevant Workbench behavior check pass.

Do not commit `bin/`, `obj/`, maps copied from a private deployment, credentials, runtime logs, database dumps, or proprietary client/server assets.