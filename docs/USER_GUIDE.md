# Astonia Map Workbench User Guide

The Workbench is a local Windows editor for Server 3 map files. It reads map data and legacy sprite art for editing and review. It does not connect to a running area server, upload files, restart processes, or change the live client.

## Launch

Run `AstoniaMapWorkbench.exe` from the Release output:

`bin/Release/net9.0-windows/AstoniaMapWorkbench.exe`

The editor remembers its window size, panel splitter positions, and zoom level in:

`%LOCALAPPDATA%/AstoniaMapWorkbench/layout.json`

The editor launches maximized on the monitor containing the mouse pointer. This prevents a saved window rectangle from opening between dual monitors. Panel splitter positions and zoom are still restored.

## Open a map

1. Choose **File > Open map...**.
2. Select a Server 3 `.map` file, normally under `zones/<area>/`.
3. The editor loads nearby `.chr` and `.itm` template names for validation.
4. If the map is inside a Server 3 checkout, the editor attempts to detect that checkout and build a profile automatically.

Maps containing comments or directives that the editor cannot preserve open in **INSPECTION ONLY** mode. Use **Save verbatim workspace copy...** for a safe copy. Do not deploy an inspection-only map after editing it outside the Workbench without reviewing the original grammar.

### Current versus legacy files

The Workbench is stored under `Legacy Source CODE` for historical/project-organization reasons. That location does not decide which map is edited. The map source is shown in the status bar after opening:

- **CURRENT SERVER 3:** `AU 3.0S/astonia_community_server3-main/zones/<area>/...` and the recommended authoring source;
- **LEGACY EDITOR DATA:** `Legacy Source CODE/Astonia3 Map Editor/Astonia3 Editor/zones/...`, useful for comparison only;
- **EXTERNAL MAP:** any other path, which requires an explicit review before deployment.

The Open dialog starts in the current Server 3 `zones` folder when this workspace path exists. The default `pak` folder is legacy art data only; it supplies compatible sprite pixels and does not make the opened map a legacy map. Saving never updates either checkout automatically: save to a workspace path, review the diff, then deliberately promote it into the current Server 3 checkout.

## Server profile: what it means

A server profile is a local JSON description of the Server 3 map rules used for validation. It contains:

- the Server 3 checkout path and detected Git revision;
- supported map directives such as `field`, `origin`, `from`, `to`, `gsprite`, `fsprite`, `flag`, `ch`, and `it`;
- authorable static map flags;
- runtime-managed flags that should not be authored into a static map;
- the checkout's `zones` directory.

### Generate a profile

Choose **Server profile > Generate from Server 3 checkout...** and select the authoritative Server 3 repository containing `create.c`. The editor reads the `MF_tab` flag table from that checkout and records the current Git revision. Use this when the server source has changed or when starting work against a different checkout.

### Load a profile

Choose **Server profile > Load profile...** and select a previously saved `*.json` profile. Loading only changes the editor's validation rules and flag list. It does not connect to that server, read its live state, modify its files, or restart an area.

A profile can become stale if the Server 3 source changes. Generate a new profile from the authoritative checkout when the profile revision no longer matches the map's target server.

### Save a profile

Choose **Server profile > Save active profile...** to save the currently detected or loaded rules as JSON for reuse by another local Workbench session.

## Map canvas

- **Left click:** select one tile.
- **Left click-drag:** pan the map.
- **Middle-drag:** pan the map.
- **Mouse wheel or zoom slider:** zoom in or out. The slider can zoom farther out than the original editor.
- **Arrow keys:** move the current tile selection, even when a Tile editor control has focus.
- **Ctrl-drag:** draw a rectangular selection window. If the pointer reaches a canvas edge while dragging, the viewport auto-pans so the selection can continue beyond the visible area.
- **Shift-click:** select every tile containing the clicked tile's matching sprite on the same ground or foreground layer. It does not match unrelated layers.
- **Show walls / ceilings:** toggle foreground layers independently, similar to lowering walls for room inspection. This changes only the preview.
- **Lower walls (F8 view):** resolves current-client composite IDs before applying the cut-sprite transformation to eligible wall sprites while leaving tables, chairs, and other non-cut foreground objects visible. This is a preview equivalent of the in-game F8 command. Some walls intentionally have no cut variant and will remain full height.
- **Show NPCs:** toggle previews of `ch=` characters independently. NPCs are resolved from the `sprite=` value in their `.chr` template; this changes only the preview.

The map canvas uses the legacy isometric layer order: ground 1, ground 2, foreground 1, and foreground 2.

The Workbench also recognizes the Server 3 extended wall IDs `59155`-`59158` and previews them through their client base sprites `14030`-`14033` when the raw extended archive block is not present. The live client may apply additional color, lighting, animation, or metadata transformations that are not visible in a static Workbench preview.

## Editing a selection

The Tile editor applies values to the current selection. A multi-tile selection takes precedence over the X/Y/Width/Height rectangle controls. With one tile selected, the rectangle controls can be used to apply an edit to a manually entered rectangle.

### See and change flags

Select a tile or region. The **Active flags** box shows the flags currently present on the first selected tile. The **Static flags** checklist is the editing control:

- checked means add that flag to every selected tile;
- unchecked means remove it from every selected tile;
- indeterminate means preserve each selected tile's existing value.

Choose **Paint target > Flags only**, then click **Apply tile / rectangle**. The Validation tab will identify unknown or runtime-managed flags.

### Add an item or NPC

1. Select the destination tile, or Ctrl-drag a region.
2. Search in **Item and NPC templates** on the left.
3. Click an `item` or `npc` result. It fills the corresponding Tile editor field.
4. Choose **Paint target > Whole tile** and click **Apply tile / rectangle**.

The map stores the template name (`it=` or `ch=`), not a live runtime object. The destination area's `.itm`/`.chr` templates must contain that name. Use **Validate** before saving and deployment.

### Layer commands

The four sprite fields correspond directly to the Server 3 map layers:

- **Ground layer 1:** `gsprite` low 16 bits;
- **Ground layer 2:** `gsprite` high 16 bits;
- **Wall / ceiling layer 1:** `fsprite` low 16 bits;
- **Wall / ceiling layer 2:** `fsprite` high 16 bits.

Choose the layer in **Paint target**, enter the desired sprite ID, and apply it to the selection. The layer-specific commands change only that layer. **Whole tile** changes both sprite layers and the item/NPC references. The clear-layer commands remove only their named layer.

The editor starts in **Flags only** mode to prevent accidental sprite replacement. Use **Ground layer 1/2** to add ground, **Wall / ceiling layer 1/2** to add wall sections, **Item only** to place an item without changing sprites, and **NPC only** to place an NPC without changing sprites. **Whole tile (replace all)** is deliberately explicit and should be reserved for intentional full-tile replacement.

Clicking a template result or double-clicking a sprite returns to the Tile editor. **Preview changes** also returns to the Tile editor before showing its diff summary.

For Item-only and NPC-only edits, the preview also draws the selected template's client-resolved sprite on the selected tile(s). Cancelling the confirmation leaves the map unchanged and clears no undo history.

The Tile editor shows the current selection count and affected layer. **Preview changes** reports the operation, tile count, number of tiles that will change, and affected layer without modifying the map. Applying a multi-tile edit shows the same summary and asks for confirmation. Whole tile replacement always shows an explicit destructive warning. Undo and redo cover applied tile edits, region paste, region clearing, and tile paste; previewing or cancelling never creates an undo entry.

### Region copy/paste and large deletion

Use **Ctrl-drag** to select a rectangle, then:

- **Edit > Copy selected region to file...** saves a portable `.mapregion.json` artifact containing all four sprite layers, static flags, and item/NPC template names;
- open another map, select the destination origin tile, and use **Edit > Paste region from file...**;
- **Edit > Clear selected region** removes the selected forest, room, shop, or other large area as one undoable operation.

This supports a workflow such as copying Jeremy's shop to a region file, clearing or redesigning the original, and pasting it back or into another map. The paste copies template names, not live server character/item instances; the destination map must have those templates available and should be validated before deployment. Region files are intentionally plain JSON so they can be archived, reviewed, and reused outside the editor.

### Static flags

When multiple tiles are selected, the Static flags list uses three states:

- checked: every selected tile has the flag;
- unchecked: no selected tile has the flag;
- indeterminate: selected tiles have mixed values.

Applying an edit adds checked flags, removes unchecked flags, and preserves indeterminate flags per tile. This makes it safe to inspect mixed terrain before deciding whether to normalize it.

Runtime-managed flags such as `MF_TMOVEBLOCK`, `MF_TSIGHTBLOCK`, `MF_TSOUNDBLOCK`, and `MF_DOOR` are rejected by validation as static authoring targets. Use the underlying item or character flags that cause runtime behavior instead.

### Clearing data

Use the Paint target list and **Apply tile / rectangle** to clear selected content:

- **Clear whole tile:** removes both sprite layers, item/NPC template references, and static flags;
- **Clear ground layer 1/2:** removes only that ground sprite layer;
- **Clear wall / ceiling layer 1/2:** removes only that foreground sprite layer;
- **Clear flags:** removes static flags without changing sprites or template references.

All clearing operations are undoable.

## Sprite browser

The original Sprite search searches the opened map for tiles already using a sprite ID. For visual discovery, open the **Sprite browser** tab:

1. Enter one sprite ID or a range, for example `12000-12020`.
2. Choose **Browse archive**.
3. Review the decoded thumbnails from the legacy `pak` folder.
4. Click a thumbnail to search the opened map for all tiles using that sprite.

The browser reads the current Uncharted `gx1_mod.zip`, `gx1_patch.zip`, and `gx1.zip` archives when they are found beside this workspace, then falls back to compatible legacy pak art. If neither source is available, choose **Art assets > Choose compatible pak art folder...** and select the directory containing files such as `00000000.pak`.

The browser displays the decoded/base art available to the Workbench. Runtime composite or animated IDs may therefore show their base sprite rather than the final client-transformed frame. Unresolved art is omitted from the canvas and reported by **Validate**; the canvas does not draw warning squares over the map.

Clicking a browser thumbnail selects it for placement; it does not search the opened map. Choose **Ground layer 1**, **Ground layer 2**, **Wall / ceiling layer 1**, or **Wall / ceiling layer 2**, then click **Use selected sprite**. The sprite ID is placed in the corresponding Tile editor field and the matching Paint target is selected. Apply it to the current tile or selection from the Tile editor.

Use the browser source selector to browse an Archive ID/range or the sprites currently used by **Map Ground 1** or **Map Ground 2**. The source selector controls what is shown; the placement selector controls which map layer receives the chosen sprite.

The browser keeps a persistent **Working group** and **Recent 5** list in `%LOCALAPPDATA%/AstoniaMapWorkbench/sprite-workspace.json`. Drag a thumbnail into Working group, or use **Add selected**. Select a group/recent sprite to reuse it without searching again.

Selecting a sprite also enables **Click-to-place preview**. Click a map tile to preview the sprite there on the chosen layer, then use **Apply tile / rectangle** to commit it. Normal click placement is preview-only until Apply; Ctrl-drag and Shift-click remain available for selection workflows.

### Free draw preview

Enable **Free draw preview**, choose a sprite and placement layer, then Ctrl-drag across the map. The selected squares show the proposed sprite immediately, but the map is not changed until **Apply tile / rectangle** is confirmed. Each painted tile receives its own undo entry, so Ctrl+Z can step backward tile by tile. Turning Free draw off restores ordinary Ctrl-drag region selection.

Current ZIP PNGs use the same magenta-key transparency, transparent-border cropping, and centered anchor offsets as the Uncharted client. This keeps wall-mounted objects and ground artwork from displaying their source-image rectangles.

## Validation and saving

- **Validate:** switches to the Validation tab and checks parser findings, unsupported directives, unknown flags, runtime-managed flags, item/NPC template references, and every nonzero map sprite against current client ZIPs plus compatible pak art.
- **Save verbatim workspace copy...:** copies the original file exactly and never rewrites unknown content.
- **Save edited workspace map...:** writes a normalized explicit-field map and creates a `.bak` beside an existing destination. It refuses to rewrite inspection-only maps.
- **Export review manifest...:** creates a Markdown handoff stating the source map, profile revision, and required deployment review steps.

Always review the saved map diff before deployment. The Workbench does not replace the map in the canonical checkout automatically.

## Applying a map to Server 3

1. Save an edited workspace map to a separate workspace location.
2. Review validation findings and the exact file diff.
3. Copy or merge the approved map into the target `zones/<area>/` directory using the normal project workflow.
4. Restart only the affected area-server process. Server 3 loads `.map`, `.chr`, and `.itm` files once at process startup; it does not hot-reload them.
5. Connect the client to that area and perform the owner/runtime test.

A map file is not client data. The area server parses it into runtime tiles, and the client receives a reduced runtime map view through the binary protocol.

## Guide maintenance

This file is the canonical user-facing description of Workbench behavior. Any change to controls, selection, validation, profile handling, map saving, or deployment safety must update this guide and `CHANGELOG.md` in the same change. Do not describe a feature here until it has been built and its verification status is recorded.
