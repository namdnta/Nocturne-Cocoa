# M1 — Core Architecture

Status: implemented (pending first run in the Unity Editor).

## How it fits together

```
Bootstrap scene
└── GameSystems                (BootstrapLoader, DontDestroyOnLoad)
    ├── SceneFlowController    loads one gameplay scene at a time
    ├── SaveManager            reads/writes JSON save files
    ├── InputManager           Input System -> simple game events, Gameplay/UI modes
    ├── AudioManager           music crossfade, SFX pool, volume settings
    ├── GameSession            current play-through (SaveData), New/Continue/Save
    └── DevHotkeys             DEV ONLY test shortcuts
```

1. `BootstrapLoader.Awake` initializes the systems in a fixed order, passing dependencies in
   explicitly (`gameSession.Initialize(saveManager, sceneFlow)`), then registers them in `Services`.
2. `BootstrapLoader.Start` starts a new game (or resumes the autosave, if enabled) and loads
   `Cafe_Exploration`.
3. Scene objects built in later milestones (player, UI) get systems with
   `Services.Get<InputManager>()` in `Start()`, or listen to ScriptableObject event channels.

No manager has a static `Instance`. `Services` is the single place systems are looked up, and
only `BootstrapLoader` registers them.

## Files

| File | Responsibility |
|---|---|
| `Scripts/NocturneCocoa.Runtime.asmdef` | Runtime assembly for all game code (references Unity.InputSystem). |
| `Scripts/Core/Bootstrap/BootstrapLoader.cs` | Entry point. Persists GameSystems, initializes and registers systems, loads the first scene. Destroys duplicate bootstraps. *(renamed from `BootsrapLoader.cs`, same GUID)* |
| `Scripts/Core/Services/Services.cs` | Type-keyed registry of persistent systems (replaces per-class singletons). |
| `Scripts/Core/Events/EventChannelSO.cs` | Generic base for ScriptableObject event channels with a payload. |
| `Scripts/Core/Events/VoidEventChannelSO.cs` | Event channel with no payload. |
| `Scripts/Core/Events/String/Int/BoolEventChannelSO.cs` | Concrete payload channels (Unity can't create generic assets). |
| `Scripts/Core/Events/VoidEventListener.cs` | Inspector-only reaction to a void channel via UnityEvent. |
| `Scripts/Core/Session/GameSession.cs` | Owns runtime progress (`SaveData`). New Game, Continue, Save, AutoSave, story flags, play time. `BeforeSave` / `AfterLoad` hooks for other systems. |
| `Scripts/Core/Input/InputManager.cs` | Wraps the project-wide Input Actions. Events: `MoveChanged`, `InteractPressed`, `PausePressed`, `CancelPressed`. Modes: None / Gameplay / UI. |
| `Scripts/Core/Audio/AudioManager.cs` | `PlayMusic` (crossfade), `StopMusic`, `PlaySfx`, master/music/SFX volume saved in PlayerPrefs. |
| `Scripts/Core/Debug/DevHotkeys.cs` | Development-only keyboard shortcuts for testing (compiled out of release builds). |
| `Scripts/SceneManagement/SceneFlowController.cs` | Async single-scene loading with validation, progress, events and optional minimum load time. *(rewritten; same GUID)* |
| `Scripts/SceneManagement/SceneNames.cs` | Scene name constants. |
| `Scripts/SaveSystem/SaveData.cs` | Versioned save model: player (scene, position), progress (day, story flags), play time. |
| `Scripts/SaveSystem/SaveManager.cs` | Slot files (autosave + manual slots), temp-file writes, `.bak` backup and fallback, validation, events. |
| `Scripts/SaveSystem/SaveMigrator.cs` | Save version handling and migration steps. |
| `Scripts/SaveSystem/ISaveSerializer.cs`, `JsonSaveSerializer.cs` | Swappable save format; JsonUtility by default. |
| `Scripts/Editor/NocturneCocoa.Editor.asmdef` | Editor-only assembly. |
| `Scripts/Editor/NocturneProjectSetup.cs` | Menu **Nocturne Cocoa > Setup > Run M1 Project Setup**: creates placeholder scenes, fixes the build scene list, adds and wires the GameSystems components. Safe to re-run. |
| `Scripts/Editor/BootstrapPlayModeStarter.cs` | Menu **Nocturne Cocoa > Always Start From Bootstrap** (on by default): Play always boots through Bootstrap, then returns you to the scene you were editing. |
| `Scripts/Editor/SaveDebugMenu.cs` | Menu **Nocturne Cocoa > Saves > Open Save Folder / Delete All Saves**. |
| `Assets/InputSystem_Actions.inputactions` | Added **Player/Pause** (Esc, gamepad Start). Interact is now a tap instead of a hold. |

Removed: `Assets/Scenes/apala.cs` (empty template script).

## Inspector setup

Run **Nocturne Cocoa > Setup > Run M1 Project Setup** once. It does all of the following:

- Creates `Cafe_Exploration`, `Dialogue_Scene`, `Cooking_Scene` in `Assets/_Project/Scenes/`
  (camera only, different background colour each so you can tell them apart).
- Sets the build scene list to Bootstrap (index 0), then the three gameplay scenes.
- In Bootstrap, adds `BootstrapLoader` to `GameSystems` and one child per system, and fills in
  the BootstrapLoader references.

Optional fields (leave empty for now):

- `BootstrapLoader > Continue From Auto Save`: resume the last autosave on Play.
- `SceneFlowController > Minimum Load Duration`: for a future fade or loading screen.
- `InputManager > Actions Asset`: empty means project-wide actions.
- Event channel slots on SceneFlowController, SaveManager and GameSession.

## ScriptableObject assets

None are required for M1. When UI needs them (M7), create these via
**Create > Nocturne Cocoa > Events** in `Assets/_Project/ScriptableObjects/Events/`:

| Asset | Type | Assign to |
|---|---|---|
| `OnSceneLoadStarted` | String Event Channel | SceneFlowController |
| `OnSceneLoaded` | String Event Channel | SceneFlowController |
| `OnGameSaved` | Int Event Channel | SaveManager |
| `OnGameLoaded` | Int Event Channel | SaveManager |
| `OnSessionStarted` | Void Event Channel | GameSession |

## How to test

1. Open the project and check that the Console has no compile errors.
2. Run **Nocturne Cocoa > Setup > Run M1 Project Setup**.
3. Press **Play** in the Bootstrap scene. Expect these Console messages: `[Save] Save folder: ...`,
   `[Bootstrap] ... initialized`, `[Session] New game started`, `[SceneFlow] Loaded 'Cafe_Exploration'`,
   and `[Save] Saved slot 0` (the autosave). The Game view turns warm brown.
4. Press **F2** (purple, Dialogue) and **F3** (blue, Cooking), then **F1** to go back. Each change autosaves.
5. Press **E** and **Esc** and check that `[Dev] Interact pressed` and `[Dev] Pause pressed` appear.
6. Press **F5** to save to slot 1, then **F10** to start a new game, then **F9**. The game should
   reload slot 1 and go back to the scene you saved in. Press **F12** to compare play time and the
   created date.
7. Use **Nocturne Cocoa > Saves > Open Save Folder** to see `autosave.json`, `slot_1.json` and the `.bak` files.
8. Corrupt `slot_1.json` (delete half of it) and press F9. The game should load from `.bak` and
   log a warning.
9. Open `Cafe_Exploration` and press Play. It should boot through Bootstrap and land back in Cafe.

## Known limitations and placeholders

- No fade or loading screen yet (M7). The hooks are `SceneLoadStarted`, `LoadProgressChanged`
  and `minimumLoadDuration`.
- No Main Menu yet (M7). Bootstrap starts a new game directly, unless `Continue From Auto Save`
  is ticked.
- `GameSession` saves the exact scene, including Cooking and Dialogue. Decide in M3/M4 whether
  continuing should always resume in the café.
- JsonUtility cannot save dictionaries. Use lists of `[Serializable]` entries, or switch
  `ISaveSerializer` to Newtonsoft JSON later.
- The Bootstrap scene has no camera, so "No cameras rendering" may flash for one frame. This is harmless.
- `DevHotkeys` should be removed from GameSystems once the Pause Menu exists.