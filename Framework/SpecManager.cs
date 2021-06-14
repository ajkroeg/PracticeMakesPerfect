using System.Collections.Generic;
using System.Linq;
using SVGImporter;
using BattleTech;
using BattleTech.Designed;
using BattleTech.UI;
using Harmony;
using static PracticeMakesPerfect.Framework.GlobalVars;

namespace PracticeMakesPerfect.Framework
{
    internal static class PilotExtensions
    {
        internal static string FetchGUID(this Pilot pilot)
        {
            var guid = pilot.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith(spGUID));
            if (string.IsNullOrEmpty(guid))
            {
                ModInit.modLog.LogMessage($"WTF IS GUID NULL?!");
                return "NOTAPILOT";
            }
            return guid;
        }
    }

    public class SpecManager
    {
        private static SpecManager _instance;
        public List<OpForSpec> OpForSpecList;
        private List<OpForSpec> OpForDefaultList;
        public List<MissionSpec> MissionSpecList;
        private List<MissionSpec> MissionDefaultList;

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
                    ModInit.modLog.LogMessage($"Processing default OpForSpecs for {faction.Value.FactionValue.Name}!");

                    foreach (var opforDefault in OpForDefaultList)
                    {
                        if (OpForSpecList.Any(x => x.factionID == faction.Value.FactionValue.Name &&
                                                   x.killsRequired == opforDefault.killsRequired))
                        {
                            ModInit.modLog.LogMessage($"Default OpForSpecs for {faction.Value.FactionValue.Name} have same reqs as existing OpForSpec, aborting!");
                            continue;
                        }

                        var op = new OpForSpec(opforDefault)
                        {
                            OpForSpecID = opforDefault.OpForSpecID.Insert(0, $"{faction.Value.FactionValue.Name}"),
                            OpForSpecName = opforDefault.OpForSpecName.Insert(0, $"[{faction.Value.Name}]: "),
                            killsRequired = opforDefault.killsRequired,
                            applyToFaction = new List<string> {faction.Value.Name},
                            factionID = faction.Value.FactionValue.Name,
                            repMult = opforDefault.repMult,
                            storeDiscount = opforDefault.storeDiscount,
                            storeBonus = opforDefault.storeBonus,
                            cashMult = opforDefault.cashMult,
                            killBounty = opforDefault.killBounty,
                            description =
                                opforDefault.description.Replace("{faction}", $"{faction.Value.Demonym} forces"),
                            effects = opforDefault.effects
                        };

                        ModInit.modLog.LogMessage($"Adding {op.OpForSpecName} for {op.factionID}!\n 1st effectdata Id was {op.effects[0].Description.Id}");
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
                    ModInit.modLog.LogMessage($"Processing default MissionSpecs for {contract.Name}!");
                    foreach (var defaultMissionSpec in MissionDefaultList)
                    {
                        if (MissionSpecList.Any(x => x.contractTypeID == contract.Name &&
                                                   x.missionsRequired == defaultMissionSpec.missionsRequired))
                        {
                            ModInit.modLog.LogMessage($"Default MissionSpecs for {contract.Name} have same reqs as existing MissionSpec, aborting!");
                            continue;
                        }

                        var con = new MissionSpec(defaultMissionSpec)
                        {
                            MissionSpecID = defaultMissionSpec.MissionSpecID.Insert(0, $"{contract.Name}"),
                            MissionSpecName = defaultMissionSpec.MissionSpecName.Insert(0, $"[{contract.Name}]: "),
                            missionsRequired = defaultMissionSpec.missionsRequired,
                            AdvTargetInfoUnits = defaultMissionSpec.AdvTargetInfoUnits,
                            cashMult = defaultMissionSpec.cashMult,
                            contractTypeID = contract.Name,
                            description = defaultMissionSpec.description.Replace("{contract}", $"{contract.Name}"),
                            effects = defaultMissionSpec.effects
                        };

                        ModInit.modLog.LogMessage($"Adding {con.MissionSpecName} for {con.MissionSpecID}!\n 1st effectdata Id was {con.effects[0].Description.Id}");
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
                foreach (var op4Spec in ManagerInstance.OpForSpecList.Where(x => x.OpForSpecID == id && (x.factionID == opforID || x.applyToFaction.Contains(opforID))))
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
                foreach (var mSpec in ManagerInstance.MissionSpecList.Where(x => x.MissionSpecID == id && x.contractTypeID == missionID))
                {
                    this.ApplyPassiveMissionSpecEffects(actor, mSpec);
                    ModInit.modLog.LogMessage($"Gathered {mSpec.MissionSpecID} for {p.Description.Callsign}{pKey}");
                }
            }
        }

        private void ApplyPassiveOp4SpecEffects(AbstractActor actor, OpForSpec op4Spec)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();
            ModInit.modLog.LogMessage(
                $"processing {op4Spec.effects.Count} op4Spec effects for {p.Description.Callsign}{pKey}");
            foreach (var effectData in op4Spec.effects)
            {
                ModInit.modLog.LogMessage(
                    $"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                var id = ($"op4Spec_{p.Description.Callsign}_{effectData.Description.Id}");

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    ModInit.modLog.LogMessage($"Applying {id}");
                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, actor, default(WeaponHitInfo), 1);
                }

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.AllAllies)
                {
                    foreach (var ally in new List<AbstractActor>(actor.Combat.GetAllAlliesOf(actor)))
                    {
                        ModInit.modLog.LogMessage(
                            $"Applying {id} to {ally.GetPilot().Callsign}, an ally of {p.Callsign}");
                        actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, ally, default(WeaponHitInfo),
                            1);
                    }
                }

                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.AllLanceMates)
                {
                    foreach (var lancemate in new List<AbstractActor>(actor.Combat.AllActors.FindAll(x => x.team == actor.team && x != actor)))
                    {
                        ModInit.modLog.LogMessage(
                            $"Applying {id} to {lancemate.GetPilot().Callsign}, a lancemate of {p.Callsign}");
                        actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, lancemate,
                            default(WeaponHitInfo), 1);
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
                            default(WeaponHitInfo), 1);
                    }
                }
            }
        }

        internal static void ApplyStratComs(AbstractActor actor)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();

            foreach (var stratcom in ManagerInstance.StratComs.Where(x=>x.StratComID == SpecHolder.HolderInstance.activeStratCom))
            {
                foreach (var effectData in stratcom.effects)
                {
                    ModInit.modLog.LogMessage(
                        $"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                    var id = ($"stratcom{p.Description.Callsign}_{effectData.Description.Id}");

                    ModInit.modLog.LogMessage($"Applying {id}");
                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, actor,
                        default(WeaponHitInfo), 1);
                }
            }
        }

        internal void SetStratCom(string stratcom, Pilot pilot, SGBarracksDossierPanel dossier, SGBarracksMWDetailPanel details)
        {
            SpecHolder.HolderInstance.activeStratCom = stratcom;
            ModInit.modLog.LogMessage(
                $"Active StratCom changed to: {stratcom}");
            dossier.SetPilot(pilot, details, pilot.GUID != sim.Commander.GUID, false);
        }

        internal void ResetMissionSpecs(Pilot pilot, SGBarracksDossierPanel dossier, SGBarracksMWDetailPanel details)
        {
            var pKey = pilot.FetchGUID();
            SpecHolder.HolderInstance.MissionSpecMap[pKey] = new List<string>();
            SpecHolder.HolderInstance.MissionsTracker[pKey] = new Dictionary<string, int>();
            ModInit.modLog.LogMessage(
                $"Clearing Mission specializations and progress for {pilot.Description.Callsign}");

            dossier.SetPilot(pilot, details, pilot.GUID != sim.Commander.GUID, false);
            
        }

        internal void ResetOpForSpecs(Pilot pilot, SGBarracksDossierPanel dossier, SGBarracksMWDetailPanel details)
        {
            var pKey = pilot.FetchGUID();
            SpecHolder.HolderInstance.OpForSpecMap[pKey] = new List<string>();
            SpecHolder.HolderInstance.OpForKillsTracker[pKey] = new Dictionary<string, int>();
            ModInit.modLog.LogMessage(
                $"Clearing Opfor specializations and progress for {pilot.Description.Callsign}");

            dossier.SetPilot(pilot, details, pilot.GUID != sim.Commander.GUID, false);
            
        }


        private void ApplyPassiveMissionSpecEffects(AbstractActor actor, MissionSpec missionSpec)
        {
            var p = actor.GetPilot();
            var pKey = p.FetchGUID();
            var playerTeam = actor.Combat.LocalPlayerTeam;
            ModInit.modLog.LogMessage(
                $"processing {missionSpec.effects.Count} missionSpec effects for {p.Description.Callsign}{pKey}");
            foreach (var effectData in missionSpec.effects)
            {
                var id = ($"missionSpec{p.Description.Callsign}_{effectData.Description.Id}");
                if (effectData.targetingData.effectTriggerType != EffectTriggerType.Passive)
                {
                    ModInit.modLog.LogMessage(
                        $"effectData for {id} targeting not passive, skipping to next effectData");
                    continue;
                }
                ModInit.modLog.LogMessage(
                    $"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");


                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    ModInit.modLog.LogMessage($"Applying {id}");
                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, actor, default(WeaponHitInfo), 1);
                }

                if (missionSpec.AdvTargetInfoUnits.Count < 1)
                {


                    if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                        effectData.targetingData.effectTargetType == EffectTargetType.AllAllies)
                    {
                        foreach (var ally in new List<AbstractActor>(actor.Combat.GetAllAlliesOf(actor)))
                        {
                            ModInit.modLog.LogMessage(
                                $"Applying {id} to {ally.GetPilot().Callsign}, an ally of {p.Callsign}");
                            actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, ally, default(WeaponHitInfo),
                                1);
                        }
                    }

                    if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                        effectData.targetingData.effectTargetType == EffectTargetType.AllLanceMates)
                    {
                        foreach (var lancemate in new List<AbstractActor>(actor.Combat.AllActors.FindAll(x => x.team == actor.team)))
                        {
                            ModInit.modLog.LogMessage(
                                $"Applying {id} to {lancemate.GetPilot().Callsign}, a lancemate of {p.Callsign}");
                            actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, lancemate,
                                default(WeaponHitInfo), 1);
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
                                default(WeaponHitInfo), 1);
                        }
                    }
                    continue;
                }

                if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Mech))
                {
                    foreach (var mech in actor.Combat.AllActors.Where(x => (x.UnitType & UnitType.Mech) != 0))
                    {
                        id =
                            ($"missionSpec_{mech.GetPilot().Description.Callsign}_{effectData.Description.Id}"
                            );

                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                            ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies &&
                              actor.team.IsEnemy(playerTeam)) ||
                             effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                             actor.team.IsFriendly(playerTeam)))
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, mech,
                                default(WeaponHitInfo), 1);
                        }
                    }
                }

                if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Vehicle))
                {
                    foreach (var vehicle in actor.Combat.AllActors.Where(x=>(x.UnitType & UnitType.Vehicle) != 0))
                    {
                        id =
                            ($"missionSpec_{vehicle.GetPilot().Description.Callsign}_{effectData.Description.Id}"
                            );

                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                            ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies &&
                              actor.team.IsEnemy(playerTeam)) ||
                             effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                             actor.team.IsFriendly(playerTeam)))
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, vehicle,
                                default(WeaponHitInfo), 1);
                        }
                    }
                }

                if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Turret))
                {
                    foreach (var turret in actor.Combat.AllActors.Where(x=>(x.UnitType & UnitType.Turret) != 0))
                    {
                        id =
                            ($"missionSpec_{turret.GetPilot().Description.Callsign}_{effectData.Description.Id}"
                            );

                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                            ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies &&
                              actor.team.IsEnemy(playerTeam)) ||
                             effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                             actor.team.IsFriendly(playerTeam)))
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, turret,
                                default(WeaponHitInfo), 1);
                        }
                    }
                }

                if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Building))
                {
                    foreach (var building in actor.Combat.GetAllCombatants().Where(x => (x.UnitType & UnitType.Building) != 0))
                    {
                        id =
                            ($"missionSpec_{building?.DisplayName}_{effectData.Description.Id}"
                            );

                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                            ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies &&
                              actor.team.IsEnemy(playerTeam)) ||
                             effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                             actor.team.IsFriendly(playerTeam)))
                        {
                            
                            if (!building.StatCollection.ContainsStatistic("DamageReductionMultiplierAll"))
                            {
                                building.StatCollection.AddStatistic<float>("DamageReductionMultiplierAll", 1f);
                                ModInit.modLog.LogMessage($"Adding dmg modifier stat to building if missing so it verks");
                            }
                            

                            ModInit.modLog.LogMessage($"Applying {id}");
                            actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, building,
                                default(WeaponHitInfo), 1);
                        }
                    }
                }

                if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Primary))
                {

                    foreach (var unit in actor.Combat.AllActors)
                    {
                        id =
                            ($"missionSpec_{unit.GetPilot().Description.Callsign}_{effectData.Description.Id}"
                            );

                        foreach (var encounterObjectGameLogic in actor.Combat.EncounterLayerData
                            .encounterObjectGameLogicList)
                        {
                            if (encounterObjectGameLogic as DefendLanceWithEscapeChunkGameLogic != null)
                            {
                                var encounterAsChunk = encounterObjectGameLogic as DefendLanceWithEscapeChunkGameLogic;
                                var encounterAsOGL = encounterAsChunk.ensureUnitsSurviveObjective.encounterObject;
                                if (Traverse.Create(encounterAsOGL).Property("IsContractObjectivePrimary")
                                    .GetValue<bool>())
                                {
                                    ModInit.modLog.LogMessage($"Checking for primary target unit.");
                                    if (encounterAsOGL.GetTargetUnits().Contains(unit))
                                    {
                                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                            ((effectData.targetingData.effectTargetType ==
                                              EffectTargetType.AllEnemies &&
                                              actor.team.IsEnemy(playerTeam)) ||
                                             effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                             actor.team.IsFriendly(playerTeam)))
                                        {
                                            ModInit.modLog.LogMessage($"Applying {id}");
                                            actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, unit,
                                                default(WeaponHitInfo), 1);
                                        }

                                        return;
                                    }
                                }
                            }
                            else if (encounterObjectGameLogic as DefendXUnitsChunkGameLogic != null)
                            {
                                var encounterAsChunk = encounterObjectGameLogic as DefendXUnitsChunkGameLogic;
                                var encounterAsOGL = encounterAsChunk.defendXUnitsObjective.encounterObject;
                                if (Traverse.Create(encounterAsOGL).Property("IsContractObjectivePrimary")
                                    .GetValue<bool>())
                                {
                                    ModInit.modLog.LogMessage($"Checking for primary target unit.");
                                    if (encounterAsOGL.GetTargetUnits().Contains(unit))
                                    {
                                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                            ((effectData.targetingData.effectTargetType ==
                                              EffectTargetType.AllEnemies &&
                                              actor.team.IsEnemy(playerTeam)) ||
                                             effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                             actor.team.IsFriendly(playerTeam)))
                                        {
                                            ModInit.modLog.LogMessage($"Applying {id}");
                                            actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, unit,
                                                default(WeaponHitInfo), 1);
                                        }

                                        return;
                                    }
                                }
                            }
                            else if (encounterObjectGameLogic as DestroyXUnitsChunkGameLogic != null)
                            {
                                var encounterAsChunk = encounterObjectGameLogic as DestroyXUnitsChunkGameLogic;
                                var encounterAsOGL = encounterAsChunk.destroyXUnitsObjective.encounterObject;
                                if (Traverse.Create(encounterAsOGL).Property("IsContractObjectivePrimary")
                                    .GetValue<bool>())
                                {
                                    ModInit.modLog.LogMessage($"Checking for primary target unit.");
                                    if (encounterAsOGL.GetTargetUnits().Contains(unit))
                                    {
                                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                            ((effectData.targetingData.effectTargetType ==
                                              EffectTargetType.AllEnemies &&
                                              actor.team.IsEnemy(playerTeam)) ||
                                             effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                             actor.team.IsFriendly(playerTeam)))
                                        {
                                            ModInit.modLog.LogMessage($"Applying {id}");
                                            actor.Combat.EffectManager.CreateEffect(effectData, id, -1, actor, unit,
                                                default(WeaponHitInfo), 1);
                                        }

                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        internal void GatherAndApplyActivePrimarySpecEffects(AbstractActor playerUnit, ICombatant target)
        {
            var p = playerUnit.GetPilot();
            var pKey = p.FetchGUID();

            foreach (var missionSpecID in SpecHolder.HolderInstance.MissionSpecMap[pKey])
            {
                foreach (var missionSpec in ManagerInstance.MissionSpecList.Where(x =>
                    x.MissionSpecID == missionSpecID && x.contractTypeID == playerUnit.Combat.ActiveContract.ContractTypeValue.Name))
                {
                    foreach (var effectData in missionSpec.effects)
                    {
                        var id = ($"missionSpec_{p.Description.Callsign}_{effectData.Description.Id}");

                        if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Primary) && effectData.targetingData.effectTriggerType ==
                            EffectTriggerType.OnWeaponFire &&
                            effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, playerUnit,
                                default(WeaponHitInfo), 1);
                        }

                        if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Primary) && effectData.targetingData.effectTriggerType ==
                            EffectTriggerType.OnWeaponFire &&
                            effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, target,
                                default(WeaponHitInfo), 1);
                        }
                    }
                }
            }

        }

        internal void GatherAndApplyGlobalEffects(AbstractActor unit, ICombatant target)
        {
            var p = unit.GetPilot();

            foreach (var playerUnit in unit.Combat.LocalPlayerTeam.units)
            {
                ModInit.modLog.LogMessage($"Getting key for {playerUnit.GetPilot().Callsign}.");
                var pKey = playerUnit.GetPilot().FetchGUID();

                if (SpecHolder.HolderInstance.OpForSpecMap[pKey].Count > 0)
                {
                    foreach (var OpForSpecID in SpecHolder.HolderInstance.OpForSpecMap[pKey])
                    {
                        foreach (var op4Spec in ManagerInstance.OpForSpecList.Where(x =>
                            x.OpForSpecID == OpForSpecID && (x.factionID == target.team.FactionValue.Name ||
                                                             x.applyToFaction.Contains(unit.team.FactionValue.Name))))
                        {

                            foreach (var effectData in op4Spec.effects)
                            {
                                ModInit.modLog.LogMessage(
                                    $"processing {effectData.Description.Name} for {p.Description.Callsign}");

                                var id = ($"op4Spec_{p.Description.Callsign}_{effectData.Description.Id}");

                                if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnWeaponFire &&
                                    effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                                {
                                    ModInit.modLog.LogMessage($"Applying {id}");
                                    unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                        default(WeaponHitInfo), 1);
                                }
                            }
                        }
                    }
                }

                foreach (var missionSpecID in SpecHolder.HolderInstance.MissionSpecMap[pKey])
                {
                    foreach (var missionSpec in ManagerInstance.MissionSpecList.Where(x =>
                        x.MissionSpecID == missionSpecID && x.contractTypeID == unit.Combat.ActiveContract.ContractTypeValue.Name))
                    {
                        foreach (var effectData in missionSpec.effects)
                        {
                            var id = ($"missionSpec_{p.Description.Callsign}_{effectData.Description.Id}");
                            if (missionSpec.AdvTargetInfoUnits.Count < 1)
                            {

                                if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnWeaponFire &&
                                    effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                                {
                                    ModInit.modLog.LogMessage($"Applying {id}");
                                    unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                        default(WeaponHitInfo), 1);
                                    return;
                                }
                            }

                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Mech) &&
                                (target.UnitType & UnitType.Mech) != 0 &&
                                effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                    default(WeaponHitInfo), 1);
                                return;
                            }


                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Vehicle) &&
                                (target.UnitType & UnitType.Vehicle) != 0 &&
                                effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                    default(WeaponHitInfo), 1);
                                return;
                            }

                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Turret) &&
                                (target.UnitType & UnitType.Turret) != 0 &&
                                effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                    default(WeaponHitInfo), 1);
                                return;
                            }

                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Building) &&
                                (target.UnitType & UnitType.Building) != 0 &&
                                effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                    default(WeaponHitInfo), 1);
                                return;
                            }

                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.NotPlayer) &&
                                !target.team.IsLocalPlayer &&
                                effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}. This should only happen on a threeway.");
                                unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                    default(WeaponHitInfo), 1);
                                return;
                            }

                        }
                    }
                }
            }
        }


        internal void GatherAndApplyActiveBuildingSpecEffects(AbstractActor unit, ICombatant building)
        {
            var p = unit.GetPilot();
            var pKey = p.FetchGUID();

            foreach (var OpForSpecID in SpecHolder.HolderInstance.OpForSpecMap[pKey])
            {
                foreach (var op4Spec in ManagerInstance.OpForSpecList.Where(x => x.OpForSpecID == OpForSpecID && (x.factionID == building.team.FactionValue.Name || x.applyToFaction.Contains(building.team.FactionValue.Name))))
                {
                    //                   ModInit.modLog.LogMessage($"Gathered {op4Spec.OpForSpecID} for {p.Description.Callsign}{pKey}");

                    foreach (var effectData in op4Spec.effects)
                    {
                        ModInit.modLog.LogMessage(
                            $"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                        var id = ($"op4Spec_{p.Description.Callsign}_{effectData.Description.Id}");


                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnWeaponFire &&
                            effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                default(WeaponHitInfo), 1);
                        }

                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnWeaponFire &&
                            effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                default(WeaponHitInfo), 1);
                        }
                    }
                }
            }

            foreach (var missionSpecID in SpecHolder.HolderInstance.MissionSpecMap[pKey])
            {
                foreach (var missionSpec in ManagerInstance.MissionSpecList.Where(x =>
                    x.MissionSpecID == missionSpecID && x.contractTypeID == unit.Combat.ActiveContract.ContractTypeValue.Name))
                {
                    foreach (var effectData in missionSpec.effects)
                    {
                        var id = ($"missionSpec_{p.Description.Callsign}_{effectData.Description.Id}");

                        if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Building) &&
                            (building.UnitType & UnitType.Building) != 0 && effectData.targetingData.effectTriggerType ==
                            EffectTriggerType.OnWeaponFire &&
                            effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                default(WeaponHitInfo), 1);
                        }

                        if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Building) &&
                            (building.UnitType & UnitType.Building) != 0 && effectData.targetingData.effectTriggerType ==
                            EffectTriggerType.OnWeaponFire &&
                            effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit,
                                default(WeaponHitInfo), 1);
                        }
                    }
                }
            }

        }

        internal void GatherAndApplyActiveSpecEffects(AbstractActor playerUnit, AbstractActor opforUnit) // need to figure out how to target primaries only here and also make opfor teams inflict +++ damage to eachother only
        {
            var p = playerUnit.GetPilot();
            var pKey = p.FetchGUID();

            foreach (var OpForSpecID in SpecHolder.HolderInstance.OpForSpecMap[pKey])
            {
                foreach (var op4Spec in ManagerInstance.OpForSpecList.Where(x => x.OpForSpecID == OpForSpecID && (x.factionID == opforUnit.team.FactionValue.Name || x.applyToFaction.Contains(opforUnit.team.FactionValue.Name))))
                {
 //                   ModInit.modLog.LogMessage($"Gathered {op4Spec.OpForSpecID} for {p.Description.Callsign}{pKey}");

                    foreach (var effectData in op4Spec.effects)
                    {
                        ModInit.modLog.LogMessage(
                            $"processing {effectData.Description.Name} for {p.Description.Callsign}{pKey}");

                        var id = ($"op4Spec_{p.Description.Callsign}_{effectData.Description.Id}");


                        if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnWeaponFire &&
                            effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                        {
                            ModInit.modLog.LogMessage($"Applying {id}");
                            playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, playerUnit,
                                default(WeaponHitInfo), 1);
                        }
                    }
                }
            }

            foreach (var missionSpecID in SpecHolder.HolderInstance.MissionSpecMap[pKey])
            {

                    foreach (var missionSpec in ManagerInstance.MissionSpecList.Where(x => x.MissionSpecID == missionSpecID && x.contractTypeID == playerUnit.Combat.ActiveContract.ContractTypeValue.Name))
                    {
                        foreach (var effectData in missionSpec.effects)
                        {
                            var id = ($"missionSpec_{p.Description.Callsign}_{effectData.Description.Id}");
                            if (missionSpec.AdvTargetInfoUnits.Count < 1)
                            {
                                if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnWeaponFire &&
                                    effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                                {
                                    ModInit.modLog.LogMessage($"Applying {id}");
                                    playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, playerUnit,
                                        default(WeaponHitInfo), 1);
                                }

                                if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnWeaponFire &&
                                    effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                                {
                                    ModInit.modLog.LogMessage($"Applying {id}");
                                    playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, opforUnit,
                                        default(WeaponHitInfo), 1);
                                }

                                continue;
                            }


                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Mech) &&
                                (playerUnit.UnitType & UnitType.Mech) != 0 && effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, playerUnit,
                                    default(WeaponHitInfo), 1);
                            }

                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Mech) &&
                                (opforUnit.UnitType & UnitType.Mech) != 0 && effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, opforUnit,
                                    default(WeaponHitInfo), 1);
                            }

                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Vehicle) &&
                                (playerUnit.UnitType & UnitType.Vehicle) != 0 && effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, playerUnit,
                                    default(WeaponHitInfo), 1);
                            }

                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Vehicle) &&
                                (opforUnit.UnitType & UnitType.Vehicle) != 0 && effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, opforUnit,
                                    default(WeaponHitInfo), 1);
                            }

                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Turret) &&
                                (playerUnit.UnitType & UnitType.Turret) != 0 && effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, playerUnit,
                                    default(WeaponHitInfo), 1);
                            }

                            if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Turret) &&
                                (opforUnit.UnitType & UnitType.Turret) != 0 && effectData.targetingData.effectTriggerType ==
                                EffectTriggerType.OnWeaponFire &&
                                effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies)
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                playerUnit.Combat.EffectManager.CreateEffect(effectData, id, -1, playerUnit, opforUnit,
                                    default(WeaponHitInfo), 1);
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
                if (pKey == "NOTAPILOT") continue;

                ModInit.modLog.LogMessage($"Checking {playerunit.GetPilot().Callsign} for effects to apply");
                foreach (var opForSpecID in SpecHolder.HolderInstance.OpForSpecMap[pKey])
                {
                    ModInit.modLog.LogMessage($"Found {opForSpecID}");
                    foreach (var op4Spec in ManagerInstance.OpForSpecList.Where(x => x.OpForSpecID == opForSpecID))
                    {
                        foreach (var effectData in op4Spec.effects)
                        {
                            var id = ($"op4Spec_{playerunit.GetPilot().Description.Callsign}_{effectData.Description.Id}");

                            if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies && actor.team.IsEnemy(playerTeam)) ||
                                 effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                 actor.team.IsFriendly(playerTeam)))
                            {
                                ModInit.modLog.LogMessage($"Applying {id}");
                                actor.Combat.EffectManager.CreateEffect(effectData, id, -1, playerunit, actor,
                                    default(WeaponHitInfo), 1);
                            }
                        }
                    }
                }

                foreach (var missionSpecID in SpecHolder.HolderInstance.MissionSpecMap[pKey])
                {
                    ModInit.modLog.LogMessage($"Found {missionSpecID}");
                    foreach (var missionSpec in ManagerInstance.MissionSpecList.Where(x => x.MissionSpecID == missionSpecID && playerunit.Combat.ActiveContract.ContractTypeValue.Name == x.contractTypeID))
                    {
                        if (missionSpec.AdvTargetInfoUnits.Count < 1)
                        {
                            foreach (var effectData in missionSpec.effects)
                            {
                                var id = ($"missionSpec_{playerunit.GetPilot().Description.Callsign}_{effectData.Description.Id}");

                                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                    ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies && actor.team.IsEnemy(playerTeam)) ||
                                     effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                     actor.team.IsFriendly(playerTeam)))
                                {
                                    ModInit.modLog.LogMessage($"Applying {id}");
                                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, playerunit, actor,
                                        default(WeaponHitInfo), 1);
                                }
                            }
                            continue;
                        }

                        if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Mech) &&
                                 (actor.UnitType & UnitType.Mech) != 0)
                        {
                            foreach (var effectData in missionSpec.effects)
                            {
                                var id =
                                    ($"missionSpec_{playerunit.GetPilot().Description.Callsign}_{effectData.Description.Id}"
                                    );

                                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                    ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies &&
                                      actor.team.IsEnemy(playerTeam)) ||
                                     effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                     actor.team.IsFriendly(playerTeam)))
                                {
                                    ModInit.modLog.LogMessage($"Applying {id}");
                                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, playerunit, actor,
                                        default(WeaponHitInfo), 1);
                                }
                            }
                        }

                        if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Vehicle) && (actor.UnitType & UnitType.Vehicle) != 0)
                        {
                            foreach (var effectData in missionSpec.effects)
                            {
                                var id =
                                    ($"missionSpec_{playerunit.GetPilot().Description.Callsign}_{effectData.Description.Id}"
                                    );

                                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                    ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies &&
                                      actor.team.IsEnemy(playerTeam)) ||
                                     effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                     actor.team.IsFriendly(playerTeam)))
                                {
                                    ModInit.modLog.LogMessage($"Applying {id}");
                                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, playerunit, actor,
                                        default(WeaponHitInfo), 1);
                                }
                            }
                        }
                        if (missionSpec.AdvTargetInfoUnits.Contains(AdvTargetUnitData.Turret) && (actor.UnitType & UnitType.Turret) != 0)
                        {
                            foreach (var effectData in missionSpec.effects)
                            {
                                var id =
                                    ($"missionSpec_{playerunit.GetPilot().Description.Callsign}_{effectData.Description.Id}"
                                    );

                                if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                    ((effectData.targetingData.effectTargetType == EffectTargetType.AllEnemies &&
                                      actor.team.IsEnemy(playerTeam)) ||
                                     effectData.targetingData.effectTargetType == EffectTargetType.AllAllies &&
                                     actor.team.IsFriendly(playerTeam)))
                                {
                                    ModInit.modLog.LogMessage($"Applying {id}");
                                    actor.Combat.EffectManager.CreateEffect(effectData, id, -1, playerunit, actor,
                                        default(WeaponHitInfo), 1);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}