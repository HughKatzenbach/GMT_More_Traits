using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Diagnostics;
using Verse;

namespace Garthor_More_Traits
{
	// Spawn of Satan trait.  Doesn't close doors.

	/// <summary>
	/// If a Satan Spawn approaches an autodoor, then have it stay stuck open.
	/// </summary>
	/// <remarks>
	/// This is the method called if the door opens quickly enough to not slow pawns down.  Generally: powered autodoors of light materials.
	/// </remarks>
	[HarmonyPatch(typeof(Building_Door), "Notify_PawnApproaching")]
	public static class Building_Door_Notify_PawnApproaching_Patch
	{
		static void Postfix(Building_Door __instance, Pawn p)
		{
			if(p?.story?.traits?.HasTrait(GMT_DefOf.GMT_Satan_Spawn) ?? false)
			{
				// If the door was closed, this pawn wasn't allowed to interact with it.  If it's open, then it's fair game.
				if (__instance.Open)
				{
					AccessTools.Field(typeof(Building_Door), "ticksUntilClose").SetValue(__instance, int.MaxValue);
				}
			}
		}
	}

	/// <summary>
	/// If a Satan Spawn approaches a normal door, then have it stay stuck open.
	/// </summary>
	[HarmonyPatch(typeof(Building_Door), "StartManualOpenBy")]
	public static class Building_Door_StartManualOpenBy_Patch
	{
		static void Postfix(Building_Door __instance, Pawn opener)
		{
			if (opener?.story?.traits?.HasTrait(GMT_DefOf.GMT_Satan_Spawn) ?? false)
			{
				// Again, only if the door was already opened.  There's no condition on this function in vanilla, but who knows with mods.
				if (__instance.Open)
				{
					AccessTools.Field(typeof(Building_Door), "ticksUntilClose").SetValue(__instance, int.MaxValue);
				}
			}
		}
	}

	/// <summary>
	/// If a Satan Spawn tries to close a door, have it stay open
	/// </summary>
	[HarmonyPatch(typeof(Building_Door), "StartManualCloseBy")]
	public static class Building_Door_StartManualCloseBy_Patch
	{
		static bool Prefix(Building_Door __instance, Pawn closer)
		{
			if (closer?.story?.traits?.HasTrait(GMT_DefOf.GMT_Satan_Spawn) ?? false)
			{
				// We don't close doors.
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// If a Satan Spawn touches a door, set its Friendly Touched time to Big, because it's used to make doors automatically close
	/// </summary>
	[HarmonyPatch(typeof(Building_Door), "CheckFriendlyTouched")]
	public static class Building_Door_CheckFriendlyTouched_Patch
	{
		static void Postfix(Building_Door __instance, Pawn p)
		{
			if (p?.story?.traits?.HasTrait(GMT_DefOf.GMT_Satan_Spawn) ?? false)
			{
				// Recreate the function's logic
				if (!p.HostileTo(__instance) && __instance.PawnCanOpen(p))
				{
					AccessTools.Field(typeof(Building_Door), "lastFriendlyTouchTick").SetValue(__instance, int.MaxValue);
				}
			}
		}
	}

	/// <summary>
	/// Disable ticksUntilClose being reset each tick when a Satan Spawn stands in a door
	/// </summary>
	/// <remarks>
	/// I am getting real tired of this at this point.
	/// </remarks>
	[HarmonyPatch(typeof(Building_Door), "Tick")]
	public static class Building_Door_Tick_Patch
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase mb)
		{
			// Look for where we call CellContains, and add another conditional
			int i = 0;
			CodeInstruction instr;
			List<CodeInstruction> instrs = instructions.ToList();
			var cellContains = AccessTools.Method(typeof(ThingGrid), "CellContains", new Type[] { typeof(IntVec3), typeof(ThingCategory) });
			bool found = false;
			for(; i < instrs.Count; ++i)
			{
				instr = instrs[i];
				// Locate the call to cellContains
				if(instr.Is(OpCodes.Callvirt, cellContains))
				{
					found = true;
					yield return instr;
					break;
				}
				yield return instr;
			}

			if (found)
			{
				// Add another condition: don't mess with ticksToClose if it's very large.
				// It should only be very large if a Satan Spawn has jammed it open.  This saves us from having to actually do a more expensive search.
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Building_Door), "ticksUntilClose"));
				yield return new CodeInstruction(OpCodes.Ldc_I4, 10000);
				yield return new CodeInstruction(OpCodes.Clt);
				yield return new CodeInstruction(OpCodes.And);

				for (++i; i < instrs.Count; ++i)
				{
					yield return instrs[i];
				}
			}
			else
			{
				Log.Error("(GMT) Error patching Building_Door.Tick()");
			}
		}
	}


}
