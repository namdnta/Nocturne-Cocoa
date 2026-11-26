# M2 — Player & Exploration

Status: implemented (pending first run in the Unity Editor). Builds on M1.

## How it fits together

```
Cafe_Exploration (scene)
├── Main Camera          Camera + CameraController (follows player, clamped to CameraBounds)
├── Player               Rigidbody2D, CapsuleCollider2D (feet),
│   │                    PlayerMovement, PlayerInteractor, PlayerController, PlayerAnimator
│   └── Sprite           SpriteRenderer (+ Animator later)
├── InteractionPrompt    InteractionPromptIndicator
│   └── Icon             SpriteRenderer
└── M2_TestRoom          grey-box room (floor, walls, table, counter, cooking station,
                         spawn points, CameraBounds, GlobalLight2D)
```

Each frame `PlayerController` reads `InputManager.Move` (M1) and passes it to `PlayerMovement`. On
**Interact** it asks `PlayerInteractor` to use whatever `Interactable` is in front of the player.

**Where the player appears when a scene starts:**
1. The entry point passed to `SceneFlowController.LoadScene(scene, entryPointId)`.
2. Otherwise, the saved position, if it belongs to this scene. This covers Continue and coming back from Cooking.
3. Otherwise, the `PlayerSpawnPoint` marked Default.

**Taking control away:** dialogue (M3) and crafting (M4) call `player.AddControlLock(this)` and
`RemoveControlLock(this)`. Several locks can be active at once.

## Files

| File | Responsibility |
|---|---|
| `Exploration/Player/PlayerController.cs` | Connects input to movement and interaction, handles control locks, places the player at scene start, writes position to the save. |
| `Exploration/Player/PlayerMovement.cs` | Rigidbody2D movement with 8-way or 4-way modes, acceleration, facing direction, Teleport and Stop. |
| `Exploration/Player/PlayerAnimator.cs` | Sends MoveX/MoveY/IsMoving/Speed to an Animator and flips the sprite for left. Skips any parameter that's missing. |
| `Exploration/Interaction/Interactable.cs` | Base class for interactables: prompt text, priority, on/off switch, focus events, `CanInteract` / `OnInteract`. |
| `Exploration/Interaction/PlayerInteractor.cs` | Detects targets with a circle in front of the player (priority first, then distance). Events: `TargetChanged`, `Interacted`. |
| `Exploration/Interaction/UnityEventInteractable.cs` | Generic interactable set up in the Inspector, plus a placeholder "inspect" message. |
| `Exploration/Interaction/SceneTransitionInteractable.cs` | Press Interact to change scene (e.g. the cooking station opens Cooking_Scene). |
| `Exploration/Interaction/SceneTransitionTrigger.cs` | Walk into a trigger to change scene, with a short delay so it doesn't fire on spawn. |
| `Exploration/Interaction/InteractionPromptIndicator.cs` | PLACEHOLDER: bobbing icon above the current target, until the HUD exists (M7). |
| `Exploration/Spawning/PlayerSpawnPoint.cs` | Named spawn and entry points, one of them marked Default. |
| `Exploration/Camera/CameraController.cs` | Smooth follow, room bounds, optional pixel snapping. |
| `Editor/M2TestRoomBuilder.cs` | Menu **Nocturne Cocoa > Setup > Build M2 Test Room**. |
| `Editor/PlaceholderSprites.cs` | Generates white placeholder sprites in `Art/Shared/Placeholders/`. |

**M1 files changed:**
- `SceneFlowController.cs`: `LoadScene(scene, entryPointId = null)` and `CurrentEntryPointId`. Existing calls still work.
- `SaveData.cs`: added `player.positionSceneName`. Old saves still load (the field is just empty).

## Inspector setup

Run **Nocturne Cocoa > Setup > Build M2 Test Room**. It does the following:
- Sets the Renderer 2D asset to **Transparency Sort Mode = Custom Axis (0, 1, 0)**, so objects lower on screen draw in front (3/4 view).
- Builds the test room, the player, the prompt and the camera in `Cafe_Exploration` shown above.

**To build things by hand later:**
- **Player:** Rigidbody2D, a Collider2D at the feet, then PlayerController (it adds PlayerMovement and
  PlayerInteractor for you), plus PlayerAnimator. Put the sprite on a child object with the pivot at the feet.
- **Furniture:** use a sprite with a bottom pivot and a collider on its base only, so the player can walk behind it.
- **Interactable:** add a collider (trigger or solid) and one of the Interactable components.
- **Camera:** add CameraController to the Main Camera, and assign a BoxCollider2D trigger covering the room as **Bounds**.
- **Each exploration scene:** at least one PlayerSpawnPoint with **Is Default** ticked, and a Global Light 2D.

**Real art (later):**
- Import sprites with Pixels Per Unit = 16, Filter Mode = Point and Compression = None.
- Add URP's **Pixel Perfect Camera** to the Main Camera.
- Create an Animator Controller with **MoveX/MoveY** (float) and **IsMoving** (bool), using 2D blend trees for idle and walk.
  If you draw separate left-facing sprites, turn off PlayerAnimator > Flip Sprite For Left.

## ScriptableObject assets

None are required for M2.

## How to test

1. Run **Build M2 Test Room**, then press Play from any scene.
2. You appear at the bottom of the café. Walk with WASD, the arrow keys or the left stick. Walls block you,
   and the camera follows without showing outside the room.
3. Walk above the table: you are drawn behind it. Walk below it: you are drawn in front of it (Y-sorting).
4. Face the counter and a yellow diamond appears. Press **E** and the Console shows the inspect message.
5. Face the red station and press **E**. The game moves to the blue Cooking scene.
6. Press **F1** to go back. You are standing where you left, in front of the station.
7. Walk somewhere, press **F5** (save), walk away, press **F9** (load). You are back at the saved spot.
8. Press **F10** (new game). You are back at the default spawn.

## Known limitations and placeholders

- The test room and placeholder sprites are grey-box only. Delete `M2_TestRoom`, `Player` and
  `InteractionPrompt` when the real café is built (keep a Player set up the same way).
- No way back from Cooking_Scene yet except F1. That comes with M4.
- NPCs are not in M2. NPC movement and talking come in M3, using `Interactable` as the base.
- The prompt is a world-space sprite. A proper HUD prompt showing `PromptText` comes in M7.