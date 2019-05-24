using System;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HugsLib.Core;
using HugsLib.Utils;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace KriilMod_CD
{
    public class JobDriver_TrainCombat : JobDriver
    {
        private int jobStartTick = -1;

        private static readonly float trainCombatLearningFactor = .15f;

        public Thing Dummy
        {
            get
            {
                return this.job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.jobStartTick, "jobStartTick", 0, false);
        }

        public override string GetReport()
        {
            if (this.Dummy != null)
            {
                return this.job.def.reportString.Replace("TargetA", this.Dummy.LabelShort);
            }
            return base.GetReport();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return ReservationUtility.Reserve(pawn, this.job.GetTarget(TargetIndex.A), this.job, 1, -1, null);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //fail if can't do violence
            base.AddFailCondition(delegate
            {
                return this.pawn.story.WorkTagIsDisabled(WorkTags.Violent);
            });

            this.jobStartTick = Find.TickManager.TicksGame;

            Func<bool> designationValidator = () => !(
                // Dummy must have the any designation
                TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation)
                ||
                // Dummy must have the melee designation, and the pawn has a melee weapon held
                TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly) &&
                pawn.equipment.Primary.def.IsMeleeWeapon
                ||
                // Dummy must have the ranged designation, and the pawn has a ranged weapon held
                TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly) &&
                pawn.equipment.Primary.def.IsRangedWeapon
                ||
                // Dummy must have any designation, and the pawn is unarmed.
                (TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation) ||
                 TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly) ||
                 TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly)) &&
                pawn.equipment.Primary == null);
            
            //make sure thing has train combat designation
            if (designationValidator())
            {
                yield break;
            }

            // Make sure our dummy isn't already in use
            this.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            
            //fail if dummy is despawned null or forbidden
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            /**** START SWITCH TO TRAINING WEAPON ****/
            //Pick up a training weapon if one is nearby. Remember previous weapon     
            ThingWithComps startingEquippedWeapon = this.pawn.equipment.Primary;
            ThingWithComps trainingWeapon = null;

            if (startingEquippedWeapon == null || !startingEquippedWeapon.def.IsWithinCategory(CombatTrainingDefOf.TrainingWeapons))
            {
                trainingWeapon = GetNearestTrainingWeapon(startingEquippedWeapon);
                if (trainingWeapon != null && !trainingWeapon.IsForbidden(pawn))
                {
                    //reserve training weapon, goto, and equip
                    if (this.Map.reservationManager.CanReserve(this.pawn, trainingWeapon, 1, -1, null, false))
                    {
                        this.pawn.Reserve(trainingWeapon, this.job, 1, -1, null, true);
                        this.job.SetTarget(TargetIndex.B, trainingWeapon);
                        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
                        yield return CreateEquipToil(TargetIndex.B);
                    }

                    //reserve previous weapon and set as target c
                    if (this.Map.reservationManager.CanReserve(this.pawn, startingEquippedWeapon, 1, -1, null, false))
                    {
                        this.pawn.Reserve(startingEquippedWeapon, this.job, 1, -1, null, true);
                        this.job.SetTarget(TargetIndex.C, startingEquippedWeapon);
                    }
                }
            }
            Toil reequipStartingWeaponLabel = Toils_General.Label();
            /**** END SWITCH TO TRAINING WEAPON ****/

            //set the job's attack verb to melee or shooting - needed to gotoCastPosition or stack overflow occurs
            yield return Toils_Combat.TrySetJobToUseAttackVerb(TargetIndex.A);
            //based on attack verb, go to cast position
            Toil gotoCastPos = Toils_Combat.GotoCastPosition(TargetIndex.A, true, 0.95f).EndOnDespawnedOrNull(TargetIndex.A);
            yield return gotoCastPos;
            //try going to new cast position if the target can't be hit from current position
            yield return Toils_Jump.JumpIfTargetNotHittable(TargetIndex.A, gotoCastPos);

            //training loop - jump if done training -> cast verb -> jump to done training
            //if done training jumnp to reequipStartingWeaponLabel
            Toil doneTraining = Toils_Jump.JumpIf(reequipStartingWeaponLabel, delegate
            {
                SkillRecord skill = GetCurrentSkill();

                if (skill.LearningSaturatedToday || (skill.Level == 20 && skill.xpSinceLastLevel >= skill.XpRequiredForLevelUp - 1))
                {
                    return true;
                }
                else
                {
                    return Dummy.Destroyed || Find.TickManager.TicksGame > this.jobStartTick + 5000 || designationValidator();
                }
            });
            yield return doneTraining;
            Toil castVerb = Toils_Combat.CastVerb(TargetIndex.A, false);
            castVerb.AddFinishAction(delegate
            {
                LearnAttackSkill();
            });
            yield return castVerb;
            yield return Toils_Jump.Jump(doneTraining);
            yield return reequipStartingWeaponLabel;
            //gain room buff
            yield return Toils_General.Do(delegate
            {
                TryGainCombatTrainingRoomThought();
            });
            //equip strating weapon
            if (trainingWeapon != null && startingEquippedWeapon != null)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.C);
                yield return CreateEquipToil(TargetIndex.C);
            }
            yield break;
        }

        /*
         * Calculates the xp gained. seems like shooting was based off 170f and melee of 200f. Just used 200f for consistency
         */
        private float CalculateXp(Verb verb, Pawn pawn)
        {
            return trainCombatLearningFactor * 200f * verb.verbProps.AdjustedFullCycleTime(verb, pawn);
        }

        /*
         * Causes pawn to get impressive combat training room mood buff
         */
        private void TryGainCombatTrainingRoomThought()
        {
            Room room = pawn.GetRoom(RegionType.Set_Passable);
            if (room != null)
            {
                //get the impressive stage index for the current room
                int scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(room.GetStat(RoomStatDefOf.Impressiveness));
                //if the stage index exists in the definition (in xml), gain the memory (and buff)
                if (CombatTrainingDefOf.TrainedInImpressiveCombatTrainingRoom.stages[scoreStageIndex] != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(CombatTrainingDefOf.TrainedInImpressiveCombatTrainingRoom, scoreStageIndex), null);
                }
            }
        }

        /*
         * Returns the current skill: shooting or melee.
         */
        private SkillRecord GetCurrentSkill()
        {
            Verb verbToUse = pawn.jobs.curJob.verbToUse;
            if (verbToUse.verbProps.IsMeleeAttack) {
                return pawn.skills.GetSkill(SkillDefOf.Melee);
            }
            else
            {
                return pawn.skills.GetSkill(SkillDefOf.Shooting);
            }
        }

        /*
         * Causes pawn to learn a combat skill based on the verb of the current job
         */
        private void LearnAttackSkill()
        {
            Verb verbToUse = pawn.jobs.curJob.verbToUse;
            float xpGained = CalculateXp(verbToUse, pawn);
            if (verbToUse.verbProps.IsMeleeAttack)
            {
                pawn.skills.Learn(SkillDefOf.Melee, xpGained, false);
                CombatTrainingTracker.TrackPawnMeleeSkill(pawn, pawn.skills.GetSkill(SkillDefOf.Melee));
            }
            else
            {
                pawn.skills.Learn(SkillDefOf.Shooting, xpGained, false);
                CombatTrainingTracker.TrackPawnShootingSkill(pawn, pawn.skills.GetSkill(SkillDefOf.Shooting));
            }
        }

        
        private ThingWithComps GetNearestTrainingWeaponOfType(ThingDef weaponType)
        {
            ThingRequest request = ThingRequest.ForDef(weaponType);
            ThingWithComps nearestTrainingWeapon = (ThingWithComps)GenClosest.RegionwiseBFSWorker(this.TargetA.Thing.Position, pawn.Map, request, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), (Thing x) => pawn.CanReserve(x, 1, -1, null, false), null, 0, 12, 50f, out int regionsSearched, RegionType.Set_Passable, true);
            return nearestTrainingWeapon;
        }

        private ThingWithComps GetNearestTrainingWeaponMelee()
        {
            return GetNearestTrainingWeaponOfType(CombatTrainingDefOf.MeleeWeapon_TrainingKnife);
        }

        private ThingWithComps GetNearestTrainingWeaponRanged()
        {
            ThingWithComps nearestTrainingWeapon = null;
            ThingDef weaponType;
            // Get a BB gun if available.
            if (!pawn.Faction.def.techLevel.IsNeolithicOrWorse())
            {
                weaponType = CombatTrainingDefOf.Gun_TrainingBBGun;
                nearestTrainingWeapon = GetNearestTrainingWeaponOfType(weaponType);
            }
            // If no BB gun was found (perhaps due to tech level), look for a bow.
            if (nearestTrainingWeapon == null)
            {
                weaponType = CombatTrainingDefOf.Bow_TrainingShort;
                nearestTrainingWeapon = GetNearestTrainingWeaponOfType(weaponType);
            }

            return nearestTrainingWeapon;
        }

        /* 
         * Returns the nearest training weapon.  Enforces training weapons of the same type (melee or ranged) of the
         * weapon passed in, unless the pawn is unarmed.
         */
        private ThingWithComps GetNearestTrainingWeapon(Thing currentWeapon)
        {
            ThingWithComps nearestTrainingWeapon = null;
            
            // If the pawn has a melee weapon, look for a training knife.
            if (currentWeapon != null && currentWeapon.def.IsMeleeWeapon)
            {
                nearestTrainingWeapon = GetNearestTrainingWeaponMelee();
            }
            // If the pawn has a ranged weapon, look for a training ranged weapon.
            if (currentWeapon != null && !currentWeapon.def.IsMeleeWeapon)
            {
                nearestTrainingWeapon = GetNearestTrainingWeaponRanged();
            }
            
            // If the pawn does not have a weapon, and the dummy is restricted, look for the appropriate weapon type.
            if (currentWeapon == null && !this.TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation))
            {
                if (this.TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly))
                {
                    nearestTrainingWeapon = GetNearestTrainingWeaponMelee();
                }
                else if (this.TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly))
                {
                    nearestTrainingWeapon = GetNearestTrainingWeaponRanged();
                }
            }
            
            // If the pawn does not have a weapon, and the dummy is not restricted, look for the closest training weapon of any kind.
            if (currentWeapon == null && this.TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation))
            {
                ThingRequest request = ThingRequest.ForGroup(ThingRequestGroup.Weapon);
                nearestTrainingWeapon = (ThingWithComps) GenClosest.RegionwiseBFSWorker(this.TargetA.Thing.Position,
                    pawn.Map, request, PathEndMode.OnCell,
                    TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                    (Thing x) => CombatTrainingDefOf.TrainingWeapons.DescendantThingDefs.Contains(x.def) &&
                                 pawn.CanReserve(x, 1, -1, null, false), null, 0, 12, 50f, out int regionsSearched,
                    RegionType.Set_Passable, true);
            }

            return nearestTrainingWeapon;
        }

        /*
         * Returns a toil that equips the target index weapon 
         */
        private Toil CreateEquipToil(TargetIndex index)
        {
            LocalTargetInfo equipment = pawn.jobs.curJob.GetTarget(index);
            Toil equipToil = new Toil
            {
                initAction = delegate ()
                {

                    ThingWithComps thingWithComps = (ThingWithComps)equipment;
                    ThingWithComps thingWithComps2;

                    if (thingWithComps.def.stackLimit > 1 && thingWithComps.stackCount > 1)
                    {
                        thingWithComps2 = (ThingWithComps)thingWithComps.SplitOff(1);
                    }
                    else
                    {
                        thingWithComps2 = thingWithComps;
                        thingWithComps2.DeSpawn(DestroyMode.Vanish);
                    }
                    this.pawn.equipment.MakeRoomFor(thingWithComps2);
                    this.pawn.equipment.AddEquipment(thingWithComps2);
                    if (thingWithComps.def.soundInteract != null)
                    {
                        thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(this.pawn.Position, this.pawn.Map, false));
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            equipToil.FailOnDespawnedNullOrForbidden(index);
            return equipToil;
        }
    }
}
