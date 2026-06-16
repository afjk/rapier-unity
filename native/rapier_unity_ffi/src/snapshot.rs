use crate::world::RapierUnityWorld;

pub fn snapshot_size(_world: &RapierUnityWorld) -> usize {
    0
}

pub fn snapshot_write(_world: &RapierUnityWorld, out_bytes: *mut u8, len: usize) -> bool {
    // TODO: Implement stable snapshot serialization for rollback/replay.
    len == 0 || !out_bytes.is_null()
}

pub fn snapshot_read(_world: &mut RapierUnityWorld, _bytes: *const u8, _len: usize) -> bool {
    // TODO: Implement stable snapshot restore once the serialization format is versioned.
    false
}
