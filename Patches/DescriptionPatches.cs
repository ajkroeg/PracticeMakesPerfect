using System;
using static PracticeMakesPerfect.Framework.GlobalVars;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using UnityEngine.UI;
using PracticeMakesPerfect.Framework;
using Harmony;
using UnityEngine;
using UnityEngine.Events;

namespace PracticeMakesPerfect.Patches
{
    public class DescriptionPatches
    {
        [HarmonyPatch(typeof(SGFactionReputationWidget), "Init",
            new Type[] {typeof(SimGameState), typeof(FactionValue), typeof(UnityAction), typeof(bool)})]

        public static class SGFactionReputationWidget_Init_Patch
        {
            public static void Postfix(SGFactionReputationWidget __instance, SimGameState sim, FactionValue faction,
                HBSTooltip tooltipReference, FactionDef currentFactionDef, UnityAction refreshCallback = null,
                bool bIsAar = false)
            {
                var specDesc = Descriptions.GetOpForSpecializationDescription(faction?.Name);

                var fdesc = currentFactionDef?.Description;
                if (!string.IsNullOrEmpty(specDesc))
                {
                    if (!fdesc.Contains(specDesc))
                    {
                        fdesc += specDesc;
                    }
                }

                Traverse.Create(currentFactionDef).Property("Description").SetValue(fdesc);

            }
        }


        [HarmonyPatch(typeof(SGContractsWidget), "PopulateContract", new Type[] {typeof(Contract), typeof(Action)})]

        public static class SGContractsWidget_onContractSelected_Patch
        {
            public static void Postfix(SGContractsWidget __instance, Contract contract,
                HBSTooltip contractTypeTooltip, Action onNegotiated = null)
            {
                string arg;
                if (contract.IsPriorityContract)
                {
                    arg = "Priority";
                }
                else
                {
                    arg = contract.Override.ContractTypeValue.Name;
                }

                var text2 = string.Format("ContractType{0}", arg);
                text2.Replace(" ", null);

                var specDesc =
                    Descriptions.GetMissionSpecializationDescription(contract.Override.ContractTypeValue.Name);
                if (!string.IsNullOrEmpty(text2) &&
                    sim.DataManager.Exists(BattleTechResourceType.BaseDescriptionDef, text2))
                {
                    var def2 = sim.DataManager.BaseDescriptionDefs.Get(text2);

                    if (!string.IsNullOrEmpty(specDesc))
                    {
                        if (!def2.Details.Contains(specDesc))
                        {
                            var details = def2.GetLocalizedDetails();
                            details.Append(specDesc);

                            var deets = details.ToString();
                            Traverse.Create(def2).Field("localizedDetails").SetValue(details);
                            Traverse.Create(def2).Property("Details").SetValue(deets);
                        }
                    }

                    contractTypeTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(def2));
                }
            }
        }


        [HarmonyPatch(typeof(SGBarracksDossierPanel), "SetPilot",
            new Type[] {typeof(Pilot), typeof(SGBarracksMWDetailPanel), typeof(bool), typeof(bool)})]

        public static class SGBarracksDossierPanel_SetPilot_Patch
        {
            public static void Postfix(SGBarracksDossierPanel __instance, Pilot p, SGBarracksMWDetailPanel details,
                bool isDissmissable, bool isThumb, Image portrait)
            {
                if (p == null) return;

                var tooltip = portrait.gameObject.GetComponent<HBSTooltip>() ??
                              portrait.gameObject.AddComponent<HBSTooltip>();

                var desc = tooltip.GetText();
                if (String.IsNullOrEmpty(desc))
                {
                    desc = "";
                }

                var specDesc = Descriptions.GetPilotSpecializationsOrProgress(p);
                desc += specDesc;

                var descDef = new BaseDescriptionDef("PilotSpecs", p.Callsign, desc, null);
                tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));
            }
        }

        [HarmonyPatch(typeof(SGBarracksMWDetailPanel), "OnServiceSectionClicked")]

        public static class SGBarracksMWDetailPanel_OnServiceClicked
        {
            public static bool Prefix(SGBarracksMWDetailPanel __instance, Pilot curPilot, SGBarracksDossierPanel dossier)
            {
                var background = UIManager.Instance.UILookAndColorConstants.PopupBackfill;

                var hk = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (!hk) return true;
                var resetSpecs = true;
                if (curPilot.IsPlayerCharacter && SpecManager.ManagerInstance.StratComs.Count > 1)
                {
                    GenericPopupBuilder
                        .Create("Change Stratcoms or Reset Specs?",
                            "Do you wish to change StratComs or reset specializations?.")
                        .AddButton("Change StratComs", () =>
                        {
                            var stratDesc = "";
                            var stratcoms = SpecManager.ManagerInstance.StratComs;
                            foreach (var stratCom in stratcoms)
                            {
                                var description =
                                    $"<b>{stratCom.StratComName}:</b> {stratCom.description}\n\n";
                                stratDesc += description;
                            }

                            var popup = GenericPopupBuilder.Create("Select Active StratCom", stratDesc).CancelOnEscape()
                                .AddFader(background);
                            foreach (var stratCom in stratcoms)
                            {
                                popup.AddButton(stratCom.StratComName, () =>
                                {
                                    SpecManager.ManagerInstance.SetStratCom(stratCom.StratComID, curPilot, dossier, __instance);
                                    resetSpecs = false;
                                });
                            }
                            popup.Render();
                        })
                        .AddButton("Reset Specs", () =>
                        {
                            if (!String.IsNullOrEmpty(ModInit.modSettings.argoUpgradeToReset) && resetSpecs &&
                                !sim.CompanyTags.Contains(ModInit.modSettings.argoUpgradeToReset))
                            {
                                GenericPopupBuilder
                                    .Create("Unable To Reset Specializations", $"Required Argo Upgrade Not Found: {ModInit.modSettings.argoUpgradeToReset}.")
                                    .AddButton("Understood")
                                    .CancelOnEscape()
                                    .AddFader(background)
                                    .Render();
                            }

                            else if ((!String.IsNullOrEmpty(ModInit.modSettings.argoUpgradeToReset) && resetSpecs &&
                                      sim.CompanyTags.Contains(ModInit.modSettings.argoUpgradeToReset)) ||
                                     String.IsNullOrEmpty(ModInit.modSettings.argoUpgradeToReset))
                            {
                                GenericPopupBuilder
                                    .Create("Reset Specializations",
                                        "Are you sure you want to reset all specializations and progress for this pilot?")
                                    .AddButton("Cancel")
                                    .AddButton("Reset Mission Specs",
                                        () => SpecManager.ManagerInstance.ResetMissionSpecs(curPilot, dossier,
                                            __instance))
                                    .AddButton("Reset Opfor Specs",
                                        () => SpecManager.ManagerInstance.ResetOpForSpecs(curPilot, dossier,
                                            __instance))
                                    .CancelOnEscape()
                                    .AddFader(background)
                                    .Render();
                            }
                        }).CancelOnEscape()
                        .AddFader(background)
                        .Render();
                    return false;
                }

                if (!String.IsNullOrEmpty(ModInit.modSettings.argoUpgradeToReset) && resetSpecs &&
                    !sim.CompanyTags.Contains(ModInit.modSettings.argoUpgradeToReset))
                {
                    GenericPopupBuilder
                        .Create("Unable To Reset Specializations", $"Required Argo Upgrade Not Found: {ModInit.modSettings.argoUpgradeToReset}.")
                        .AddButton("Understood")
                        .CancelOnEscape()
                        .AddFader(background)
                        .Render();
                    return false;
                }

                else if ((!String.IsNullOrEmpty(ModInit.modSettings.argoUpgradeToReset) && resetSpecs &&
                         sim.CompanyTags.Contains(ModInit.modSettings.argoUpgradeToReset)) || String.IsNullOrEmpty(ModInit.modSettings.argoUpgradeToReset))
                {
                    GenericPopupBuilder
                        .Create("Reset Specializations", "Are you sure you want to reset all specializations and progress for this pilot?")
                        .AddButton("Cancel")
                        .AddButton("Reset Mission Specs", () => SpecManager.ManagerInstance.ResetMissionSpecs(curPilot, dossier, __instance))
                        .AddButton("Reset Opfor Specs", () => SpecManager.ManagerInstance.ResetOpForSpecs(curPilot, dossier, __instance))
                        .CancelOnEscape()
                        .AddFader(background)
                        .Render();
                    return false;
                }
                else
                {
                    return false;
                }
            }
        }


    }
}