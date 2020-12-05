using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using BattleTech;
using PracticeMakesPerfect.Framework;
using static PracticeMakesPerfect.Framework.GlobalVars;
using System.Threading.Tasks;
using BattleTech.DataObjects;
using BattleTech.Save;
using BattleTech.Save.Test;
using BattleTech.UI;
using ErosionBrushPlugin;
using TB.ComponentModel;

namespace PracticeMakesPerfect.Patches
{
    class SaveAndStatePatches
    {
        [HarmonyPatch(typeof(SGCharacterCreationCareerBackgroundSelectionPanel), "Done")]
        public static class SGCharacterCreationCareerBackgroundSelectionPanel_Done_Patch
        {
            public static void Postfix(SGCharacterCreationCareerBackgroundSelectionPanel __instance)
            {
                SpecManager.ManagerInstance.PreloadIcons();
                SpecManager.ManagerInstance.ProcessDefaults();
                sim = UnityGameInstance.BattleTechGame.Simulation;

                SpecHolder.HolderInstance.AddToMaps(sim.Commander);

                foreach (Pilot p in sim.PilotRoster)
                {
                    SpecHolder.HolderInstance.AddToMaps(p);
                }

                return;
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Dehydrate",
            new Type[] {typeof(SimGameSave), typeof(SerializableReferenceContainer)})]
        public static class SGS_Dehydrate_Patch
        {
            public static void Prefix(SimGameState __instance)
            {
                SpecManager.ManagerInstance.PreloadIcons();
                sim = __instance;
                var curPilots = new List<string>();

                SpecHolder.HolderInstance.AddToMaps(sim.Commander);
                curPilots.Add(sim.Commander.FetchGUID());
                foreach (Pilot p in sim.PilotRoster)
                {
                    SpecHolder.HolderInstance.AddToMaps(p);
                    curPilots.Add(p.FetchGUID());
                }

                if (ModInit.modSettings.removeDeprecated)
                {
                    SpecHolder.HolderInstance.CleanMaps(curPilots);
                }

                SpecHolder.HolderInstance.SerializeSpecState();
            }

            public static void Postfix(SimGameState __instance)
            {
                if (!ModInit.modSettings.debugKeepTags)
                {
                    if (sim.CompanyTags.Any(x => x.StartsWith(OP4SpecStateTag)))
                    {
                        var op4State = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(OP4SpecStateTag))?.Substring(OP4SpecStateTag.Length);
                        GlobalVars.sim.CompanyTags.Remove(op4State);
                    }

                    if (sim.CompanyTags.Any(x => x.StartsWith(OP4SpecTrackerTag)))
                    {
                        var op4Tracker = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(OP4SpecTrackerTag))?.Substring(OP4SpecTrackerTag.Length);
                        GlobalVars.sim.CompanyTags.Remove(op4Tracker);
                    }

                    if (sim.CompanyTags.Any(x => x.StartsWith(MissionSpecStateTag)))
                    {
                        var missionState = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(MissionSpecStateTag))?.Substring(MissionSpecStateTag.Length);
                        GlobalVars.sim.CompanyTags.Remove(missionState);
                    }

                    if (sim.CompanyTags.Any(x => x.StartsWith(MissionSpecTrackerTag)))
                    {
                        var missionTracker = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(MissionSpecTrackerTag))?.Substring(MissionSpecTrackerTag.Length);
                        GlobalVars.sim.CompanyTags.Remove(missionTracker);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Rehydrate", new Type[] {typeof(GameInstanceSave)})]
        public static class SGS_Rehydrate_Patch
        {
            public static void Postfix(SimGameState __instance)
            {
                sim = __instance;
                SpecManager.ManagerInstance.PreloadIcons();
                SpecManager.ManagerInstance.ProcessDefaults();
                var curPilots = new List<string>();
                SpecHolder.HolderInstance.DeserializeSpecState();
                ModInit.modLog.LogMessage($"Successfully deserialized or determined deserializing unnecessary.");

                if (ModInit.modSettings.DebugCleaning)
                {
                    SpecHolder.HolderInstance.MissionsTracker = new Dictionary<string, Dictionary<string, int>>();
                    SpecHolder.HolderInstance.OpForKillsTracker = new Dictionary<string, Dictionary<string, int>>();
                    SpecHolder.HolderInstance.MissionSpecMap = new Dictionary<string, List<string>>();
                    SpecHolder.HolderInstance.OpForSpecMap = new Dictionary<string, List<string>>();
                }

                SpecHolder.HolderInstance.AddToMaps(sim.Commander);
                curPilots.Add(sim.Commander.FetchGUID());

                foreach (Pilot p in sim.PilotRoster)
                {
                    SpecHolder.HolderInstance.AddToMaps(p);
                    curPilots.Add(p.FetchGUID());
                    SpecHolder.HolderInstance.ProcessTaggedSpecs(p);
                }
                SpecHolder.HolderInstance.CleanMaps(curPilots);
            }
        }

        [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster",
            new Type[] {typeof(PilotDef), typeof(bool), typeof(bool)})]
        public static class SGS_AddPilotToRoster_Patch
        {
            public static void Postfix(SimGameState __instance, PilotDef def, bool updatePilotDiscardPile = false,
                bool initialHiringDontSpawnMessage = false)
            {
                var p = __instance.PilotRoster.FirstOrDefault(x => x.pilotDef.Description.Id == def.Description.Id);
                SpecHolder.HolderInstance.AddToMaps(p);
                SpecHolder.HolderInstance.ProcessTaggedSpecs(p);
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "OnInitializeContractComplete", new Type[] {typeof(MessageCenterMessage)})]
        public static class TurnDirector_OnInitializeContractComplete
        {
            public static void Postfix(TurnDirector __instance)
            {
                var combat = __instance.Combat;
                
                var contractID = __instance.Combat.ActiveContract.Override.ContractTypeValue.Name;

                ModInit.modLog.LogMessage($"ActiveContract.Override.ContractTypeValue.Name: {contractID}");

                var opforID = __instance.Combat.ActiveContract.Override.targetTeam.FactionValue.Name;

                var playerUnits = combat.AllActors.Where(x => x.team.IsLocalPlayer).ToList();
                foreach (var actor in playerUnits)
                {
                    actor.StatCollection.AddStatistic<bool>(ModInit.modSettings.dummyOpForStat, false);
                    if (!playerUnits.Any(x => x.GetPilot().IsPlayerCharacter))
                    {
                        SpecManager.ManagerInstance.ApplyStratComs(actor);
                    }
                    var p = actor.GetPilot();
                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(spGUID)))
                    {
                        SpecHolder.HolderInstance.AddToMaps(p);
                    }
                    var pKey = actor.GetPilot().FetchGUID();

                    if (!SpecHolder.HolderInstance.OpForKillsTEMPTracker.ContainsKey(pKey) && ModInit.modSettings.WhiteListOpFor.Contains(opforID))
                    {
                        SpecHolder.HolderInstance.OpForKillsTEMPTracker.Add(pKey, new Dictionary<string, int>());
                        SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].Add(opforID, 0);
                    }

                    SpecManager.ManagerInstance.GatherPassiveMissionSpecs(actor, contractID);

                    SpecManager.ManagerInstance.GatherPassiveOpforSpecs(actor, opforID);
                }
            }
        }

        [HarmonyPatch(typeof(LoadTransitioning), "BeginCombatRestart", new Type[] {typeof(Contract)})]
        static class LoadTransitioning_BeginCombatRestart_Patch
        {
            static void Prefix(Contract __instance)
            {
                SpecHolder.HolderInstance.OpForKillsTEMPTracker = new Dictionary<string, Dictionary<string, int>>();
                ModInit.modLog.LogMessage(
                    $"Resetting combatInjuriesMap due to RestartMission button. Somebody must like CTD's.");
            }
        }

        [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] {typeof(MissionResult), typeof(bool)})]
        static class Contract_CompleteContract_Patch
        {
            static void Prefix(Contract __instance, MissionResult result, bool isGoodFaithEffort)
            {
                var playerUnits = UnityGameInstance.BattleTechGame.Combat.AllActors.Where(x => x.team.IsLocalPlayer);
                foreach (var unit in playerUnits)
                {
                    var p = unit.GetPilot();
                    var pKey = p.FetchGUID();
                    var contractID = __instance.Override.ContractTypeValue.Name;

                    if (ModInit.modSettings.WhiteListMissions.Contains(contractID))
                    {
                        if (!SpecHolder.HolderInstance.MissionsTracker[pKey].ContainsKey(contractID))
                        {
                            SpecHolder.HolderInstance.MissionsTracker[pKey].Add(contractID, 0);
                            ModInit.modLog.LogMessage(
                                $"No key for {contractID} found in {p.Callsign}'s MissionsTracker. Adding it with default value 0.");
                        }

                        SpecHolder.HolderInstance.MissionsTracker[pKey][contractID] += 1;
                        ModInit.modLog.LogMessage($"Adding 1 to {p.Callsign}'s MissionsTracker for {contractID}");

                        var mspecsCollapsed = SpecManager.ManagerInstance.MissionSpecList.Where(x =>
                                SpecHolder.HolderInstance.MissionSpecMap[pKey].Any(y => y == x.MissionSpecID))
                            .Select(x => x.contractTypeID).ToList();

                        if (SpecManager.ManagerInstance.MissionSpecList.Any(x => x.contractTypeID == contractID))
                        {
                            foreach (var missionSpec in SpecManager.ManagerInstance.MissionSpecList.Where(x => x.contractTypeID == contractID))
                            {
                                if (missionSpec.missionsRequired == 0)
                                {
                                    ModInit.modLog.LogMessage($"{missionSpec.MissionSpecName} has 0 required missions set, ignoring.");
                                    continue;
                                }
                                if ((SpecHolder.HolderInstance.MissionSpecMap[pKey].Count <
                                     ModInit.modSettings.MaxMissionSpecializations) ||
                                    (!ModInit.modSettings.MissionTiersCountTowardMax && mspecsCollapsed.Contains(missionSpec.contractTypeID)))
                                {
                                    if (SpecHolder.HolderInstance.MissionsTracker[pKey][contractID] >=
                                        missionSpec.missionsRequired && contractID == missionSpec.contractTypeID && !SpecHolder
                                            .HolderInstance.MissionSpecMap[pKey].Contains(missionSpec.MissionSpecID))
                                    {
                                        ModInit.modLog.LogMessage(
                                            $"{p.Callsign} has achieved {missionSpec.MissionSpecName} for {missionSpec.contractTypeID}!");
                                        SpecHolder.HolderInstance.MissionSpecMap[pKey].Add(missionSpec.MissionSpecID);
                                    }
                                }
                                else
                                {
                                    ModInit.modLog.LogMessage($"{p.Callsign} already has the maximum, {ModInit.modSettings.MaxMissionSpecializations}, Mission Specializations!");
                                }
                            }
                        }
                    }

                    foreach (var key in SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].Keys)
                    {
                        if (ModInit.modSettings.WhiteListOpFor.Contains(key))
                        {
                            if (!SpecHolder.HolderInstance.OpForKillsTracker[pKey].ContainsKey(key))
                            {
                                SpecHolder.HolderInstance.OpForKillsTracker[pKey].Add(key, 0);
                                ModInit.modLog.LogMessage(
                                    $"No key for {key} found in {p.Callsign}'s OpForKillsTracker. Adding it with default value 0.");
                            }
                            SpecHolder.HolderInstance.OpForKillsTracker[pKey][key] +=
                                SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey][key];
                            ModInit.modLog.LogMessage(
                                $"Adding {SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey][key]} {key} kills to {p.Callsign}'s OpForKillsTracker");
                        }
                        
                    }
                    var opforspecCollapsed = SpecManager.ManagerInstance.OpForSpecList.Where(x =>
                            SpecHolder.HolderInstance.OpForSpecMap[pKey].Any(y => y == x.OpForSpecID))
                        .Select(x => x.factionID).ToList();


                    foreach (var opforSpec in SpecManager.ManagerInstance.OpForSpecList)
                    {
                        if (opforSpec.killsRequired == 0)
                        {
                            ModInit.modLog.LogMessage($"{opforSpec.OpForSpecName} has 0 required kills set, ignoring.");
                            continue;
                        }
                        if ((SpecHolder.HolderInstance.OpForSpecMap[pKey].Count <
                             ModInit.modSettings.MaxOpForSpecializations) ||
                            (!ModInit.modSettings.OpForTiersCountTowardMax && opforspecCollapsed.Contains(opforSpec.factionID)))
                        {
                            foreach (var opfor in new List<string>(SpecHolder.HolderInstance.OpForKillsTracker[pKey]
                                .Keys))
                            {
                                if (SpecHolder.HolderInstance.OpForKillsTracker[pKey][opfor] >=
                                    opforSpec.killsRequired && opfor == opforSpec.factionID && !SpecHolder
                                        .HolderInstance.OpForSpecMap[pKey].Contains(opforSpec.OpForSpecID))
                                {
                                    ModInit.modLog.LogMessage(
                                        $"{p.Callsign} has achieved {opforSpec.OpForSpecName} against {opforSpec.factionID}!");
                                    SpecHolder.HolderInstance.OpForSpecMap[pKey].Add(opforSpec.OpForSpecID);
                                }
                            }
                        }
                    }
                }
                SpecHolder.HolderInstance.OpForKillsTEMPTracker = new Dictionary<string, Dictionary<string, int>>();
            }
        }
    }
}