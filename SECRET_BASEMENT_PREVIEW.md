# Secret Workshop Basement Preview

Branch: `feature/full-world-realism-preview`

The thief's hideout is placed beneath the **05 - Workshop** side building in `OfficeCompoundExpansion_Generated`. It no longer opens the ground floor or changes the staircase of the main `OfficeHeist_Generated` building.

## Before building

1. In GitHub Desktop, switch to `feature/full-world-realism-preview` and Pull.
2. Open Unity 6.3 and load `Assets/Scenes/Office_Heist.unity`.
3. Duplicate the scene first so the preview can be reviewed separately.
4. Wait for Unity to finish script compilation.
5. Make sure `OfficeCompoundExpansion_Generated` exists in the active scene. If it does not, run **Gardien Furtif > Environment > Complete Existing Office Compound**.

## Build the basement

Run:

`Tools > Gardien Furtif > Secret Basement > 1. Build Preview`

The builder:
- Places the thief hideout below the side Workshop building, not below the main office.
- Restores the main office slab, corridor/meeting floor finishes and original stair landing if an earlier main-office basement preview exists.
- Cuts a concealed opening in the Workshop floor and adds a hinged dark-walnut trapdoor.
- Adds a human-scale staircase, handrails and guide lights down into the hideout.
- Keeps the planning room, gear workshop, stolen-goods vault, escape tunnel, furniture, props and four loot collectibles.
- Uses a brighter slate/stone, warm oak, copper and amber palette while keeping the dark-walnut hatch/door finish.

## Controls

1. Enter Play mode.
2. Walk up to the closed Workshop floor hatch.
3. Aim the camera at the hatch panel. The door prompt should show `[E] OPEN`.
4. Press `E` to lift the hatch, descend the stairs and reach the hideout.
5. Aim at the hatch from nearby and press `E` again to close it.

The player needs a camera tagged `MainCamera`, and their collider should be on an object tagged `Player` or under a `CharacterController` tagged `Player`. Existing office doors continue to use the same `E` interaction.

## Remove and restore

Run:

`Tools > Gardien Furtif > Secret Basement > 3. Remove Preview & Restore Office`

This removes the Workshop basement/trapdoor, re-enables the original Workshop floor, and restores the main office staircase/floor if a legacy main-office basement preview was present. It also restores previously converted office door leaves.

## Validation

The new geometry and hatch controller are committed but still need to be tested in the Unity Editor/Play mode. Check the stair clearance, player fit, hatch trigger/raycast, and lighting. If guards need to patrol the basement after approval, rebake/update the NavMesh for the new level.
