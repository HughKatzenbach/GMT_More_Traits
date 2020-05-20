using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Garthor_More_Traits
{
    // Extension for TraitDefs to allow them to apply a hediff to the pawn when the trait is added to the pawn
    public class GMT_Trait_ModExtension : DefModExtension
    {
        public List<HediffDef> appliedHediffs;
    }

    /// <summary>
    /// Patches TraitSet to add hediffs to a pawn as specified by the TraitDef, when adding a trait
    /// </summary>
    [HarmonyPatch(typeof(TraitSet), "GainTrait")]
    public static class TraitSet_GainTrait_Patch
    {
        private static FieldInfo fieldinfo_pawn = null;
        // Must be a Prefix because we need to not do anything if the trait is already present
        public static bool Prefix(TraitSet __instance, Trait trait)
        {
            if (__instance.HasTrait(trait.def)) return true;
            var collection = trait.def.GetModExtension<GMT_Trait_ModExtension>()?.appliedHediffs ?? null;
            if (collection != null)
            {
                foreach (HediffDef def in collection)
                {
                    if (fieldinfo_pawn == null) fieldinfo_pawn = typeof(TraitSet).GetField("pawn",BindingFlags.Instance | BindingFlags.NonPublic);
                    (fieldinfo_pawn.GetValue(__instance) as Pawn).health.AddHediff(def);
                }
            }
            return true;
        }
    }
}
