using System;
using UnityEngine;
using Verse;

namespace KriilMod_CD
{
    public class TrainCombatDesignatorDef : Def
    {
        public string iconTexture;
        public string dragHighlightTexture;
        public bool Injected { get; set; }
        public Type designatorClass;

        public DesignationCategoryDef Category { get; private set; }

        public Texture2D IconTexture { get; private set; }

        public Texture2D DragHighlightTex { get; private set; }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            this.Category = DefDatabase<DesignationCategoryDef>.GetNamed(this.defName, true);
            LongEventHandler.ExecuteWhenFinished(delegate ()
            {
                this.IconTexture = ContentFinder<Texture2D>.Get(this.iconTexture, true);
                this.DragHighlightTex = ContentFinder<Texture2D>.Get(this.dragHighlightTexture, true);
            });
        }
    }
}
