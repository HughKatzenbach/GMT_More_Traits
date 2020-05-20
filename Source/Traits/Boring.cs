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
using Verse.AI;

namespace Garthor_More_Traits
{
	// Boring trait: socially interacting with pawns will cause a build up of the Bored hediff, leading to small stat penalties and occasional falling asleep

	public static class GMT_Boring_Helper
	{
		// Default severity of boring
		internal const float boring_severity = 0.15f;

		// Boring multipliers for certain interaction types
		internal static readonly Dictionary<InteractionDef, float> bored_factors = new Dictionary<InteractionDef, float>()
		{
			// Pawns do a lot of these at once, so they should significantly less impactful
			{ InteractionDefOf.BuildRapport, 0.15f },
			{ InteractionDefOf.AnimalChat,   0.05f },
			// Insulting sprees should be a bit less impactful, but boring someone with insults is too funny to drop this too low
			{ InteractionDefOf.Insult,       0.4f }
		};
	}

	[HarmonyPatch(typeof(RimWorld.Pawn_InteractionsTracker), "TryInteractWith")]
	public static class Pawn_InteractionsTracker_TryInteractWith_Patch_Boring
	{
		static void Postfix(Pawn_InteractionsTracker __instance, Pawn recipient, InteractionDef intDef)
		{
			// pawn is a private field, so we have to use reflection to access it.
			Pawn pawn = AccessTools.Field(__instance.GetType(), "pawn").GetValue(__instance) as Pawn;
			if(pawn?.story?.traits?.HasTrait(GMT_DefOf.GMT_Boring) ?? false)
			{
				// Boring pawns don't find each other boring
				if (recipient?.story?.traits?.HasTrait(GMT_DefOf.GMT_Boring) ?? false) return;

				Hediff hediff = HediffMaker.MakeHediff(GMT_DefOf.GMT_Hediff_Bored, recipient);
				hediff.Severity = GMT_Boring_Helper.boring_severity * GMT_Boring_Helper.bored_factors.TryGetValue(intDef, 1.0f);
				recipient.health.AddHediff(hediff);
			}
		}
	}

	public class GMT_MentalState_BoredToSleep : MentalState
	{
	}

	public class GMT_MentalStateWorker_BoredToSleep : MentalStateWorker
	{
	}
}