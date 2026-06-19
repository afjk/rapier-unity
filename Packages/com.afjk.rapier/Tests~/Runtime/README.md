# Runtime Test Placeholders

Full Unity runtime tests are intentionally deferred until native plugin packaging is automated.

Planned tests:

- Create and dispose `RapierWorld`.
- Create two independent worlds and verify handles do not cross worlds.
- Create a dynamic body with a box collider and verify transform changes after stepping.
- Verify `RapierWorldBehaviour` steps in `FixedUpdate`.
- Verify `RapierRigidbody` registers only into an explicit selected world.
- Verify `RapierPhysics.Raycast` returns a `RapierRaycastHit` after the world has stepped.
- Verify deterministic replay sample hashes match for identical setup.

