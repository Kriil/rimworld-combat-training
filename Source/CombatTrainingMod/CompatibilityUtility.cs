using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace KriilMod_CD
{
    [StaticConstructorOnStartup]
    public static class CompatibilityUtility
    {
        public static readonly bool HospitalityEnabled = ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name == "Hospitality");

        public static bool IsGuest(Pawn pawn)
        {
            if(!HospitalityEnabled)
            {
                return false;
            }

            try
            {
                bool isGuest = ((Func<bool>)(() =>
                {
                    // Copied from Hospitality.GuestUtility
                    if (!IsValidGuestPawn(pawn)) return false;

                    var compGuest = pawn?.GetComp<Hospitality.CompGuest>();
                    var lord = compGuest?.lord;
                    var job = lord?.LordJob;
                    return job is LordJob_VisitColony;
                }))();
                return isGuest;
            }
            catch (TypeLoadException ex)
            {
                Log.Warning("Failed to check whether ped is a guest.");
                return false;
            }
        }

        private static bool IsValidGuestPawn(this Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.Destroyed) return false;
            if (!pawn.Spawned) return false;
            if (pawn.thingIDNumber == 0) return false;
            if (pawn.Name == null) return false;
            if (pawn.Dead) return false;
            if (pawn.RaceProps?.Humanlike != true) return false;
            if (pawn.guest == null) return false;
            if (pawn.guest.HostFaction != Faction.OfPlayer && pawn.Map.ParentFaction != Faction.OfPlayer) return false;
            if (pawn.Faction == null) return false;
            if (pawn.IsPrisonerOfColony || pawn.Faction == Faction.OfPlayer) return false;
            if (pawn.HostileTo(Faction.OfPlayer)) return false;
            return true;
        }
    }
}
