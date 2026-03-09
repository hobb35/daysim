## DaySim MVP (Unity 2D)

DaySim is a **real‑time life mirroring game** inspired by The Sims. Your avatar mirrors what you are doing in real life, and you log actions via **text (MVP)** and **voice (future)**. Actions like *wake up*, *brush teeth*, *make breakfast*, and *exercise* award XP and level up your character, reinforcing good habits.

This folder contains **core gameplay scripts and design notes** you can drop into a Unity 2D project.

---

### High‑level MVP scope

- **Platform**: Unity 2D
- **Input (MVP)**: Text UI field for logging what you are doing right now.
- **Input (Later)**: Voice recognition (Unity / OS speech APIs or external service).
- **Avatar**: Simple 2D character with a few animations (idle, walk, brush teeth, eat, sleep).
- **Core systems**:
  - Action parsing (map text like `"brush teeth"` → `UserActionType.BrushTeeth`)
  - Real‑time timeline: actions are stamped with real `DateTime`
  - Avatar state machine that mirrors latest action
  - Simple XP / level system that rewards healthy habits
  - Persistent save of player profile & action history (JSON, ScriptableObject, or PlayerPrefs – up to you)

---

### Folder structure (in this repo)

- `DaySim/`
  - `DaySimManager.cs` – central coordinator and entry point
  - `UserAction.cs` – data model + parsing helpers for user actions
  - `AvatarStats.cs` – XP, leveling, and habit‑based rewards
  - `ActionLogger.cs` – logs actions, maintains history, notifies listeners
  - `AvatarController.cs` – hooks avatar animation to current action
  - `HabitStreakTracker.cs` – tracks daily streaks per habit category
  - `Quests/QuestTracker.cs` – simple daily goals tied to habits
  - `Quests/QuestConfig.cs` – ScriptableObject container for quest definitions
  - `Persistence/DaySimSaveSystem.cs` – JSON save/load for stats + history
  - `Config/DaySimConfig.cs` – ScriptableObject for tuning XP/rewards
  - `UI/ActionHistoryView.cs` – scrolling list of recent actions
  - `UI/DaySimUIRoot.cs` – scene-level wiring for input + panels
  - `Graphics/AvatarVisualDefinition.cs` – ScriptableObject to define avatar skins/animators
  - `Graphics/EnvironmentLayoutConfig.cs` – ScriptableObject describing where furniture/props go
  - `Graphics/EnvironmentSpawner.cs` – spawns furniture/props from the layout config
  - `Input/VoiceToActionBridge.cs` – glue between speech-to-text and DaySimManager

You can copy the `DaySim` folder directly into your Unity project under `Assets/Scripts/DaySim/` (or any folder you like).

---

### How to integrate into a new Unity project

1. **Create Unity 2D project**
   - Open Unity Hub → New Project → 2D (URP or built‑in).
2. **Copy scripts**
   - Create `Assets/Scripts/DaySim/` in your Unity project.
   - Copy all `.cs` files from this repo’s `DaySim` folder into that Unity folder.
3. **Create a DaySim scene**
   - Create a new scene, e.g. `DaySimScene`.
   - Add a `Canvas` with:
     - `InputField` for text input bound to `DaySimManager` / `DaySimUIRoot`.
     - `Button` to submit the current action.
     - `Text` fields for:
       - current action
       - XP and level
       - streak summary
       - quests summary
     - An optional `ScrollRect` + `Text` pair for `ActionHistoryView`.
   - Create an empty `GameObject` called `DaySimManager` and attach the `DaySimManager` component.
   - Wire the UI elements to the serialized fields on `DaySimManager`.
4. **Avatar setup**
   - Create a 2D avatar sprite with basic animations.
   - Add an `Animator` with states like `Idle`, `Walk`, `BrushTeeth`, `Eat`, `Sleep`.
   - Add `AvatarController` to the avatar GameObject and link it to `DaySimManager`.
5. **Play**
   - Press Play and type in actions like `"wake up"`, `"brush teeth"`, `"eat breakfast"`.
   - Watch your avatar respond and your XP/level update.

---

### MVP game loop (concept)

1. **Player does something in real life.**
2. **Player logs it in DaySim** (text input or voice → text).
3. **System parses the action** and creates a `UserAction` with:
   - type (e.g. `BrushTeeth`, `EatBreakfast`)
   - timestamp (real‑world `DateTime.UtcNow`)
   - duration (optional; can default or be user‑specified)
4. **Avatar mirrors** that action via animation/state.
5. **Stats update**: XP added, maybe streaks, and level recalculated.
6. **Repeat all day**; history creates a *day diary* of healthy habits.

---

### Next steps / extensions

- Hook up **voice recognition**:
  - Unity / Windows speech APIs, or cloud speech‑to‑text, feeding into the same `UserAction.ParseFromText` method.
- Add **habit categories** (sleep, hygiene, nutrition, social, exercise).
- Design a **streak system** (e.g. brushing teeth twice a day for 7+ days).
- Add **simple quests** (“Do all morning routine actions before 9am”).
- Create **visual feedback** (confetti, sounds, UI flashes) for completing habits or leveling up.

This repo will start with a small, clean MVP code skeleton and can grow over time into a richer “Sims‑style but real‑time” life simulator.

