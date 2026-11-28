# Reception Realism Preview

This is a reversible visual upgrade for the **main entrance reception** in the Unity scene `Office_Heist`.

## Branch

`feature/reception-realism-preview`

It is based on `feature/player-gameplay`. It does not modify `main`, `feature/player-gameplay`, or the separate `feature/secret-thief-basement` branch.

## Build it in Unity

1. In GitHub Desktop, switch to `feature/reception-realism-preview` and Pull.
2. Open Unity 6.3 and load `Assets/Scenes/Office_Heist.unity`.
3. Let scripts compile. Make sure the existing `OfficeHeist_Generated` building root is in the scene. If it is missing, run the original Office Heist level builder first.
4. Use **Tools > Gardien Furtif > Reception Realism > 1. Build Reception Preview**.

## What changes

- Replaces only `OfficeHeist_Generated/Furniture/Reception` while the preview is active.
- Adds a layered walnut-and-stone reception counter, brass trims, fluted front, two detailed computer stations, keyboards, desk phone, visitor forms, pen cup and task lamp.
- Rebuilds the waiting lounge with upholstery, cushions, glass/brass coffee table, rug and planters, and restores a detailed visitor self-service kiosk.
- Adds reception wall panels, signage, blinds, clock, noticeboard, extinguisher, waste bin, brochure stand, ceiling grid and accent lighting.
- Uses generated Unity materials and textures. It does not require external downloads or third-party assets.

## Revert it

Use **Tools > Gardien Furtif > Reception Realism > 2. Remove Preview & Restore Original**.

The original Reception group is moved into a disabled backup under `Reception_Realism_PREVIEW` while the preview is active, then restored by the remove command. The generated material assets remain under `Assets/Generated/ReceptionRealism/Materials` so a later preview build can reuse them.

## Scope and validation

- Does not intentionally change scene architecture, doors, Mission System, Player Controller, guard AI, spawn markers or patrol waypoints.
- Replaces the reception furniture colliders with similarly sized counter, sofa, chair, table and planter colliders to preserve the existing gameplay layout.
- This is a procedural visual upgrade, not a photogrammetry/asset-pack replacement. The scene must still be opened in Unity and checked in Play mode for final visual quality, collision clearance and NavMesh effects. No Unity runtime test has been run from this commit.
- Duplicate `Office_Heist` before the first preview run for an extra safety copy.
