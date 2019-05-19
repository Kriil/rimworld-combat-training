using System;
using RimWorld;
using System.Collections.Generic;
using HugsLib.Utils;
using Verse;

namespace KriilMod_CD
{
    public class Building_CombatDummy : Building
    {
        [Flags]
        public enum TrainingTypes
        {
            None = 0,
            Melee = 1,
            Ranged = 2,
            Any = 3
        }

        public TrainingTypes trainingType = TrainingTypes.None;

        protected void determineDesignation()
        {
            this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignation, false);
            this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly, false);
            this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly, false);
            switch (trainingType)
            {
                case TrainingTypes.None:
                    break;
                case TrainingTypes.Melee:
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly, true);
                    break;
                case TrainingTypes.Ranged:
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly, true);
                    break;
                case TrainingTypes.Any:
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignation, true);
                    break;
            }
        }
        
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            // Melee toggle
            yield return new Command_Toggle
            {
                isActive = () => ((int)trainingType & (int)TrainingTypes.Melee) != 0,
                toggleAction = delegate
                {
                    trainingType ^= TrainingTypes.Melee;
                    determineDesignation();
                },
                defaultDesc = "CommandTrainCombatMeleeOnlyDesc".Translate(),
                icon = TexCommand.AttackMelee,
                defaultLabel = "CommandTrainCombatMeleeOnlyLabel".Translate()
            };
            // Ranged toggle
            yield return new Command_Toggle
            {
                isActive = () => ((int)trainingType & (int)TrainingTypes.Ranged) != 0,
                toggleAction = delegate
                {
                    trainingType ^= TrainingTypes.Ranged;
                    determineDesignation();
                },
                defaultDesc = "CommandTrainCombatRangedOnlyDesc".Translate(),
                icon = TexCommand.Attack,
                defaultLabel = "CommandTrainCombatRangedOnlyLabel".Translate()
            };
        }
    }
}
