# Deterministic Replay

This sample plan compares two explicit worlds with identical setup.

## Scenario

1. Create two `RapierWorld` instances.
2. Apply the same gravity and timestep to both.
3. Create the same fixed floor and dynamic body in the same order.
4. Step both worlds for 600 ticks.
5. Compare `StateHash()` after each tick or at the end.

```csharp
using AFJK.Rapier;
using UnityEngine;

using var a = RapierWorld.Create();
using var b = RapierWorld.Create();

a.SetGravity(new Vector3(0, -9.81f, 0));
b.SetGravity(new Vector3(0, -9.81f, 0));
a.SetTimestep(1f / 60f);
b.SetTimestep(1f / 60f);

var bodyA = a.CreateRigidBody(RapierBodyDesc.Dynamic(new Vector3(0, 5, 0)));
var bodyB = b.CreateRigidBody(RapierBodyDesc.Dynamic(new Vector3(0, 5, 0)));
a.CreateBoxCollider(bodyA, RapierBoxColliderDesc.Unit);
b.CreateBoxCollider(bodyB, RapierBoxColliderDesc.Unit);

for (var tick = 0; tick < 600; tick++)
{
    a.Step();
    b.Step();

    var hashA = a.StateHash();
    var hashB = b.StateHash();
    if (hashA != hashB)
    {
        Debug.LogError($"Rapier hash mismatch at tick {tick}: {hashA} != {hashB}");
        break;
    }
}
```

The current hash is intended for same-version comparisons. Cross-platform and cross-version guarantees require more validation and a versioned snapshot format.

