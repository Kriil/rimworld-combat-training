using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
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
            
            //make sure thing has train combat designation
            this.FailOn(() => !this.TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation) &&
                              !(pawn.equipment.Primary.def.IsMeleeWeapon &&
                              this.TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly)) &&
                              !(pawn.equipment.Primary.def.IsRangedWeapon &&
                              this.TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly)));
            
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
                if (!LearningSaturated())
                {
                    return Dummy.Destroyed || Find.TickManager.TicksGame > this.jobStartTick + 5000;
                }
                return true;

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
         * Returns true if learning has been saturated for today
         */
        private bool LearningSaturated()
        {
            Verb verbToUse = pawn.jobs.curJob.verbToUse;
            // float currentSkill = 0f;
            bool saturated = false;
            if (verbToUse.verbProps.IsMeleeAttack)
            {
                saturated = pawn.skills.GetSkill(SkillDefOf.Melee).LearningSaturatedToday;
            }
            else
            {
                saturated = pawn.skills.GetSkill(SkillDefOf.Shooting).LearningSaturatedToday;
            }
            return saturated;
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
            }
            else
            {
                pawn.skills.Learn(SkillDefOf.Shooting, xpGained, false);
            }
        }

        /*
         * Returns the nearest training weapon.  Prioritizes training weapons of the same type (melee or ranged) of the weapon passed in.
         */
        private ThingWithComps GetNearestTrainingWeapon(Thing currentWeapon)
        {
            ThingRequest request;
            ThingWithComps nearestTrainingWeapon = null;
            //if not armed or armed with melee weapon, look for a training knife
            if (currentWeapon == null || (currentWeapon != null && currentWeapon.def.IsMeleeWeapon))
            {
                request = ThingRequest.ForDef(CombatTrainingDefOf.MeleeWeapon_TrainingKnife);
                nearestTrainingWeapon = (ThingWithComps)GenClosest.RegionwiseBFSWorker(this.TargetA.Thing.Position, pawn.Map, request, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), (Thing x) => pawn.CanReserve(x, 1, -1, null, false), null, 0, 12, 50f, out int regionsSearched, RegionType.Set_Passable, true);
            }
            //if using ranged weapon, look for training weapon base on tech level
            if (currentWeapon != null && !currentWeapon.def.IsMeleeWeapon)
            {
                if (pawn.Faction.def.techLevel.IsNeolithicOrWorse())
                {
                    request = ThingRequest.ForDef(CombatTrainingDefOf.Bow_TrainingShort);
                }
                else
                {
                    request = ThingRequest.ForDef(CombatTrainingDefOf.Gun_TrainingBBGun);
                }
                nearestTrainingWeapon = (ThingWithComps)GenClosest.RegionwiseBFSWorker(this.TargetA.Thing.Position, pawn.Map, request, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), (Thing x) => pawn.CanReserve(x, 1, -1, null, false), null, 0, 12, 50f, out int regionsSearched, RegionType.Set_Passable, true);
            }
            
            // Before finding an alternative (re: not the same) type of training weapon, ensure this is allowed
            // Targets with the designation TrainCombatDesignation allow for weapons types than what's currently held
            if (this.TargetA.Thing.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation))
            {
                //if training weapon wasn't found, cover two cases
                //was not armed or using a melee waepon, equip ranged weapon instead
                //not neolithic, but have a short bow available
                if (nearestTrainingWeapon == null)
                {
                    request = ThingRequest.ForDef(CombatTrainingDefOf.Bow_TrainingShort);
                    nearestTrainingWeapon = (ThingWithComps) GenClosest.RegionwiseBFSWorker(this.TargetA.Thing.Position,
                        pawn.Map, request, PathEndMode.OnCell,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                        (Thing x) => pawn.CanReserve(x, 1, -1, null, false), null, 0, 12, 50f, out int regionsSearched,
                        RegionType.Set_Passable, true);
                }

                //if training weapon wasn't found, cover last case
                //was not armed or using a melee waepon, equip ranged weapon instead - no bow avaialble - check BB gun
                if (nearestTrainingWeapon == null)
                {
                    request = ThingRequest.ForDef(CombatTrainingDefOf.Gun_TrainingBBGun);
                    nearestTrainingWeapon = (ThingWithComps) GenClosest.RegionwiseBFSWorker(this.TargetA.Thing.Position,
                        pawn.Map, request, PathEndMode.OnCell,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                        (Thing x) => pawn.CanReserve(x, 1, -1, null, false), null, 0, 12, 50f, out int regionsSearched,
                        RegionType.Set_Passable, true);
                }
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
