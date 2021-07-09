﻿using RimWorld;
using System.Collections.Generic;
using Verse;

namespace KCSG
{
    public struct WeightedStruct
    {
        public StructureLayoutDef structureLayoutDef;
        public float weight;

        public WeightedStruct(StructureLayoutDef structureLayoutDef, float weight)
        {
            this.structureLayoutDef = structureLayoutDef;
            this.weight = weight;
        }

        public static WeightedStruct FromString(string str)
        {
            str = str.TrimStart(new char[]
            {
                '('
            });
            str = str.TrimEnd(new char[]
            {
                ')'
            });
            string[] array = str.Split(new char[]
            {
                ','
            });

            if (array.Length == 2)
            {
                return new WeightedStruct(DefDatabase<StructureLayoutDef>.GetNamedSilentFail(array[0]), float.Parse(array[1].TrimStart(new char[] { ' ' })));
            }
            else return new WeightedStruct(DefDatabase<StructureLayoutDef>.GetNamedSilentFail(array[0]), 1f);
        }
    }

    public class FallingStructure : DefModExtension
    {
        public List<string> weightedStruct = new List<string>();
        public List<ThingDef> thingsToSpawnInDropPod = new List<ThingDef>();
        public List<FactionDef> canBeUsedBy = new List<FactionDef>();
        public bool spawnDormantWhenPossible = true;
        public bool needToHaveSettlements = true;
        internal List<WeightedStruct> WeightedStructs = new List<WeightedStruct>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string str in weightedStruct)
            {
                WeightedStructs.Add(WeightedStruct.FromString(str));
            }

            if (WeightedStructs.Count == 0)
            {
                Log.Error($"FallingStructure defModExtension can't have an empty or null WeightedStructs");
            }
            if (canBeUsedBy.Count == 0)
            {
                Log.Error($"FallingStructure defModExtension can't have an empty or null canBeUsedBy");
            }
            return base.ConfigErrors();
        }
    }
}