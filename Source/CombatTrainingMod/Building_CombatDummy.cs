using System.Collections.Generic;
using Verse;
using UnityEngine;
using RimWorld;

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
                    //icon = ContentFinder<Texture2D>.Get("TrainCombatIcon", true),                    
                    //icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true), //icon for thing command button
                    defaultLabel = "CommandTrainCombatLabel".Translate()
                };
            }
        }
    }
}
