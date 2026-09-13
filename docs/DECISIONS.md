# Decisions

## Sprite preview source

The Workbench decodes the legacy editor's `pak` directory in read-only mode. It does not copy, modify, or publish sprite assets. The visual preview is a design aid; final runtime appearance must still be checked in the target community client and staging server.

## Map safety

Maps containing comments or directives the Workbench cannot preserve remain inspection-only. Workspace saves and review manifests do not upload a map or restart a server.

## Server and client boundary

The Workbench edits staged Server 3 `.map` files and reads nearby `.chr`/`.itm` template names; it does not connect to an area-server socket. Server 3 loads those files once during zone-process startup. The client receives a runtime map window containing sprites, occupancy, effects, and flags through the binary protocol, not the authoring directives. Therefore a saved map must be reviewed in the target `zones/<area>/` checkout and the affected area process restarted before the client can see it. Cross-area travel is a server redirect and reconnect, not shared live map state.

The Workbench follows the server loader for `origin`, field reset, and `from`/`to` rectangle expansion, then writes explicit `field=` records. It never authors runtime-managed flags such as `MF_TMOVEBLOCK`, `MF_TSIGHTBLOCK`, `MF_TSOUNDBLOCK`, or `MF_DOOR`.

## Canvas interaction

The map viewport uses a left click for tile selection and a left click-drag for panning; middle-drag remains supported. Ctrl-clicking two corners selects a rectangular section. Shift-clicking selects every tile containing the clicked tile's preferred foreground sprite, which is intended for wall and collision-flag workflows. Applying an edit uses the explicit multi-selection when more than one tile is selected, otherwise it uses the X/Y/width/height rectangle controls.
