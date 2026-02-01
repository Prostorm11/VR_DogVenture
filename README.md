# VR Word Puzzle Game (DogVenture)

A Virtual Reality (VR) word puzzle game developed in Unity, where players solve increasingly difficult word challenges while interacting with an emotionally reactive dog companion.

## Project Overview

The VR Word Puzzle Game is an immersive educational puzzle experience built for VR headsets. Players create meaningful words from a given base word to score points and advance through levels. A virtual dog companion reacts dynamically to player performance—becoming happy when the player succeeds and sad when the player fails.

The game combines learning, fun, and emotional feedback to create an engaging VR experience.

---

## 🎮 Gameplay

1. A **base word** appears as floating letter blocks (e.g., "HEARTS")
2. Player **grabs letters** with VR controllers and arranges them into slots
3. Player **submits** the word to check if it's valid
4. **Correct words** earn points and trigger happy dog animations
5. **Incorrect words** trigger sad dog reactions
6. After each correct word, new scrambled letters appear
7. Complete enough words to **level up** with harder challenges

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   └── GameManager.cs          # Main game controller
│   ├── WordPuzzle/
│   │   ├── WordValidator.cs        # Word validation logic
│   │   ├── WordSpawner.cs          # Spawns letter blocks
│   │   ├── LetterBlock.cs          # Grabbable VR letter cube
│   │   ├── LetterSlot.cs           # Snap zone for letters
│   │   └── WordBuildingArea.cs     # Word assembly zone
│   ├── UI/
│   │   ├── ScoreDisplay.cs         # Score/level display
│   │   └── FeedbackDisplay.cs      # Correct/incorrect feedback
│   └── Events/
│       ├── GameEvents.cs           # Event system for game communication
│       └── DogReactionHandler.cs   # Dog reaction interface
├── Prefabs/                         # Reusable game objects
├── Data/
│   └── WordLists/                  # Word dictionary files
└── Scenes/
    └── SampleScene.unity           # Main game scene
```

---

## 🚀 Getting Started

### Prerequisites
- Unity 6000.0.x (Unity 6)
- XR Interaction Toolkit 3.3.0
- TextMeshPro
- VR headset (Quest, etc.) or XR Device Simulator

### Setup Steps

#### 1. Create the Letter Block Prefab
1. Create a new **3D Cube** (`GameObject > 3D Object > Cube`)
2. Scale it to `(0.1, 0.1, 0.1)` for a comfortable grab size
3. Add components:
   - **XR Grab Interactable** (for VR grabbing)
   - **Rigidbody** (Use Gravity: ON, Is Kinematic: OFF)
   - **Box Collider** (Is Trigger: OFF)
4. Add a **TextMeshPro - 3D Text** as a child for the letter display
5. Attach the `LetterBlock.cs` script
6. Save as prefab in `Assets/Prefabs/`

#### 2. Create the Letter Slot Prefab
1. Create a **3D Cube** scaled to `(0.12, 0.02, 0.12)` (flat platform)
2. Add a **Box Collider** (Is Trigger: ON) sized slightly larger
3. Attach the `LetterSlot.cs` script
4. Create materials for empty/occupied/highlight states
5. Save as prefab in `Assets/Prefabs/`

#### 3. Set Up the Game Scene
1. Open `SampleScene.unity`
2. Create an empty GameObject named `GameManager`
   - Attach `GameManager.cs`
   - Attach `WordValidator.cs`
   - Attach `WordSpawner.cs`
3. Create an empty GameObject named `WordBuildingArea`
   - Position it in front of the player
   - Attach `WordBuildingArea.cs`
   - Assign the slot prefab
4. Create UI for score/feedback (Canvas or 3D text)
   - Attach `ScoreDisplay.cs` and `FeedbackDisplay.cs`

#### 4. Configure References
In Unity Inspector, assign:
- `GameManager`: WordValidator and WordSpawner references
- `WordSpawner`: Letter Block prefab, spawn position
- `WordBuildingArea`: Letter Slot prefab

#### 5. Add a Submit Button
Create an XR interactable button that calls `WordBuildingArea.SubmitWord()`

---

## 🐕 Dog Integration (For Team)

The `DogReactionHandler.cs` script listens for game events:

```csharp
// Events your dog system can listen to:
GameEvents.OnWordCorrect    // (string word, int points) - Trigger happy animation
GameEvents.OnWordIncorrect  // (string attemptedWord) - Trigger sad animation
GameEvents.OnLevelUp        // (int newLevel) - Trigger excited animation
```

Attach `DogReactionHandler.cs` to your dog GameObject and configure:
- Dog Animator reference
- Animation trigger names
- Audio clips for barks/whines

---

## 🎯 Key Features to Implement

### Immediate (MVP)
- [x] Letter block grabbing with XR Interaction Toolkit
- [x] Word validation system
- [x] Score tracking
- [x] Event system for dog reactions

### Next Steps
- [ ] Create Letter Block prefab with materials
- [ ] Create Letter Slot prefab with snap zones
- [ ] Add particle effects for correct/incorrect
- [ ] Integrate dog animations
- [ ] Add sound effects
- [ ] Create word list JSON file

### Future Enhancements
- [ ] Hint system
- [ ] Timer mode
- [ ] Multiplayer
- [ ] More dog interactions

---

## 🔧 Branch Information

Working branch: `feature/floating-words`

Repository: https://github.com/Prostorm11/VR_DogVenture

---

## 📝 Adding Word Lists

Create a text file with one word per line and assign it to `WordValidator.wordListFile`:

```
STAR
RATE
EARS
HEART
EARTH
...
```

Or use the built-in fallback dictionary for testing.

---

## 👥 Team Responsibilities

| Area | Status | Notes |
|------|--------|-------|
| Game Logic | ✅ Scripts created | Needs prefab setup |
| Letter Visuals | 🔄 In Progress | Create prefabs in Unity |
| Environment/Terrain | 🔄 Team working | - |
| Dog Animations | 🔄 Team working | Use DogReactionHandler |

---

## 🎨 Visual Setup in Unity Editor

### Materials Needed
1. **Letter Block Materials**
   - Default (white/neutral)
   - Selected (blue glow)
   - Placed (green)
   - Correct (bright green)
   - Incorrect (red)

2. **Letter Slot Materials**
   - Empty (transparent/outline)
   - Occupied (solid)
   - Highlight (yellow glow)
