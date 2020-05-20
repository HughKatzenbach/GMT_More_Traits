using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Diagnostics;
using Verse;

namespace Garthor_More_Traits
{
	// Caravaneer trait: 15% faster caravan move speed (non-stacking, not when downed, not when prisoner)

	public static class GMT_Caravaneer_Helper
	{
		// How much faster a caravaneer makes a caravan move
		internal const float caravaneer_bonus = 0.15f;
	}
	// note: Maybe change the format of the bonus string?  Say who is providing the bonus?  Change its location?
	[HarmonyPatch(typeof(RimWorld.Planet.CaravanTicksPerMoveUtility), "GetTicksPerMove")]
	[HarmonyPatch(new Type[] { typeof(List<Pawn>), typeof(float), typeof(float), typeof(StringBuilder) })]
	public static class CaravanTicksPerMoveUtility_GetTicksPerMove_Patch
	{
		static void Postfix(ref int __result, List<Pawn> pawns, StringBuilder explanation)
		{
			foreach(Pawn p in pawns)
			{
				if (!p.Downed && !p.IsPrisoner && (p.story?.traits?.HasTrait(GMT_DefOf.GMT_Caravaneer) ?? false))
				{
					__result = Mathf.RoundToInt(__result / (GMT_Caravaneer_Helper.caravaneer_bonus + 1));
					if (explanation != null)
					{
						explanation.AppendLine();
						explanation.Append($"  {"GMT_CaravaneerPresent".Translate()} ({GMT_Caravaneer_Helper.caravaneer_bonus:P0}): {(60000f / (float)__result):0.#} {"TilesPerDay".Translate()}");
					}
					break;
				}
			}
		}
	}
}
