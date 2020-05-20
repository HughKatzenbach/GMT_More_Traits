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
	[StaticConstructorOnStartup]
	class Main
	{
		internal static bool modpresent_JecsTools = false;

		static Main()
		{
			var harmony = new Harmony("Garthor.More_Traits");
			// Check for presence of the JecsTools mod
			if (AccessTools.TypeByName("AbilityUser.Verb_UseAbility") != null)
			{
				modpresent_JecsTools = true;
			}
			harmony.PatchAll();
		}
	}
}
