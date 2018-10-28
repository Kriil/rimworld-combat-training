using HugsLib;
using HugsLib.Utils;

namespace KriilMod_CD
{
    public class CombatTrainingController : ModBase
    {
        public override string ModIdentifier
        {
            get
            {
                return "CombatTraining";
            }
        }

        protected override bool HarmonyAutoPatch
        {
            get
            {
                return false;
            }
        }

        private static ModLogger staticLogger;

        internal static new ModLogger Logger
        {
            get
            {
                ModLogger result;
                if ((result = CombatTrainingController.staticLogger) == null)
                {
                    result = (CombatTrainingController.staticLogger = new ModLogger("CombatTraining"));
                }
                return result;
            }
        }
    }
}
