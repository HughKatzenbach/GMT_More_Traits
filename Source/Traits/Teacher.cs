using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Garthor_More_Traits
{
	// Teacher trait.  Will teach some skills to other pawns when interacting with them.

	public static class GMT_Teacher_Helper
	{
		// Experience points granted by a teacher per interaction, per difference in skill level
		internal const float teacher_xp_per_level_difference = 200f;

		// Teaching multipliers for certain interaction types
		internal static readonly Dictionary<InteractionDef, float> teaching_factors = new Dictionary<InteractionDef, float>()
		{
			// Pawns do a lot of these at once, so they should significantly less impactful
			{ InteractionDefOf.BuildRapport, 0.1f },
			// Insults probably shouldn't be as informative as normal conversation
			{ InteractionDefOf.Insult,       0.4f }
		};
	}

	[HarmonyPatch(typeof(RimWorld.Pawn_InteractionsTracker), "TryInteractWith")]
	public static class Pawn_InteractionsTracker_TryInteractWith_Patch_Teacher
	{
		static void Postfix(Pawn_InteractionsTracker __instance, Pawn recipient, InteractionDef intDef)
		{
			// pawn is a private field, so we have to use reflection to access it.
			Pawn pawn = AccessTools.Field(__instance.GetType(), "pawn").GetValue(__instance) as Pawn;

			// bail if one of the pawns doesn't have any skills.
			if (pawn.skills == null || recipient.skills == null) return;

			if (pawn?.story?.traits?.HasTrait(GMT_DefOf.GMT_Teacher) ?? false)
			{
				// Make 2 attempts to teach a skill.  Broad base of skills => better chance of teaching.
				for (int i = 0; i < 2; ++i)
				{
					SkillDef random = DefDatabase<SkillDef>.AllDefsListForReading.RandomElement<SkillDef>();

					int diff = pawn.skills.GetSkill(random).Level - recipient.skills.GetSkill(random).Level;
					if (diff > 0)
					{
						recipient.skills.GetSkill(random).Learn(diff * GMT_Teacher_Helper.teacher_xp_per_level_difference * GMT_Teacher_Helper.teaching_factors.TryGetValue(intDef, 1.0f));
						break;
					}
				}
			}
		}
	}
}
