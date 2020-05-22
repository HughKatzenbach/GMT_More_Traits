using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Garthor_More_Traits.Compatibility
{
    /// <summary>
    /// Patch Syr's Doormats mod to have Slobs ignore doormats
    /// </summary>
    [PatchIfMod("syrchalis.doormats")]
    public static class Syr_Doormats
    {
        [HarmonyPatch(typeof(SyrDoorMats.Building_DoorMat), "Notify_PawnApproaching")]
        [HarmonyPrefix]
        public static bool Prefix_Notify_PawnApproaching(Pawn pawn)
        {
            // Skip the method for pawns that are Slobs
            return !(pawn?.story?.traits?.HasTrait(GMT_DefOf.GMT_Slob) ?? false);
        }

        [HarmonyPatch(typeof(SyrDoorMats.CostToMoveIntoCellPatch), "CostToMoveIntoCell_Postfix")]
        [HarmonyPrefix]
        public static bool Prefix_CostToMoveIntoCell_Postfix(Pawn pawn)
        {
            // Skip the method for pawns that are Slobs
            return !(pawn?.story?.traits?.HasTrait(GMT_DefOf.GMT_Slob) ?? false);
        }
    }
}
