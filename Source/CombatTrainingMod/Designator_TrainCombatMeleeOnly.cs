using RimWorld;
using Verse;

namespace KriilMod_CD
{
    public class Designator_TrainCombatMeleeOnly : Designator_BaseTrainCombat
    {
        public Designator_TrainCombatMeleeOnly()
        {
            this.defaultLabel = "DesignatorTrainCombatMeleeOnly".Translate();
            this.defaultDesc = "DesignatorTrainCombatMeleeOnlyDesc".Translate();
            this.icon = icon = TexCommand.AttackMelee;
            this.defOf = CombatTrainingDefOf.TrainCombatDesignationMeleeOnly;
        }
    }
}