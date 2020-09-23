using System;
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
                int missionXP = xpEarned;
                int mechsKilled = ___UnitData.pilot.MechsKilled;
                int othersKilled = ___UnitData.pilot.OthersKilled;
                int newXP = 0;
                if (ModInit.Settings.useMissionXPforBonus == true)
                {
                    for (int i = 0; i < mechsKilled; i++)
                    {
                        newXP += Mathf.RoundToInt(missionXP * ModInit.Settings.bonusXP_MissionMechKills);
                        ModInit.modLog.LogMessage($"adding {Mathf.RoundToInt(missionXP * ModInit.Settings.bonusXP_MissionMechKills)}XP to {___UnitData.pilot.Description.Callsign} for mech kill, contract multiplier");
                    }

                    for (int j = 0; j < othersKilled; j++)
                    {
                        newXP += Mathf.RoundToInt(missionXP * ModInit.Settings.bonusXP_MissionOtherKills);
                        ModInit.modLog.LogMessage($"adding {Mathf.RoundToInt(missionXP * ModInit.Settings.bonusXP_MissionOtherKills)}XP to {___UnitData.pilot.Description.Callsign} for other kill, contract multiplier");
                    }


                }

                for (int i = 0; i < mechsKilled; i++)
                {
                    newXP += ModInit.Settings.bonusXP_MechKills;
                    ModInit.modLog.LogMessage($"adding {ModInit.Settings.bonusXP_MechKills}XP to {___UnitData.pilot.Description.Callsign} for mech kill, flat bonus");

                }
                for (int j = 0; j < othersKilled; j++)
                {
                    newXP += ModInit.Settings.bonusXP_OtherKills;
                    ModInit.modLog.LogMessage($"adding {ModInit.Settings.bonusXP_OtherKills}XP to {___UnitData.pilot.Description.Callsign} for other kill, flat bonus");
                }


                newXP += Mathf.RoundToInt((___UnitData.pilot.StructureDamageInflicted * ModInit.Settings.bonusXP_StrDamageMult) + (___UnitData.pilot.ArmorDamageInflicted * ModInit.Settings.bonusXP_ArmDamageMult));

                ModInit.modLog.LogMessage($"adding {Mathf.RoundToInt(___UnitData.pilot.StructureDamageInflicted * ModInit.Settings.bonusXP_StrDamageMult)}XP from Structure Damage and {Mathf.RoundToInt(___UnitData.pilot.ArmorDamageInflicted * ModInit.Settings.bonusXP_ArmDamageMult)}XP from Armor Damage to {___UnitData.pilot.Description.Callsign}");

                //new XP from effects and abilities added here
                newXP += ___UnitData.pilot.StatCollection.GetValue<int>("effectXP");

                ModInit.modLog.LogMessage($"adding stat effectXP: {___UnitData.pilot.StatCollection.GetValue<int>("effectXP")}XP to {___UnitData.pilot.Description.Callsign} from per-use effects/ability bonuses");


                //check this math shit
                if (ModInit.Settings.missionXPEffects > 0f && ModInit.Settings.missionXPeffectBonusDivisor > 0f)
                {

                    var contractFX_XP = Mathf.RoundToInt((missionXP * ModInit.Settings.missionXPEffects) *

                                                         ((float) ___UnitData.pilot.StatCollection.GetValue<int>(
                                                              "effectXP") /
                                                          ModInit.Settings.missionXPeffectBonusDivisor));
                    newXP += contractFX_XP;

                    ModInit.modLog.LogMessage($"adding {contractFX_XP}XP to {___UnitData.pilot.Description.Callsign} following formula ContractXP * ModInit.Settings.missionXPEffects * (effectXP/ModInit.Settings.missionXPeffectBonusDivisor)");
                }


                if (newXP > ModInit.Settings.bonusXP_CAP && ModInit.Settings.bonusXP_CAP > 0)
                {
                    newXP = ModInit.Settings.bonusXP_CAP;
                    ModInit.modLog.LogMessage($"total XP bonus exceeded ModInit.Settings.bonusXP_CAP of {ModInit.Settings.bonusXP_CAP}. {ModInit.Settings.bonusXP_CAP}XP to {___UnitData.pilot.Description.Callsign} instead");
                }

                ___UnitData.pilot.AddExperience(0, "FromKillsOrDmg", newXP);
                ModInit.modLog.LogMessage($"Total bonus XP awarded to {___UnitData.pilot.Description.Callsign}: {newXP}");


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

                return;
            }
        }
    }
}

