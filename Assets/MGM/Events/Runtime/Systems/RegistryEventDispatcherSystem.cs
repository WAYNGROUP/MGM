﻿using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Wayn.Mgm.Event.Registry
{
    public abstract class RegistryEventDispatcher<COMMAND> : SystemBase
        where COMMAND : struct, IEventRegistryCommand
    {
        private List<NativeQueue<COMMAND>> CommandsQueues = new List<NativeQueue<COMMAND>>();
        public NativeMultiHashMap<MapKey, COMMAND> CommandsMap;
        private JobHandle JobHandle;

        private JobHandle CrossFrameJobHandle;

        public JobHandle finalJobHandle;

        public void AddJobHandleFromProducer(JobHandle jh)
        {
            JobHandle = JobHandle.CombineDependencies(JobHandle, jh);
        }

        public void AddConsumerJobHandle(JobHandle jh)
        {
            CrossFrameJobHandle = JobHandle.CombineDependencies(CrossFrameJobHandle, jh);
        }

        public NativeQueue<COMMAND>.ParallelWriter CreateCommandsQueue()
        {
            CommandsQueues.Insert(0,new NativeQueue<COMMAND>(Allocator.TempJob));
            return CommandsQueues[0].AsParallelWriter();
        }


        protected override void OnDestroy()
        {
            foreach (NativeQueue<COMMAND> CommandsQueue in CommandsQueues)
            {
                CommandsQueue.Dispose();
            }
            CommandsMap.Dispose();
        }

        struct AllocateCommandsMap : IJob
        {
            [ReadOnly]
            public NativeArray<int> TotalCommandCount;

            [WriteOnly]
            public NativeMultiHashMap<MapKey, COMMAND> CommandsMap;

            public void Execute()
            {
                CommandsMap.Capacity = TotalCommandCount[0];
            }
        }


        struct CountCommands : IJob
        {
            [ReadOnly]
            public NativeQueue<COMMAND> CommandsQueue;

            public NativeArray<int> TotalCommandCount;

            public void Execute()
            {
                TotalCommandCount[0] += CommandsQueue.Count;
            }
        }

        [BurstCompile]
        struct MapCommands : IJob
        {
         
            public NativeQueue<COMMAND> CommandsQueue;

            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeMultiHashMap<MapKey, COMMAND>.ParallelWriter CommandsMap;

            public void Execute()
            {
                COMMAND command;
                while (CommandsQueue.TryDequeue(out command))
                {
                    CommandsMap.Add(new MapKey() { Value = command.RegistryReference.TypeId }, command);
                }

            }
        }




        protected override void OnUpdate()
        {
    
            var inputDeps = JobHandle.CombineDependencies(Dependency, JobHandle, CrossFrameJobHandle);

            if (CommandsMap.IsCreated)
            {
                JobHandle = CommandsMap.Dispose(inputDeps);
            }
            CommandsMap = new NativeMultiHashMap<MapKey, COMMAND>(0, Allocator.TempJob);

            // Schedule in sequence the realocation of the necessary memory to handle each commands based on the queues sizes.
            // Not done in parallel as the resize consist of an new allocation and a copy.
            // Doing it in parallel would result in branching allocations.
            NativeArray<int> counter = new NativeArray<int>(1,Allocator.TempJob);
 
            for (int i = 0; i < CommandsQueues.Count; i++)
            {
                JobHandle = new CountCommands()
                {
                    CommandsQueue = CommandsQueues[i],
                    TotalCommandCount = counter
                }.Schedule(JobHandle);
            }

            JobHandle AllocationJH = new AllocateCommandsMap()
            {
                TotalCommandCount = counter,
                CommandsMap = CommandsMap
            }.Schedule(JobHandle);

            JobHandle CounterDisposedJH = counter.Dispose(AllocationJH);

            NativeArray<JobHandle> MapperJobHanldes = new NativeArray<JobHandle>(CommandsQueues.Count, Allocator.TempJob);
            var CommandsMapParallelWriter = CommandsMap.AsParallelWriter();
           
            for (int i = 0; i < CommandsQueues.Count; i++)
            {
                var jh = new MapCommands()
                {
                    CommandsMap = CommandsMapParallelWriter,
                    CommandsQueue = CommandsQueues[i]
                }.Schedule(AllocationJH);

                MapperJobHanldes[i] = CommandsQueues[i].Dispose(jh);
            }

            CommandsQueues.Clear();

            Dependency = JobHandle.CombineDependencies(AllocationJH, JobHandle.CombineDependencies(MapperJobHanldes));
            finalJobHandle = Dependency;
            MapperJobHanldes.Dispose();


        }

    }

    public struct MapKey : IEquatable<MapKey>
    {
        public int Value;

        public override bool Equals(object obj)
        {
            if (!(obj is MapKey))
            {
                return false;
            }

            var key = (MapKey)obj;
            return Value == key.Value;
        }

        public bool Equals(MapKey other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }


}
