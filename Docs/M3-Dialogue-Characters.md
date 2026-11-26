# M3 — Dialogue & Characters

Status: implemented. The dialogue flow logic has been tested in isolation; it has not yet been run in the Unity Editor.

## How it fits together

```
Bootstrap / GameSystems (persistent)
├── RelationshipSystem   affection points + "has met" per character, stored in SaveData
├── DialogueManager      runs conversations (nodes, choices, conditions, actions), no UI code
├── EventSystem          uGUI input (InputSystemUIInputModule)
└── DialogueCanvas
    └── DialogueBox      DialogueUI: name, portrait, typewriter text, choices

Cafe_Exploration                      Dialogue_Scene
└── M3_SampleNPCs                     └── DialogueStage   side-view characters (left/right slots)
    ├── NPC_Luna   NpcController + NpcMovement (Idle)
    └── NPC_Mochi  NpcController + NpcMovement (Wander)
```

**One logic, two presentations.** `DialogueManager` only sends events (`DialogueStarted`, `LineShown`,
`DialogueEnded`). Two presentations listen to them:
- **Overlay** (`DialoguePresentation.Overlay`): the dialogue box appears over the top-down café.
- **Side view** (`DialoguePresentation.SideViewScene`): the manager loads `Dialogue_Scene`, `DialogueStage`
  shows the characters, the same dialogue box shows the text, and at the end the game returns to the
  previous scene. The player reappears where they were standing.

**Talking to an NPC:**
1. The player presses E.
2. `NpcController` picks the first conversation whose conditions pass, pauses `NpcMovement`, faces the player,
   marks the character as met, and calls `DialogueManager.StartDialogue`.
3. Input switches to UI mode, so the player stops moving.
4. When the dialogue ends, the NPC resumes and input returns to Gameplay.

## Data (ScriptableObjects)

| Asset | Create menu | Notes |
|---|---|---|
| CharacterProfileSO | Nocturne Cocoa > Characters > Character Profile | ID, name and colour, expressions (portrait + side-view sprite each), voice blip, journal text. |
| CharacterDatabaseSO | Nocturne Cocoa > Characters > Character Database | All characters, looked up by ID. |
| DialogueSO | Nocturne Cocoa > Dialogue > Dialogue | Presentation, start node, list of nodes. |
| DialogueDatabaseSO | Nocturne Cocoa > Dialogue > Dialogue Database | All dialogues, so `StartDialogue("id")` works. Assign it to DialogueManager. |

### Writing a dialogue

Each node has:
- an **id**,
- a **speaker** (empty means narrator) and an optional **name override** such as "???",
- an **expression** and the **text**,
- a **next node id** (empty ends the dialogue),
- optional **choices**, **jumps** and **onEnter** actions.

- **Choices:** the player picks one. A choice is hidden unless all its conditions pass, and it can run actions.
- **Jumps:** checked before next. The first jump whose conditions pass wins.
- **Logic node:** a node with empty text shows nothing and jumps straight on. Use it for branching:
  "if affection is 5 or more, go to `warm`, otherwise `plain`".

**Conditions:**
- HasFlag / MissingFlag
- RelationshipAtLeast / RelationshipBelow
- HasMetCharacter
- DialogueCompleted
- DayAtLeast

**Actions:**
- SetFlag / ClearFlag
- ChangeRelationship (an empty character means the speaker)
- RaiseEvent: broadcasts a name such as `unlock_recipe_x`, through `DialogueManager.CustomEvent` and an optional channel.

The Console warns about duplicate node IDs and links to missing nodes whenever you edit a DialogueSO.

## Files

| File | Responsibility |
|---|---|
| `Data/Characters/CharacterProfileSO.cs` | Character static data (shared by top-down and side-view). |
| `Data/Characters/CharacterDatabaseSO.cs` | Character lookup by ID. |
| `Data/Dialogue/DialogueSO.cs` | A conversation, plus link validation. |
| `Data/Dialogue/DialogueNode.cs` | Node, choice and jump data. |
| `Data/Dialogue/DialogueConditions.cs` | Condition and action data types. |
| `Data/Dialogue/DialogueDatabaseSO.cs` | Dialogue lookup by ID. |
| `Dialogue/DialogueManager.cs` | Conversation flow, actions, side-view scene round-trip, input mode, completion tracking. |
| `Dialogue/DialogueConditionEvaluator.cs` | Evaluates conditions against GameSession and RelationshipSystem. |
| `Dialogue/DialogueLine.cs` | What a presentation needs to show one line. |
| `Dialogue/DialogueStage.cs` | Side-view stage: left and right character slots, dims whoever isn't speaking. |
| `Characters/RelationshipSystem.cs` | Affection, levels, "met", and events (`CharacterMet`, `PointsChanged`, `LevelChanged`). |
| `Exploration/NPC/NpcController.cs` | Talkable NPC (extends Interactable) with conditional conversation selection. |
| `Exploration/NPC/NpcMovement.cs` | Idle, Wander or Patrol movement with obstacle checks and pause locks. |
| `Exploration/Characters/CharacterAnimator.cs` | Animator driver shared by the player and NPCs. |
| `Exploration/Characters/ICharacterMotor.cs` | The interface CharacterAnimator reads (facing, velocity, moving). |
| `UI/Dialogue/DialogueUI.cs` | The dialogue box. |
| `UI/Dialogue/TypewriterEffect.cs` | Typewriter text with punctuation pauses and voice blips. |
| `UI/Dialogue/DialogueChoiceButton.cs` | A choice button. |
| `Editor/M3DialogueBuilder.cs` | Menu **Nocturne Cocoa > Setup > Build M3 Dialogue & NPCs**. |

**Earlier files changed:**
- `SaveData`: added `relationships` and `progress.completedDialogueIds`. Old saves are repaired automatically.
- `GameSession`: added `HasCompletedDialogue` / `MarkDialogueCompleted`. Never resumes into Dialogue_Scene.
- `InputManager`: added a `SubmitPressed` event.
- `InputSystem_Actions`: UI/Submit now also uses E and Space.
- `BootstrapLoader`: initializes and registers RelationshipSystem and DialogueManager (optional, with a warning if they're missing).
- `PlayerAnimator`: now a thin subclass of `CharacterAnimator`.
- `PlayerMovement`: implements `ICharacterMotor`.
- `Interactable`: `promptText` is now `protected`.
- `PlaceholderSprites`: added portrait and side-view placeholders.
- `M2TestRoomBuilder`: shares its helpers with the M3 builder.
- Both asmdefs: now reference TextMeshPro and uGUI.

## Inspector setup

1. **TextMeshPro:** open **Window > TextMeshPro > Import TMP Essential Resources** once.
2. Run **Nocturne Cocoa > Setup > Build M3 Dialogue & NPCs**. It creates:
   - Sample assets in `ScriptableObjects/Characters/Samples` and `ScriptableObjects/Dialogue/Samples`, plus both databases.
   - In Bootstrap: RelationshipSystem, DialogueManager (with the Dialogue Database assigned), EventSystem and DialogueCanvas.
     It also fills in the new BootstrapLoader fields.
   - In Dialogue_Scene: DialogueStage with a backdrop, floor, two slots and a light.
   - In Cafe_Exploration: NPC_Luna and NPC_Mochi.

**Making your own NPC:**
1. Create a GameObject with a Rigidbody2D (Kinematic) and a Collider2D at the feet.
2. Add NpcMovement, NpcController and CharacterAnimator.
3. Add a child object with a SpriteRenderer.
4. On NpcController, assign the Profile, the Conversations and a Fallback Dialogue.

## How to test

1. Press Play. Walk to **Luna** (blue, right of the table) and press **E**.
   - Her name shows as "???". Pick **"Welcome! We just reopened."**
   - Text types out; press E, Space or Enter to finish it, then again to continue.
   - The Console shows `[Relationship] 'luna' 0 -> 5` and the flag `met_luna` is set.
2. Talk to Luna again. Her line is now the "warm" one (affection ≥ 5).
   Start a new game with F10, pick the other answer instead, then talk again: you get the "plain" line.
3. Find **Mochi** (orange, wandering). Mochi stops and turns to you. Press E.
   - The game switches to the side-view **Dialogue_Scene**, with Mochi on the left.
   - Pick an answer. At the end you return to the café, standing where you were.
4. Talk to Mochi again. You get a short overlay line (`DialogueCompleted` condition).
5. Press F5, then F10, then F9. Affection, flags and completed dialogues come back with the save.
6. Use the mouse: click the dialogue box to advance, and click a choice to pick it.

## Known limitations and placeholders

- **Sample content:** Luna, Mochi and their four dialogues are examples to learn from. Delete them, or
  replace their text and art when the real story is written.
- **Editing dialogue:** dialogues are edited as node lists in the Inspector. A visual node editor or a
  spreadsheet/CSV import would make writing long scripts easier. That's worth proposing once the writing workflow is known.
- **Player character:** there's no player character profile yet, so the player's lines are written as choices.
- **Character Journal:** the journal UI that shows profiles and relationship levels is part of M7.
  RelationshipSystem already provides `GetLevelName`, and the profile has `JournalDescription`.
- **Localization:** dialogue text is plain strings. Unity's Localization package can be added later.