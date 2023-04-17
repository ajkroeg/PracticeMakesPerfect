using System;
using BattleTech;
using System.Collections.Generic;
using System.Linq;
using CustAmmoCategories;
using UnityEngine;

namespace PracticeMakesPerfect.Patches
{
    public class CAC_AMS_XP
    {
        [HarmonyPatch(typeof(AttackDirector.AttackSequence), "GenerateToHitInfo", new Type[] {})]
        public static class AttackDirector_GenerateToHitInfo
        {
            public static void Postfix(AttackDirector.AttackSequence __instance)
            {
                var AMSPilots = new Dictionary<Pilot, float>();
                foreach (var intercept in __instance.Interceptables())
                {
                    if (intercept.interceptInfo.Intercepted)
                    {
                        var AMSPilot = intercept.interceptInfo.InterceptedAMS.parent.GetPilot();
                        if (AMSPilot == null) continue;
                        if (!AMSPilots.ContainsKey(AMSPilot))
                        {
                            AMSPilots.Add(intercept.interceptInfo.InterceptedAMS.parent.GetPilot(),
                                ModInit.modSettings.AMSKillsXP);
                        }
                        else
                        {
                            AMSPilots[AMSPilot] += ModInit.modSettings.AMSKillsXP;
                        }
                    }
                }

                foreach (var pilot in AMSPilots)
                {
                    var bonusXP = Mathf.RoundToInt(pilot.Value);
                    ModInit.modLog.LogMessage($"Adding {bonusXP} to {pilot.Key.Description.Callsign}'s 'effectXP' pilot stat for AMS interception.");
                    var stat = pilot.Key.StatCollection.GetStatistic("effectXP");
                    pilot.Key.StatCollection.Int_Add(stat, bonusXP);
                }
            }
        }
    }
}