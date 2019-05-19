using RimWorld;
using Verse;

namespace KriilMod_CD
{
    public class Designator_TrainCombatRangedOnly : Designator_BaseTrainCombat
    {
        public Designator_TrainCombatRangedOnly()
        {
            this.defaultLabel = "DesignatorTrainCombatRangedOnly".Translate();
            this.defaultDesc = "DesignatorTrainCombatRangedOnlyDesc".Translate();
            this.icon = icon = TexCommand.Attack;
            this.defOf = CombatTrainingDefOf.TrainCombatDesignationRangedOnly;
        }
    }
}