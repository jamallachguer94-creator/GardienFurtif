# Secret Thief Basement Preview

This is an optional, reversible preview for the `Office_Heist` scene.

## Branch

Use `feature/secret-thief-basement`. It is based on the latest `feature/player-gameplay` commit and does not modify `main` or the original gameplay branch.

## Before building

1. In GitHub Desktop, switch to `feature/secret-thief-basement` and pull the latest changes.
2. Open the project in Unity and wait for scripts to compile.
3. Open `Assets/Scenes/Office_Heist.unity`.
4. For a clean visual review, duplicate the scene in the Project window and open the duplicate. Keep the original scene untouched.
5. Confirm the scene contains `OfficeHeist_Generated`. If it does not, run `Tools > Gardien Furtif > Office Heist > Build or Rebuild Level` first.

## Build the preview

Run:

`Tools > Gardien Furtif > Secret Basement > 1. Build Preview`

The builder adds a basement hideout below the office, extends the current staircase down, opens a reversible stairwell in the ground-floor slab, and adds:

- Thief planning room with a planning table, heist map, monitors, laptop, documents and seating.
- Gear workshop with workbench, lockers, tool board and storage crates.
- Stolen-goods vault with shelves, safe and display area.
- Escape tunnel with lighting and an exterior exit approach.
- Basement wall/floor materials, copper accents, warm/cool lights and decorative details.
- Four collectible items: hidden cash, encrypted laptop, gold bars and black briefcase.

The preview also upgrades the existing office doors to interactive hinged doors.

## Controls

- Approach a door, aim the camera at its panel and press `E` to open or close it.
- Approach a collectible and press `E` to collect it through the existing Mission interaction system.
- Existing player controls remain unchanged.

The door interaction radius is approximately 3 metres. The player must use a camera tagged `MainCamera`, and the player object should retain its `Player` tag and `CharacterController`.

## Door-only actions

- `Tools > Gardien Furtif > Secret Basement > 2. Upgrade Existing Office Doors`
- `Tools > Gardien Furtif > Secret Basement > 4. Restore Original Office Doors`

The original door leaves are preserved as inactive backups when upgraded.

## Undo the preview

Run:

`Tools > Gardien Furtif > Secret Basement > 3. Remove Preview & Restore Office`

This removes the generated basement, restores the original ground-floor slab, corridor and meeting-room floor finishes, restores the old staircase landing, restores original office doors, and deletes the preview material folder.

## Validation note

The files have been committed, but the generated geometry and runtime interactions still need to be opened and tested in Unity. If guards should patrol the basement, rebuild/bake the NavMesh for the new floor after the visual layout is approved.
