using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace KriilMod_CD
{
    class JobDriver_EquipWeapon : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
            if (this.Map.reservationManager.CanReserve(pawn, target, 1, -1, null))
            {
                return ReservationUtility.Reserve(pawn, target, this.job, 1, -1, null);
            } else
            {
                CombatTrainingController.Logger.Message("Couldn't reserve " + target.Thing.Label);
                return false;
            }
            
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            base.AddEndCondition(delegate
            {
                if (this.pawn.story.WorkTagIsDisabled(WorkTags.Violent))
                {
                    return JobCondition.Incompletable;
                }
                return JobCondition.Ongoing;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return CreateEquipToil(TargetIndex.A);            
            yield break;
        }

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
