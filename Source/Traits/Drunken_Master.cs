using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Garthor_More_Traits
{
    // Increases melee dodge and hit with drunkenness
 
    [StaticConstructorOnStartup]
    public static class GMT_Drunken_Master_Helper
    {
        static internal List<ThingDef> alcoholic_items = null;

        static GMT_Drunken_Master_Helper()
        {
            // Cache a list of alcoholic items so we're not iterating over all thingdefs every time we generate a pawn with this trait.
            // This is cached with the lifespan of the application, but there shouldn't be new defs added except by mods, which require restart.
            if (alcoholic_items == null)
            {
                alcoholic_items = DefDatabase<ThingDef>.AllDefsListForReading.Where(delegate (ThingDef x)
                {
                    if (x.category != ThingCategory.Item)
                    {
                        return false;
                    }
                    CompProperties_Drug compProperties = x.GetCompProperties<CompProperties_Drug>();
                    return compProperties != null && compProperties.chemical == ChemicalDefOf.Alcohol;
                }).ToList();
            }
        }
    }

    /// <summary>
    /// Increases stat based on drunkenness level
    /// </summary>
    public class GMT_StatPart_Drunken_Master : StatPart
    {
        /// <summary>
        /// Curve mapping stages of the AlcoholHigh hediff to stat changes
        /// </summary>
        private SimpleCurve curve;
        public override void TransformValue(StatRequest req, ref float val)
        {
            string label;
            float value = Calculate_Drunk_Bonus(req, out label);
            if (value > 0) val += value;
        }

        public override string ExplanationPart(StatRequest req)
        {
            string stage_label;
            float value = Calculate_Drunk_Bonus(req, out stage_label);
            if(value != int.MinValue)
            {
                if(value != 0)
                {
                    return $"{GMT_DefOf.GMT_Drunken_Master.degreeDatas[0].label} ({stage_label}): {value:+0.0;0.0}";
                }
                else
                {
                    // TODO: display "Drunken Master (sober): X".  Localization issues with "sober".
                    return null;
                }
            }
            return null;
        }

        private float Calculate_Drunk_Bonus(StatRequest req, out string label)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn?.story?.traits?.HasTrait(GMT_DefOf.GMT_Drunken_Master) ?? false)
            {
                Hediff alcohol = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.AlcoholHigh);
                if (alcohol != null)
                {
                    int stage = alcohol.CurStageIndex;
                    label = alcohol.CurStage.label;
                    return curve.Evaluate(stage);
                }
                label = null;
                return 0f;
            }
            label = null;
            return int.MinValue;
        }
    }

    /// <summary>
    /// Postfixes FindCombatEnhancingDrug to take a beer if the pawn is a Drunken Master
    /// 1.1 Compatibility patch
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_TakeCombatEnhancingDrug), "FindCombatEnhancingDrug")]
    public class JobGiver_TakeCombatEnhancingDrug_FindCombatEnhancingDrug_Patch
    {
        static bool Prepare()
        {
            MethodInfo target = typeof(JobGiver_TakeCombatEnhancingDrug).GetMethod("FindCombatEnhancingDrug", BindingFlags.NonPublic | BindingFlags.Instance);
            if (target == null || target.HasAttribute<ObsoleteAttribute>())
            {
                return false;
            }
            return true;
        }
        static void Postfix(ref Thing __result, Pawn pawn)
        {
            if (pawn?.story?.traits?.HasTrait(GMT_DefOf.GMT_Drunken_Master) ?? false)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    Thing thing = pawn.inventory.innerContainer[i];
                    CompDrug compDrug = thing.TryGetComp<CompDrug>();
                    if (compDrug != null && compDrug.Props.chemical == ChemicalDefOf.Alcohol)
                    {
                        var doer = thing.def.ingestible.outcomeDoers.Find(
                                        (IngestionOutcomeDoer x) => ((x as IngestionOutcomeDoer_GiveHediff)?.hediffDef ?? null) == HediffDefOf.AlcoholHigh
                                    ) as IngestionOutcomeDoer_GiveHediff;
                        if (doer == null) continue;

                        Hediff hediff = HediffMaker.MakeHediff(doer.hediffDef, pawn, null);
                        hediff.Severity = doer.severity;
                        // Only count this as a drug if it won't down the pawn
                        if (!pawn.health.WouldBeDownedAfterAddingHediff(hediff))
                        {
                            Log.Message("result");
                            __result = thing;
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Postfixes FindCombatEnhancingDrug to take a beer if the pawn is a Drunken Master
    /// Used for 1.2 onward
    /// </summary>
    // TODO: it may be preferable to patch GetCombatEnhancingDrugs instead, but that's more effort.
    [HarmonyPatch(typeof(Pawn_InventoryTracker), "FindCombatEnhancingDrug")]
    public class Pawn_InventoryTracker_FindCombatEnhancingDrug_Patch
    {
        static bool Prepare()
        {
            MethodInfo target = typeof(Pawn_InventoryTracker).GetMethod("FindCombatEnhancingDrug", BindingFlags.Public | BindingFlags.Instance);
            if (target == null || target.HasAttribute<ObsoleteAttribute>())
            {
                return false;
            }
            return true;
        }
        static void Postfix(Pawn_InventoryTracker __instance, ref Thing __result)
        {
            Pawn pawn = __instance.pawn;
            if (pawn?.story?.traits?.HasTrait(GMT_DefOf.GMT_Drunken_Master) ?? false)
            {
                for (int i = 0; i < __instance.innerContainer.Count; i++)
                {
                    Thing thing = __instance.innerContainer[i];
                    CompDrug compDrug = thing.TryGetComp<CompDrug>();
                    if (compDrug != null && compDrug.Props.chemical == ChemicalDefOf.Alcohol)
                    {
                        var doer = thing.def.ingestible.outcomeDoers.Find(
                                        (IngestionOutcomeDoer x) => ((x as IngestionOutcomeDoer_GiveHediff)?.hediffDef ?? null) == HediffDefOf.AlcoholHigh
                                    ) as IngestionOutcomeDoer_GiveHediff;
                        if (doer == null) continue;

                        Hediff hediff = HediffMaker.MakeHediff(doer.hediffDef, pawn, null);
                        hediff.Severity = doer.severity;
                        // Only count this as a drug if it won't down the pawn
                        if (!pawn.health.WouldBeDownedAfterAddingHediff(hediff))
                        {
                            __result = thing;
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Postfixes GiveDrugsIfAddicted so that Drunken Master pawns will generate with a few beers on-hand.
    /// </summary>
    [HarmonyPatch(typeof(PawnInventoryGenerator), "GiveDrugsIfAddicted")]
    public static class PawnInventoryGenerator_GiveDrugsIfAddicted_Patch
    {
        static void Postfix(Pawn p)
        {
            if (p?.story?.traits?.HasTrait(GMT_DefOf.GMT_Drunken_Master) ?? false)
            {
                // if they already have an alcohol addiction, they don't need any more beer
                if (p.health.hediffSet.GetHediffs<Hediff_Addiction>().Any((Hediff_Addiction x) => x.Chemical == ChemicalDefOf.Alcohol)) return;

                // Give them some beers (or something else alcoholic).  Smaller number than normal addiction, rather them not get hammered.
                ThingDef def;
                if (GMT_Drunken_Master_Helper.alcoholic_items == null) return;
                if (GMT_Drunken_Master_Helper.alcoholic_items.Where((ThingDef x) => p.Faction == null || x.techLevel <= p.Faction.def.techLevel).TryRandomElement(out def))
                {
                    int stackCount = Rand.RangeInclusive(Mathf.RoundToInt(0*p.BodySize), Mathf.RoundToInt(2*p.BodySize));
                    Thing thing = ThingMaker.MakeThing(def, null);
                    thing.stackCount = stackCount;
                    p.inventory.TryAddItemNotForSale(thing);
                }
            }
        }
    }
}
