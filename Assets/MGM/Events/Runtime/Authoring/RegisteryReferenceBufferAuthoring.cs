﻿using System.Collections.Generic;

using Unity.Entities;

using UnityEngine;

namespace Wayn.Mgm.Event.Registry
{
    /// <summary>
    ///  This class handle all the complexity of managing the population of the DynamicBuffer with the correct RegistryReference
    /// </summary>
    /// <typeparam name="BUFFER"></typeparam>
    /// <typeparam name="ELEMENT"></typeparam>
    /// <typeparam name="AUTHORING"></typeparam>
    public abstract class RegisteryReferenceBufferAuthoring<BUFFER, ELEMENT, AUTHORING, EFFECT_COMPONENT_DATA_ELEMENT> : MonoBehaviour, IConvertGameObjectToEntity
        where BUFFER : struct, IRegistryReferenceBuffer
        where ELEMENT : IRegistryEvent
        where AUTHORING : RegisteryReferenceAuthoring<ELEMENT>
        where EFFECT_COMPONENT_DATA_ELEMENT : RegistryEventComponentDataElement<ELEMENT, BUFFER>, new()
    {
        [SerializeField]
        public List<AUTHORING> Entries = new List<AUTHORING>();

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {

            if (!dstManager.HasComponent<RegistryEventReferenceComponentData>(entity))
            {
                dstManager.AddComponentData(entity, new RegistryEventReferenceComponentData());
            }

            RegistryEventReferenceComponentData component = dstManager.GetComponentData<RegistryEventReferenceComponentData>(entity);

            List<ELEMENT> elems = new List<ELEMENT>();

            foreach (AUTHORING entry in Entries)
            {
                elems.Add(entry.Entry);
            }

            RegistryEventComponentDataElement<ELEMENT, BUFFER> elem = new EFFECT_COMPONENT_DATA_ELEMENT();
            elem.SetElementList(elems);
            component.listOfManagedBuffer.Add(elem);
            dstManager.SetComponentData(entity, component);
        }
    }
}
