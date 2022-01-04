using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace KriilMod_CD
{
    public class JobDriver_TrainCombat : JobDriver
    {
        private static readonly float trainCombatLearningFactor = .15f;
        private int jobStartTick = -1;

        public Thing Dummy => job.GetTarget(TargetIndex.A).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref jobStartTick, "jobStartTick");
        }

        public override string GetReport()
        {
            if (Dummy != null)
            {
                return job.def.reportString.Replace("TargetA", Dummy.LabelShort);
            }

            return base.GetReport();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //fail if can't do violence
            AddFailCondition(() => pawn.WorkTagIsDisabled(WorkTags.Violent));

            jobStartTick = Find.TickManager.TicksGame;

            bool DesignationValidator()
            {
                return !(
                    // Dummy must have the any designation
                    TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation) ||
                    // Dummy must have the melee designation, and the pawn has a melee weapon held
                    TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly) &&
                    pawn.equipment.Primary.def.IsMeleeWeapon ||
                    // Dummy must have the ranged designation, and the pawn has a ranged weapon held
                    TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly) &&
                    pawn.equipment.Primary.def.IsRangedWeapon ||
                    // Dummy must have any designation, and the pawn is unarmed.
                    (TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation) ||
                     TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly) ||
                     TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly)) &&
                    pawn.equipment.Primary == null);
            }

            //make sure thing has train combat designation
            if (DesignationValidator())
            {
                yield break;
            }

            // Make sure our dummy isn't already in use
            this.FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            //fail if dummy is despawned null or forbidden
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            /**** START SWITCH TO TRAINING WEAPON ****/
            //Pick up a training weapon if one is nearby. Remember previous weapon     
            var startingEquippedWeapon = pawn.equipment.Primary;
            ThingWithComps trainingWeapon = null;

            if (startingEquippedWeapon == null ||
                !startingEquippedWeapon.def.IsWithinCategory(CombatTrainingDefOf.TrainingWeapons))
            {
                trainingWeapon = GetNearestTrainingWeapon(startingEquippedWeapon);
                if (trainingWeapon != null && !trainingWeapon.IsForbidden(pawn))
                {
                    //reserve training weapon, goto, and equip
                    if (Map.reservationManager.CanReserve(pawn, trainingWeapon))
                    {
                        pawn.Reserve(trainingWeapon, job);
                        job.SetTarget(TargetIndex.B, trainingWeapon);
                        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                            .FailOnDespawnedNullOrForbidden(TargetIndex.B);
                        yield return CreateEquipToil(TargetIndex.B);
                    }

                    //reserve previous weapon and set as target c
                    if (Map.reservationManager.CanReserve(pawn, startingEquippedWeapon))
                    {
                        pawn.Reserve(startingEquippedWeapon, job);
                        job.SetTarget(TargetIndex.C, startingEquippedWeapon);
                    }
                }
            }

            var reequipStartingWeaponLabel = Toils_General.Label();
            /**** END SWITCH TO TRAINING WEAPON ****/

            //set the job's attack verb to melee or shooting - needed to gotoCastPosition or stack overflow occurs
            yield return Toils_Combat.TrySetJobToUseAttackVerb(TargetIndex.A);
            //based on attack verb, go to cast position
            var gotoCastPos = Toils_Combat.GotoCastPosition(TargetIndex.A, TargetIndex.B, true, 0.95f)
                .EndOnDespawnedOrNull(TargetIndex.A);
            yield return gotoCastPos;
            //try going to new cast position if the target can't be hit from current position
            yield return Toils_Jump.JumpIfTargetNotHittable(TargetIndex.A, gotoCastPos);

            //training loop - jump if done training -> cast verb -> jump to done training
            //if done training jumnp to reequipStartingWeaponLabel
            var doneTraining = Toils_Jump.JumpIf(reequipStartingWeaponLabel, delegate
            {
                if (LearningSaturated())
                {
                    return true;
                }

                return Dummy.Destroyed || Find.TickManager.TicksGame > jobStartTick + 5000 || DesignationValidator();
            });
            yield return doneTraining;
            var castVerb = Toils_Combat.CastVerb(TargetIndex.A, false);
            castVerb.AddFinishAction(LearnAttackSkill);
            yield return castVerb;
            yield return Toils_Jump.Jump(doneTraining);
            yield return reequipStartingWeaponLabel;
            //gain room buff
            yield return Toils_General.Do(TryGainCombatTrainingRoomThought);
            //equip strating weapon
            if (trainingWeapon == null || startingEquippedWeapon == null)
            {
                yield break;
            }

            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.C);
            yield return CreateEquipToil(TargetIndex.C);
        }

        /*
         * Calculates the xp gained. seems like shooting was based off 170f and melee of 200f. Just used 200f for consistency
         */
        private float CalculateXp(Verb verb, Pawn localPawn)
        {
            return trainCombatLearningFactor * 200f * verb.verbProps.AdjustedFullCycleTime(verb, localPawn);
        }

        /*
         * Causes pawn to get impressive combat training room mood buff
         */
        private void TryGainCombatTrainingRoomThought()
        {
            var room = pawn.GetRoom(RegionType.Set_Passable);
            if (room == null)
            {
                return;
            }

            //get the impressive stage index for the current room
            var scoreStageIndex =
                RoomStatDefOf.Impressiveness.GetScoreStageIndex(room.GetStat(RoomStatDefOf.Impressiveness));
            //if the stage index exists in the definition (in xml), gain the memory (and buff)
            if (CombatTrainingDefOf.TrainedInImpressiveCombatTrainingRoom.stages[scoreStageIndex] != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(
                    ThoughtMaker.MakeThought(CombatTrainingDefOf.TrainedInImpressiveCombatTrainingRoom,
                        scoreStageIndex));
            }
        }

        private bool LearningSaturated()
        {
            var verbToUse = pawn.jobs.curJob.verbToUse;
            var saturated = false;

            var skill = pawn.skills.GetSkill(verbToUse.verbProps.IsMeleeAttack
                ? SkillDefOf.Melee
                : SkillDefOf.Shooting);

            if (skill.LearningSaturatedToday ||
                skill.Level == 20 && skill.xpSinceLastLevel >= skill.XpRequiredForLevelUp - 1)
            {
                saturated = true;
            }

            return saturated;
        }

        /*
         * Causes pawn to learn a combat skill based on the verb of the current job
         */
        private void LearnAttackSkill()
        {
            var verbToUse = pawn.jobs.curJob.verbToUse;
            var xpGained = CalculateXp(verbToUse, pawn);
            if (verbToUse.verbProps.IsMeleeAttack)
            {
                pawn.skills.Learn(SkillDefOf.Melee, xpGained);
                CombatTrainingTracker.TrackPawnMeleeSkill(pawn, pawn.skills.GetSkill(SkillDefOf.Melee));
            }
            else
            {
                pawn.skills.Learn(SkillDefOf.Shooting, xpGained);
                CombatTrainingTracker.TrackPawnShootingSkill(pawn, pawn.skills.GetSkill(SkillDefOf.Shooting));
            }
        }


        private ThingWithComps GetNearestTrainingWeaponOfType(ThingDef weaponType)
        {
            var request = ThingRequest.ForDef(weaponType);
            var nearestTrainingWeapon = (ThingWithComps)GenClosest.RegionwiseBFSWorker(TargetA.Thing.Position, pawn.Map,
                request, PathEndMode.OnCell, TraverseParms.For(pawn), x => pawn.CanReserve(x), null, 0, 12, 50f,
                out _, RegionType.Set_Passable, true);
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
            if (nearestTrainingWeapon != null)
            {
                return nearestTrainingWeapon;
            }

            weaponType = CombatTrainingDefOf.Bow_TrainingShort;
            nearestTrainingWeapon = GetNearestTrainingWeaponOfType(weaponType);

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
            if (currentWeapon == null && !TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation))
            {
                if (TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly))
                {
                    nearestTrainingWeapon = GetNearestTrainingWeaponMelee();
                }
                else if (TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly))
                {
                    nearestTrainingWeapon = GetNearestTrainingWeaponRanged();
                }
            }

            // If the pawn does not have a weapon, and the dummy is not restricted, look for the closest training weapon of any kind.
            if (currentWeapon != null || !TargetThingA.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation))
            {
                return nearestTrainingWeapon;
            }

            var request = ThingRequest.ForGroup(ThingRequestGroup.Weapon);
            nearestTrainingWeapon = (ThingWithComps)GenClosest.RegionwiseBFSWorker(TargetA.Thing.Position,
                pawn.Map, request, PathEndMode.OnCell,
                TraverseParms.For(pawn),
                x => CombatTrainingDefOf.TrainingWeapons.DescendantThingDefs.Contains(x.def) &&
                     pawn.CanReserve(x), null, 0, 12, 50f, out _,
                RegionType.Set_Passable, true);

            return nearestTrainingWeapon;
        }

        /*
         * Returns a toil that equips the target index weapon 
         */
        private Toil CreateEquipToil(TargetIndex index)
        {
            var equipment = pawn.jobs.curJob.GetTarget(index);
            var equipToil = new Toil
            {
                initAction = delegate
                {
                    var thingWithComps = (ThingWithComps)equipment;
                    ThingWithComps thingWithComps2;

                    if (thingWithComps.def.stackLimit > 1 && thingWithComps.stackCount > 1)
                    {
                        thingWithComps2 = (ThingWithComps)thingWithComps.SplitOff(1);
                    }
                    else
                    {
                        thingWithComps2 = thingWithComps;
                        thingWithComps2.DeSpawn();
                    }

                    pawn.equipment.MakeRoomFor(thingWithComps2);
                    pawn.equipment.AddEquipment(thingWithComps2);
                    if (thingWithComps.def.soundInteract != null)
                    {
                        thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            equipToil.FailOnDespawnedNullOrForbidden(index);
            return equipToil;
        }
    }
}