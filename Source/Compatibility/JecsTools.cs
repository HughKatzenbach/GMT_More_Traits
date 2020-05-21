using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Garthor_More_Traits.Compatibility
{
    [StaticConstructorOnStartup]
    internal static class JecsTools
    {
        internal static bool active = false;
        static JecsTools()
        {
            if (AccessTools.TypeByName("AbilityUser.Verb_UseAbility") != null)
            {
                active = true;
            }
        }

        /// <summary>
        /// Determines if a verb is a harmful JecsTools Verb_UseAbility verb
        /// </summary>
        /// <param name="verb">The verb to test</param>
        /// <param name="harmful">Whether or not the verb is harmful, if it is an appropriate verb</param>
        /// <returns>true if the supplied verb is a JecsTools Verb_UseAbility and the data in the out argument is valid, false if it is not an appropriate verb type</returns>
        internal static bool isHarmfulVerb(Verb verb, out bool harmful)
        {
            AbilityUser.VerbProperties_Ability verbProps = ((verb as AbilityUser.Verb_UseAbility)?.UseAbilityProps ?? null);
            if (verbProps != null)
            {
                harmful = verbProps.isViolent;
                return true;
            }
            harmful = false;
            return false;
        }
    }
}
