using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using BattleTech;
using PracticeMakesPerfect.Framework;
using static PracticeMakesPerfect.Framework.GlobalVars;
using BattleTech.Save;
using BattleTech.Save.Test;
using BattleTech.UI;
using UnityEngine;

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

                if (String.IsNullOrEmpty(SpecHolder.HolderInstance.activeStratCom))
                {
                    SpecHolder.HolderInstance.activeStratCom =
                        SpecManager.ManagerInstance.StratComs[0].StratComID;
                    ModInit.modLog.LogMessage($"no active StratCom detected, setting to first available.");
                }
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

        [HarmonyPatch(typeof(TurnDirector), "StartFirstRound")]
        public static class TurnDirector_StartFirstRound
        {
            public static void Postfix(TurnDirector __instance)
            {
                var combat = __instance.Combat;
                
                var contractID = __instance.Combat.ActiveContract.Override.ContractTypeValue.Name;

                ModInit.modLog.LogMessage($"ActiveContract.Override.ContractTypeValue.Name: {contractID}");

                var opforID = __instance.Combat.ActiveContract.Override.targetTeam.FactionValue.Name;
                var opAllyID = __instance.Combat.ActiveContract.Override.targetsAllyTeam.FactionValue.Name;
                var hostiletoALLID = __instance.Combat.ActiveContract.Override.hostileToAllTeam.FactionValue.Name;

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

                    if (!SpecHolder.HolderInstance.OpForKillsTEMPTracker.ContainsKey(pKey))
                    {
                        SpecHolder.HolderInstance.OpForKillsTEMPTracker.Add(pKey, new Dictionary<string, int>());
                        ModInit.modLog.LogMessage($"{p.Callsign} was missing OpForKillsTEMPTracker. Adding an empty one.");
                    }

                    if (ModInit.modSettings.WhiteListOpFor.Contains(opforID) && !SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].ContainsKey(opforID))
                    {
                        SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].Add(opforID, 0);
                        ModInit.modLog.LogMessage($"Initializing {p.Callsign}'s OpForKillsTEMPTracker for target team {opforID}.");
                    }

                    if (ModInit.modSettings.WhiteListOpFor.Contains(opAllyID) && string.IsNullOrEmpty(opAllyID) && !SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].ContainsKey(opAllyID))
                    {
                        SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].Add(opAllyID, 0);
                        ModInit.modLog.LogMessage($"Initializing {p.Callsign}'s OpForKillsTEMPTracker for targets ally team {opAllyID}.");
                    }

                    if (ModInit.modSettings.WhiteListOpFor.Contains(hostiletoALLID) && string.IsNullOrEmpty(hostiletoALLID) && !SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].ContainsKey(hostiletoALLID))
                    {
                        SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].Add(hostiletoALLID, 0);
                        ModInit.modLog.LogMessage($"Initializing {p.Callsign}'s OpForKillsTEMPTracker for hostile to all team {hostiletoALLID}.");
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

        [HarmonyPatch(typeof(SimGameState), "GetFinalReputationChange", new Type[] {typeof(FactionValue), typeof(int)})]
        static class SimGameState_GetFinalReputationChange
        {
            static void Postfix(SimGameState __instance, FactionValue faction, int valueChange)
            {
                SpecHolder.HolderInstance.emplRep = valueChange;
            }
        }


        [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] {typeof(MissionResult), typeof(bool)})]
        static class Contract_CompleteContract_Patch
        {
            [HarmonyPriority(Priority.First)]
            static void Postfix(Contract __instance, MissionResult result, bool isGoodFaithEffort)
            {
                var employer = __instance.Override.employerTeam.FactionDef.FactionValue.Name;
                var target = __instance.Override.targetTeam.FactionDef.FactionValue.Name;
                var employerRepMult = 1f;
                var targetRepMult = 1f;
                var contractPayOutMult = 1f;

                SpecHolder.HolderInstance.kills = 0;
                SpecHolder.HolderInstance.bounty = 0;
                SpecHolder.HolderInstance.emplRep = 0;

                var playerUnits = UnityGameInstance.BattleTechGame.Combat.AllActors.Where(x => x.team.IsLocalPlayer);
                foreach (var unit in playerUnits)
                {
                    var p = unit.GetPilot();
                    var pKey = p.FetchGUID();

                    var contractID = __instance.Override.ContractTypeValue.Name;
                    //processing mission outcomes

                    if (SpecHolder.HolderInstance.OpForSpecMap.ContainsKey(pKey))
                    {
                        var opSpecs = new List<OpForSpec>();
                        var employerRepMultTemp = new List<float>(){0f};
                        foreach (var spec in SpecHolder.HolderInstance.OpForSpecMap[pKey])
                        {
                            opSpecs.AddRange(SpecManager.ManagerInstance.OpForSpecList.Where(x=>x.OpForSpecID == spec));
                        }

                        foreach (var opSpec in opSpecs)
                        {
                            if (opSpec.factionID == target || opSpec.applyToFaction.Contains(target))
                            {
                                if (opSpec.repMult.ContainsKey(employer))
                                {
                                    employerRepMultTemp.Add(opSpec.repMult[employer]);
                                    //employerRepMult += (opSpec.repMult[employer]);
                                    ModInit.modLog.LogMessage($"current employer reputation multiplier: {opSpec.repMult[employer]}");
                                }

                                if (opSpec.repMult.ContainsKey(employer_string))
                                {
                                    employerRepMultTemp.Add(opSpec.repMult[employer_string]);
                                    //employerRepMult += (opSpec.repMult[employer_string]);
                                    ModInit.modLog.LogMessage($"current employer reputation multiplier: {opSpec.repMult[employer_string]}");
                                }

                                if (opSpec.repMult.ContainsKey(owner_string) && sim.CurSystem.OwnerValue.Name == employer)
                                {
                                    employerRepMultTemp.Add(opSpec.repMult[owner_string]);
//                                        employerRepMult += (opSpec.repMult[owner_string]);
                                    ModInit.modLog.LogMessage($"current employer reputation multiplier: {opSpec.repMult[owner_string]}");
                                }

                                employerRepMult += employerRepMultTemp.Max();

                                if (opSpec.repMult.ContainsKey(target) && !opSpec.repMult.ContainsKey(target_string))
                                {
                                    targetRepMult *= (opSpec.repMult[target]);
                                    ModInit.modLog.LogMessage($"current target reputation multiplier: {targetRepMult}");
                                }
                                if (!opSpec.repMult.ContainsKey(target) && opSpec.repMult.ContainsKey(target_string))
                                {
                                    targetRepMult *= (opSpec.repMult[target_string]);
                                    ModInit.modLog.LogMessage($"current target reputation multiplier: {targetRepMult}");
                                }
                                if (opSpec.repMult.ContainsKey(target) && opSpec.repMult.ContainsKey(target_string))
                                {
                                    targetRepMult *= Math.Min(opSpec.repMult[target], opSpec.repMult[target_string]);
                                    ModInit.modLog.LogMessage($"current target reputation multiplier: {targetRepMult}");
                                }

                                if (opSpec.cashMult.ContainsKey(employer) && !opSpec.cashMult.ContainsKey(employer_string))
                                {
                                    contractPayOutMult += (opSpec.cashMult[employer]);
                                    ModInit.modLog.LogMessage($"current contract payout multiplier: {contractPayOutMult}");
                                }

                                if (!opSpec.cashMult.ContainsKey(employer) && opSpec.cashMult.ContainsKey(employer_string))
                                {
                                    contractPayOutMult += (opSpec.cashMult[employer_string]);
                                    ModInit.modLog.LogMessage($"current contract payout multiplier: {contractPayOutMult}");
                                }

                                if (opSpec.cashMult.ContainsKey(employer) && opSpec.cashMult.ContainsKey(employer_string))
                                {
                                    contractPayOutMult += Math.Max(opSpec.cashMult[employer_string], opSpec.cashMult[employer]);
                                    ModInit.modLog.LogMessage($"current contract payout multiplier: {contractPayOutMult}");
                                }
                                    
                            }
                                
                        }

                        if (SpecHolder.HolderInstance.OpForKillsTEMPTracker.ContainsKey(pKey))
                        {
                            if (SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].ContainsKey(target))
                            {
                                SpecHolder.HolderInstance.kills += SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey][target];
                                ModInit.modLog.LogMessage($"OpForKillsTEMPTracker was {SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey][target]}");
                            }

                            foreach (var opSpec in opSpecs)
                            {
                                if (opSpec.factionID.Contains(target) || opSpec.applyToFaction.Contains(target))
                                {
                                    if (opSpec.killBounty.ContainsKey(employer) &&
                                        !opSpec.killBounty.ContainsKey(employer_string))
                                    {
                                        SpecHolder.HolderInstance.bounty += opSpec.killBounty[employer];
                                        ModInit.modLog.LogMessage($"Kill bounty: {SpecHolder.HolderInstance.bounty}");
                                    }
                                    if (!opSpec.killBounty.ContainsKey(employer) &&
                                        opSpec.killBounty.ContainsKey(employer_string))
                                    {
                                        SpecHolder.HolderInstance.bounty += opSpec.killBounty[employer_string];
                                        ModInit.modLog.LogMessage($"Kill bounty: {SpecHolder.HolderInstance.bounty}");
                                    }
                                    if (opSpec.killBounty.ContainsKey(employer) &&
                                        opSpec.killBounty.ContainsKey(employer_string))
                                    {
                                        SpecHolder.HolderInstance.bounty += Math.Max(opSpec.killBounty[employer], opSpec.killBounty[employer_string]);
                                        ModInit.modLog.LogMessage($"Kill bounty: {SpecHolder.HolderInstance.bounty}");
                                    }
                                }
                            }
                        }
                    }

                    var taggedMSpecCt = 0;
                    var taggedOPSpecCt = 0;

                    if (!ModInit.modSettings.TaggedMissionSpecsCountTowardMax)
                    {

                        foreach (var tag in ModInit.modSettings.taggedMissionSpecs)
                        {
                            if (p.pilotDef.PilotTags.Contains(tag.Key))
                            {
                                taggedMSpecCt += tag.Value.Count;
                            }
                        }

                        ModInit.modLog.LogMessage(
                            $"{taggedMSpecCt} tagged Mission Specs found for {p.Callsign}.");
                        foreach (var tag in ModInit.modSettings.taggedOpForSpecs)
                        {
                            if (p.pilotDef.PilotTags.Contains(tag.Key))
                            {
                                taggedOPSpecCt += tag.Value.Count;
                            }
                        }
                    }

                    //processing spec progress below
                    if (ModInit.modSettings.WhiteListMissions.Contains(contractID))
                    {
                        if (!SpecHolder.HolderInstance.MissionsTracker[pKey].ContainsKey(contractID))
                        {
                            SpecHolder.HolderInstance.MissionsTracker[pKey].Add(contractID, 0);
                            ModInit.modLog.LogMessage(
                                $"No key for {contractID} found in {p.Callsign}'s MissionsTracker. Adding it with default value 0.");
                        }

                        if (ModInit.modSettings.MissionSpecSuccessRequirement == 0 ||
                            (ModInit.modSettings.MissionSpecSuccessRequirement == 1 && isGoodFaithEffort) ||
                            (ModInit.modSettings.MissionSpecSuccessRequirement == 2 && result == MissionResult.Victory))
                        {
                            SpecHolder.HolderInstance.MissionsTracker[pKey][contractID] += 1;
                            ModInit.modLog.LogMessage($"Adding 1 to {p.Callsign}'s MissionsTracker for {contractID}");
                        }

                        var mspecsCollapsed = SpecManager.ManagerInstance.MissionSpecList.Where(x =>
                                SpecHolder.HolderInstance.MissionSpecMap[pKey].Any(y => y == x.MissionSpecID))
                            .Select(x => x.contractTypeID).Distinct().ToList();

                        if (SpecManager.ManagerInstance.MissionSpecList.Any(x => x.contractTypeID == contractID))
                        {
                            foreach (var missionSpec in SpecManager.ManagerInstance.MissionSpecList.Where(x =>
                                x.contractTypeID == contractID))
                            {
                                if (missionSpec.missionsRequired == 0)
                                {
                                    ModInit.modLog.LogMessage(
                                        $"{missionSpec.MissionSpecName} has 0 required missions set, ignoring.");
                                    continue;
                                }

                                if ((SpecHolder.HolderInstance.MissionSpecMap[pKey].Count - taggedMSpecCt <
                                     ModInit.modSettings.MaxMissionSpecializations) ||
                                    (!ModInit.modSettings.MissionTiersCountTowardMax &&
                                     (mspecsCollapsed.Count - taggedMSpecCt < ModInit.modSettings.MaxMissionSpecializations) || mspecsCollapsed.Contains(missionSpec.contractTypeID)))
                                {
                                    if (SpecHolder.HolderInstance.MissionsTracker[pKey][contractID] >=
                                        missionSpec.missionsRequired && contractID == missionSpec.contractTypeID &&
                                        !SpecHolder
                                            .HolderInstance.MissionSpecMap[pKey]
                                            .Contains(missionSpec.MissionSpecID))
                                    {
                                        ModInit.modLog.LogMessage(
                                            $"{p.Callsign} has achieved {missionSpec.MissionSpecName} for {missionSpec.contractTypeID}!");
                                        SpecHolder.HolderInstance.MissionSpecMap[pKey]
                                            .Add(missionSpec.MissionSpecID);
                                        if (!mspecsCollapsed.Contains(missionSpec.contractTypeID))
                                        {
                                            mspecsCollapsed.Add(missionSpec.contractTypeID);
                                        }
                                    }
                                }
                                else
                                {
                                    ModInit.modLog.LogMessage(
                                        $"{p.Callsign} already has the maximum, {ModInit.modSettings.MaxMissionSpecializations}, Mission Specializations!");
                                }

                                if (missionSpec.cashMult > 0 && SpecHolder.HolderInstance.MissionSpecMap[pKey].Any(y=>y == missionSpec.MissionSpecID))
                                {
                                    contractPayOutMult += (missionSpec.cashMult);
                                    ModInit.modLog.LogMessage(
                                        $"current contract payout multiplier: {contractPayOutMult} from {missionSpec.MissionSpecName}");
                                }
                            }
                        }
                    }

                    if (ModInit.modSettings.WhiteListOpFor.Contains(__instance.Override.targetTeam.FactionValue.Name))
                    {
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
                    }

                    var opforspecCollapsed = SpecManager.ManagerInstance.OpForSpecList.Where(x =>
                            SpecHolder.HolderInstance.OpForSpecMap[pKey].Any(y => y == x.OpForSpecID))
                        .Select(x => x.factionID).Distinct().ToList();

                    foreach (var opforSpec in SpecManager.ManagerInstance.OpForSpecList)
                    {
                        if (opforSpec.killsRequired == 0)
                        {
                            ModInit.modLog.LogMessage($"{opforSpec.OpForSpecName} has 0 required kills set, ignoring.");
                            continue;
                        }
                        if ((SpecHolder.HolderInstance.OpForSpecMap[pKey].Count - taggedOPSpecCt <
                             ModInit.modSettings.MaxOpForSpecializations) ||
                            (!ModInit.modSettings.OpForTiersCountTowardMax && (opforspecCollapsed.Count - taggedOPSpecCt < ModInit.modSettings.MaxOpForSpecializations) || opforspecCollapsed.Contains(opforSpec.factionID)))
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
                                    if(!opforspecCollapsed.Contains(opforSpec.factionID))
                                    {
                                        opforspecCollapsed.Add(opforSpec.factionID);
                                    }
                                }
                            }
                        }
                        else
                        {
                            ModInit.modLog.LogMessage(
                                $"{p.Callsign} already has the maximum, {opforspecCollapsed.Count}/{ModInit.modSettings.MaxOpForSpecializations}, OpFor Specializations!");
                        }
                    }
                }

                SpecHolder.HolderInstance.totalBounty = SpecHolder.HolderInstance.kills * SpecHolder.HolderInstance.bounty;
                ModInit.modLog.LogMessage($"{SpecHolder.HolderInstance.totalBounty} in bounties awarded.");

                var employerRep = Mathf.RoundToInt(__instance.EmployerReputationResults * employerRepMult);
                var targetRep = Mathf.RoundToInt(__instance.TargetReputationResults * targetRepMult);
                var contractPayout = Mathf.RoundToInt((__instance.MoneyResults * contractPayOutMult) + SpecHolder.HolderInstance.totalBounty);

                ModInit.modLog.LogMessage($"Employer Reputation Change: {__instance.EmployerReputationResults} x {employerRepMult} = {employerRep}");
                ModInit.modLog.LogMessage($"Target Reputation Change: {__instance.TargetReputationResults} x {targetRepMult} = {targetRep}");
                ModInit.modLog.LogMessage($"Contract Payout: ({__instance.MoneyResults} x {contractPayOutMult}) + {SpecHolder.HolderInstance.totalBounty} = {contractPayout}");


                Traverse.Create(__instance).Property("EmployerReputationResults").SetValue(employerRep);
                Traverse.Create(__instance).Property("TargetReputationResults").SetValue(targetRep);
                Traverse.Create(__instance).Property("MoneyResults").SetValue(contractPayout);

                SpecHolder.HolderInstance.OpForKillsTEMPTracker = new Dictionary<string, Dictionary<string, int>>();
            }
        }
    }
}