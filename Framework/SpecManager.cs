using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using SVGImporter;
using BattleTech;
using Harmony;
using Localize;
using static PracticeMakesPerfect.Framework.GlobalVars;
using Newtonsoft.Json.Linq;
using UnityEngine.Experimental.Rendering;

namespace PracticeMakesPerfect.Framework
{
    static class PilotExtensions
    {
        internal static string FetchGUID(this Pilot pilot)
        {
            var guid = pilot.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith(spGUID));
            if (string.IsNullOrEmpty(guid))
            {
                ModInit.modLog.LogMessage($"WTF IS GUID NULL?!");
            }
            return guid;
        }
    }

    public class SpecManager
    {
        private static SpecManager _instance;
        public List<OpForSpec> OpForSpecList;
        public List<OpForSpec> OpForDefaultList;
        public List<MissionSpec> MissionSpecList;
        public List<MissionSpec> MissionDefaultList;

        public List<StratCom> StratComs;


        public static SpecManager ManagerInstance
        {
            get
            {
                if (_instance == null) _instance = new SpecManager();
                return _instance;
            }
        }

        internal void Initialize()
        {
            StratComs = new List<StratCom>();
            ModInit.modLog.LogMessage($"Initializing StratComs!");
            foreach (var StratComEffect in ModInit.modSettings.StratComs)
            {
                ModInit.modLog.LogMessage($"Adding effects for {StratComEffect.StratComName}!");
                foreach (var jObject in StratComEffect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    StratComEffect.effects.Add(effectData);
                }

                StratComs.Add(StratComEffect);
            }


            OpForSpecList = new List<OpForSpec>();
            ModInit.modLog.LogMessage($"Initializing OpForSpecs!");
            foreach (var OpForEffect in ModInit.modSettings.OpForSpecList)
            {
                ModInit.modLog.LogMessage($"Adding effects for {OpForEffect.OpForSpecName}!");
                foreach (var jObject in OpForEffect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    OpForEffect.effects.Add(effectData);
                }

                OpForSpecList.Add(OpForEffect);
            }

            OpForDefaultList = new List<OpForSpec>();
            foreach (var OpForDefault in ModInit.modSettings.OpForDefaultList)
            {
                ModInit.modLog.LogMessage($"Adding effects for default {OpForDefault.OpForSpecName}!");
                foreach (var jObject in OpForDefault.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    OpForDefault.effects.Add(effectData);
                }
                OpForDefaultList.Add(OpForDefault);
            }

            MissionSpecList = new List<MissionSpec>();
            ModInit.modLog.LogMessage($"Initializing MissionSpecs!");
            foreach (var MissionEffect in ModInit.modSettings.MissionSpecList)
            {
                ModInit.modLog.LogMessage($"Adding effects for {MissionEffect.MissionSpecName}!");
                foreach (var jObject in MissionEffect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    MissionEffect.effects.Add(effectData);
                }
                MissionSpecList.Add(MissionEffect);
            }

            MissionDefaultList = new List<MissionSpec>();
            foreach (var MissionDefault in ModInit.modSettings.MissionDefaultList)
            {
                ModInit.modLog.LogMessage($"Adding effects for default {MissionDefault.MissionSpecName}!");
                foreach (var jObject in MissionDefault.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    MissionDefault.effects.Add(effectData);
                }
                MissionDefaultList.Add(MissionDefault);
            }
        }

        internal void ProcessDefaults()
        {
            var factions = UnityGameInstance.BattleTechGame.DataManager.Factions;
            ModInit.modLog.LogMessage($"Initializing Default OpForSpecs!");
            foreach (var faction in factions)
            {
                if (ModInit.modSettings.WhiteListOpFor.Contains(faction.Value.FactionValue.Name))
                {
                    ModInit.modLog.LogMessage($"Creating default OpForSpecs for {faction.Value.FactionValue.Name}!");

                    foreach (var opforDefault in OpForDefaultList)
                    {
                        var op = new OpForSpec(opforDefault);

                        op.OpForSpecID = opforDefault.OpForSpecID.Insert(0, $"{faction.Value.FactionValue.Name}");

                        op.OpForSpecName = opforDefault.OpForSpecName.Insert(0, $"[{faction.Value.Name}]: ");

                        op.killsRequired = opforDefault.killsRequired;

                        op.applyToFaction = opforDefault.applyToFaction;

                        op.factionID = faction.Value.FactionValue.Name;

                        op.description =
                            opforDefault.description.Replace("{faction}", $"{faction.Value.Demonym} forces");

                        ModInit.modLog.LogMessage($"Adding {op.OpForSpecName} for {op.factionID}!");
                        OpForSpecList.Add(op);
                    }
                }
            }
            var contractTypes = ContractTypeEnumeration.ContractTypeValueList;
            ModInit.modLog.LogMessage($"Initializing Default MissionSpecs!");
            foreach (var contract in contractTypes)
            {
                if (ModInit.modSettings.WhiteListMissions.Contains(contract.Name))
                {
                    ModInit.modLog.LogMessage($"Creating default MissionSpecs for {contract.Name}!");
                    foreach (var defaultMissionSpec in ModInit.modSettings.MissionDefaultList)
                    {
                        var con = new MissionSpec(defaultMissionSpec);

                        con.MissionSpecID = defaultMissionSpec.MissionSpecID.Insert(0, $"{contract.Name}");

                        con.MissionSpecName = defaultMissionSpec.MissionSpecName.Insert(0, $"[{contract.Name}]: ");

                        con.missionsRequired = defaultMissionSpec.missionsRequired;

                        con.contractTypeID = contract.Name;

                        con.description =
                            defaultMissionSpec.description.Replace("{contract}", $"{contract.Name}");

                        ModInit.modLog.LogMessage($"Adding {con.MissionSpecName} for {con.MissionSpecID}!");
                        MissionSpecList.Add(con);
                    }
                }
            }
        }

        internal void PreloadIcons()
        {
            var dm = UnityGameInstance.BattleTechGame.DataManager;
            var loadRequest = dm.CreateLoadRequest();
            foreach (var op4Spec in SpecManager.ManagerInstance.OpForSpecList)
            {
                foreach (var effectData in op4Spec.effects)
                {
                    loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, effectData.Description.Icon,
                        null);
                }
            }

            foreach (var missionSpec in SpecManager.ManagerInstance.MissionSpecList)
            {
                foreach (var effectData in missionSpec.effects)
                {
                    loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, effectData.Description.Icon,
                        null);
                }
            }

            foreach (var missionSpec in SpecManager.ManagerInstance.StratComs)
            {
                foreach (var effectData in missionSpec.effects)
                {
                    loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, effectData.Description.Icon,
                        null);
                }
            }

            loadRequest.ProcessRequests();
        }

        internal void GatherPassiveOpforSpecs(AbstractActor actor, string opforID)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();
            foreach (var id in SpecHolder.HolderInstance.OpForSpecMap[pKey])
            {
                foreach (OpForSpec op4Spec in ManagerInstance.OpForSpecList.Where(x => x.OpForSpecID == id && x.factionID == opforID && !x.applyToFaction))
                {
                    this.ApplyPassiveOp4SpecEffects(actor, op4Spec);
                    ModInit.modLog.LogMessage($"Gathered {op4Spec.OpForSpecID} for {p.Description.Callsign}{pKey}");
                }
            }
        }

        internal void GatherPassiveMissionSpecs(AbstractActor actor, string missionID)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();
            foreach (var id in SpecHolder.HolderInstance.MissionSpecMap[pKey])
            {
                foreach (MissionSpec mSpec in ManagerInstance.MissionSpecList.Where(x => x.MissionSpecID == id && x.contractTypeID == missionID))
                {
                    this.ApplyPassiveMissionSpecEffects(actor, mSpec);
                    ModInit.modLog.LogMessage($"Gathered {mSpec.MissionSpecID} for {p.Description.Callsign}{pKey}");
                }
            }
        }

        protected void ApplyPassiveOp4SpecEffects(AbstractActor actor, OpForSpec op4Spec)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();
            ModInit.modLog.LogMessage(
                $"processing {op4Spec.effects.Count} op4Spec effects for {p.Description.Callsign}{pKey}");
            foreach (EffectData effectData in op4Spec.effects)
            {
                ModInit.modLog.LogMessage(
                    $"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                string id = ($"op4Spec_{p.Description.Callsign}_{effectData.Description.Id}");

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    ModInit.modLog.LogMessage($"Applying {id}");
                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, actor, default(WeaponHitInfo), 1,
                        false);
                }

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.AllAllies)
                {
                    foreach (var ally in new List<AbstractActor>(actor.Combat.GetAllAlliesOf(actor)))
                    {
                        ModInit.modLog.LogMessage(
                            $"Applying {id} to {ally.GetPilot().Callsign}, an ally of {p.Callsign}");
                        actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, ally, default(WeaponHitInfo),
                            1,
                            false);
                    }
                }

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.AllLanceMates)
                {
                    foreach (var lancemate in new List<AbstractActor>(actor.Combat.AllActors.FindAll((AbstractActor x) => x.team == actor.team)))
                    {
                        ModInit.modLog.LogMessage(
                            $"Applying {id} to {lancemate.GetPilot().Callsign}, a lancemate of {p.Callsign}");
                        actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, lancemate,
                            default(WeaponHitInfo), 1,
                            false);
                    }
                }

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                {
                    foreach (var enemy in new List<AbstractActor>(actor.Combat.GetAllEnemiesOf(actor)))
                    {
                        ModInit.modLog.LogMessage(
                            $"Applying {id} to {enemy.GetPilot().Callsign}, an enemy of {p.Callsign}");
                        actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, enemy,
                            default(WeaponHitInfo), 1,
                            false);
                    }
                }

            }
        }

        internal void ApplyStratComs(AbstractActor actor)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();
            foreach (StratCom stratcom in ManagerInstance.StratComs)
            {
                foreach (EffectData effectData in stratcom.effects)
                {
                    ModInit.modLog.LogMessage(
                        $"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                    string id = ($"stratcom{p.Description.Callsign}_{effectData.Description.Id}");

                    ModInit.modLog.LogMessage($"Applying {id}");
                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, actor,
                        default(WeaponHitInfo), 1,
                        false);
                }
            }
        }

        protected void ApplyPassiveMissionSpecEffects(AbstractActor actor, MissionSpec missionSpec)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();
            ModInit.modLog.LogMessage(
                $"processing {missionSpec.effects.Count} missionSpec effects for {p.Description.Callsign}{pKey}");
            foreach (EffectData effectData in missionSpec.effects)
            {
                ModInit.modLog.LogMessage(
                    $"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                string id = ($"missionSpec{p.Description.Callsign}_{effectData.Description.Id}");

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    ModInit.modLog.LogMessage($"Applying {id}");
                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, actor, default(WeaponHitInfo), 1,
                        false);
                }

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.AllAllies)
                {
                    foreach (var ally in new List<AbstractActor>(actor.Combat.GetAllAlliesOf(actor)))
                    {
                        ModInit.modLog.LogMessage(
                            $"Applying {id} to {ally.GetPilot().Callsign}, an ally of {p.Callsign}");
                        actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, ally, default(WeaponHitInfo),
                            1,
                            false);
                    }
                }

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.AllLanceMates)
                {
                    foreach (var lancemate in new List<AbstractActor>(actor.Combat.AllActors.FindAll((AbstractActor x) => x.team == actor.team)))
                    {
                        ModInit.modLog.LogMessage(
                            $"Applying {id} to {lancemate.GetPilot().Callsign}, a lancemate of {p.Callsign}");
                        actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, lancemate,
                            default(WeaponHitInfo), 1,
                            false);
                    }
                }

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                {
                    foreach (var enemy in new List<AbstractActor>(actor.Combat.GetAllEnemiesOf(actor)))
                    {
                        ModInit.modLog.LogMessage(
                            $"Applying {id} to {enemy.GetPilot().Callsign}, an enemy of {p.Callsign}");
                        actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, enemy,
                            default(WeaponHitInfo), 1,
                            false);
                    }
                }

            }
        }

        internal void GatherAndApplyActiveOp4SpecEffects(AbstractActor playerUnit, AbstractActor opforUnit)
        {
            var p = playerUnit.GetPilot();
            var pKey = p.FetchGUID();

            foreach (var OpForSpecID in SpecHolder.HolderInstance.OpForSpecMap[pKey])
            {
                foreach (OpForSpec op4Spec in ManagerInstance.OpForSpecList.Where(x =>
                    x.OpForSpecID == OpForSpecID &&
                    x.applyToFaction))
                {
                    foreach (EffectData effectData in op4Spec.effects)
                    {
                        ModInit.modLog.LogMessage(
                            $"Checking for existing effects: {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                        string id = ($"op4Spec_{p.Description.Callsign}_{effectData.Description.Id}");


                        ModInit.modLog.LogMessage($"stopping effects with id: {id}");
                        playerUnit.Combat.EffectManager.StopAllEffectsWithID(id);
                    }
                }

                foreach (OpForSpec op4Spec in ManagerInstance.OpForSpecList.Where(x => x.OpForSpecID == OpForSpecID && x.factionID == opforUnit.team.FactionValue.Name && x.applyToFaction))
                {
 //                   ModInit.modLog.LogMessage($"Gathered {op4Spec.OpForSpecID} for {p.Description.Callsign}{pKey}");

                    foreach (EffectData effectData in op4Spec.effects)
                    {
                        ModInit.modLog.LogMessage(
                            $"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                        string id = ($"op4Spec_{p.Description.Callsign}_{effectData.Description.Id}");


                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnWeaponFire &&
                            effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, playerUnit,
                                default(WeaponHitInfo), 1,
                                false);
                        }

                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnWeaponFire &&
                            effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, opforUnit,
                                default(WeaponHitInfo), 1,
                                false);
                        }
                    }
                }
            }
        }

        internal void GatherAndApplyPostSpawnEffects(AbstractActor actor)
        {
            var playerUnits = actor.Combat.AllActors.Where(x => x.team.IsLocalPlayer);
            var playerTeam = actor.Combat.LocalPlayerTeam;
            foreach (var playerunit in playerUnits)
            {
                var pKey = playerunit.GetPilot().FetchGUID();
                foreach (var opForSpecID in SpecHolder.HolderInstance.OpForSpecMap[pKey])
                {
                    foreach (OpForSpec op4Spec in ManagerInstance.OpForSpecList.Where(x => x.OpForSpecID == opForSpecID))
                    {
                        foreach (EffectData effectData in op4Spec.effects)
                        {
                            string id = ($"op4Spec_{playerunit.GetPilot().Description.Callsign}_{effectData.Description.Id}");

                            if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies && actor.team.IsEnemy(playerTeam)) ||
                                 effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                 actor.team.IsFriendly(playerTeam)))
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                actor.Combat.EffectManager.CreateEffect(effectData, id, -1, playerunit, actor,
                                    default(WeaponHitInfo), 1,
                                    false);
                            }
                        }
                    }
                }

                foreach (var missionSpecID in SpecHolder.HolderInstance.MissionSpecMap[pKey])
                {
                    foreach (MissionSpec missionSpec in ManagerInstance.MissionSpecList.Where(x => x.MissionSpecID == missionSpecID))
                    {
                        foreach (EffectData effectData in missionSpec.effects)
                        {
                            string id = ($"missionSpec_{playerunit.GetPilot().Description.Callsign}_{effectData.Description.Id}");

                            if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies && actor.team.IsEnemy(playerTeam)) ||
                                 effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                 actor.team.IsFriendly(playerTeam)))
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                actor.Combat.EffectManager.CreateEffect(effectData, id, -1, playerunit, actor,
                                    default(WeaponHitInfo), 1,
                                    false);
                            }
                        }
                    }
                }
            }
        }
    }
}