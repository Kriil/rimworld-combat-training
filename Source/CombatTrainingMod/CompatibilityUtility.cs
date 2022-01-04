﻿using System;
using Hospitality;
using RimWorld;
using Verse;
using LordJob_VisitColony = RimWorld.LordJob_VisitColony;

namespace KriilMod_CD
{
    [StaticConstructorOnStartup]
    public static class CompatibilityUtility
    {
        public static readonly bool HospitalityEnabled =
            ModLister.GetActiveModWithIdentifier("Orion.Hospitality") != null;

        public static bool IsGuest(Pawn pawn)
        {
            var isGuest = false;
            if (!HospitalityEnabled)
            {
                return false;
            }

            try
            {
                isGuest = ((Func<bool>)(() =>
                {
                    // Copied from Hospitality.GuestUtility
                    if (!IsValidGuestPawn(pawn))
                    {
                        return false;
                    }

                    var compGuest = pawn?.GetComp<CompGuest>();
                    var lord = compGuest?.lord;
                    var job = lord?.LordJob;
                    return job is LordJob_VisitColony;
                }))();
            }
            catch (TypeLoadException ex)
            {
                Log.Warning("Failed to check whether ped is a guest. " + ex.Message);
            }

            return isGuest;
        }

        private static bool IsValidGuestPawn(this Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Destroyed)
            {
                return false;
            }

            if (!pawn.Spawned)
            {
                return false;
            }

            if (pawn.thingIDNumber == 0)
            {
                return false;
            }

            if (pawn.Name == null)
            {
                return false;
            }

            if (pawn.Dead)
            {
                return false;
            }

            if (pawn.RaceProps?.Humanlike != true)
            {
                return false;
            }

            if (pawn.guest == null)
            {
                return false;
            }

            if (pawn.guest.HostFaction != Faction.OfPlayer && pawn.Map.ParentFaction != Faction.OfPlayer)
            {
                return false;
            }

            if (pawn.Faction == null)
            {
                return false;
            }

            if (pawn.IsPrisonerOfColony || pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }

            if (pawn.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            return true;
        }
    }
}