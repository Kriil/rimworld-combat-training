using System.Collections.Generic;
using RimWorld;
using Verse;

namespace KriilMod_CD
{
    /// <summary>
    /// This class is used to track pawns' combat training xp to avoid having them continuously train.
    /// It does this by keeping track of each pawn's current combat XP since midnight and comparing it to the pawn's current xp
    /// to determine if they should skip the combat training job. 
    /// For pawns with a skill less than level 20, this will have them train until they have maxed out training for the day,
    /// and then restart them only if they drop below 3000 daily xp.
    /// If they are at level 20 and max out their XP, then they will only train to max xp once on that day.
    /// </summary>
	public static class CombatTrainingTracker
	{
        private static Dictionary<string, SkillXpValues> PawnMeleeSkillValues = new Dictionary<string, SkillXpValues>();
        private static Dictionary<string, SkillXpValues> PawnShootingSkillValues = new Dictionary<string, SkillXpValues>();

        // TrackPawnMeleeSkill and TrackPawnShootingSkill are expected to be called when the pawn's combat training xp is updated.
        // They store the pawn's combat xp for use in ShouldSkipCombatTraining.
        public static void TrackPawnMeleeSkill(Pawn pawn, SkillRecord skill)
        {
            int dayOfYear = GenLocalDate.DayOfYear(pawn);
            PawnMeleeSkillValues[pawn.ThingID] = new SkillXpValues(dayOfYear, skill.xpSinceMidnight, skill.xpSinceLastLevel);
        }

        public static void TrackPawnShootingSkill(Pawn pawn, SkillRecord skill)
        {
            int dayOfYear = GenLocalDate.DayOfYear(pawn);
            PawnShootingSkillValues[pawn.ThingID] = new SkillXpValues(dayOfYear, skill.xpSinceMidnight, skill.xpSinceLastLevel);
        }

		public static bool ShouldSkipCombatTraining(Pawn pawn)
		{
            // Reset the pawn's skill values if it is a new day.
            ClearYesterdaysSkillValues(pawn);

            SkillRecord skill = GetCurrentSkill(pawn);

            // Skip training if the max full rate xp has been reached today.
            if (skill.xpSinceMidnight > SkillRecord.MaxFullRateXpPerDay)
            {
                return true;
            }

            // If the pawn has already trained today then it will have a SkillXpValues object.
            // If it does not then it has not trained today, so do not skip training.
            SkillXpValues lastSkillXpValues = GetLastSkillXpValues(pawn);
            if (lastSkillXpValues == null)
            {
                return false;
            }

            // If the pawn is 20 and hit the maximum possible XP during the last training session today, then skip training.
            if (skill.Level == 20 && lastSkillXpValues.XpSinceLastLevel >= skill.XpRequiredForLevelUp - 1f)
            {
                return true;
            }

            // If the pawn's last training session caused it to go over the max daily XP, but the skill has now degraded below 
            // 75% of the max daily xp, then do not skip training.
            if (lastSkillXpValues.XpSinceMidnight >= SkillRecord.MaxFullRateXpPerDay)  
            {
                return skill.xpSinceMidnight > SkillRecord.MaxFullRateXpPerDay * 0.75;
            }

            // Otherwise, do not skip training.
            return false;
		}

        private static void ClearYesterdaysSkillValues(Pawn pawn)
        {
            // This gets the day of the year in the pawn's local time, which is important because the daily XP reset is 
            // done by using midnight in the pawn's local time. See Pawn_SkillTracker::SkillsTick()
            // Here the day is used instead of the hour because it is not guaranteed that the job will be checked every hour, or even every day.
            int dayOfYear = GenLocalDate.DayOfYear(pawn);
            if (PawnMeleeSkillValues.ContainsKey(pawn.ThingID) && PawnMeleeSkillValues[pawn.ThingID].DayOfYear != dayOfYear)
            { 
                PawnMeleeSkillValues.Remove(pawn.ThingID);
            }
            if (PawnShootingSkillValues.ContainsKey(pawn.ThingID) && PawnShootingSkillValues[pawn.ThingID].DayOfYear != dayOfYear)
            {
                PawnShootingSkillValues.Remove(pawn.ThingID);
            }
        }

        private static SkillRecord GetCurrentSkill(Pawn pawn)
        {
            SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
            SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
            var weapon = pawn.equipment.Primary;

            if (weapon == null)
            {
                return melee;
            }

            if (weapon.def.IsRangedWeapon)
            {
                return shooting;
            }

            return melee;
        }

        private static SkillXpValues GetLastSkillXpValues(Pawn pawn)
        {
            var weapon = pawn.equipment.Primary;

            if (weapon != null && weapon.def.IsRangedWeapon && PawnShootingSkillValues.ContainsKey(pawn.ThingID))
            {
                return PawnShootingSkillValues[pawn.ThingID];
            }
            else if (PawnMeleeSkillValues.ContainsKey(pawn.ThingID))
            {
                return PawnMeleeSkillValues[pawn.ThingID];
            }

            return null;
        }
	}
    public class SkillXpValues
    {
        public int DayOfYear;
        public float XpSinceMidnight;
        public float XpSinceLastLevel;
        public SkillXpValues(int dayOfYear, float xpSinceMidnight, float xpSinceLastLevel)
        {
            this.DayOfYear = dayOfYear;
            this.XpSinceMidnight = xpSinceMidnight;
            this.XpSinceLastLevel = xpSinceLastLevel;
        }
    }
}

