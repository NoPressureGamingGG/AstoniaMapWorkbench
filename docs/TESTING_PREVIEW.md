# Tester preview — 2026-10-13

This preview is for collecting tester feedback on the map editor. It includes
multi-map tabs and copy/paste, tile stamping, direct delete/clear actions,
the tutorial, NPC draft form, sprite browser, wall-family filtering, depth
sorting, and Server 3.5S core map loading/profile support. The local Release
build and self-contained win-x64 publish passed with zero warnings and errors.
All 30 maps in the available 3.5S checkout parsed, and one map passed a
save/reopen check. Confirm the GitHub Actions build succeeds before downloading.
3.5 client assets, templates, and runtime behavior are not certified. No server
deployment; no private client/server assets or maps bundled.

## Obtain and report

Use this preview branch's GitHub Actions self-contained win-x64 artifact once CI
passes; extract and run AstoniaMapWorkbench.exe. CI success must be checked rather
than assumed. Existing art/template inputs are still needed for relevant previews.
Use GitHub Issues and include this commit/build, Windows/display details, steps,
expected/actual behavior, relevant sprite IDs/map coordinates and screenshots.
Do not attach private assets, credentials or database/runtime data.

## Focused checks remaining

- Open two maps at once; try region and sprite copy/paste across tabs, including blank source tiles, and verify Undo.
- Try Ctrl+Shift+C/V tile stamping and Brush left-click/drag; confirm one stroke is undoable.
- Select tiles and try Delete sprites, Remove NPC and Clear flags. Confirm unrelated tile data remains and Undo restores the edit.
- Try Ctrl-drag selection and the cut-wall preview on foreground walls; compare with the client where available.
- Tutorial first-run display, Back/Next, contextual actions, Skip persistence and Help re-entry.
- Visible ID/range input; browse typed IDs and selected tiles, including an 8x3 selection; selecting a thumbnail must not paint until explicit Apply.
- Four-layer details and 21006/21007 cut-preview behavior: ground sprites do not use foreground cut rules; Workbench preview is not proof of exact live client behavior.
- Depth ordering of foreground, items and characters; NPC draft sprite/help/save workflow.
- Validate, save to a separate test copy, reopen and compare intended edits. Never use a sole copy of valuable map work to test a preview.
- Open a Server 3.5S map; confirm the status identifies the 3.5S profile, then save a separate copy and reopen it. Runtime/client compatibility is outside this preview's verification.

No claim that every check above passed. Known pending audit findings must be
recorded before stable promotion. Updater/API installation design is only a
proposal; no updater added in this snapshot. Main/stable release is separate.