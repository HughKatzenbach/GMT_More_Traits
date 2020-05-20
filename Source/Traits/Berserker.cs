using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Garthor_More_Traits
{
	// Berserker trait: When damaged, can enter rage.  Rage majorly increases combat stats, but renders pawn uncontrollable.

	/// <summary>
	/// Increases severity as berserker is damaged.  Has a chance to trigger berserker rage, increasing with severity.  Clears berserker rage at 0 severity.
	/// </summary>
	public class GMT_Hediff_Berserker_Ire : HediffWithComps
	{
		private static FieldInfo fieldinfo_visible;
		// getter/setter for private field in Hediff
		protected bool visible
		{
			get
			{
				// use reflection to expose the private targetAcquireRadius field
				if (fieldinfo_visible == null) fieldinfo_visible = typeof(Hediff).GetField("visible", BindingFlags.Instance | BindingFlags.NonPublic);
				return (bool)fieldinfo_visible.GetValue(this);
			}
			set
			{
				if (fieldinfo_visible == null) fieldinfo_visible = typeof(Hediff).GetField("visible", BindingFlags.Instance | BindingFlags.NonPublic);
				fieldinfo_visible.SetValue(this, value);
			}
		}

		// Severity will hit 100% after taking (coreHealth * scaleFactor) damage
		protected static float scaleFactor = 0.50f;

		// Rate, per second, that ire severity will naturally drain
		public const float ire_trickle = 0.06f;

		// Chance of entering rage when hit, based on current severity
		public const float rage_chance_factor = 0.20f;

		public override void Tick()
		{
			base.Tick();

			if (this.pawn.IsHashIntervalTick(60) && this.Severity > this.def.minSeverity)
			{
				this.Severity -= ire_trickle;

				if (this.Severity == this.def.minSeverity)
				{
					Exit_Rage();
					this.visible = false;
				}
			}
		}
		public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
			// If the pawn does not have the berserker trait, remove this hediff
			// This handles situations where the trait gets removed (by other mods, or scenario forced trait rules might do it too)
			if((this.pawn?.story?.traits?.GetTrait(GMT_DefOf.GMT_Berserker) ?? null) == null)
			{
				this.pawn.health.RemoveHediff(this);
				Hediff rage = this.pawn.health.hediffSet.GetFirstHediffOfDef(GMT_DefOf.GMT_Hediff_Berserker_Rage);
				if (rage != null) this.pawn.health.RemoveHediff(rage);
			}

			if ((dinfo.Instigator as Pawn) != null && this.pawn.HostileTo(dinfo.Instigator as Pawn))
			{
				// Check chance before increasing severity.  Don't want to go into rage on the first hit.
				if (Rand.Chance(rage_chance_factor * this.Severity))
				{
					Enter_Rage();
				}
				else
				{
					this.Severity += totalDamageDealt / this.pawn.RaceProps.body.corePart.def.GetMaxHealth(this.pawn) / scaleFactor;
				}
			}
		}

		protected virtual void Enter_Rage()
		{
			// TODO: may be some conditions where we don't want to enter the state
			if(this.pawn.mindState.mentalStateHandler.TryStartMentalState(GMT_DefOf.GMT_MentalState_Berserking))
			{
				this.Severity = 1f;
			}
		}

		protected virtual void Exit_Rage()
		{
			if (this.pawn.MentalStateDef == GMT_DefOf.GMT_MentalState_Berserking)
			{
				this.pawn.MentalState.RecoverFromState();
				// If we are a player pawn, they should be drafted again so they don't run away
				if(this.pawn.IsColonist && !this.pawn.health.Downed)
				{
					this.pawn.drafter.Drafted = true;
				}
			}
		}
	}

	/// <summary>
	/// Provides combat buffs.  Trickles severity into Berserker Ire to keep it active longer.
	/// </summary>
	public class GMT_Hediff_Berserker_Rage : HediffWithComps
	{
		// Amount of ire trickle that being enraged counters
		public const float rage_ire_trickle_factor = 0.4f;
		public override void Tick()
		{
			base.Tick();

			if (this.pawn.IsHashIntervalTick(60))
			{
				// Trickle in some severity each second
				Hediff ire = this.pawn.health.hediffSet.GetFirstHediffOfDef(GMT_DefOf.GMT_Hediff_Berserker_Ire);
				if(ire != null) ire.Severity += GMT_Hediff_Berserker_Ire.ire_trickle * rage_ire_trickle_factor;

				// If we lost our mental state unexpectedly, end this state
				if (this.pawn.MentalStateDef != GMT_DefOf.GMT_MentalState_Berserking)
				{
					this.pawn.health.RemoveHediff(this);
				}
			}
		}
	}

	public class GMT_MentalState_Berserking : MentalState
	{
		public Pawn insult_target;
		public int lastInsultTicks = int.MinValue;

		public override void MentalStateTick()
		{
			// Insult a nearby pawn
			if(this.pawn.IsHashIntervalTick(180))
			{
				IEnumerable<Pawn> targets = from thing in GenRadial.RadialDistinctThingsAround(this.pawn.Position, this.pawn.Map, 5.0f, false) 
											where (thing as Pawn) != null && InteractionUtility.CanReceiveRandomInteraction(thing as Pawn)
											select thing as Pawn;
				Pawn target;
				if (targets.TryRandomElement(out target)) {
					// Try to insult the target.  Use the enemy insult interaction for enemies, so we don't start social fights with them.
					this.pawn.interactions.TryInteractWith(target, /*this.pawn.HostileTo(target)*/true ? GMT_DefOf.GMT_Interaction_InsultEnemy : InteractionDefOf.Insult);
				}
			}

			base.MentalStateTick();
		}

		// When we enter the Berserking mental state, gain the Rage buff
		public override void PreStart()
		{
			base.PreStart();

			this.pawn.health.AddHediff(GMT_DefOf.GMT_Hediff_Berserker_Rage);
		}

		// When we leave the Berserking mental state, remove the Rage buff
		public override void PostEnd()
		{
			base.PostEnd();

			Hediff rage = this.pawn.health.hediffSet.GetFirstHediffOfDef(GMT_DefOf.GMT_Hediff_Berserker_Rage);
			if (rage != null) this.pawn.health.RemoveHediff(rage);
		}
	}

	public class GMT_MentalStateWorker_Berserking : MentalStateWorker
	{
	}
	/*
	/// <summary>
	/// Insult nearby friendlies.
	/// </summary>
	public class GMT_Berserker_Ranting : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			GMT_MentalState_Berserking mentalState_Berserking = pawn.MentalState as GMT_MentalState_Berserking;
			if (mentalState_Berserking == null || mentalState_Berserking.insult_target == null || !pawn.CanReach(mentalState_Berserking.insult_target, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
			{
				return null;
			}
			return JobMaker.MakeJob(JobDefOf.Insult, mentalState_Berserking.insult_target);
		}
	}
	*/
	/// <summary>
	/// Attacks nearby enemies, including ones that are non-threats (downed enemies, unpowered turrets).
	/// </summary>
	public class GMT_JobGiver_FightDownedEnemies : JobGiver_AIFightEnemies
	{
		// Copy of private field in JobGiver_AIFightEnemy
		private static readonly IntRange My_ExpiryInterval_Melee = new IntRange(360, 480);

		private static FieldInfo fieldinfo_targetAcquireRadius;
		// Getter to access private JobGiver_AIFightEnemytargetAcquireRadius
		protected float targetAcquireRadius
		{
			get
			{
				// use reflection to expose the private targetAcquireRadius field
				if (fieldinfo_targetAcquireRadius == null) fieldinfo_targetAcquireRadius = typeof(JobGiver_AIFightEnemy).GetField("targetAcquireRadius", BindingFlags.Instance | BindingFlags.NonPublic);
				return (float)fieldinfo_targetAcquireRadius.GetValue(this);
			}
		}
		// Copy of JobGiver_AIFightEnemy, but it only will use melee verbs
		protected override Job TryGiveJob(Pawn pawn)
		{
			this.UpdateEnemyTarget(pawn);
			Thing enemyTarget = pawn.mindState.enemyTarget;
			if (enemyTarget == null)
			{
				return null;
			}
			Pawn pawn2 = enemyTarget as Pawn;
			if (pawn2 != null && pawn2.IsInvisible())
			{
				return null;
			}
			Verb verb = pawn.meleeVerbs.TryGetMeleeVerb(enemyTarget);
			if (verb != null)
			{
				return this.MeleeAttackJob(enemyTarget);
			}
			return null;
		}

		// Similar to base FindAttackTarget, but instead we will consider non-threats and non-auto-targetable enemies, so we target downed pawns
		protected override Thing FindAttackTarget(Pawn pawn)
		{
			TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedReachableIfCantHitFromMyPos;
			return (Thing)AttackTargetFinder.BestAttackTarget(pawn, targetScanFlags, (Thing x) => this.ExtraTargetValidator(pawn, x), 0f, this.targetAcquireRadius, this.GetFlagPosition(pawn), this.GetFlagRadius(pawn), false, true);
		}

		// Override MeleeAttackJob to use our custom melee attack job that allows for attacking downed enemies
		protected override Job MeleeAttackJob(Thing enemyTarget)
		{
			Job job = JobMaker.MakeJob(GMT_DefOf.GMT_Job_AttackMeleeDowned, enemyTarget);
			job.expiryInterval = GMT_JobGiver_FightDownedEnemies.My_ExpiryInterval_Melee.RandomInRange;
			job.checkOverrideOnExpire = true;
			job.expireRequiresEnemiesNearby = true;
			return job;
		}
	}

	public class GMT_JobDriver_AttackMeleeDowned : JobDriver_AttackMelee
	{
		// Copy of JobDriver_AttackMelee.MakeNewToils(), but allows for meleeing downed pawns unconditionally.
		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_General.DoAtomic(delegate
			{
				Pawn pawn = this.job.targetA.Thing as Pawn;
				if (pawn != null && pawn.Downed)
				{
					this.job.killIncappedTarget = true;
				}
			});
			yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
			yield return Toils_Combat.FollowAndMeleeAttack(TargetIndex.A, delegate
			{
				Thing thing = this.job.GetTarget(TargetIndex.A).Thing;
				if (this.pawn.meleeVerbs.TryMeleeAttack(thing, this.job.verbToUse, false))
				{
					if (this.pawn.CurJob == null || this.pawn.jobs.curDriver != this)
					{
						return;
					}
					// Uses private field, not concerned with it
					/*
					this.numMeleeAttacksMade++;
					if (this.numMeleeAttacksMade >= this.job.maxNumMeleeAttacks)
					{
						base.EndJobWith(JobCondition.Succeeded);
						return;
					}
					*/
				}
			}).FailOnDespawnedOrNull(TargetIndex.A);
			yield break;
		}
	}

	// TODO: JobGiver to switch to a melee weapon (in lieu of ranged), either by picking one up, or via cross-mod compatibility (Simple Sidearms, CE, etc.)
}
