# Changelog

## Initial project setup

- Added runtime bootstrap so the template scene can launch the game without manual scene authoring.
- Added `GameManager`, `NodeSpawner`, `PulseController`, `UIManager`, `AudioSystem`, `PersistenceSystem`, and `AdHooksSystem`.
- Configured runtime Canvas creation and game loop ownership.

## Basic node generation

- Added runtime node data model with pooled `NodeView` and `ConnectionView`.
- Implemented straight-line node spawning with dynamic spacing and branch-ready links.
- Added UI circle nodes and Canvas-based path rendering.

## Pulse movement system

- Added pooled `PulseView` with smooth interpolation between nodes.
- Implemented speed scaling, pulse trail visuals, and pulse style switching.

## Tap timing system

- Added tap evaluation against arrival timing windows and active node state.
- Added miss detection and run-ending logic.

## Dynamic accept zones

- Implemented rotating and ping-pong accept zones.
- Added fake zones and node visual state updates.

## Score and combo

- Added score gain, perfect timing combo escalation, and combo reset on failure.
- Added HUD updates for score, combo, high score, and daily best.

## Difficulty progression

- Added score-driven pulse speed growth.
- Added moving nodes, distance variance, and overload thresholds.

## Multi pulse gameplay

- Added support for two and three simultaneous pulses with staggered offsets.
- Single tap validation now requires all active pulses to succeed.

## Branching nodes

- Added safe and risk paths.
- Tap side chooses branch direction and applies higher risk score reward.

## Endless high intensity mode

- Added overload mode after score threshold.
- Added stronger visuals and faster pulse progression during overload.

## Input polish

- Added touch and mouse input support.
- Added hold-to-slow-time mechanic with rechargeable energy meter.

## Juice and feedback

- Added trail rendering, success punch, screen flash, and shake.
- Added near-miss feedback state.

## Audio feedback

- Added procedural hit, perfect, miss, and combo escalation tones.

## Daily challenge system

- Added deterministic daily seed and daily best score persistence.
- Added leaderboard payload structure for backend integration.

## Pulse styles

- Added round, square, zig-zag, and ghost pulse styles.
- Added ghost pulse revive shield behavior.

## Ads integration hooks

- Added rewarded revive and interstitial extension points without SDK coupling.

## Performance optimization

- Added reusable component pooling for nodes, lines, and pulses.
- Kept the runtime Canvas-based and allocation-light for mobile.

## Guided tutorial and visual refresh

- Added a first-time guided tutorial that pauses gameplay and tells the player exactly what to do at each step.
- Added retry handling for missed tutorial actions and a clean handoff into the normal run after completion.
- Added stronger background color motion, node glow animation, and brighter pulse glow for better readability and feedback.

## Third-party procedural UI integration

- Integrated `TranslucentImage` for frosted-glass tutorial, hint, and game-over cards with a runtime blur source.
- Integrated `MPUIKit` for procedural node shapes, pulse shapes, glow orbs, and panel accents.
- Integrated `ProceduralUIImage` for rounded HUD cards, buttons, energy bars, and connection strips.
- Converted the runtime canvas to a camera-backed UI setup so blur-based overlays render correctly.
