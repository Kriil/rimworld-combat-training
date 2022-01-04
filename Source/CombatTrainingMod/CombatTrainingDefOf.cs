using RimWorld;
using Verse;

namespace KriilMod_CD
{
    [DefOf]
    public static class CombatTrainingDefOf
    {
        public static DesignationDef TrainCombatDesignation;
        public static DesignationDef TrainCombatDesignationMeleeOnly;
        public static DesignationDef TrainCombatDesignationRangedOnly;
        public static WorkGiverDef TrainCombat;
        public static WorkTypeDef TrainingCombat;
        public static JobDef TrainOnCombatDummy;
        public static RoomRoleDef CombatTrainingRoom;
        public static ThoughtDef TrainedInImpressiveCombatTrainingRoom;
        public static ThingDef MeleeWeapon_TrainingKnife;
        public static ThingDef Gun_TrainingBBGun;
        public static ThingDef Bow_TrainingShort;
        public static ThingCategoryDef TrainingWeapons;

        static CombatTrainingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CombatTrainingDefOf));
        }
    }
}