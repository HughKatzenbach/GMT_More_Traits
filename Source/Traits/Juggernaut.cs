using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Garthor_More_Traits.Traits
{
    // Juggernaut trait: cannot be stunned by hits, is not slowed down by melee combat or by high stopping power ranged attacks.
    public static class GMT_Juggernaut_Helper
    {

    }

    /// <summary>
    /// Prefix Pawn_StanceTracker.StaggerFor() to stagger for 0 ticks if the associated pawn has the Juggernaut trait
    /// </summary>
    [HarmonyPatch(typeof(Pawn_StanceTracker), "StaggerFor")]
    [HarmonyPatch(new Type[] { typeof(int) })]
    public static class Pawn_StanceTracker_StaggerFor_Patch
    {
        static void Prefix(Pawn_StanceTracker __instance, ref int ticks)
        {
            if (__instance.pawn?.story?.traits?.HasTrait(GMT_DefOf.GMT_Juggernaut) ?? false)
            {
                ticks = 0;
            }
        }
    }


    /// <summary>
    /// Prefix StunHandler.StunFor() to stun for 0 ticks if the associated pawn has the Juggernaut trait
    /// </summary>
    //[HarmonyPatch(typeof(StunHandler), "StunFor")]
    //[HarmonyPatch(new Type[] { typeof(int), typeof(Thing), typeof(bool) })]
    [HarmonyPatch]
    public class StunHandler_StunFor_Patch
    {
        public MethodBase TargetMethod()
        {
            // Return the _NewTmp method if it exists, otherwise StunFor (for old versions, or new ones once the NewTmp is removed)
            MethodInfo newTmp = typeof(StunHandler).GetMethod("StunFor_NewTmp");
            if (newTmp != null) return newTmp;
            else return typeof(StunHandler).GetMethod("StunFor");
        }
        static void Prefix(StunHandler __instance, ref int ticks, ref bool addBattleLog)
        {
            if ((__instance.parent as Pawn)?.story?.traits?.HasTrait(GMT_DefOf.GMT_Juggernaut) ?? false)
            {
                ticks = 0;
                // Also hide the stun from the battle log
                addBattleLog = false;
            }
        }
    }
}
