# Deterministic Dice

Open `DeterministicDice.unity` and enter Play Mode.

The scene runs two identical Rapier component worlds side by side.

Each world contains:
- a fixed floor
- a fixed wall
- a dynamic dice

The dice is rolled once, then thrown again from the resulting state.
Both worlds receive the same authored setup, timestep, initial velocity, and impulses at the same ticks.

The worlds are authored with `RapierWorldBehaviour`, `RapierRigidbody`, and `RapierBoxCollider`.
Given the same timestep, initial state, and inputs at the same ticks, both worlds produce the same
`StateHash` and the same dice result. The second throw is applied after the dice has already rolled
once, showing that determinism holds across continued simulation, not only from the initial frame.

The sample compares `StateHash()` after each tick.
If the hashes stay equal, the HUD shows `MATCH`.
If they differ, the HUD reports the first diverged tick.

This demonstrates deterministic Rapier simulation using Unity-style authoring components.

## Notes

- Both `RapierWorldBehaviour` components use **Manual** step mode, gravity `(0, -9.81, 0)`, a
  `1 / 60` timestep, and `HierarchyOrder` registration, so the demo controller can step World A and
  World B explicitly in a known order.
- The two world roots are offset in the Scene (`-3` and `+3` on X) only so both are visible. On
  reset the demo controller normalizes every body into its own world-root-local frame, so the two
  native worlds are bit-identical regardless of where their roots sit.
