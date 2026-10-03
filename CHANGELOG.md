# Changelog

## Unreleased

- Add a skippable first-run map-editing tutorial, reopenable from Help.
- Make sprite ID search input explicit and show per-layer sprite/cut-variant IDs; clarify that client F8 disables automatic cut walls.
- Add direct map-search-to-browser shortcuts and recognize the 21000-21028 wall family in wall visibility filtering.
- Keep sprite inspection separate from painting so choosing a thumbnail or target cannot silently set an active brush.
- Add exact selection-region sprite browsing and page gallery thumbnails to avoid large-catalog UI overload.
- Add a persistent File > Open recent maps submenu for the last three maps.
- Add an editable start/end sprite range with a +500 default, Enter-to-browse, a 2,000-ID cap, and selected-sprite preview beside Recent 5.
- Match the client's depth sorting for foreground sprites, items, and characters in the map canvas.
- Add a beginner map-edit tutorial covering sprite layers, tile placement, flags, and safe saving.
- Add a searchable character-sprite picker with native, pixel-art 2x, and smooth 2x preview modes.
- Keep the selected NPC sprite thumbnail and ID visible in the draft form, and fix the template dropdown layout.
- Add smaller circular NPC draft help markers with whole-icon hover targets and faster tooltips.
- Show the catalogued Server 3.0S driver description beside the NPC draft Driver ID.
- Add selection/layer summaries, field-level edit previews, explicit confirmation for multi-tile and Whole tile edits, and safe Flags-only defaults while keeping all mutations undoable.
- Fix focused-control arrow navigation, same-layer Shift selection, wrapped right tabs, template/sprite double-click navigation, and client cut-metadata coverage for dungeon walls.
- Render item/NPC template previews for Item-only/NPC-only operations and align the active build with the published standalone source copy.
- Make Sprite Browser thumbnails placement-ready with explicit Ground 1/2 and Wall/Ceiling 1/2 targets instead of invoking map-search validation.
- Add Map Ground 1/2 browser sources and Free draw preview mode with per-tile undo entries.
- Fix Free Draw and click-to-place previews not repainting the canvas after their temporary tile state changed.
- Add no-ID Sprite Browser sources for map walls/foreground, all map sprites, and the current client art catalog.
- Add an NPC build-draft artifact form for engineer handoff without writing server `.chr` files.
- Fix Free Draw rectangle selection overwriting the chosen brush with the sprite from the drag-start tile.
- Fix Free Draw preview reading the ordinary selection instead of the accumulated stroke, and report committed tile count.
- Keep browser click-to-place and Free Draw previews persistent across multiple clicks and rectangle sweeps until Apply.
- Commit the exact preview snapshots on Apply so painted sprites remain in the map after the pending preview is cleared.
- Add persistent Working group and Recent 5 sprite lists, thumbnail drag/drop, and click-to-place previews for the selected layer.
- Repopulate the Sprite Browser gallery when Working group or Recent 5 is selected, and wrap the browser toolbar on narrow panels.
- Keep sprite selection, layer choice, Free draw, and Apply preview together in the Sprite Browser tab with highlighted thumbnails.
- Fix Ctrl-drag selections outside the map bounds causing a NumericUpDown crash from negative coordinates.

- Render map ground and foreground layers from the read-only legacy Astonia `.pak` sprite archive.
- Keep movement, sight, and sound blockers as an optional diagnostic overlay rather than the primary map view.
- Keep the isometric viewport stable while selecting tiles, show an editing grid, and support arrow-key navigation plus tile copy/paste with undo.
- Match Server 3 `origin`, field-reset, and `from`/`to` map semantics; recognize template labels with inline comments.
- Make the map canvas resizable through persistent splitters, support left-drag panning and deeper zoom-out, and add Ctrl rectangle selection plus Shift foreground-sprite matching selection.
- Add a visual Sprite browser tab for scanning legacy archive ID ranges and sending a chosen sprite into map search.
- Preview the Server 3 extended wall family `59155`-`59158` through client-compatible base sprites when its raw archive block is absent, and make the grid overlay optional.
- Load current Uncharted `gx1_mod.zip`/`gx1_patch.zip`/`gx1.zip` sprite assets before legacy pak fallback, and report unresolved sprite IDs in the visible Validation tab without obscuring the map canvas.
- Match the current client PNG renderer by treating magenta as transparent, cropping transparent borders, and applying centered sprite offsets; this prevents pink sprite boxes and misplaced opaque bars.
- Add portable `.mapregion.json` copy/paste for all four map layers, flags, and template references, plus one-step undoable clearing of large selections.
- Add independent preview toggles for walls/ceilings and NPCs; resolve map `ch=` entries from `.chr` template sprite IDs.
- Match the client's F8 `nocut` view with a separate Lower walls toggle, and resolve NPC variants to client idle frames instead of raw character IDs.
- Add the canonical [Workbench user guide](docs/USER_GUIDE.md), including profile meaning, mixed static flags, clearing operations, and safe deployment steps.
