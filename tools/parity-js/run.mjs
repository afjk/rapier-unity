#!/usr/bin/env node
// Cross-host parity runner: builds the same fixtures used by
// native/rapier_unity_ffi's `parity_golden` test on top of the public
// `@dimforge/rapier3d-deterministic-compat` npm package, steps them to the
// same sample ticks, and computes the same canonical world-state hash.
//
// Default mode verifies against the committed `<fixture>.golden.json` files
// and exits non-zero on any mismatch. Set RAPIER_PARITY_RECORD=1 to write
// the golden files instead (only meant for local debugging — see the repo's
// parity policy: a golden is only valid once native and JS agree on every
// tick of every fixture; never commit a JS- or native-only recording).

import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { init, World, RigidBodyDesc, ColliderDesc, ShapeType } from "@dimforge/rapier3d-deterministic-compat";

import { worldStateHash, hashToHex, stableIdHash, ShapeTag } from "./canonical-hash.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const FIXTURES_DIR = path.join(__dirname, "..", "..", "fixtures", "rapier");

const FIXTURES = ["parity-basic-001.json", "parity-freefall-001.json", "parity-contact-basic-001.json"];

const RAPIER_CORE_VERSION = "0.30.0";
const HASH_VERSION = "SceneSyncCanonicalPhysicsHashV1";

function f(value) {
  return Math.fround(value);
}

function buildWorld(fixture) {
  const gravity = { x: f(fixture.gravity[0]), y: f(fixture.gravity[1]), z: f(fixture.gravity[2]) };
  const world = new World(gravity);
  world.timestep = f(fixture.timestep);

  const bodyMeta = new Map(); // handle -> { stableId, canSleep }
  const colliderStableIds = new Map(); // handle -> stableId

  for (const b of fixture.bodies) {
    if (b.shape !== "box") {
      throw new Error(`fixture body '${b.id}': unsupported shape '${b.shape}' (v0 parity fixtures are box-only)`);
    }

    const fixed = b.type === "fixed";
    const rotation = b.rotation ?? [0, 0, 0, 1];
    const linvel = fixed ? [0, 0, 0] : b.linearVelocity ?? [0, 0, 0];
    const angvel = fixed ? [0, 0, 0] : b.angularVelocity ?? [0, 0, 0];
    const canSleep = b.canSleep ?? true;

    const bodyDesc = (fixed ? RigidBodyDesc.fixed() : RigidBodyDesc.dynamic())
      .setTranslation(f(b.position[0]), f(b.position[1]), f(b.position[2]))
      .setRotation({ x: f(rotation[0]), y: f(rotation[1]), z: f(rotation[2]), w: f(rotation[3]) })
      .setLinvel(f(linvel[0]), f(linvel[1]), f(linvel[2]))
      .setAngvel({ x: f(angvel[0]), y: f(angvel[1]), z: f(angvel[2]) })
      .setLinearDamping(f(Math.max(0, b.linearDamping ?? 0)))
      .setAngularDamping(f(Math.max(0, b.angularDamping ?? 0)))
      .setCanSleep(canSleep)
      .setCcdEnabled(!fixed && (b.ccd ?? false));

    const body = world.createRigidBody(bodyDesc);
    const stableId = stableIdHash(b.id);
    bodyMeta.set(body.handle, { stableId, canSleep });

    const colliderDesc = ColliderDesc.cuboid(f(b.halfExtents[0]), f(b.halfExtents[1]), f(b.halfExtents[2]))
      .setDensity(f(Math.max(0, b.density ?? 1)))
      .setFriction(f(Math.max(0, b.friction ?? 0.5)))
      .setRestitution(f(Math.max(0, b.restitution ?? 0.2)))
      .setSensor(false)
      .setTranslation(0, 0, 0)
      .setRotation({ x: 0, y: 0, z: 0, w: 1 });

    const collider = world.createCollider(colliderDesc, body);
    colliderStableIds.set(collider.handle, stableId);
  }

  return { world, bodyMeta, colliderStableIds };
}

function colliderShape(collider) {
  switch (collider.shapeType()) {
    case ShapeType.Ball:
      return { tag: ShapeTag.Ball, radius: collider.radius() };
    case ShapeType.Cuboid:
      return { tag: ShapeTag.Cuboid, halfExtents: collider.halfExtents() };
    default:
      return { tag: ShapeTag.Unknown };
  }
}

function snapshotWorld(world, bodyMeta, colliderStableIds) {
  const bodies = world.bodies.getAll().map((body) => {
    const meta = bodyMeta.get(body.handle);
    return {
      stableId: meta.stableId,
      bodyType: body.bodyType(),
      gravityScale: body.gravityScale(),
      linearDamping: body.linearDamping(),
      angularDamping: body.angularDamping(),
      additionalSolverIterations: body.additionalSolverIterations(),
      ccdEnabled: body.isCcdEnabled(),
      softCcdPrediction: body.softCcdPrediction(),
      canSleep: meta.canSleep,
      translation: body.translation(),
      rotation: body.rotation(),
      linvel: body.linvel(),
      angvel: body.angvel(),
      isSleeping: body.isSleeping(),
      isEnabled: body.isEnabled(),
    };
  });

  const colliders = world.colliders.getAll().map((collider) => {
    const parent = collider.parent();
    const localTranslation = collider.translationWrtParent();
    const localRotation = collider.rotationWrtParent();
    return {
      stableId: colliderStableIds.get(collider.handle),
      parentStableId: parent ? bodyMeta.get(parent.handle).stableId : null,
      localPose:
        localTranslation && localRotation ? { translation: localTranslation, rotation: localRotation } : null,
      shape: colliderShape(collider),
      density: collider.density(),
      friction: collider.friction(),
      frictionCombineRule: collider.frictionCombineRule(),
      restitution: collider.restitution(),
      restitutionCombineRule: collider.restitutionCombineRule(),
      isSensor: collider.isSensor(),
      isEnabled: collider.isEnabled(),
    };
  });

  return { gravity: world.gravity, timestep: world.timestep, bodies, colliders };
}

function sampleTicksOf(fixture) {
  const ticks = [...new Set(fixture.sampleTicks.filter((t) => t >= 0))];
  ticks.sort((a, b) => a - b);
  return ticks;
}

function runFixture(fixture) {
  if (fixture.rapierCoreVersion !== RAPIER_CORE_VERSION) {
    throw new Error(`fixture targets core ${fixture.rapierCoreVersion}, expected ${RAPIER_CORE_VERSION}`);
  }

  const { world, bodyMeta, colliderStableIds } = buildWorld(fixture);
  const hashes = {};

  let current = 0;
  for (const target of sampleTicksOf(fixture)) {
    while (current < target) {
      world.step();
      current += 1;
    }
    const snapshot = snapshotWorld(world, bodyMeta, colliderStableIds);
    hashes[String(target)] = hashToHex(worldStateHash(snapshot));
  }

  world.free();
  return hashes;
}

async function main() {
  await init();

  const record = process.env.RAPIER_PARITY_RECORD === "1";
  let ok = true;

  for (const name of FIXTURES) {
    const fixturePath = path.join(FIXTURES_DIR, name);
    const fixture = JSON.parse(await readFile(fixturePath, "utf8"));
    const hashes = runFixture(fixture);
    const goldenPath = path.join(FIXTURES_DIR, name.replace(".json", ".golden.json"));

    if (record) {
      const golden = {
        profile: fixture.profile,
        hashVersion: HASH_VERSION,
        rapierCoreVersion: RAPIER_CORE_VERSION,
        hashes,
      };
      await writeFile(goldenPath, `${JSON.stringify(golden, null, 2)}\n`);
      console.log(`recorded ${goldenPath}`);
      continue;
    }

    let golden;
    try {
      golden = JSON.parse(await readFile(goldenPath, "utf8"));
    } catch (err) {
      console.error(`missing golden ${goldenPath}: ${err.message}`);
      console.error("run: RAPIER_PARITY_RECORD=1 node tools/parity-js/run.mjs");
      ok = false;
      continue;
    }

    if (golden.hashVersion !== HASH_VERSION) {
      console.error(`${name}: hash version drift (golden=${golden.hashVersion}, expected=${HASH_VERSION})`);
      ok = false;
    }
    if (golden.rapierCoreVersion !== RAPIER_CORE_VERSION) {
      console.error(`${name}: core version drift (golden=${golden.rapierCoreVersion}, expected=${RAPIER_CORE_VERSION})`);
      ok = false;
    }

    const ticks = Object.keys(hashes).sort((a, b) => Number(a) - Number(b));
    let fixtureOk = true;
    for (const tick of ticks) {
      if (golden.hashes[tick] !== hashes[tick]) {
        console.error(`${name}: tick ${tick} diverged (js=${hashes[tick]}, golden=${golden.hashes[tick]})`);
        fixtureOk = false;
        ok = false;
      }
    }
    if (fixtureOk) {
      console.log(`${name}: ok (${ticks.length} ticks match)`);
    }
  }

  if (!record && !ok) {
    process.exitCode = 1;
  }
}

main().catch((err) => {
  console.error(err.stack ?? String(err));
  process.exitCode = 1;
});
