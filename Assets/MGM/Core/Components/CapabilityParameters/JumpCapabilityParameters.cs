﻿using System;
using Unity.Entities;

namespace MGM
{
    [Serializable]
    public struct JumpCapabilityParameters : IComponentData
    {
        public float Force;
        public int MaxJumpNumber;
        public int CurrentJumpCount;
        public bool JumpTrigerred;
    }
}
