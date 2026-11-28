# Whole World Realism Preview

Branch: `feature/full-world-realism-preview`

This is a combined preview branch containing the Reception Realism Preview, the Secret Thief Basement and interactive office doors, plus the whole-world realism pass. It preserves the latest `Office_Heist.unity` scene update from the reception branch. The original source branches are left untouched; this combined work is isolated from `main`.

## Before running

1. In GitHub Desktop, switch to `feature/full-world-realism-preview` and Pull.
2. Open Unity 6.3 and load `Assets/Scenes/Office_Heist.unity`.
3. Make one duplicate copy of `Office_Heist` before the first run.
4. Let Unity finish script compilation.
6. Make sure `OfficeHeist_Generated` (or legacy `OfficeEnvironment_Generated`) and `OfficeCompoundExpansion_Generated` exist in the active scene. If the main office is missing, use **Tools > Gardien Furtif > Office Heist > Build or Rebuild Level**. This branch's main office builder creates interactive office doors. If the compound is missing, use **Gardien Furtif > Environment > Complete Existing Office Compound**.

## Recommended build order

1. If the outdoor compound is missing, run **Gardien Furtif > Environment > Complete Existing Office Compound**.
2. To add the thief's underground hideout, run **Tools > Gardien Furtif > Secret Basement > 1. Build Preview**. The hideout is placed under the side Workshop building; the main-office staircase is restored.
3. For older static office doors, run **Tools > Gardien Furtif > Secret Basement > 2. Upgrade Existing Office Doors**. Newly generated main offices already have interactive doors.
4. For the detailed reception furniture and finishes, run **Tools > Gardien Furtif > Reception Realism > 1. Build Reception Preview**.
5. Run **Tools > Gardien Furtif > World Realism > 1. Restore Original Door Colors** if you only want the door repair, or go straight to the full preview (it restores the original door materials automatically).
6. Run **Tools > Gardien Furtif > World Realism > 2. Build Whole World Realism Preview**.
7. If the Mission UI needs to be regenerated, run **Tools > Gardien Furtif > Mission > 2. Build / Update Mission UI**. This rebuilds the UI Canvas and saves the MissionUI prefab.

## Menus

- **Tools > Gardien Furtif > World Realism > 1. Restore Original Door Colors**
- **Tools > Gardien Furtif > World Realism > 2. Build Whole World Realism Preview**
- **Tools > Gardien Furtif > World Realism > 3. Remove Preview & Restore Materials**
- **Tools > Gardien Furtif > Secret Basement > 3. Remove Preview & Restore Office**
- **Tools > Gardien Furtif > Secret Basement > 4. Restore Original Office Doors**
- **Tools > Gardien Furtif > Reception Realism > 2. Remove Preview & Restore Original**

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

## Mission guide and results UI

- The guide character appears in a large lower-left speech panel with the original thief illustration and a readable typewriter message.
- Objective rows, loot text, interaction prompts and feedback text have been enlarged.
- The interaction prompt is a dark amber oval with a high-contrast outline and is kept above the dialogue panel.
- The win/loss screen uses a large orange/gold round medallion, a large loot-value score, three-star rating, objective stats and a character illustration. The thief appears on success; the eccentric villain appears on failure.
- Original character art is loaded from `Assets/Resources/GardienFurtif/UI/`. Unity imports these PNGs as Resources textures; the icon factory creates runtime Sprites from them.
- The result screen pauses gameplay and the Retry button / `R` key restarts `Office_Heist`. The scene is now listed in Build Settings so replay has a valid build index.
- No separate Main Menu or next-level scene exists in the current project, so the result screen deliberately does not show non-functional Menu/Next buttons.

## Gameplay isolation

- Cars, pedestrians and animals are generated only under `WorldRealism_PREVIEW` and only at outer-road/courtyard coordinates.
- Ambient actors have no colliders, rigidbodies, NavMeshAgents, tags, audio emitters or gameplay scripts; movement is a lightweight waypoint animation.
- The world realism pass does not change the Player, Mission System, Guard AI, patrol points, spawn points, door interaction logic or objective triggers. It only restores the original wood material on the office door panels and frames.
- The underground hideout is a separate preview below `05 - Workshop`; it uses a hinged trapdoor with the existing `OfficeDoorController` interaction. The main office stairwell opening is no longer used.
- Door renderers are excluded from the general material remapping. Original dark-walnut material is restored to the office door panels, leaves, jambs, header trims and wooden insets; separate metal handles keep their metal finish. The preview-removal command preserves the door-color correction.
- Original shared materials for other remapped renderers are captured and restored when the preview is removed.

## Revert and validation

Use **World Realism > 3. Remove Preview & Restore Materials** to remove the world-only geometry/actors and restore the material assignments captured before the preview. The explicit correction to original door colors is kept. To remove the other previews too, use **Reception Realism > 2. Remove Preview & Restore Original** for the reception, and **Secret Basement > 3. Remove Preview & Restore Office** for the basement.

This is procedural geometry and procedural textures, not imported photorealistic asset packs or scanned 3D people/vehicles/animals. Unity runtime, lighting appearance, transparent glass, character facing, path clearance and frame rate still need testing in the actual scene. No Unity runtime test was run from this branch creation.

Before rebuilding either generated building/compound root, remove the world preview first so its separate decorative objects do not remain over the rebuilt architecture.

After building, save with Ctrl+S. Enter Play mode to check traffic, pedestrian and animal motion, verify that all of them remain outside the main office, and inspect the Console for compilation/runtime errors.
