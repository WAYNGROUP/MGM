﻿using Unity.Entities;
namespace Wayn.Mgm.Effects
{
    public interface IEffectReferenceBuffer : IBufferElementData
    {
        EffectReference EffectReference { get; set; }
    }
}