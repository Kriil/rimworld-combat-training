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
            // Ranged toggle
            /*yield return new Command_Toggle
            {
                isActive = () => this.HasDesignation(CombatTrainingDefOf.TrainCombatDesignation),
                toggleAction = delegate
                {
                    trainingType = (trainingType == TrainingTypes.Any) ? TrainingTypes.None : TrainingTypes.Any;
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignation, trainingType == TrainingTypes.Any);
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly, false);
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly, false);
                },
                defaultDesc = "CommandTrainCombatDesc".Translate(),
                icon = TexCommand.Draft,
                defaultLabel = "CommandTrainCombatLabel".Translate()
            };
            yield return new Command_Toggle
            {
                isActive = () => this.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly),
                toggleAction = delegate
                {
                    trainingType = (trainingType == TrainingTypes.Melee) ? TrainingTypes.None : TrainingTypes.Melee;
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly, trainingType == TrainingTypes.Melee);
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignation, false);
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly, false);
                },
                defaultDesc = "CommandTrainCombatMeleeOnlyDesc".Translate(),
                icon = TexCommand.AttackMelee,
                defaultLabel = "CommandTrainCombatMeleeOnlyLabel".Translate()
            };
            yield return new Command_Toggle
            {
                isActive = () => this.HasDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly),
                toggleAction = delegate
                {
                    trainingType = (trainingType == TrainingTypes.Ranged) ? TrainingTypes.None : TrainingTypes.Ranged;
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationRangedOnly,
                        trainingType == TrainingTypes.Ranged);
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignationMeleeOnly, false);
                    this.ToggleDesignation(CombatTrainingDefOf.TrainCombatDesignation, false);
                },
                defaultDesc = "CommandTrainCombatRangedOnlyDesc".Translate(),
                icon = TexCommand.Attack,
                defaultLabel = "CommandTrainCombatRangedOnlyLabel".Translate()
            };*/
        }
    }
}
