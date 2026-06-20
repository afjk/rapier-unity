# Deterministic Dice

Open `DeterministicDice.unity` and enter Play Mode.

The scene runs two identical Rapier component worlds side by side.

Each world contains:
- a fixed floor
- three fixed walls, leaving the `-Z` side open
- a dynamic dice

The dice is rolled once automatically, then can be thrown manually without waiting for it to settle.
Hold Space to spin the dice and release Space to toss it; while a manual throw is still moving,
hold Space again to start the next toss from the current pose.
Both worlds receive the same authored setup, timestep, initial velocity, and player input at the same ticks.

The worlds are authored with `RapierWorldBehaviour`, `RapierRigidbody`, and `RapierBoxCollider`.
Given the same timestep, initial state, and inputs at the same ticks, both worlds produce the same
`StateHash` and the same dice result. Manual throws can interrupt a moving dice after the first
automatic roll has started, showing that determinism holds across continued simulation and
interactive input, not only from the initial frame.

The sample compares `StateHash()` after each tick.
If the hashes stay equal, the HUD shows `MATCH`.
If they differ, the HUD reports the first diverged tick.

This demonstrates deterministic Rapier simulation using Unity-style authoring components.

## Notes

- Both `RapierWorldBehaviour` components use **Manual** step mode, gravity `(0, -9.81, 0)`, a
  `1 / 60` timestep, and `HierarchyOrder` registration, so the demo controller can step World A and
  World B explicitly in a known order.
- The two world roots are offset in the Scene (`-7` and `+7` on X) only so both are visible. On
  reset the demo controller normalizes every body into its own world-root-local frame, so the two
  native worlds are bit-identical regardless of where their roots sit.
