using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Harmony;
using BattleTech;
using PracticeMakesPerfect.Framework;
using static PracticeMakesPerfect.Framework.GlobalVars;
using System.Threading.Tasks;
using BattleTech.UI;
using JetBrains.Annotations;
using UnityEngine;

namespace PracticeMakesPerfect.Patches
{
    class CombatPatches
    {
        
        [HarmonyPatch(typeof(Turret), "FlagForDeath",
            new Type[] {typeof(string), typeof(DeathMethod), typeof(DamageType), typeof(int), typeof(int), typeof(string), typeof(bool)})]

        public static class Turret_FlagForDeath_Patch
        {
            [HarmonyBefore(new string[] { "us.frostraptor.ConcreteJungle" })]

            public static void Prefix(Turret __instance, string reason, DeathMethod deathMethod,
                DamageType damageType, int location, int stackItemID, string attackerID, bool isSilent)
            {
                if (__instance.IsFlaggedForDeath)  return;
                AbstractActor attacker = __instance.Combat.FindActorByGUID(attackerID);
                if (attacker != null && attacker != __instance && attacker.team.IsLocalPlayer)
                {
                    Pilot p = attacker.GetPilot();
                    if (p != null)
                    {
                        
                        var pKey = p.FetchGUID();
                        var opfor = __instance.team.FactionValue.Name;
                        if (ModInit.modSettings.WhiteListOpFor.Contains(opfor))
                        {
                            if (!SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].ContainsKey(opfor))
                            {
                                SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].Add(opfor, 0);
                                ModInit.modLog.LogMessage(
                                    $"No key for {opfor} found in {p.Callsign}'s OpForTracker. Adding it.");
                            }
                            ModInit.modLog.LogMessage($"Adding 1 to {p.Callsign}'s {opfor} OpForTracker.");
                            SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey][opfor] += 1;
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(CombatHUDWeaponPanel), "RefreshDisplayedWeapons", new Type[] {typeof(bool), typeof(bool)})]
        public static class CombatHUDWeaponPanel_RefreshDisplayedWeapons_patch
        {
            [HarmonyPriority(Priority.First)]
            public static void Prefix(CombatHUDWeaponPanel __instance, AbstractActor ___displayedActor, bool consideringJump = false, bool useCOILPathingPreview = true)
            {
                try
                {
                    var target = Traverse.Create(__instance).Property("target").GetValue<ICombatant>();
                    if (___displayedActor == null || target == null) return;
                    var attacker = ___displayedActor;


                    AbstractActor playerUnit = null;
                    AbstractActor opforUnit = null;
                    if (attacker.team.IsLocalPlayer)
                    {
                        if (!target.IsPilotable) return;
                        playerUnit = attacker;
                        opforUnit = target.GetPilot().ParentActor;
//                        ModInit.modLog.LogMessage($"Attacker is player unit {playerUnit.GetPilot().Callsign}, target is opfor {opforUnit.DisplayName}.");
                    }
                    else if (target.team.IsLocalPlayer)
                    {
                        playerUnit = target.GetPilot().ParentActor;
                        opforUnit = attacker;
//                        ModInit.modLog.LogMessage($"Attacker is opfor {attacker.DisplayName}, target is player unit {playerUnit.GetPilot().Callsign}.");
                    }
                    else
                    {
                        ModInit.modLog.LogMessage($"Player not involved here.");
                        return;
                    }

                    SpecManager.ManagerInstance.GatherAndApplyActiveOp4SpecEffects(playerUnit, opforUnit);

                    //var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                    //Traverse.Create(HUD).Method("updateHUDElements", ___displayedActor);


                }
                catch (Exception ex)
                {
                    ModInit.modLog.LogException(ex);
                }
            }
        }

        [HarmonyPatch(typeof(Team), "AddUnit", new Type[] {typeof(AbstractActor)})]
        public static class Team_AddUnit
        {
            public static void Postfix(Team __instance, AbstractActor unit)
            {
                if (unit.Combat.TurnDirector.CurrentRound > 1)
                {
                    ModInit.modLog.LogMessage($"{unit.GetPilot().Callsign} spawned after end of first round, gathering and applying post-spawn effects.");
                    SpecManager.ManagerInstance.GatherAndApplyPostSpawnEffects(unit);
                }
            }
        }
    }
}