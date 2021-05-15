using Harmony;
using BattleTech;
using BattleTech.UI;
using System.Collections.Generic;
using UnityEngine;

namespace PracticeMakesPerfect.Patches
{
    public class PracticeMakesPerfect
    {
        [HarmonyPatch(typeof(AAR_UnitStatusWidget), "FillInPilotData")]
        public static class FillInPilotData_Patch

        {
            public static bool Prefix(AAR_UnitStatusWidget __instance, ref int xpEarned, UnitResult ___UnitData)
            {
                var missionXP = xpEarned;
                var mechsKilled = ___UnitData.pilot.MechsKilled;
                var othersKilled = ___UnitData.pilot.OthersKilled;
                var newXP = 0;
                if (ModInit.modSettings.useMissionXPforBonus)
                {
                    for (var i = 0; i < mechsKilled; i++)
                    {
                        newXP += Mathf.RoundToInt(missionXP * ModInit.modSettings.bonusXP_MissionMechKills);
                        ModInit.modLog.LogMessage($"adding {Mathf.RoundToInt(missionXP * ModInit.modSettings.bonusXP_MissionMechKills)}XP to {___UnitData.pilot.Description.Callsign} for mech kill, contract multiplier");
                    }

                    for (var j = 0; j < othersKilled; j++)
                    {
                        newXP += Mathf.RoundToInt(missionXP * ModInit.modSettings.bonusXP_MissionOtherKills);
                        ModInit.modLog.LogMessage($"adding {Mathf.RoundToInt(missionXP * ModInit.modSettings.bonusXP_MissionOtherKills)}XP to {___UnitData.pilot.Description.Callsign} for other kill, contract multiplier");
                    }


                }

                for (var i = 0; i < mechsKilled; i++)
                {
                    newXP += ModInit.modSettings.bonusXP_MechKills;
                    ModInit.modLog.LogMessage($"adding {ModInit.modSettings.bonusXP_MechKills}XP to {___UnitData.pilot.Description.Callsign} for mech kill, flat bonus");

                }
                for (var j = 0; j < othersKilled; j++)
                {
                    newXP += ModInit.modSettings.bonusXP_OtherKills;
                    ModInit.modLog.LogMessage($"adding {ModInit.modSettings.bonusXP_OtherKills}XP to {___UnitData.pilot.Description.Callsign} for other kill, flat bonus");
                }


                newXP += Mathf.RoundToInt((___UnitData.pilot.StructureDamageInflicted * ModInit.modSettings.bonusXP_StrDamageMult) + (___UnitData.pilot.ArmorDamageInflicted * ModInit.modSettings.bonusXP_ArmDamageMult));

                ModInit.modLog.LogMessage($"adding {Mathf.RoundToInt(___UnitData.pilot.StructureDamageInflicted * ModInit.modSettings.bonusXP_StrDamageMult)}XP from Structure Damage and {Mathf.RoundToInt(___UnitData.pilot.ArmorDamageInflicted * ModInit.modSettings.bonusXP_ArmDamageMult)}XP from Armor Damage to {___UnitData.pilot.Description.Callsign}");

                //new XP from effects and abilities added here
                newXP += ___UnitData.pilot.StatCollection.GetValue<int>("effectXP");

                ModInit.modLog.LogMessage($"adding stat effectXP: {___UnitData.pilot.StatCollection.GetValue<int>("effectXP")}XP to {___UnitData.pilot.Description.Callsign} from per-use effects/ability bonuses");


                //check this math shit
                if (ModInit.modSettings.missionXPEffects > 0f && ModInit.modSettings.missionXPeffectBonusDivisor > 0f)
                {

                    var contractFX_XP = Mathf.RoundToInt((missionXP * ModInit.modSettings.missionXPEffects) *

                                                         (___UnitData.pilot.StatCollection.GetValue<int>(
                                                              "effectXP") /
                                                          ModInit.modSettings.missionXPeffectBonusDivisor));
                    newXP += contractFX_XP;

                    ModInit.modLog.LogMessage($"adding {contractFX_XP}XP to {___UnitData.pilot.Description.Callsign} following formula ContractXP * ModInit.modSettings.missionXPEffects * (effectXP/ModInit.modSettings.missionXPeffectBonusDivisor)");
                }


                if (newXP > ModInit.modSettings.bonusXP_CAP && ModInit.modSettings.bonusXP_CAP > 0)
                {
                    newXP = ModInit.modSettings.bonusXP_CAP;
                    ModInit.modLog.LogMessage($"total XP bonus exceeded ModInit.modSettings.bonusXP_CAP of {ModInit.modSettings.bonusXP_CAP}. {ModInit.modSettings.bonusXP_CAP}XP to {___UnitData.pilot.Description.Callsign} instead");
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
                var list = new List<Pilot>(__instance.PilotRoster) {__instance.Commander};
                foreach (var pilot in list)
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

