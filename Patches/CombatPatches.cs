using System;
using System.Collections.Generic;
using Harmony;
using BattleTech;
using PracticeMakesPerfect.Framework;
using BattleTech.Designed;
using BattleTech.UI;
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
                            if (!SpecHolder.HolderInstance.OpForKillsTEMPTracker.ContainsKey(pKey))
                            {
                                SpecHolder.HolderInstance.OpForKillsTEMPTracker.Add(pKey, new Dictionary<string, int>());
                                SpecHolder.HolderInstance.OpForKillsTEMPTracker[pKey].Add(opfor, 0);
                                ModInit.modLog.LogMessage(
                                    $"*****Something done fucked up because {p.Callsign} should probably already have an OpForKillsTEMPTracker. Adding it anyway.");
                            }
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

        [HarmonyPatch(typeof(AttackDirector), "CreateAttackSequence",
            new Type[] {typeof(int), typeof(AbstractActor), typeof(ICombatant), typeof(Vector3), typeof(Quaternion), typeof(int), typeof(List<Weapon>), typeof(MeleeAttackType), typeof(int), typeof(bool)})]
        public static class AttackDirectorAttackSequence_OnAttackSequenceFire
        {
            [HarmonyPriority(Priority.First)]
            public static void Prefix(AttackDirector __instance, int stackItemUID, AbstractActor attacker, ICombatant target, Vector3 attackPosition, Quaternion attackRotation, int attackSequenceIdx, List<Weapon> selectedWeapons, MeleeAttackType meleeAttackType, int calledShotLocation, bool isMoraleAttack)
            {

                try
                {
                    if (attacker == null || target == null) return;

                    if (attacker.team.IsLocalPlayer)
                    {
                        //    opforUnit = target.GetPilot().ParentActor;
                        if ((target.UnitType & UnitType.Building) != 0)
                        {
                            SpecManager.ManagerInstance.GatherAndApplyActiveBuildingSpecEffects(attacker, target);
                            return;
                        }

                        foreach (var encounterObjectGameLogic in attacker.Combat.EncounterLayerData.encounterObjectGameLogicList)
                        {
                            if (encounterObjectGameLogic as DefendLanceWithEscapeChunkGameLogic != null)
                            {
                                var encounterAsChunk = encounterObjectGameLogic as DefendLanceWithEscapeChunkGameLogic;
                                var encounterAsOGL = encounterAsChunk.ensureUnitsSurviveObjective.encounterObject;
                                if (Traverse.Create(encounterAsOGL).Property("IsContractObjectivePrimary").GetValue<bool>())
                                {
                                    ModInit.modLog.LogMessage($"Checking for primary target unit.");
                                    if (encounterAsOGL.GetTargetUnits().Contains(target))
                                    {
                                        ModInit.modLog.LogMessage($"Primary target found.");
                                        SpecManager.ManagerInstance.GatherAndApplyActivePrimarySpecEffects(attacker,
                                            target);
                                        return;
                                    }
                                }
                            }
                            else if (encounterObjectGameLogic as DefendXUnitsChunkGameLogic != null)
                            {
                                var encounterAsChunk = encounterObjectGameLogic as DefendXUnitsChunkGameLogic;
                                var encounterAsOGL = encounterAsChunk.defendXUnitsObjective.encounterObject;
                                if (Traverse.Create(encounterAsOGL).Property("IsContractObjectivePrimary").GetValue<bool>())
                                {
                                    ModInit.modLog.LogMessage($"Checking for primary target unit.");
                                    if (encounterAsOGL.GetTargetUnits().Contains(target))
                                    {
                                        ModInit.modLog.LogMessage($"Primary target found.");
                                        SpecManager.ManagerInstance.GatherAndApplyActivePrimarySpecEffects(attacker,
                                            target);
                                        return;
                                    }
                                }
                            }
                            else if (encounterObjectGameLogic as DestroyXUnitsChunkGameLogic != null)
                            {
                                var encounterAsChunk = encounterObjectGameLogic as DestroyXUnitsChunkGameLogic;
                                var encounterAsOGL = encounterAsChunk.destroyXUnitsObjective.encounterObject;
                                if (Traverse.Create(encounterAsOGL).Property("IsContractObjectivePrimary").GetValue<bool>())
                                {
                                    ModInit.modLog.LogMessage($"Checking for primary target unit.");
                                    if (encounterAsOGL.GetTargetUnits().Contains(target))
                                    {
                                        ModInit.modLog.LogMessage($"Primary target found.");
                                        SpecManager.ManagerInstance.GatherAndApplyActivePrimarySpecEffects(attacker,
                                            target);
                                        return;
                                    }
                                }
                            }

                            var opforUnit = target.GetPilot().ParentActor;
                            SpecManager.ManagerInstance.GatherAndApplyActiveSpecEffects(attacker, opforUnit);
                        }


                    }
                    else if (target.team.IsLocalPlayer)
                    {
                        var playerUnit = target.GetPilot().ParentActor;
                        SpecManager.ManagerInstance.GatherAndApplyGlobalEffects(attacker, target);
                        //                        ModInit.modLog.LogMessage($"Attacker is opfor {attacker.DisplayName}, target is player unit {playerUnit.GetPilot().Callsign}.");
                        SpecManager.ManagerInstance.GatherAndApplyActiveSpecEffects(playerUnit, attacker);
                    }
                    else
                    {
                        ModInit.modLog.LogMessage($"Should be menage a trois.");
                        SpecManager.ManagerInstance.GatherAndApplyGlobalEffects(attacker, target);
                    }

                    

                    //var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                    //Traverse.Create(HUD).Method("updateHUDElements", ___displayedActor);


                }
                catch (Exception ex)
                {
                    ModInit.modLog.LogException(ex);
                }
            }
        }



        [HarmonyPatch(typeof(CombatHUDWeaponPanel), "RefreshDisplayedWeapons", new Type[] {typeof(bool), typeof(bool)})]
        public static bool Prepare()
        {
            return false;
        }
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
                    AbstractActor opforUnit = target as AbstractActor;
                    if (___displayedActor.team.IsLocalPlayer)
                    {
                        playerUnit = ___displayedActor;
                    //    opforUnit = target.GetPilot().ParentActor;
                        if ((target.UnitType & UnitType.Building) != 0)
                        {
                            SpecManager.ManagerInstance.GatherAndApplyActiveBuildingSpecEffects(playerUnit, target);
                            return;
                        }


                        foreach (var encounterObjectGameLogic in playerUnit.Combat.EncounterLayerData.encounterObjectGameLogicList)
                        {
                            if (encounterObjectGameLogic as DefendLanceWithEscapeChunkGameLogic != null) 
                            {
                                var encounterAsChunk = encounterObjectGameLogic as DefendLanceWithEscapeChunkGameLogic;
                                var encounterAsOGL = encounterAsChunk.ensureUnitsSurviveObjective.encounterObject;
                                if (Traverse.Create(encounterAsOGL).Property("IsContractObjectivePrimary").GetValue<bool>())
                                {
                                    ModInit.modLog.LogMessage($"Checking for primary target unit.");
                                    if (encounterAsOGL.GetTargetUnits().Contains(target))
                                    {
                                        ModInit.modLog.LogMessage($"Primary target found.");
                                        SpecManager.ManagerInstance.GatherAndApplyActivePrimarySpecEffects(playerUnit,
                                            target);
                                        return;
                                    }
                                }
                            }
                            else if (encounterObjectGameLogic as DefendXUnitsChunkGameLogic != null)
                            {
                                var encounterAsChunk = encounterObjectGameLogic as DefendXUnitsChunkGameLogic;
                                var encounterAsOGL = encounterAsChunk.defendXUnitsObjective.encounterObject;
                                if (Traverse.Create(encounterAsOGL).Property("IsContractObjectivePrimary").GetValue<bool>())
                                {
                                    ModInit.modLog.LogMessage($"Checking for primary target unit.");
                                    if (encounterAsOGL.GetTargetUnits().Contains(target))
                                    {
                                        ModInit.modLog.LogMessage($"Primary target found.");
                                        SpecManager.ManagerInstance.GatherAndApplyActivePrimarySpecEffects(playerUnit,
                                            target);
                                        return;
                                    }
                                }
                            }
                            else if (encounterObjectGameLogic as DestroyXUnitsChunkGameLogic != null)
                            {
                                var encounterAsChunk = encounterObjectGameLogic as DestroyXUnitsChunkGameLogic;
                                var encounterAsOGL = encounterAsChunk.destroyXUnitsObjective.encounterObject;
                                if (Traverse.Create(encounterAsOGL).Property("IsContractObjectivePrimary").GetValue<bool>())
                                {
                                    ModInit.modLog.LogMessage($"Checking for primary target unit.");
                                    if (encounterAsOGL.GetTargetUnits().Contains(target))
                                    {
                                        ModInit.modLog.LogMessage($"Primary target found.");
                                        SpecManager.ManagerInstance.GatherAndApplyActivePrimarySpecEffects(playerUnit,
                                            target);
                                        return;
                                    }
                                }
                            }
                        }
                        

                    }
                    else if (target.team.IsLocalPlayer)
                    {
                        playerUnit = target.GetPilot().ParentActor;
                        opforUnit = ___displayedActor;
//                        ModInit.modLog.LogMessage($"Attacker is opfor {attacker.DisplayName}, target is player unit {playerUnit.GetPilot().Callsign}.");
                    }
                    else
                    {
                        ModInit.modLog.LogMessage($"Player not involved here.");
                        return;
                    }

                    SpecManager.ManagerInstance.GatherAndApplyActiveSpecEffects(playerUnit, opforUnit);

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