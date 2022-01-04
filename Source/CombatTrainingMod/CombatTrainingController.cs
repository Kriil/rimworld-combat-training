using HugsLib;
using HugsLib.Utils;

namespace KriilMod_CD
{
    public class CombatTrainingController : ModBase
    {
        private static ModLogger staticLogger;

        public override string ModIdentifier => "CombatTraining";

        protected override bool HarmonyAutoPatch => false;

        internal new static ModLogger Logger
        {
            get
            {
                ModLogger result;
                if ((result = staticLogger) == null)
                {
                    result = staticLogger = new ModLogger("CombatTraining");
                }

                return result;
            }
        }
    }
}