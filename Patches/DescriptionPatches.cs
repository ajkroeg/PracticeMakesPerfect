﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static PracticeMakesPerfect.Framework.GlobalVars;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using UnityEngine.UI;
using PracticeMakesPerfect.Framework;
using Harmony;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace PracticeMakesPerfect.Patches
{
    class DescriptionPatches
    {
        [HarmonyPatch(typeof(SGFactionReputationWidget), "Init", new Type[] {typeof(SimGameState), typeof(FactionValue), typeof(UnityAction), typeof(bool)})]

        public static class SGFactionReputationWidget_Init_Patch
        {
            public static void Postfix(SGFactionReputationWidget __instance, SimGameState sim, FactionValue faction, HBSTooltip ___TooltipReference, FactionDef ___CurrentFactionDef, UnityAction RefreshCallback = null, bool bIsAAR = false)
            {
                var specDesc = Descriptions.getOpForSpecializationDescription(faction?.Name);

                var fdesc = ___CurrentFactionDef?.Description;
                if (!string.IsNullOrEmpty(specDesc))
                {
                    if (!fdesc.Contains(specDesc))
                    {
                        fdesc += specDesc;
                    }
                }
                Traverse.Create(___CurrentFactionDef).Property("Description").SetValue(fdesc);

            }
        }


        [HarmonyPatch(typeof(SGContractsWidget), "PopulateContract", new Type[]{typeof(Contract), typeof(Action)})]

        public static class SGContractsWidget_onContractSelected_Patch
        {
            public static void Postfix(SGContractsWidget __instance, Contract contract, HBSTooltip ___ContractTypeTooltip, Action onNegotiated = null)
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
                string text2 = string.Format("ContractType{0}", arg);
                text2.Replace(" ", null);

                var specDesc = Descriptions.getMissionSpecializationDescription(contract.Override.ContractTypeValue.Name);
                if (!string.IsNullOrEmpty(text2) && sim.DataManager.Exists(BattleTechResourceType.BaseDescriptionDef, text2))
                {
                    BaseDescriptionDef def2 = sim.DataManager.BaseDescriptionDefs.Get(text2);

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
                    ___ContractTypeTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(def2));
                }
            }
        }


        [HarmonyPatch(typeof(SGBarracksDossierPanel), "SetPilot",
            new Type[] {typeof(Pilot), typeof(SGBarracksMWDetailPanel), typeof(bool), typeof(bool)})]

        public static class SGBarracksDossierPanel_SetPilot_Patch
        {
            public static void Postfix(SGBarracksDossierPanel __instance, Pilot p, SGBarracksMWDetailPanel details,
                bool isDissmissable, bool isThumb, Image ___portrait)
            {
                if (p == null) return;

                HBSTooltip tooltip = ___portrait.gameObject.GetComponent<HBSTooltip>() ?? ___portrait.gameObject.AddComponent<HBSTooltip>();

                string desc = tooltip.GetText();
                if (String.IsNullOrEmpty(desc))
                {
                    desc = "";
                }
                var specDesc = Descriptions.getPilotSpecializationsOrProgress(p);
                desc += specDesc;

                var descDef = new BaseDescriptionDef("PilotSpecs", p.Callsign, desc, null);
                tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));

            }
        }
    }
}
