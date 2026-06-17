use rapier3d::prelude::{ColliderHandle, ImpulseJointHandle, RigidBodyHandle};

#[repr(C)]
#[derive(Clone, Copy, Debug, Eq, Hash, PartialEq)]
pub struct RapierUnityRigidBodyHandle {
    pub index: u32,
    pub generation: u32,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Eq, Hash, PartialEq)]
pub struct RapierUnityJointHandle {
    pub index: u32,
    pub generation: u32,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Eq, Hash, PartialEq)]
pub struct RapierUnityColliderHandle {
    pub index: u32,
    pub generation: u32,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Eq, Hash, PartialEq)]
pub struct RapierUnityPidControllerHandle {
    pub id: u64,
}

impl RapierUnityRigidBodyHandle {
    pub const INVALID: Self = Self {
        index: u32::MAX,
        generation: u32::MAX,
    };

    pub fn is_valid(self) -> bool {
        self != Self::INVALID
    }
}

impl RapierUnityColliderHandle {
    pub const INVALID: Self = Self {
        index: u32::MAX,
        generation: u32::MAX,
    };

    pub fn is_valid(self) -> bool {
        self != Self::INVALID
    }
}

impl RapierUnityPidControllerHandle {
    pub const INVALID: Self = Self { id: 0 };

    pub fn is_valid(self) -> bool {
        self.id != 0
    }
}

impl From<RigidBodyHandle> for RapierUnityRigidBodyHandle {
    fn from(value: RigidBodyHandle) -> Self {
        let (index, generation) = value.into_raw_parts();
        Self { index, generation }
    }
}

impl From<RapierUnityRigidBodyHandle> for RigidBodyHandle {
    fn from(value: RapierUnityRigidBodyHandle) -> Self {
        RigidBodyHandle::from_raw_parts(value.index, value.generation)
    }
}

impl From<ColliderHandle> for RapierUnityColliderHandle {
    fn from(value: ColliderHandle) -> Self {
        let (index, generation) = value.into_raw_parts();
        Self { index, generation }
    }
}

impl From<RapierUnityColliderHandle> for ColliderHandle {
    fn from(value: RapierUnityColliderHandle) -> Self {
        ColliderHandle::from_raw_parts(value.index, value.generation)
    }
}

impl RapierUnityJointHandle {
    pub const INVALID: Self = Self {
        index: u32::MAX,
        generation: u32::MAX,
    };

    pub fn is_valid(self) -> bool {
        self != Self::INVALID
    }
}

impl From<ImpulseJointHandle> for RapierUnityJointHandle {
    fn from(value: ImpulseJointHandle) -> Self {
        let (index, generation) = value.into_raw_parts();
        Self { index, generation }
    }
}

impl From<RapierUnityJointHandle> for ImpulseJointHandle {
    fn from(value: RapierUnityJointHandle) -> Self {
        ImpulseJointHandle::from_raw_parts(value.index, value.generation)
    }
}
