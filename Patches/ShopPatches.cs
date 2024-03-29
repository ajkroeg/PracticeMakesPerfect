﻿using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using PracticeMakesPerfect.Framework;
using static PracticeMakesPerfect.Framework.GlobalVars;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using UnityEngine;

namespace PracticeMakesPerfect.Patches
{
    public class ShopPatches
    {
        [HarmonyPatch(typeof(SimGameState), "GetReputationShopAdjustment", new Type[] {typeof(FactionValue)})]
        public static class SGS_GetReputationShopAdjustmentFV_Patch
        {
            public static bool Prepare() => ModInit.modSettings.enableSpecializations;
            public static void Postfix(SimGameState __instance, ref float __result, FactionValue faction)
            {
                if (GlobalVars.sim == null) return;
                var curPilots = new List<string> {GlobalVars.sim.Commander.FetchGUID()};
                foreach (var p in GlobalVars.sim.PilotRoster)
                {
                    SpecHolder.HolderInstance.AddToMaps(p);
                    curPilots.Add(p.FetchGUID());
                }

                var discount = 0f;
                foreach (var opSpec in from pKey in curPilots where SpecHolder.HolderInstance.OpForSpecMap.ContainsKey(pKey) from spec in SpecHolder.HolderInstance.OpForSpecMap[pKey] select SpecManager.ManagerInstance.OpForSpecList.FirstOrDefault(x => x.OpForSpecID == spec) into opSpec where opSpec.storeDiscount.ContainsKey(faction.Name) select opSpec)
                {
                    discount += opSpec.storeDiscount[faction.Name];
                    ModInit.modLog.LogMessage($"Current discount from specs: {discount}");
                }

                ModInit.modLog.LogMessage($"Total discount from specs: {discount}");
                __result += discount;
            }
        }


        [HarmonyPatch(typeof(SG_Shop_Screen), "AddShopItemToWidget",
            new Type[] {typeof(ShopDefItem), typeof(Shop), typeof(IMechLabDropTarget), typeof(bool), typeof(bool)})]
        [HarmonyPriority(Priority.First)]
        public static class SH_Shop_Screen_AddShopItemToWidget
        {
            public static bool Prepare() => ModInit.modSettings.enableSpecializations;
            public static void Prefix(SG_Shop_Screen __instance, ShopDefItem itemDef, Shop shop, //removed ref from shopdefitem?
                IMechLabDropTarget targetWidget, bool isSelling = false, bool isBulkAdd = false)
            {
                if (GlobalVars.sim == null) return;
                if (!isSelling) return;
                var curPilots = new List<string> {GlobalVars.sim.Commander.FetchGUID()};
                foreach (var p in GlobalVars.sim.PilotRoster)
                {
                    SpecHolder.HolderInstance.AddToMaps(p);
                    curPilots.Add(p.FetchGUID());
                }

                var sellBonus = 1f;
                string shopOwner; 
                if (shop.ThisShopType == Shop.ShopType.BlackMarket)
                {
                    shopOwner = FactionEnumeration.GetAuriganPiratesFactionValue().Name;
                    ModInit.modLog.LogMessage($"System: {__instance.theSystem.Name}. shopOwner: {shopOwner}");
                }
                else
                {
                    shopOwner = sim.CurSystem.Def.OwnerValue.Name;
                    //shopOwner = Traverse.Create(shop).Field("system").GetValue<StarSystem>().Def.OwnerValue.Name;
                    ModInit.modLog.LogMessage($"System: {__instance.theSystem.Name}. shopOwner: {shopOwner}");
                }

                foreach (var pKey in curPilots)
                {
                    if (SpecHolder.HolderInstance.OpForSpecMap.ContainsKey(pKey))
                    {
                        foreach (var spec in SpecHolder.HolderInstance.OpForSpecMap[pKey])
                        {
                            var opSpec =
                                SpecManager.ManagerInstance.OpForSpecList.FirstOrDefault(x => x.OpForSpecID == spec);
                            if (opSpec != null && opSpec.storeBonus.ContainsKey(shopOwner))
                            {
                                sellBonus += opSpec.storeBonus[shopOwner];
                                ModInit.modLog.LogMessage($"Current sell multiplier from specs: {sellBonus}");
                            }
                        }
                    }
                }

                ModInit.modLog.LogMessage($"Total sell multiplier from specs: {sellBonus}");
                ModInit.modLog.LogMessage($"Original sell price: {itemDef.SellCost}");
                var cost = itemDef.SellCost * sellBonus;
                ModInit.modLog.LogMessage($"Final sell price: {cost}");
                itemDef.SellCost = Mathf.RoundToInt(cost);
            }
        }

        [HarmonyPatch(typeof(SG_Stores_MiniFactionWidget), "FillInData",
            new Type[] {typeof(FactionValue)})]
        public static class SG_Stores_MiniFactionWidget_FillInData_Patch
        {
            public static bool Prepare() => ModInit.modSettings.enableSpecializations;
            public static void Postfix(SG_Stores_MiniFactionWidget __instance, FactionValue theFaction)
            {
                if (GlobalVars.sim == null) return;
                var sellBonus = 0f;

                var curPilots = new List<string>();
                curPilots.Add(GlobalVars.sim.Commander.FetchGUID());
                foreach (var p in GlobalVars.sim.PilotRoster)
                {
                    SpecHolder.HolderInstance.AddToMaps(p);
                    curPilots.Add(p.FetchGUID());
                }

                foreach (var pKey in curPilots)
                {
                    if (SpecHolder.HolderInstance.OpForSpecMap.ContainsKey(pKey))
                    {
                        foreach (var spec in SpecHolder.HolderInstance.OpForSpecMap[pKey])
                        {
                            var opSpec =
                                SpecManager.ManagerInstance.OpForSpecList.FirstOrDefault(x => x.OpForSpecID == spec);
                            if (opSpec != null && opSpec.storeBonus.ContainsKey(__instance.owningFactionValue.Name))
                            {
                                sellBonus += opSpec.storeBonus[__instance.owningFactionValue.Name];
                                ModInit.modLog.LogMessage($"Current sell multiplier from specs: {sellBonus}");
                            }
                        }
                    }
                }

                if (sellBonus == 0f) return;
                __instance.ReputationBonusText.AppendTextAndRefresh(", {0}% Sell Bonus", new object[]
                    {
                        Mathf.RoundToInt(sellBonus * 100f)
                    });
            }
        }
    }
}
