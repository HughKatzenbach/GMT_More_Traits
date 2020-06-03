using HarmonyLib;
using Garthor_More_Traits;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using System.Diagnostics;

namespace Garthor_More_Traits
{
	// Animal Friend trait: animals cannot target pawn, pawn cannot target animals

	public static class GMT_Animal_Friend_Helper
	{
		/// <summary>
		/// Returns true if two Things should be friendly due to Animal Empathy: one is an Animal Friend and the other is an Animal (or Insect Hive)
		/// </summary>
		/// <param name="t1"></param>
		/// <param name="t2"></param>
		/// <returns></returns>
		internal static bool ShouldBeFriendly(Thing t1, Thing t2)
		{
			Pawn p1 = t1 as Pawn;
			Pawn p2 = t2 as Pawn;
			if((p1 != null) && isAnimalFriend(p1) && isAnimalOrHive(t2))
			{
				return true;
			}
			if((p2 != null) && isAnimalFriend(p2) && isAnimalOrHive(t1))
			{
				return true;
			}
			return false;
		}
		internal static bool isAnimalFriend(Pawn p)
		{
			return p?.story?.traits?.HasTrait(GMT_DefOf.GMT_Animal_Friend) ?? false;
		}

		internal static bool isAnimalOrHive(Thing t)
		{
			if (t != null && t.def == ThingDefOf.Hive) return true;
			else return isAnimalOrHive(t as Pawn);
		}

		internal static bool isAnimalOrHive(Pawn p)
		{
			return p?.RaceProps?.Animal ?? false;
		}
		internal static bool isHarmfulVerb(Verb verb)
		{
			
			// If JecsTools is present, and this is a JecsTools ability, return the isViolent property
			if(Compatibility.JecsTools.active && Compatibility.JecsTools.isHarmfulVerb(verb, out bool harmful))
			{
				return harmful;
			}
			// Otherwise, we go to our vanilla fallback: it harms health, or it's a cast ability with a negative goodwill impact.
			return verb.HarmsHealth() || ((verb as Verb_CastAbility)?.ability?.EffectComps?.Exists((CompAbilityEffect e) => { return e.Props.goodwillImpact < 0; }) ?? false);
		}
	}

	/*
	 * This is no longer necessary as of 1.1.2645: addition of neverDisabledBasedOnWorkTypes tag
	 * 
	/// <summary>
	/// Changes SkillDef.IsDisabled to not consider work types for Shooting and Melee.  This prevents Shooting from showing as disabled for Animal Friends.
	/// </summary>
	/// <remarks>
	/// Disabling the Hunting worktype causes the Shooting skill to show as disabled.
	/// This is because, by default, a skill X gets disabled if every work type with X as a relevant skill is disabled.
	/// The only work type with Shooting as a relevant skill is Hunting, so Shooting gets disabled if Hunting is disabled, even if the pawn is capable of violence.
	/// This is going to bite me in the ass later, but I don't see another option.
	/// </remarks>
	[HarmonyPatch(typeof(SkillDef), "IsDisabled")]
	public static class SkillDef_IsDisabled_Patch
	{
		static void Prefix(SkillDef __instance, ref IEnumerable<WorkTypeDef> disabledWorkTypes)
		{
			if (__instance.defName == "Shooting" || __instance.defName == "Melee")
			// Alternative condition (more generic, might cause problems?):
			//if((__instance.disablingWorkTags & WorkTags.Violent) != WorkTags.None)
			{
				disabledWorkTypes = new List<WorkTypeDef>();
			}
		}
	}
	*/

	/// <summary>
	/// Prevent predators from selecting an Animal Friend as a target for hunting.
	/// </summary>
	/// <remarks>
	/// Possibly they wouldn't be able to attack, causing the animal to follow them around in a permanent hunting state.
	/// Don't quote me on that though since this is the first thing I thought to patch.
	/// </remarks>
	[HarmonyPatch(typeof(FoodUtility), "IsAcceptablePreyFor", new Type[] { typeof(Pawn), typeof(Pawn) })]
	public static class FoodUtility_IsAcceptablePreyFor_Patch
	{
		static bool Prefix(ref bool __result, Pawn predator, Pawn prey)
		{
			if (GMT_Animal_Friend_Helper.ShouldBeFriendly(predator, prey))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Prevent animals and Animal Friends from seeing each other as hostile, so they will not automatically acquire each other as targets.
	/// </summary>
	[HarmonyPatch(typeof(GenHostility), "HostileTo", new Type[] { typeof(Thing), typeof(Thing) })]
	public static class GenHostility_HostileTo_Patch
	{
		static void Postfix(ref bool __result, Thing a, Thing b)
		{
			// Only interested in turning hostility into nonhostility
			if (__result)
			{
				// If one is an animal, and the other is an animal friend, change the result to false
				__result = !GMT_Animal_Friend_Helper.ShouldBeFriendly(a, b);
			}
		}
	}

	/// <summary>
	/// Prevent the player from using the right-click float menu to make an Animal Friend melee attack an animal or vice versa.
	/// </summary>
	/// <remarks>
	/// Blocks only the default no-weapon melee attack, as actual weapons have their own verbs.
	/// </remarks>
	[HarmonyPatch(typeof(FloatMenuUtility), "GetMeleeAttackAction")]
	public static class FloatMenuUtility_GetMeleeAttackAction_Patch
	{
		static bool Prefix(ref Action __result, Pawn pawn, LocalTargetInfo target, out string failStr)
		{
			if (GMT_Animal_Friend_Helper.ShouldBeFriendly(pawn, target.Thing))
			{
				failStr = "GMT_CannotHarmAnimals".Translate();
				__result = null;
				return false;
			}
			failStr = "";
			return true;
		}
	}

	/// <summary>
	/// Prevent the player from using the right-click float menu to make an Animal Friend ranged attack an animal or vice versa.
	/// </summary>
	/// <remarks>
	/// Does nothing in vanilla, but should be compatible with races that have an inherent ranged attack.
	/// </remarks>
	[HarmonyPatch(typeof(FloatMenuUtility), "GetRangedAttackAction")]
	public static class FloatMenuUtility_GetRangedAttackAction_Patch
	{
		static bool Prefix(ref Action __result, Pawn pawn, LocalTargetInfo target, out string failStr)
		{
			if (GMT_Animal_Friend_Helper.ShouldBeFriendly(pawn, target.Thing))
			{
				failStr = "GMT_CannotHarmAnimals".Translate();
				__result = null;
				return false;
			}
			failStr = "";
			return true;
		}
	}

	/// <summary>
	/// Cause verbs to fail to validate targets if it's between an animal friend and an animal, and the verb is considered violent.
	/// </summary>
	/// <remarks>
	/// This ought to block the user from ordering the pawn to do anything violent to an animal.
	/// A violent verb is one that either harms health (for melee and ranged attacks), or is an ability that would cause a goodwill loss,
	/// on the assumption that any hostile abilities will have goodwill loss attached.
	/// There may be a better way to evaluate abilities.
	/// </remarks>
	[HarmonyPatch(typeof(Verb), "ValidateTarget")]
	public static class Verb_ValidateTarget_Patch
	{
		static bool Prefix(Verb __instance, ref bool __result, LocalTargetInfo target)
		{
			if (GMT_Animal_Friend_Helper.isHarmfulVerb(__instance) && GMT_Animal_Friend_Helper.ShouldBeFriendly(__instance.Caster, target.Thing))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Prevent Animal Friends from slaughtering animals.
	/// </summary>
	[HarmonyPatch(typeof(WorkGiver_Slaughter), "HasJobOnThing")]
	public static class WorkGiver_Slaughter_HasJobOnThing_Patch
	{
		static bool Prefix(bool __result, Pawn pawn, Thing t)
		{
			if (GMT_Animal_Friend_Helper.ShouldBeFriendly(pawn, t))
			{
				__result = false;
				Verse.AI.JobFailReason.Is("Garthor_CannotHarmAnimals".Translate(), null);
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Give Animal Friends a bad thought if they harm an animal
	/// </summary>
	/// <remarks>
	/// This will also catch Euthanasia, so we special case it out.
	/// Botched surgery injuries do not show up here.
	/// </remarks>
	[HarmonyPatch(typeof(Thing), "PostApplyDamage")]
	public static class Thing_PostApplyDamage_Patch
	{
		static bool Prefix(Thing __instance, DamageInfo dinfo, float totalDamageDealt)
		{
			if (totalDamageDealt > 0.0f && GMT_Animal_Friend_Helper.ShouldBeFriendly(__instance, dinfo.Instigator))
			{
				if (dinfo.Def.defName != "ExecutionCut")
				{
					(dinfo.Instigator as Pawn)?.needs?.mood?.thoughts?.memories?.TryGainMemory(GMT_DefOf.GMT_Animal_Friend_Hurt_Animal, (__instance as Pawn));
				}
			}
			return true;
		}
	}

	// TODO: violent (but non-damaging) AOE abilities hitting animals?
	// TODO: does a manhunter ambush against a caravan with only Animal Friends cause you to get stuck until they calm down?

	// Note: In case of something going wrong, consider Pawn TryStartAttack for blocking attacks.
}
