using RimWorld;
using Verse;

namespace KriilMod_CD
{
    public class Designator_TrainCombatMeleeOnly : Designator_BaseTrainCombat
    {
        public Designator_TrainCombatMeleeOnly()
        {
            defaultLabel = "DesignatorTrainCombatMeleeOnly".Translate();
            defaultDesc = "DesignatorTrainCombatMeleeOnlyDesc".Translate();
            icon = icon = TexCommand.AttackMelee;
            defOf = CombatTrainingDefOf.TrainCombatDesignationMeleeOnly;
        }
    }
}