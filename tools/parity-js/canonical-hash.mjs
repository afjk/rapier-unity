// JS port of native/rapier_unity_ffi/src/hash.rs (`world_state_hash`).
// Must stay byte-for-byte aligned with the Rust implementation: same field
// order, same FNV-1a-64 mixing, same f32 bit-pattern canonicalization.

const FNV_OFFSET = 0xcbf29ce484222325n;
const FNV_PRIME = 0x100000001b3n;
const MASK64 = 0xffffffffffffffffn;

export const CANONICAL_HASH_NAME = "SceneSyncCanonicalPhysicsHashV1";
export const RAPIER_CORE_VERSION = "0.30.0";

// rapier_unity_ffi's RapierUnityWorld starts `next_pid_controller_id` at 1
// and the v0 fixtures never create a PID controller, so this stays fixed.
const INITIAL_NEXT_PID_CONTROLLER_ID = 1n;

export const ShapeTag = {
  Ball: 1,
  Cuboid: 2,
  Capsule: 3,
  Voxels: 4,
  Unknown: 255,
};

const textEncoder = new TextEncoder();
const f32Scratch = new ArrayBuffer(4);
const f32View = new Float32Array(f32Scratch);
const u32View = new Uint32Array(f32Scratch);

const CANONICAL_NAN_BITS = 0x7fc00000;

function canonicalF32Bits(value) {
  const narrowed = Math.fround(value);
  if (narrowed === 0) {
    // Collapse -0.0 into +0.0, matching `value == 0.0` in Rust.
    return 0;
  }
  if (Number.isNaN(narrowed)) {
    return CANONICAL_NAN_BITS;
  }
  f32View[0] = narrowed;
  return u32View[0];
}

class StableHasher {
  constructor() {
    this.value = FNV_OFFSET;
  }

  finish() {
    return this.value;
  }

  writeU8(byte) {
    this.value ^= BigInt(byte & 0xff);
    this.value = (this.value * FNV_PRIME) & MASK64;
  }

  writeU32(value) {
    const v = value >>> 0;
    this.writeU8(v & 0xff);
    this.writeU8((v >>> 8) & 0xff);
    this.writeU8((v >>> 16) & 0xff);
    this.writeU8((v >>> 24) & 0xff);
  }

  writeI32(value) {
    this.writeU32(value >>> 0);
  }

  writeU64(value) {
    let v = BigInt.asUintN(64, BigInt(value));
    for (let i = 0; i < 8; i++) {
      this.writeU8(Number(v & 0xffn));
      v >>= 8n;
    }
  }

  writeBytes(bytes) {
    for (const byte of bytes) {
      this.writeU8(byte);
    }
  }

  writeStr(value) {
    const bytes = textEncoder.encode(value);
    this.writeU32(bytes.length);
    this.writeBytes(bytes);
  }

  writeF32(value) {
    this.writeU32(canonicalF32Bits(value));
  }

  writeVec3(v) {
    this.writeF32(v.x);
    this.writeF32(v.y);
    this.writeF32(v.z);
  }

  writePose(translation, rotation) {
    this.writeF32(translation.x);
    this.writeF32(translation.y);
    this.writeF32(translation.z);
    this.writeF32(rotation.x);
    this.writeF32(rotation.y);
    this.writeF32(rotation.z);
    this.writeF32(rotation.w);
  }

  writeStableIdentity(stableId) {
    this.writeU8(1);
    this.writeU64(stableId);
  }

  writeOptionalStableIdentity(stableId) {
    if (stableId === null || stableId === undefined) {
      this.writeU8(0);
    } else {
      this.writeStableIdentity(stableId);
    }
  }
}

// `stableId = FNV-1a-64(UTF-8(objectId))`, matching `rapier_unity_stable_id_hash`.
export function stableIdHash(id) {
  const hasher = new StableHasher();
  hasher.writeBytes(textEncoder.encode(id));
  return hasher.finish();
}

function writeColliderShape(hasher, shape) {
  switch (shape.tag) {
    case ShapeTag.Ball:
      hasher.writeU8(ShapeTag.Ball);
      hasher.writeF32(shape.radius);
      break;
    case ShapeTag.Cuboid:
      hasher.writeU8(ShapeTag.Cuboid);
      hasher.writeVec3(shape.halfExtents);
      break;
    case ShapeTag.Capsule:
      hasher.writeU8(ShapeTag.Capsule);
      hasher.writeVec3(shape.pointA);
      hasher.writeVec3(shape.pointB);
      hasher.writeF32(shape.radius);
      break;
    case ShapeTag.Voxels:
      hasher.writeU8(ShapeTag.Voxels);
      hasher.writeVec3(shape.voxelSize);
      hasher.writeU64(BigInt(shape.voxelKeys.length));
      for (const key of shape.voxelKeys) {
        hasher.writeI32(key.x);
        hasher.writeI32(key.y);
        hasher.writeI32(key.z);
      }
      break;
    default:
      hasher.writeU8(ShapeTag.Unknown);
      break;
  }
}

function sortByStableId(items) {
  return [...items].sort((a, b) => (a.stableId < b.stableId ? -1 : a.stableId > b.stableId ? 1 : 0));
}

// `world` is a plain-data snapshot (not the live wasm World):
// {
//   gravity: {x,y,z}, timestep,
//   bodies: [{ stableId, bodyType, gravityScale, linearDamping, angularDamping,
//              additionalSolverIterations, ccdEnabled, softCcdPrediction, canSleep,
//              translation, rotation, linvel, angvel, isSleeping, isEnabled }],
//   colliders: [{ stableId, parentStableId, localPose: {translation,rotation}|null,
//                 shape: {tag, ...}, density, friction, frictionCombineRule,
//                 restitution, restitutionCombineRule, isSensor, isEnabled }],
// }
export function worldStateHash(world) {
  const hasher = new StableHasher();

  hasher.writeStr(CANONICAL_HASH_NAME);
  hasher.writeStr("rapier");
  hasher.writeStr(RAPIER_CORE_VERSION);
  hasher.writeF32(world.gravity.x);
  hasher.writeF32(world.gravity.y);
  hasher.writeF32(world.gravity.z);
  hasher.writeF32(world.timestep);

  // PID controllers: v0 fixtures never create any.
  hasher.writeU64(INITIAL_NEXT_PID_CONTROLLER_ID);
  hasher.writeU64(0n);

  const bodies = sortByStableId(world.bodies);
  hasher.writeU64(BigInt(bodies.length));
  for (const body of bodies) {
    hasher.writeStableIdentity(body.stableId);
    hasher.writeU8(body.bodyType);
    hasher.writeF32(body.gravityScale);
    hasher.writeF32(body.linearDamping);
    hasher.writeF32(body.angularDamping);
    hasher.writeU64(BigInt(body.additionalSolverIterations));
    hasher.writeU8(body.ccdEnabled ? 1 : 0);
    hasher.writeF32(body.softCcdPrediction);
    hasher.writeU8(body.canSleep ? 1 : 0);
    hasher.writePose(body.translation, body.rotation);
    hasher.writeVec3(body.linvel);
    hasher.writeVec3(body.angvel);
    hasher.writeU8(body.isSleeping ? 1 : 0);
    hasher.writeU8(body.isEnabled ? 1 : 0);
  }

  const colliders = sortByStableId(world.colliders);
  hasher.writeU64(BigInt(colliders.length));
  for (const collider of colliders) {
    hasher.writeStableIdentity(collider.stableId);
    hasher.writeOptionalStableIdentity(collider.parentStableId);
    if (collider.localPose) {
      hasher.writeU8(1);
      hasher.writePose(collider.localPose.translation, collider.localPose.rotation);
    } else {
      hasher.writeU8(0);
    }
    writeColliderShape(hasher, collider.shape);
    hasher.writeF32(collider.density);
    hasher.writeF32(collider.friction);
    hasher.writeU8(collider.frictionCombineRule);
    hasher.writeF32(collider.restitution);
    hasher.writeU8(collider.restitutionCombineRule);
    hasher.writeU8(collider.isSensor ? 1 : 0);
    hasher.writeU8(collider.isEnabled ? 1 : 0);
  }

  return hasher.finish();
}

export function hashToHex(hash) {
  return BigInt.asUintN(64, hash).toString(16).padStart(16, "0");
}
