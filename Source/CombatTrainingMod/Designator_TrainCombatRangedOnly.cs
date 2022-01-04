using RimWorld;
using Verse;

namespace KriilMod_CD
{
    public class Designator_TrainCombatRangedOnly : Designator_BaseTrainCombat
    {
        public Designator_TrainCombatRangedOnly()
        {
            defaultLabel = "DesignatorTrainCombatRangedOnly".Translate();
            defaultDesc = "DesignatorTrainCombatRangedOnlyDesc".Translate();
            icon = icon = TexCommand.Attack;
            defOf = CombatTrainingDefOf.TrainCombatDesignationRangedOnly;
        }
    }
}