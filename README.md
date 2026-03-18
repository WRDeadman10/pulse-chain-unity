# Pulse Chain

Pulse Chain is a UI-only hypercasual rhythm runner built in Unity without external art assets. The game is bootstrapped entirely at runtime from the template scene and renders nodes, pulses, branches, trails, HUD, and feedback with Canvas-based `Image` components and procedural audio.

## Project Architecture

### Runtime Bootstrap

- `Assets/Scripts/Core/GameBootstrap.cs`
  - Creates the root runtime hierarchy before the first scene loads.
  - Instantiates `GameManager`, `NodeSpawner`, `PulseController`, `UIManager`, `AudioSystem`, `PersistenceSystem`, and `AdHooksSystem`.

- `Assets/Scripts/Core/GameManager.cs`
  - Central orchestrator for run lifecycle, input routing, score/combo, slow motion energy, revive flow, and game over handling.
  - Owns the active runtime configuration and coordinates subsystems through explicit method calls and events.

### Gameplay

- `Assets/Scripts/Gameplay/PulseChainDifficultySettings.cs`
  - ScriptableObject-backed runtime difficulty model.
  - Defines node spacing, pulse speed, arrival windows, overload thresholds, branch chance, and slow motion tuning.

- `Assets/Scripts/Gameplay/PulseChainNode.cs`
  - Contains runtime node and accept-zone data models.
  - Stores branch links, movement parameters, and evaluation result types.

- `Assets/Scripts/Gameplay/NodeSpawner.cs`
  - Generates the node chain, optional risk branches, moving nodes, and fake accept zones.
  - Updates node positions and pooled UI views each frame.

- `Assets/Scripts/Gameplay/PulseController.cs`
  - Manages one to three active pulses with pooled visuals.
  - Handles pulse movement, timing validation, branch transfers, ghost revive, overload activation, and multi-pulse synchronization.

### UI

- `Assets/Scripts/UI/UIManager.cs`
  - Builds the full Canvas hierarchy at runtime.
  - Manages HUD, energy bar, game over panel, screen flash, shake, combo pulse, and restart input.

- `Assets/Scripts/UI/NodeView.cs`
  - Renders a node using a circular UI image and animated accept-zone markers.

- `Assets/Scripts/UI/PulseView.cs`
  - Renders pulse visuals, style changes, scale punch, and trail state.

- `Assets/Scripts/UI/ConnectionView.cs`
  - Draws path links between nodes with rotated UI images.

- `Assets/Scripts/UI/SpriteFactory.cs`
  - Generates procedural circle and square sprites at runtime so the project has no external art dependency.

### Systems

- `Assets/Scripts/Systems/ComponentPool.cs`
  - Lightweight reusable pool for node, line, and pulse views.

- `Assets/Scripts/Systems/AudioSystem.cs`
  - Generates procedural hit, perfect, miss, and combo tones with synthesized `AudioClip` data.

- `Assets/Scripts/Systems/PersistenceSystem.cs`
  - Tracks high score, daily challenge seed, daily best, and leaderboard-ready payloads through `PlayerPrefs`.

- `Assets/Scripts/Systems/AdHooksSystem.cs`
  - Provides monetization extension points for rewarded revive and interstitial logic without binding to an SDK.

## Scene Structure

At runtime the following structure is created automatically:

- `PulseChainRuntime`
- `PulseChainRuntime/GameManager`
- `PulseChainRuntime/NodeSpawner`
- `PulseChainRuntime/PulseController`
- `PulseChainRuntime/UIManager`
- `PulseChainRuntime/AudioSystem`
- `PulseChainRuntime/PersistenceSystem`
- `PulseChainRuntime/AdHooksSystem`

## Feature Coverage

- Straight-line node generation with escalating spacing variance
- Rotating and ping-pong accept zones
- Moving nodes and fake zones at higher difficulty
- Score, combo, overload mode, and slow motion energy
- Multi-pulse gameplay with shared tap validation
- Branching risk/reward paths based on tap side
- Procedural audio hooks
- Daily challenge seed and high score persistence
- Rewarded revive and interstitial integration hooks

## Notes

- The root formatting file in this repository is currently named `editorconfig`.
- The runtime uses only Unity UI primitives and procedural textures/audio.
- Existing template `TutorialInfo` assets were left intact.
