using System;
using System.Collections.Generic;
using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace KriilMod_CD
{
    public class WorkGiver_TrainCombat : WorkGiver_Scanner
    {
        protected Func<DesignationDef, bool> getDesignationFilter(Pawn pawn)
        {
            var startingEquippedWeapon = pawn.equipment.Primary;
            Func<DesignationDef, bool> filter;
            if (startingEquippedWeapon == null)
            {
                filter = x => x == CombatTrainingDefOf.TrainCombatDesignation ||
                              x == CombatTrainingDefOf.TrainCombatDesignationMeleeOnly ||
                              x == CombatTrainingDefOf.TrainCombatDesignationRangedOnly;
            }
            else if (startingEquippedWeapon.def.IsMeleeWeapon)
            {
                filter = x => x == CombatTrainingDefOf.TrainCombatDesignation ||
                              x == CombatTrainingDefOf.TrainCombatDesignationMeleeOnly;
            }
            else
            {
                filter = x => x == CombatTrainingDefOf.TrainCombatDesignation ||
                              x == CombatTrainingDefOf.TrainCombatDesignationRangedOnly;
            }

            return filter;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (CompatibilityUtility.IsGuest(pawn))
            {
                return true;
            }

            if (forced)
            {
                return pawn.WorkTagIsDisabled(WorkTags.Violent);
            }

            return CombatTrainingTracker.ShouldSkipCombatTraining(pawn);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                JobFailReason.Is(null, "IsIncapableOfViolence".Translate());
                return false;
            }

            if (t.IsForbidden(pawn))
            {
                return false;
            }

            LocalTargetInfo target = t;
            if (!pawn.CanReserve(target, 1, -1, null, forced))
            {
                return false;
            }

            var startingEquippedWeapon = pawn.equipment.Primary;
            if (startingEquippedWeapon == null)
            {
                if (t.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation) ||
                    t.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly) ||
                    t.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly))
                {
                    return true;
                }
            }
            else if (startingEquippedWeapon.def.IsMeleeWeapon)
            {
                if (t.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation) ||
                    t.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly))
                {
                    return true;
                }
            }
            else
            {
                if (t.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation) ||
                    t.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly))
                {
                    return true;
                }
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var verb = pawn.TryGetAttackVerb(t);
            if (verb != null)
            {
                return new Job(CombatTrainingDefOf.TrainOnCombatDummy, t)
                {
                    verbToUse = verb
                };
            }

            return null;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var filter = getDesignationFilter(pawn);
            var desList = pawn.Map.designationManager.allDesignations;
            foreach (var des in desList)
            {
                if (filter(des.def))
                {
                    yield return des.target.Thing;
                }
            }
        }
    }
}