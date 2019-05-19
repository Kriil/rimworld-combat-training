using HugsLib.Utils;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace KriilMod_CD
{
    public class WorkGiver_TrainCombat : WorkGiver_Scanner
    {

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (forced)
            {
                return pawn.story.WorkTagIsDisabled(WorkTags.Violent);
            }
            else
            {
                return pawn.skills.GetSkill(SkillDefOf.Melee).LearningSaturatedToday || pawn.skills.GetSkill(SkillDefOf.Shooting).LearningSaturatedToday;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn.story.WorkTagIsDisabled(WorkTags.Violent))
            {
                JobFailReason.Is(null, "IsIncapableOfViolence".Translate());
                return false;
            }
            if (!t.IsForbidden(pawn))
            {
                LocalTargetInfo target = t;
                if (pawn.CanReserve(target, 1, -1, null, forced))
                {
                    if (HugsLibUtility.HasDesignation(t, CombatTrainingDefOf.TrainCombatDesignation) || 
                        HugsLibUtility.HasDesignation(t, CombatTrainingDefOf.TrainCombatDesignationMeleeOnly) || 
                        HugsLibUtility.HasDesignation(t, CombatTrainingDefOf.TrainCombatDesignationRangedOnly))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {

            Verb verb = pawn.TryGetAttackVerb(t, false);
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
            List<Designation> desList = pawn.Map.designationManager.allDesignations;
            for (int i = 0; i < desList.Count; i++)
            {
                Designation des = desList[i];
                if (des.def == CombatTrainingDefOf.TrainCombatDesignation || 
                    des.def == CombatTrainingDefOf.TrainCombatDesignationMeleeOnly ||
                    des.def == CombatTrainingDefOf.TrainCombatDesignationRangedOnly)
                {
                    yield return des.target.Thing;
                }
            }
        }
    }
}