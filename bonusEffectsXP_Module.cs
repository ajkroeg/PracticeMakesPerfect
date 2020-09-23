using System;
using Harmony;
using BattleTech;
using System.Collections.Generic;
using System.Linq;
using BattleTech.Framework;
using BattleTech.UI.Tooltips;

namespace PracticeMakesPerfect
{
    
    public class bonusEffectsXP_Module
    {
        private static bool reUseRestricted = false;
        private static bool reUseDegrades = false;
        private static Effect appliedEffect = null;
        private static List<Effect> effectsList;

        [HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
        public static class AbstractActor_InitEffectStats_Patch
        {
            public static void Postfix(AbstractActor __instance)
            {
                var p = __instance.GetPilot();

                //START DOING CONDITIONALS FOR PLAYER TEAM
                p.StatCollection.AddStatistic<int>("effectXP", 0);
                ModInit.modLog.LogMessage($"Initializing bonus effectXP stat for {p?.Description?.Callsign}");
                

                return;
            }
        }


        [HarmonyPatch(typeof(EffectManager), "CreateEffect", new Type[] 
            {typeof(EffectData),typeof(string),typeof(int),typeof(ICombatant),typeof(ICombatant),typeof(WeaponHitInfo), typeof(int), typeof(bool)})]
        public static class EffectManager_CreateEffect_Patch
        {
            public static void Prefix(EffectManager __instance, EffectData effectData, ICombatant creator, ICombatant target)
            {
                var p = creator.GetPilot();
                {
                    effectsList = __instance.GetAllEffectsCreatedBy(creator.GUID);
                    if (effectsList.Count > 0)
                    {

                        appliedEffect =
                            effectsList.FirstOrDefault(x => x.EffectData == effectData && x.Target == target);

                        if (ModInit.Settings.reUseRestrictedbonusEffects_XP.ContainsKey(effectData?.Description?.Id ?? "UNUSED") ||
                            ModInit.Settings.reUseRestrictedbonusEffects_XP.ContainsKey(
                                effectData?.statisticData?.statName ?? "UNUSED"))
                        {
                            reUseRestricted = effectsList.Any(x => x.EffectData == effectData && x.Target == target);
                            ModInit.modLog.LogMessage(
                                $"Matching effect found: {effectData?.Description?.Id} from {creator?.GetPilot()?.Description?.Callsign} targeting {target?.Description?.UIName}. No double-dipping!");
                        }

                        else if (ModInit.Settings.degradingbonusEffects_XP.ContainsKey(effectData?.Description?.Id ?? "UNUSED") ||
                                 ModInit.Settings.degradingbonusEffects_XP.ContainsKey(
                                     effectData?.statisticData?.statName ?? "UNUSED"))
                        {
                            reUseDegrades = effectsList.Any(x => x?.EffectData == effectData && x?.Target == target);
                            ModInit.modLog.LogMessage(
                                $"Matching effect found: {effectData?.Description?.Id} from {creator?.GetPilot()?.Description?.Callsign} targeting {target?.Description?.UIName}. Applying degraded double-dipping!");

                        }
                    }
                }
            }
            public static void Postfix(EffectManager __instance, EffectData effectData, ICombatant creator, ICombatant target)
            {
                var p = creator.GetPilot();
                

  //              var effectsList = Traverse.Create(__instance).Field("effects").GetValue<List<Effect>>();
  //              var effectsList = __instance.GetAllEffectsCreatedBy(creator.GUID);


                ////////////////////no double dipping
                
                if (ModInit.Settings.reUseRestrictedbonusEffects_XP.ContainsKey(effectData?.Description?.Id ?? "UNUSED") &&
                    !reUseRestricted)
                {
                    
                    int effectXP = ModInit.Settings.reUseRestrictedbonusEffects_XP[effectData?.Description?.Id];

                    var stat = p.StatCollection.GetStatistic("effectXP");
                    p.StatCollection.Int_Add(stat, effectXP);
                    ModInit.modLog.LogMessage($"No existing effect {effectData.Description.Id} from {creator?.GetPilot()?.Description?.Callsign} targeting {target?.Description?.UIName}. Adding {effectXP} to {creator?.GetPilot()?.Description?.Callsign}'s 'effectXP' pilot stat. No double-dipping until effect expires.");
                    return;
                }

                else if (effectData.effectType == EffectType.StatisticEffect && ModInit.Settings.reUseRestrictedbonusEffects_XP.ContainsKey(effectData?.statisticData?.statName ?? "UNUSED") &&
                         !reUseRestricted)

                {
                    int effectXP = ModInit.Settings.reUseRestrictedbonusEffects_XP[effectData?.statisticData?.statName];

                    var stat = p.StatCollection.GetStatistic("effectXP");
                    p.StatCollection.Int_Add(stat, effectXP);
                    ModInit.modLog.LogMessage($"No existing effect {effectData.statisticData.statName} from {creator?.GetPilot()?.Description?.Callsign} targeting {target?.Description?.UIName}. Adding {effectXP} to {creator.GetPilot()?.Description?.Callsign}'s 'effectXP' pilot stat. No double-dipping until effect expires.");
                    return;
                }

                ////////////////////////////degraded double dipping
                var dummy = effectData.Description.Id;

                if (ModInit.Settings.degradingbonusEffects_XP.ContainsKey(effectData?.Description?.Id ?? "UNUSED"))
                {
                    int effectXP = ModInit.Settings.degradingbonusEffects_XP[effectData?.Description?.Id];

                    var degradationFactor = 1;
                    //use degFactor to apply sloppy seconds to XP bonus
                    if (reUseDegrades)
                    {
                        degradationFactor = new int[]
                        {
                            appliedEffect.Duration.numActivationsRemaining, 
                            appliedEffect.Duration.numMovementsRemaining, 
                            appliedEffect.Duration.numPhasesRemaining, 
                            appliedEffect.Duration.numRoundsRemaining
                        }.Max() + 1;
                    }

                    ModInit.modLog.LogMessage($"Matching effect found: {effectData.Description.Id} from {creator.GetPilot().Description.Callsign} targeting {target?.Description?.UIName}. Adding {effectXP}/{degradationFactor} = {effectXP / degradationFactor} to {creator.GetPilot().Description.Callsign}'s 'effectXP' pilot stat. Degraded double-dip: more XP closer to effect expiration.");
                        effectXP /= degradationFactor;

                        var stat = p.StatCollection.GetStatistic("effectXP");
                        p.StatCollection.Int_Add(stat, effectXP);
                    
                    return;
                }

                else if (effectData.effectType == EffectType.StatisticEffect && ModInit.Settings.degradingbonusEffects_XP.ContainsKey(effectData?.statisticData?.statName ?? "UNUSED"))

                {
                    int effectXP = ModInit.Settings.degradingbonusEffects_XP[effectData?.statisticData?.statName];

                    var degradationFactor = 1;
                    //use degFactor to apply sloppy seconds to XP bonus
                    if (reUseDegrades)
                    {
                        degradationFactor = new int[]
                        {
                            appliedEffect.Duration.numActivationsRemaining,
                            appliedEffect.Duration.numMovementsRemaining, 
                            appliedEffect.Duration.numPhasesRemaining,
                            appliedEffect.Duration.numRoundsRemaining
                        }.Max() + 1;
                    }

                    ModInit.modLog.LogMessage($"Matching effect found: {effectData.statisticData.statName} from {creator.GetPilot().Description.Callsign} targeting {target?.Description?.UIName}. Adding {effectXP}/{degradationFactor} = {effectXP / degradationFactor} to {creator.GetPilot().Description.Callsign}'s 'effectXP' pilot stat. Degraded double-dip: more XP closer to effect expiration.");
                    effectXP /= degradationFactor;

                    var stat = p.StatCollection.GetStatistic("effectXP");
                    p.StatCollection.Int_Add(stat, effectXP);

                    return;
                }

                ////////////////////////when i dip you dip we dip
                else
                {
                    if (ModInit.Settings.bonusEffects_XP.ContainsKey(effectData?.Description?.Id ?? "UNUSED"))
                    {
                        int effectXP = ModInit.Settings.bonusEffects_XP[effectData?.Description?.Id];
                        ModInit.modLog.LogMessage($"Adding {effectXP} to {creator.GetPilot().Description.Callsign}'s 'effectXP' pilot stat for applying {effectData.Description.Id} to {target?.Description?.UIName}. No restrictions on double-dipping.");

                        var stat = p.StatCollection.GetStatistic("effectXP");
                        p.StatCollection.Int_Add(stat, effectXP);
                        return;
                    }
                    else if (effectData.effectType == EffectType.StatisticEffect && ModInit.Settings.bonusEffects_XP.ContainsKey(effectData?.statisticData?.statName))

                    {
                        int effectXP = ModInit.Settings.bonusEffects_XP[effectData?.statisticData?.statName];
                        ModInit.modLog.LogMessage($"Adding {effectXP} to {creator.GetPilot().Description.Callsign}'s 'effectXP' pilot stat for applying {effectData.statisticData.statName} to {target?.Description?.UIName}. No restrictions on double-dipping.");
                        var stat = p.StatCollection.GetStatistic("effectXP");
                        p.StatCollection.Int_Add(stat, effectXP);
                        return;
                    }
                }
                return;
            }
        }

        [HarmonyPatch(typeof(ActiveProbeInvocation), "Invoke")]
        public static class ActiveProbeInvocation_Invoke_Patch
        {
            public static void Postfix(ActiveProbeInvocation __instance, CombatGameState combatGameState)
            {
                AbstractActor abstractActor = combatGameState.FindActorByGUID(__instance.SourceGUID);

                int modStat = 0;
                if (!ModInit.Settings.activeProbeXP_PerTarget)
                {
                    modStat = ModInit.Settings.activeProbeXP;
                    ModInit.modLog.LogMessage($"Adding {modStat} to {abstractActor.GetPilot().Description.Callsign}'s 'effectXP' pilot stat for Probing the Enemy, Actively.");
                }
                else
                {
                    modStat = ModInit.Settings.activeProbeXP * __instance.TargetGUIDs.Count;
                    ModInit.modLog.LogMessage($"Adding {modStat} to {abstractActor.GetPilot().Description.Callsign}'s 'effectXP' pilot stat for Probing {__instance.TargetGUIDs.Count} Enemies, Actively.");
                }
                var p = abstractActor.GetPilot();
                var stat = p.StatCollection.GetStatistic("effectXP");
                p.StatCollection.Int_Add(stat, modStat);
                return;
            }
        }


        [HarmonyPatch(typeof(SensorLockInvocation), "Invoke")]
        public static class SensorLockInvocation_Invoke_Patch
        {
            public static void Postfix(SensorLockInvocation __instance, CombatGameState combatGameState)
            {
                AbstractActor abstractActor = combatGameState.FindActorByGUID(__instance.SourceGUID);

                var p = abstractActor.GetPilot();
                var stat = p.StatCollection.GetStatistic("effectXP");
                p.StatCollection.Int_Add(stat, ModInit.Settings.sensorLockXP);
                ModInit.modLog.LogMessage($"Adding {ModInit.Settings.sensorLockXP} to {abstractActor.GetPilot().Description.Callsign}'s 'effectXP' pilot stat for Sensor Lock.");
                return;
            }
        }
    }
}
