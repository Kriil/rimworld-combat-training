using RimWorld;
using Verse;

namespace KriilMod_CD
{
    public class Designator_TrainCombatAny : Designator_BaseTrainCombat
    {
        public Designator_TrainCombatAny()
        {
            defaultLabel = "DesignatorTrainCombat".Translate();
            defaultDesc = "DesignatorTrainCombatDesc".Translate();
            icon = icon = TexCommand.Draft;
            defOf = CombatTrainingDefOf.TrainCombatDesignation;
        }
    }
}