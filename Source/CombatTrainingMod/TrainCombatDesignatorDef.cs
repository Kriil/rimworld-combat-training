using System;
using UnityEngine;
using Verse;

namespace KriilMod_CD
{
    public class TrainCombatDesignatorDef : Def
    {
        public Type designatorClass;
        public string dragHighlightTexture;
        public string iconTexture;
        public bool Injected { get; set; }

        public DesignationCategoryDef Category { get; private set; }

        public Texture2D IconTexture { get; private set; }

        public Texture2D DragHighlightTex { get; private set; }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            Category = DefDatabase<DesignationCategoryDef>.GetNamed(defName);
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                IconTexture = ContentFinder<Texture2D>.Get(iconTexture);
                DragHighlightTex = ContentFinder<Texture2D>.Get(dragHighlightTexture);
            });
        }
    }
}