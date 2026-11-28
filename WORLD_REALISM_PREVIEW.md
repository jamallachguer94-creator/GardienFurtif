# Whole World Realism Preview

Branch: `feature/full-world-realism-preview`

This branch is based on `feature/reception-realism-preview`, so it keeps the separate Reception Realism Preview work. It is an isolated preview branch; it does not modify `main`, `feature/player-gameplay`, or the existing `feature/secret-thief-basement` branch.

## Before running

1. In GitHub Desktop, switch to `feature/full-world-realism-preview` and Pull.
2. Open Unity 6.3 and load `Assets/Scenes/Office_Heist.unity`.
3. Make a duplicate copy of the scene before the first run.
4. Let Unity finish script compilation.
5. Make sure `OfficeHeist_Generated` (or legacy `OfficeEnvironment_Generated`) and `OfficeCompoundExpansion_Generated` exist in the active scene. If the main office is missing, use **Tools > Gardien Furtif > Office Heist > Build or Rebuild Level**. If the compound is missing, use **Gardien Furtif > Environment > Complete Existing Office Compound**.

## Menus

- **Tools > Gardien Furtif > World Realism > 1. Restore Original Door Colors**
- **Tools > Gardien Furtif > World Realism > 2. Build Whole World Realism Preview**
- **Tools > Gardien Furtif > World Realism > 3. Remove Preview & Restore Materials**

Run the door-color command first if you only want to repair the existing scene. The build command also restores original builder door/frame materials before applying the realism pass.

## Preview contents

### Main office
- Procedural plaster, stone, concrete, timber, metal, fabric and glass material palette with generated albedo/normal textures.
- Exterior identity sign, window sills, corner reveals and roofline trims across the south, north, east and west elevations.
- Room plaques for reception, security, archive, service, stairs, workspace, meeting, director, document archive, server room and offices.
- Framed wall-art pieces on selected blank wall spans, including the reception area.

### Existing compound
- Keeps the existing low-rise buildings, roads, gatehouses, fence, parking, trees and courtyard layout.
- Adds readable signs for the existing annex buildings, entry awnings and downlights.
- Adds wayfinding totems, benches, litter bins and planters in the outdoor courtyards.
- Adds four procedural moving cars on the outer road network, with ground-level wheels that rotate as they travel.
- Adds four low-detail ambient pedestrians on outside paths/courtyards.
- Adds one dog and one cat walking only in outside courtyard routes.

## Gameplay isolation

- Cars, pedestrians and animals are generated only under `WorldRealism_PREVIEW` and only at outer-road/courtyard coordinates.
- Ambient actors have no colliders, rigidbodies, NavMeshAgents, tags, audio emitters or gameplay scripts; movement is a lightweight waypoint animation.
- The main Player, Mission System, Guard AI, patrol points, spawn points, interior doors and objective triggers are not intentionally modified.
- Door renderers are excluded from the general material remapping. Original door materials are restored using the original generated material assets; the preview-removal command preserves that door-color correction.
- Original shared materials for other remapped renderers are captured and restored when the preview is removed.

## Revert and validation

Use **Remove Preview & Restore Materials** to remove all added geometry/actors and restore the material assignments captured before the preview. The explicit correction to original door colors is kept.

This is procedural geometry and procedural textures, not imported photorealistic asset packs or scanned 3D people/vehicles/animals. Unity runtime, lighting appearance, transparent glass, character facing, path clearance and frame rate still need testing in the actual scene. No Unity runtime test was run from this branch creation.

After building, save with Ctrl+S. Enter Play mode to check traffic, pedestrian and animal motion, verify that all of them remain outside the main office, and inspect the Console for compilation/runtime errors.
