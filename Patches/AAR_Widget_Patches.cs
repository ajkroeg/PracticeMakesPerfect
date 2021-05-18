using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using BattleTech;
using PracticeMakesPerfect.Framework;
using static PracticeMakesPerfect.Framework.GlobalVars;
using BattleTech.Framework;
using BattleTech.UI;
using UnityEngine;

namespace PracticeMakesPerfect.Patches
{
    public class AAR_Widget_Patches
    {
        [HarmonyPatch(typeof(AAR_FactionReputationResultWidget), "InitializeData",
            new Type[] {typeof(SimGameState), typeof(Contract)})]
        public static class AAR_FactionReputationResultWidget_InitializeData_Patch
        {
            public static void Postfix(AAR_FactionReputationResultWidget __instance,
                List<SGReputationWidget_Simple> ___FactionWidgets, RectTransform ___WidgetListAnchor,
                Contract ___contract)
            {
                var employer = ___contract.Override.employerTeam.FactionDef.FactionValue.Name;
                var target = ___contract.Override.targetTeam.FactionDef.FactionValue.Name;

                var curPilots = new List<string>();
                var playerUnits = UnityGameInstance.BattleTechGame.Combat.AllActors.Where(x => x.team.IsLocalPlayer);
                foreach (var unit in playerUnits)
//                foreach (Pilot p in GlobalVars.sim.PilotRoster)
                {
                    var p = unit.GetPilot();
                    SpecHolder.HolderInstance.AddToMaps(p);
                    curPilots.Add(p.FetchGUID());
                }
//                curPilots.Add(GlobalVars.sim.Commander.FetchGUID());


                var repMultDictionary = new Dictionary<string, float>();
                
                
                foreach (var pKey in curPilots)
                {
                    if (SpecHolder.HolderInstance.OpForSpecMap.ContainsKey(pKey))
                    {
                        foreach (var spec in SpecHolder.HolderInstance.OpForSpecMap[pKey])
                        {
                            var opSpec =
                                SpecManager.ManagerInstance.OpForSpecList.FirstOrDefault(x => x.OpForSpecID == spec && x.factionID == target);
                            if (opSpec == null) continue;
                            if (opSpec.repMult.Count > 0)
                            {
                                foreach (var repMult in opSpec.repMult.Where(x => x.Key != target && x.Key != employer && x.Key != target_string && x.Key != employer_string && x.Key != owner_string))
                                {
                                    if (!repMultDictionary.ContainsKey(repMult.Key))
                                    {
                                        repMultDictionary.Add(repMult.Key, repMult.Value);
                                    }
                                    else
                                    {
                                        repMultDictionary[repMult.Key] += repMult.Value;
                                    }
                                    ModInit.modLog.LogMessage($"repMultDictionary contains {repMult.Key} and {repMult.Value}");
                                }
                            }
                        }
                    }
                }

                var idx = ___FactionWidgets.Count;
                foreach (var repMult in repMultDictionary)
                {
                    var component = sim.DataManager
                        .PooledInstantiate("uixPrfWidget_AAR_FactionRepBarAndIcon",
                            BattleTechResourceType.UIModulePrefabs)
                        .GetComponent<SGReputationWidget_Simple>();
                    component.transform.SetParent(___WidgetListAnchor, false);

                    ___FactionWidgets.Add(component);

                    var faction = UnityGameInstance.BattleTechGame.DataManager.Factions
                        .FirstOrDefault(x => x.Value.FactionValue.Name == repMult.Key).Value;

                    int repChange;
                    if (___contract.Override.employerTeam.FactionDef.FactionValue.DoesGainReputation)
                    {
                        repChange = Mathf.RoundToInt( SpecHolder.HolderInstance.emplRep *
                                                         repMultDictionary[faction.FactionValue.Name]);
                        ModInit.modLog.LogMessage($"Employer {___contract.Override.employerTeam.FactionDef.FactionValue.Name} is reputation gainer: base employer change {SpecHolder.HolderInstance.emplRep}");
                    }
                    else
                    {
                        repChange = Math.Abs(Mathf.RoundToInt(___contract.TargetReputationResults *
                                                              repMultDictionary[faction.FactionValue.Name]));
                        ModInit.modLog.LogMessage($"Employer {___contract.Override.employerTeam.FactionDef.FactionValue.Name} is NOT reputation gainer: base target rep change {___contract.TargetReputationResults}");


                        if (repChange <= 1)
                        {
                            repChange = ___contract.Difficulty + 2; //this is some hacky ugly bullshit for stupid edgecase where player is working for local gov against someone with fully negatived rep.
                            ModInit.modLog.LogMessage($"{faction.Name} rep change would have been <=1, changing to contract difficulty +2: {___contract.Difficulty + 2}");
                        }
                    }

                    __instance.SetWidgetData(idx, faction.FactionValue, repChange, true);

                    sim.SetReputation(faction.FactionValue, repChange);
                    ModInit.modLog.LogMessage($"Reputation with {faction.FactionValue.Name} changed by {repChange}");

                    idx += 1;
                }
            }
        }



        [HarmonyPatch(typeof(AAR_ContractObjectivesWidget), "FillInObjectives")]
        public static class AAR_ContractObjectivesWidget_FillInObjectives_Patch
        {
            public static void Postfix(AAR_ContractObjectivesWidget __instance)
            {

                if (SpecHolder.HolderInstance.totalBounty != 0)
                {
                    var addObjectiveMethod = Traverse.Create(__instance).Method("AddObjective", new Type[] { typeof(MissionObjectiveResult) });

                    var bountyResults = $"Bounty Payouts: {SpecHolder.HolderInstance.kills} kills x {SpecHolder.HolderInstance.bounty} = ¢{SpecHolder.HolderInstance.totalBounty}";

                    var bountyPayouts = new MissionObjectiveResult(bountyResults, Guid.NewGuid().ToString(), false, true, ObjectiveStatus.Succeeded, false);

                    addObjectiveMethod.GetValue(bountyPayouts);

                    ModInit.modLog.LogMessage($"{SpecHolder.HolderInstance.totalBounty} in bounties awarded.");
                }

                else
                {
                    ModInit.modLog.LogMessage($"No bounties awarded.");
                }
                
            }
        }
    }
}