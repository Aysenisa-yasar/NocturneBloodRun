# Nocturne Blood Run

Nocturne Blood Run is a Unity 2022.3 project built around a dark village escape scenario with two playable survivors, one pursuing monster, collectible gold, unlockable weapons, and a ready-to-run Windows build.

## Project structure

- `Assets/`: scenes, scripts, prefabs, models, animations, materials, and gameplay effects
- `Packages/`: Unity package manifest and lockfile
- `ProjectSettings/`: Unity project configuration
- `Builds/Windows/`: packaged Windows build
- `Docs/Reports/`: submitted project report files
- `Docs/ReportAssets/`: diagrams and assets used in the report
- `Docs/ReportRender/`: rendered report preview outputs

## Open the project

1. Open the folder in Unity Hub.
2. Use Unity `2022.3.x`.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press Play to run the project inside the editor.

## Controls

- `Remy`: `W`, `A`, `S`, `D` and `Left Shift`
- `Peasant Girl`: `Arrow Keys` and `Right Ctrl`

## Gameplay

- Start the scenario from the in-game UI.
- Move both survivors through the village and forest.
- Collect gold to raise the team score.
- Unlock weapons after reaching the required score.
- Defeat the monster or reach safety before it catches a survivor.

## Notes

- Unity-generated solution and project files are intentionally excluded from Git.
- The Windows executable build is kept under `Builds/Windows/`.
- The report generation script now reads assets from `Docs/ReportAssets/`.
