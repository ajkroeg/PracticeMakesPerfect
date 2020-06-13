using Harmony;
using BattleTech;
using BattleTech.UI;
using System.Collections.Generic;
using UnityEngine;

namespace PracticeMakesPerfect
{
    public class PracticeMakesPerfect
    {
        [HarmonyPatch(typeof(AAR_UnitStatusWidget), "FillInPilotData")]
        public static class FillInPilotData_Patch

        {
            public static bool Prefix(AAR_UnitStatusWidget __instance, ref int xpEarned, UnitResult ___UnitData)
            {
                
                int mechsKilled = ___UnitData.pilot.MechsKilled;
                int newXP = 0;
                for (int i = 0; i < mechsKilled; i++)
                {
                    newXP += ModInit.Settings.bonusXP_MechKills;
                }
                int othersKilled = ___UnitData.pilot.OthersKilled;
                for (int j = 0; j < othersKilled; j++)
                {
                    newXP += ModInit.Settings.bonusXP_OtherKills;
                }
                newXP += Mathf.RoundToInt((___UnitData.pilot.StructureDamageInflicted * ModInit.Settings.bonusXP_StrDamageMult) + (___UnitData.pilot.ArmorDamageInflicted * ModInit.Settings.bonusXP_ArmDamageMult));

                if (newXP > ModInit.Settings.bonusXP_CAP && ModInit.Settings.bonusXP_CAP > 0)
                {
                    newXP = ModInit.Settings.bonusXP_CAP;
                }
                ___UnitData.pilot.AddExperience(0, "FromKillsOrDmg", newXP);

                xpEarned += newXP;
                return true;
            }
        }
        [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
        [HarmonyPriority(Priority.Low)]
        public static class ResolveCompleteContract_Patch

        {
            public static void Postfix(SimGameState __instance)
            {
                List<Pilot> list = new List<Pilot>(__instance.PilotRoster);
                list.Add(__instance.Commander);
                foreach (Pilot pilot in list)
                {    
                    pilot.StatCollection.AddStatistic<int>("TotalMechKills", pilot.pilotDef.MechKills);
                    pilot.StatCollection.AddStatistic<int>("TotalOtherKills", pilot.pilotDef.OtherKills);
                    pilot.StatCollection.AddStatistic<int>("TotalKills", pilot.pilotDef.MechKills + pilot.pilotDef.OtherKills);
                    pilot.StatCollection.AddStatistic<int>("TotalInjuries", pilot.pilotDef.LifetimeInjuries);
                }
            }
        }
    }
}

