﻿using System;
using Unity.Entities;
using Wayn.Mgm.Events;
using Wayn.Mgm.Events.Registry;

[Serializable]
[InternalBufferCapacity(1)]
public struct OnCollideEffectsOnSelfBuffer : IEffectReferenceBuffer
{
    public RegistryReference EffectReference { get; set; }
}
