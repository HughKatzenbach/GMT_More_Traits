using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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
    [HarmonyPatch(typeof(StunHandler), "StunFor")]
    [HarmonyPatch(new Type[] { typeof(int), typeof(Thing), typeof(bool) })]
    public static class StunHandler_StunFor_Patch
    {
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
