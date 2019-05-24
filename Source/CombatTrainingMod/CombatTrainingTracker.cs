using System.Collections.Generic;
using RimWorld;
using Verse;

namespace KriilMod_CD
{
	public static class CombatTrainingTracker
	{
        private static Dictionary<string, SkillXpValues> PawnMeleeSkillValues = new Dictionary<string, SkillXpValues>();
        private static Dictionary<string, SkillXpValues> PawnShootingSkillValues = new Dictionary<string, SkillXpValues>();

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

        /// <summary>
        /// Determines if a pawn should skip combat training.
        /// For pawns with a skill less than level 20, this will have them train until they have maxed out training for the day,
        /// and then restart them only if they drop below 75% of max.
        /// If they are at level 20, then they will only train to max xp once.
        /// </summary>
        /// <param name="pawn">The pawn that is eligible for training.</param>
        /// <returns></returns>
		public static bool ShouldSkipCombatTraining(Pawn pawn)
		{
            ClearYesterdaysShootingSkillValues(pawn);

            SkillRecord skill = GetCurrentSkill(pawn);
            if (skill.xpSinceMidnight > SkillRecord.MaxFullRateXpPerDay)
            {
                return true;
            }

            SkillXpValues lastSkillXpValues = GetLastSkillXpValues(pawn);
            if (lastSkillXpValues == null)
            {
                return false;
            }

            if (skill.Level == 20 && lastSkillXpValues.XpSinceLastLevel >= skill.XpRequiredForLevelUp - 1f)
            {
                return true;
            }

            if (lastSkillXpValues.XpSinceMidnight >= SkillRecord.MaxFullRateXpPerDay)  
            {
                return skill.xpSinceMidnight > SkillRecord.MaxFullRateXpPerDay * 0.75;
            }

            return false;
		}

        private static void ClearYesterdaysShootingSkillValues(Pawn pawn)
        {
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

