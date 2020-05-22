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
		static Main()
		{
			var harmony = new Harmony("Garthor.More_Traits");
			harmony.PatchAll();

			// Get all types in the Compatibility namespace
			var types = from x in Assembly.GetExecutingAssembly().GetTypes()
						where x.IsClass && x.Namespace == "Garthor_More_Traits.Compatibility"
						select x;

			Log.Message("Looking for compatibility patches");

			// Iterate through types tagged with the PatchIfMod attribute
			foreach (var t in types)
			{
				Log.Message($"Looking at {t}");
				var attr = t.GetCustomAttribute<Compatibility.PatchIfModAttribute>();
				if (attr != null && attr.IsModLoaded())
				{
					Dictionary<MethodBase, PatchProcessor> patches = new Dictionary<MethodBase, PatchProcessor>();
					foreach (var method in t.GetMethods())
					{
						foreach (var harmonyPatch in method.GetCustomAttributes<HarmonyPatch>())
						{
							// First add a PatchProcessor for the method we're targeting to the patches list, if not already present
							MethodBase mb = harmonyPatch.info.declaringType.GetMethod(harmonyPatch.info.methodName);
							if (!patches.ContainsKey(mb))
							{
								patches.Add(mb, new PatchProcessor(harmony, mb));
							}
							PatchProcessor patch = patches[mb];
							// Next add this method to the prefix, postfix, transpiler, or finalizer lists, as appropriate
							// (bit repetitive, but it's short and would be a bit convoluted to improve it)
							if (method.GetCustomAttributes<HarmonyPrefix>().Any())
							{
								patch.AddPrefix(new HarmonyMethod(method));
							}
							if (method.GetCustomAttributes<HarmonyPostfix>().Any())
							{
								patch.AddPostfix(new HarmonyMethod(method));
							}
							if (method.GetCustomAttributes<HarmonyTranspiler>().Any())
							{
								patch.AddTranspiler(new HarmonyMethod(method));
							}
							if (method.GetCustomAttributes<HarmonyFinalizer>().Any())
							{
								patch.AddFinalizer(new HarmonyMethod(method));
							}
						}
					}
					// Apply the patches for this mod
					foreach (PatchProcessor processor in patches.Values)
					{
						processor.Patch();
					}
				}
			}
		}
	}
}
