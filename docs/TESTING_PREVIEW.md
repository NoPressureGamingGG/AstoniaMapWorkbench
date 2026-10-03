# Tester preview — 2026-10-03

Owner authorized this preview so testers can find bugs and suggest improvements.
Source baseline48326aa plus current tutorial, NPC draft form, sprite browser,
wall-family filtering and depth sorting. Local Release build passed with0warnings
and0errors. Independent audit is in progress; interactive acceptance is incomplete.
No server deployment; no private client/server assets or maps bundled.

## Obtain and report

Use this preview branch's GitHub Actions self-contained win-x64 artifact once CI
passes; extract and run AstoniaMapWorkbench.exe. CI success must be checked rather
than assumed. Existing art/template inputs are still needed for relevant previews.
Use GitHub Issues and include this commit/build, Windows/display details, steps,
expected/actual behavior, relevant sprite IDs/map coordinates and screenshots.
Do not attach private assets, credentials or database/runtime data.

## Focused checks remaining

- Tutorial first-run display, Back/Next, contextual actions, Skip persistence and Help re-entry.
- Visible ID/range input; browse typed IDs and selected tiles, including8x3; selecting a thumbnail must not paint until explicit Apply.
- Four-layer details and21006/21007 cut-preview behavior: ground sprites do not use foreground cut rules; Workbench preview is not proof of exact live client behavior.
- Depth ordering of foreground, items and characters; NPC draft sprite/help/save workflow.
- Validate, save to a separate test copy, reopen and compare intended edits; Undo/Redo. Never use a sole copy of valuable map work to test a preview.

No claim that every check above passed. Known pending audit findings must be
recorded before stable promotion. Updater/API installation design is only a
proposal; no updater added in this snapshot. Main/stable release is separate.