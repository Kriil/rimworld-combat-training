using RimWorld;
using System.Collections.Generic;
using Verse;

namespace KriilMod_CD
{
    public class Building_CombatDummy : Building
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            Designator designator = DesignatorUtility.FindAllowedDesignator<Designator_TrainCombat>();
            if (designator != null)
            {
                yield return new Command_Action
                {
                    action = delegate
                    {
                        designator.DesignateSingleCell(this.Position);
                    },
                    defaultDesc = "CommandTrainCombatDesc".Translate(),
                    icon = TexCommand.Draft,
                    defaultLabel = "CommandTrainCombatLabel".Translate()
                };
            }
        }
    }
}
