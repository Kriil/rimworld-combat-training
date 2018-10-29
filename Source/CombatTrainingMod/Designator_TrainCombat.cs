using HugsLib.Utils;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace KriilMod_CD
{
	public class Designator_TrainCombat : Designator
	{
         /*
         * Default constructor 
         */ 
        public Designator_TrainCombat()
		{
            this.defaultLabel = "DesignatorTrainCombat".Translate();
			this.defaultDesc = "DesignatorTrainCombatDesc".Translate();
            this.icon = icon = TexCommand.Draft;
            //this.icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true); //icon for orders menu
            this.soundDragSustain = SoundDefOf.Designate_DragStandard;
			this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			this.useMouseIcon = true;
			this.soundSucceeded = SoundDefOf.Designate_Hunt;
            this.hotKey = KeyBindingDefOf.Misc9;
        }

        public override int DraggableDimensions
        {
            get
            {
                return 2;
            }
        }

        /*
         * Designates a thing at the given cell.  Checks are made to ensure that a thing at the given cell can be designated with the TrainCombatDesignation
         */
        public override void DesignateSingleCell(IntVec3 loc)
		{
            Thing combatDummy = GetDesignatable(loc);
            this.DesignateThing(combatDummy);                              
        }

        /*
         * Returns the thing at the given location if it can be designated
         */
        public Thing GetDesignatable(IntVec3 loc)
        {             
            List<Thing> thingList = loc.GetThingList(base.Map);
            foreach (Thing thing in thingList)
            {
                if (CanDesignateThing(thing).Accepted)
                {
                    return thing;
                }
            }
            return null;
        }

        /*
         * Returns true if the given thing is a combat dummy and doesn't already have the TrainCombatDesignation
         */
        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (t != null)
            {             
                if (IsCombatDummy(t) && !HugsLibUtility.HasDesignation(t, CombatTrainingDefOf.TrainCombatDesignation))
                {
                    return true;
                }
            }
            return false;
        }

        /*
         * Returns true if the given cell is inbounds, not fogged and a thing at the given cell can be designated
         */
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if(c != null)
            {
                if (!c.InBounds(base.Map) || c.Fogged(base.Map))
                {
                    return false;
                }
                if (!this.CanDesignateThing(this.GetDesignatable(c)).Accepted)
                {
                    return "MessageMustDesignateCombatDummy".Translate();
                }
            }            
            return true;
        }     

        /*
         * Gives the given thing the TrainCombatDesignation
         */ 
		public override void DesignateThing(Thing t)
		{
            if(t != null)
            {
                HugsLibUtility.ToggleDesignation(t, CombatTrainingDefOf.TrainCombatDesignation, true);
            }              
		}


        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
            GenDraw.DrawNoBuildEdgeLines();
        }


        public override void RenderHighlight(List<IntVec3> dragCells)
        {
            DesignatorUtility.RenderHighlightOverSelectableThings(this, dragCells);
        }

        private bool IsCombatDummy(Thing t)
        {
            return typeof(Building_CombatDummy).IsAssignableFrom(t.def.thingClass);
        }
    }
}
