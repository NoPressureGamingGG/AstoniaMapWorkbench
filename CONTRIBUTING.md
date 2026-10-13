# Contributing

Small fixes and usability improvements are welcome through GitHub Issues and pull requests.

The repository's release rule is simple: `main` contains completed, tested updates. Use a branch or draft pull request for work still in progress. See [DEVELOPMENT.md](DEVELOPMENT.md) for the required build, behavior check, documentation, and clean-diff steps before pushing.

Before opening a pull request:

1. Keep changes focused and explain the user-facing behavior.
2. Do not commit `bin/`, `obj/`, generated map outputs, credentials, database dumps, runtime logs, or client/server checkouts.
3. Run `dotnet build AstoniaMapWorkbench.csproj --configuration Release`.
4. Update `docs/USER_GUIDE.md` when controls or behavior change.
5. Include a reproduction map description and screenshots for rendering changes.

The shared core `.map` directives and profile generation are supported for Server 3.5S. Other 3.5 behavior (including client assets, template differences, and runtime compatibility) is not fully verified; test those separately and state their limits clearly.