using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Garthor_More_Traits
{
	[DefOf]
	public static class GMT_DefOf
	{
		static GMT_DefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(GMT_DefOf));
		}

		public static TraitDef GMT_Animal_Friend;
		public static TraitDef GMT_Boring;
		public static TraitDef GMT_Caravaneer;
		public static TraitDef GMT_Satan_Spawn;
		public static TraitDef GMT_Teacher;
		public static TraitDef GMT_Slob;
		public static TraitDef GMT_Drunken_Master;
		public static TraitDef GMT_Berserker;
		public static TraitDef GMT_Juggernaut;

		public static ThoughtDef GMT_Animal_Friend_Hurt_Animal;

		public static HediffDef GMT_Hediff_Bored;

		public static HediffDef GMT_Hediff_Berserker_Ire;
		public static HediffDef GMT_Hediff_Berserker_Rage;
		public static MentalStateDef GMT_MentalState_Berserking;
		public static JobDef GMT_Job_AttackMeleeDowned;
		public static InteractionDef GMT_Interaction_InsultEnemy; // Insult that won't start social fights, to be used on enemies
	}
}
