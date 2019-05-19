using RimWorld;
using Verse;

namespace KriilMod_CD
{
    public class Designator_TrainCombatAny : Designator_BaseTrainCombat
    {
        public Designator_TrainCombatAny()
        {
            this.defaultLabel = "DesignatorTrainCombat".Translate();
            this.defaultDesc = "DesignatorTrainCombatDesc".Translate();
            this.icon = icon = TexCommand.Draft;
            this.defOf = CombatTrainingDefOf.TrainCombatDesignation;
        }
    }
}