﻿using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using InControl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SVGImporter.LibTessDotNet;
using static PracticeMakesPerfect.Framework.GlobalVars;

namespace PracticeMakesPerfect.Framework
{
    public static class GlobalVars
    {
        public static SimGameState sim;
        public const string spGUID = "spGUID_";
        public const string aiPilotFlag = "_AI_TEMP_PMP_";
        public const string OP4SpecStateTag = "PMP_OP4SPEC_";
        public const string OP4SpecTrackerTag = "PMP_OP4TRACKER_";
        public const string MissionSpecStateTag = "PMP_MISSIONSPEC_";
        public const string MissionSpecTrackerTag = "PMP_MISSIONTRACKER_";
        public const string ActiveStratComTag = "PMP_ACTIVESTRATCOM_";

        public const string owner_string = "{OWNER}";
        public const string employer_string = "{EMPLOYER}";
        public const string target_string = "{TARGET}";

        public enum AdvTargetUnitData

        {
            NotSet,
            Primary,
            NotPlayer,
            Mech,
            Vehicle,
            Turret,
            Building
        }
    }
    public class OpForSpec
    {
        public string OpForSpecID = "";
        public string OpForSpecName = "";
        public int killsRequired = 0;
        public string factionID = ""; //e.g. TaurianConcordat
        public List<string> applyToFaction = new List<string>();
        public string description = "";

        public Dictionary<string, int> repMod = new Dictionary<string, int>();
        public Dictionary<string, float> storeDiscount = new Dictionary<string, float>();
        public Dictionary<string, float> storeBonus = new Dictionary<string, float>();
        public Dictionary<string, float> cashMult = new Dictionary<string, float>();
        public Dictionary<string, int> killBounty = new Dictionary<string, int>();

        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectDataJO = new List<JObject>();
        public OpForSpec opforDefault;

        public OpForSpec(OpForSpec opforDefault)
        {
            this.opforDefault = opforDefault;
        }
    }
    public class StratCom
    {
        public string StratComID = "";
        public string StratComName = "";
//        public int StratReq = 1;
        public string description = "";

        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectDataJO = new List<JObject>();
    }

    public class MissionSpec
    {
        public string MissionSpecID = "";
        public string MissionSpecName = "";
        public int missionsRequired = 0;
        public string contractTypeID = ""; //eg SimpleBattle

        public List<AdvTargetUnitData> AdvTargetInfoUnits = new List<AdvTargetUnitData>(); // Primary, NotPlayer, Mech, Vehicle, Turret, Building
        public float cashMult = 0f; // need to implement still

        public string description = "";

        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectDataJO = new List<JObject>();
        public MissionSpec defaultMissionSpec;

        public MissionSpec(MissionSpec defaultMissionSpec)
        {
            this.defaultMissionSpec = defaultMissionSpec;
        }
    }


    public class SpecHolder
    {
        public static SpecHolder _instance;

        public Dictionary<string, List<string>> OpForSpecMap;
        public Dictionary<string, List<string>> MissionSpecMap;

        public Dictionary<string, Dictionary<string, int>> OpForKillsTracker;
        public Dictionary<string, Dictionary<string, int>> MissionsTracker;

        public Dictionary<string, Dictionary<string, int>> OpForKillsTEMPTracker;
        public Dictionary<string, Dictionary<string, int>> OpForKillsTEMPTrackerFlattened;

        public Dictionary<string, string> SubfactionsMap = new Dictionary<string, string>();// key is "subfaction", value is "parent";

        public string activeStratCom;

        public int kills;
        public int bounty;
        public int totalBounty;
        public int emplRep;

        public static SpecHolder HolderInstance
        {
            get
            {
                if (_instance == null) _instance = new SpecHolder();
                return _instance;
            }
        }

        public void Initialize()
        {
            OpForSpecMap = new Dictionary<string, List<string>>();
            MissionSpecMap = new Dictionary<string, List<string>>();

            OpForKillsTracker = new Dictionary<string, Dictionary<string, int>>();
            MissionsTracker = new Dictionary<string, Dictionary<string, int>>();

            OpForKillsTEMPTracker = new Dictionary<string, Dictionary<string, int>>();
            OpForKillsTEMPTrackerFlattened = new Dictionary<string, Dictionary<string, int>>();

            SubfactionsMap = new Dictionary<string, string>();

            activeStratCom = null;

            kills = new int();
            bounty = new int();
            totalBounty = new int();
            emplRep = new int();

            ModInit.modLog.LogMessage($"Processing subfactions map!");
            foreach (var key in ModInit.modSettings.OpforSubfactionsMap)
            {
                foreach (var subfaction in key.Value)
                {
                    if (!SubfactionsMap.ContainsKey(subfaction))
                    {
                        SubfactionsMap.Add(subfaction, key.Key);
                        ModInit.modLog.LogMessage($"Mapped subfaction {subfaction} to parent {key.Key}!");
                    }
                }
            }
        }

        public void AddToMaps(Pilot pilot)
        {
            if (!pilot.pilotDef.PilotTags.Any(x => x.StartsWith(spGUID)))
            {
                pilot.pilotDef.PilotTags.Add($"{spGUID}{pilot.Description.Id}{Guid.NewGuid()}");
            }

            var pKey = pilot.FetchGUID();
            if (!SpecHolder.HolderInstance.MissionSpecMap.ContainsKey(pKey))
            {
                SpecHolder.HolderInstance.MissionSpecMap.Add(pKey, new List<string>());
                ModInit.modLog.LogMessage($"Added {pilot.Callsign} to MissionSpecMap with iGUID {pKey}");
            }
            if (!SpecHolder.HolderInstance.OpForSpecMap.ContainsKey(pKey))
            {
                SpecHolder.HolderInstance.OpForSpecMap.Add(pKey, new List<string>());
                ModInit.modLog.LogMessage($"Added {pilot.Callsign} to OpForSpecMap with iGUID {pKey}");
            }

            if (!SpecHolder.HolderInstance.MissionsTracker.ContainsKey(pKey))
            {
                SpecHolder.HolderInstance.MissionsTracker.Add(pKey, new Dictionary<string, int>());
                ModInit.modLog.LogMessage($"Added {pilot.Callsign} to MissionsTracker with iGUID {pKey}");
            }
            if (!SpecHolder.HolderInstance.OpForKillsTracker.ContainsKey(pKey))
            {
                SpecHolder.HolderInstance.OpForKillsTracker.Add(pKey, new Dictionary<string, int>());
                ModInit.modLog.LogMessage($"Added {pilot.Callsign} to OpForKillsTracker with iGUID {pKey}");
            }
        }

        public void CleanMaps(List<string> currentPilots)
        {
            foreach (var pKey in currentPilots)
            {
                foreach (var id in new List<string>(SpecHolder.HolderInstance.MissionSpecMap[pKey]))//.Where(x=> ModInit.modSettings.WhiteListMissions.Any(y=> x == y)))
                {
                    if (SpecManager.ManagerInstance.MissionSpecList.All(x => x.MissionSpecID != id))
                    {
                        SpecHolder.HolderInstance.MissionSpecMap[pKey].Remove(id);
                        ModInit.modLog.LogMessage($"Removed deprecated MissionSpec from {pKey} with id {id}");
                    }
                }

                foreach (var id2 in new List<string>(SpecHolder.HolderInstance.OpForSpecMap[pKey]))//.Where(x => ModInit.modSettings.WhiteListOpFor.Any(y => x == y)))
                {
                    if (SpecManager.ManagerInstance.OpForSpecList.All(x => x.OpForSpecID != id2))
                    {
                        SpecHolder.HolderInstance.OpForSpecMap[pKey].Remove(id2);
                        ModInit.modLog.LogMessage($"Removed deprecated OpForSpecMap from {pKey} with id {id2}");
                    }
                }

                foreach (var id3 in new Dictionary<string, int>(SpecHolder.HolderInstance.MissionsTracker[pKey]))//.Where(x=> ModInit.modSettings.WhiteListMissions.Any(y=> x == y)))
                {
                    if (SpecManager.ManagerInstance.MissionSpecList.All(x => x.contractTypeID != id3.Key))
                    {
                        SpecHolder.HolderInstance.MissionsTracker[pKey].Remove(id3.Key);
                        ModInit.modLog.LogMessage($"Removed deprecated MissionSpecTracker from {pKey} with id {id3.Key}");
                    }
                }

                foreach (var id4 in new Dictionary<string, int>(SpecHolder.HolderInstance.OpForKillsTracker[pKey]))//.Where(x=> ModInit.modSettings.WhiteListMissions.Any(y=> x == y)))
                {
                    if (SpecManager.ManagerInstance.OpForSpecList.All(x => x.factionID != id4.Key))
                    {
                        SpecHolder.HolderInstance.OpForKillsTracker[pKey].Remove(id4.Key);
                        ModInit.modLog.LogMessage($"Removed deprecated OpForKillsTracker from {pKey} with id {id4.Key}");
                    }
                }
            }
            //this is where SAR is resetgging things? so eithe prevent cleanup, or spcefically skip SAR pilots...with tag maybe?l
            var rm1 = SpecHolder.HolderInstance.MissionSpecMap.Keys.Where(x => x.EndsWith(aiPilotFlag));
            foreach (var key in new List<string>(rm1))
            {
                SpecHolder.HolderInstance.MissionSpecMap.Remove(key);
                ModInit.modLog.LogMessage(
                    $"Pilot with pilotID {key} not in roster or was AI pilot, removing from MissionSpecMap");
            }
            var rm2 = SpecHolder.HolderInstance.OpForSpecMap.Keys.Where(x =>
                !currentPilots.Contains(x) || x.EndsWith(aiPilotFlag));
            foreach (var key in new List<string>(rm2))
            {
                SpecHolder.HolderInstance.OpForSpecMap.Remove(key);
                ModInit.modLog.LogMessage(
                    $"Pilot with pilotID {key} not in roster or was AI pilot, removing from OpForSpecMap");
            }
            var rm3 = SpecHolder.HolderInstance.MissionsTracker.Keys.Where(x =>
                !currentPilots.Contains(x) || x.EndsWith(aiPilotFlag));
            foreach (var key in new List<string>(rm3))
            {
                SpecHolder.HolderInstance.MissionsTracker.Remove(key);
                ModInit.modLog.LogMessage(
                    $"Pilot with pilotID {key} not in roster or was AI pilot, removing from MissionsTracker");
            }
            var rm4 = SpecHolder.HolderInstance.OpForKillsTracker.Keys.Where(x =>
                !currentPilots.Contains(x) || x.EndsWith(aiPilotFlag));
            foreach (var key in new List<string>(rm4))
            {
                SpecHolder.HolderInstance.OpForKillsTracker.Remove(key);
                ModInit.modLog.LogMessage(
                    $"Pilot with pilotID {key} not in roster or was AI pilot, removing from OpForKillsTracker");
            }
        }

        public void ProcessTaggedSpecs(Pilot pilot)
        {
            var pKey = pilot.FetchGUID();
            foreach (var tag in ModInit.modSettings.taggedMissionSpecs)
            {
                if (pilot.pilotDef.PilotTags.Contains(tag.Key))
                {
                    foreach (var mspec in tag.Value)
                    {
                        if (!SpecHolder.HolderInstance.MissionSpecMap[pKey].Contains(mspec))
                        {
                            SpecHolder.HolderInstance.MissionSpecMap[pKey].Add(mspec);
                            ModInit.modLog.LogMessage($"Adding {mspec} to {pilot.Callsign}'s MissionSpecMap from Pilot Tag: {tag.Key}.");
                        }
                    }
                }
            }

            foreach (var tag in ModInit.modSettings.taggedOpForSpecs)
            {
                if (pilot.pilotDef.PilotTags.Contains(tag.Key))
                {
                    foreach (var opspec in tag.Value)
                    {
                        if (!SpecHolder.HolderInstance.OpForSpecMap[pKey].Contains(opspec))
                        {
                            SpecHolder.HolderInstance.OpForSpecMap[pKey].Add(opspec);
                            ModInit.modLog.LogMessage($"Adding {opspec} to {pilot.Callsign}'s OpForSpecMap from Pilot Tag: {tag.Key}.");
                        }
                    }
                }
            }
        }

        public void SerializeSpecState()
        {
            var op4State = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(OP4SpecStateTag));
            GlobalVars.sim.CompanyTags.Remove(op4State);
            op4State = $"{OP4SpecStateTag}{JsonConvert.SerializeObject(HolderInstance.OpForSpecMap)}";
            ModInit.modLog.LogMessage($"Serialized op4State and adding to company tags.\n\nState was {op4State}.");
            GlobalVars.sim.CompanyTags.Add(op4State);

            var op4Tracker = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(OP4SpecTrackerTag));
            GlobalVars.sim.CompanyTags.Remove(op4Tracker);
            op4Tracker = $"{OP4SpecTrackerTag}{JsonConvert.SerializeObject(HolderInstance.OpForKillsTracker)}";
            ModInit.modLog.LogMessage($"Serialized op4Tracker and adding to company tags.\n\nState was {op4Tracker}.");
            GlobalVars.sim.CompanyTags.Add(op4Tracker);

            var missionState = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(MissionSpecStateTag));
            GlobalVars.sim.CompanyTags.Remove(missionState);
            missionState = $"{MissionSpecStateTag}{JsonConvert.SerializeObject(HolderInstance.MissionSpecMap)}";
            ModInit.modLog.LogMessage($"Serialized missionState and adding to company tags.\n\nState was {missionState}.");
            GlobalVars.sim.CompanyTags.Add(missionState);

            var missionTracker = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(MissionSpecTrackerTag));
            GlobalVars.sim.CompanyTags.Remove(missionTracker);
            missionTracker = $"{MissionSpecTrackerTag}{JsonConvert.SerializeObject(HolderInstance.MissionsTracker)}";
            ModInit.modLog.LogMessage($"Serialized missionTracker and adding to company tags.\n\nState was {missionTracker}.");
            GlobalVars.sim.CompanyTags.Add(missionTracker);

            var activeStratComField = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(ActiveStratComTag));
            GlobalVars.sim.CompanyTags.Remove(activeStratComField);
            activeStratComField = $"{ActiveStratComTag}{JsonConvert.SerializeObject(HolderInstance.activeStratCom)}";
            ModInit.modLog.LogMessage($"Serialized activeStratCom and adding to company tags.\n\nState was {activeStratComField}.");
            GlobalVars.sim.CompanyTags.Add(activeStratComField);
        }

        //deserialize injurymap (dictionary) from tag and save to PilotInjuryHolder.Instance
        public void DeserializeSpecState()
        {
            if (sim.CompanyTags.Any(x => x.StartsWith(OP4SpecStateTag)))
            {
                var op4StateCTag = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(OP4SpecStateTag));
                var op4State = op4StateCTag?.Substring(OP4SpecStateTag.Length);
                HolderInstance.OpForSpecMap = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(op4State);
                ModInit.modLog.LogMessage($"Deserializing op4State and removing from company tags");
                GlobalVars.sim.CompanyTags.Remove(op4StateCTag);
            }
            else
            {
                ModInit.modLog.LogMessage($"No op4State to deserialize. Hopefully this is the first time you're running PMP Specializations!");
            }

            if (sim.CompanyTags.Any(x => x.StartsWith(OP4SpecTrackerTag)))
            {
                var op4TrackerCTag = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(OP4SpecTrackerTag));
                var op4Tracker = op4TrackerCTag?.Substring(OP4SpecTrackerTag.Length);
                HolderInstance.OpForKillsTracker = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(op4Tracker);
                ModInit.modLog.LogMessage($"Deserializing op4Tracker and removing from company tags");
                GlobalVars.sim.CompanyTags.Remove(op4TrackerCTag);
            }
            else
            {
                ModInit.modLog.LogMessage($"No op4Tracker to deserialize. Hopefully this is the first time you're running PMP Specializations!");
            }

            if (sim.CompanyTags.Any(x => x.StartsWith(MissionSpecStateTag)))
            {
                var missionStateCTag = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(MissionSpecStateTag));
                var missionState = missionStateCTag?.Substring(MissionSpecStateTag.Length);
                HolderInstance.MissionSpecMap = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(missionState);
                ModInit.modLog.LogMessage($"Deserializing missionState and removing from company tags");
                GlobalVars.sim.CompanyTags.Remove(missionStateCTag);
            }
            else
            {
                ModInit.modLog.LogMessage($"No missionState to deserialize. Hopefully this is the first time you're running PMP Specializations!");
            }

            if (sim.CompanyTags.Any(x => x.StartsWith(MissionSpecTrackerTag)))
            {
                var missionTrackerCTag = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(MissionSpecTrackerTag));
                var missionTracker = missionTrackerCTag?.Substring(MissionSpecTrackerTag.Length);
                HolderInstance.MissionsTracker = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(missionTracker);
                ModInit.modLog.LogMessage($"Deserializing missionTracker and removing from company tags");
                GlobalVars.sim.CompanyTags.Remove(missionTrackerCTag);
            }
            else
            {
                ModInit.modLog.LogMessage($"No missionTracker to deserialize. Hopefully this is the first time you're running PMP Specializations!");
            }

            if (sim.CompanyTags.Any(x => x.StartsWith(ActiveStratComTag)))
            {
                var activeStratTag = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(ActiveStratComTag));
                var activeStratTracker = activeStratTag?.Substring(ActiveStratComTag.Length);
                HolderInstance.activeStratCom = JsonConvert.DeserializeObject<string>(activeStratTracker);
                ModInit.modLog.LogMessage($"Deserializing activeStratCom and removing from company tags");
                GlobalVars.sim.CompanyTags.Remove(activeStratTag);
            }
            else
            {
                ModInit.modLog.LogMessage($"No activeStratCom to deserialize. Hopefully this is the first time you're running PMP Specializations!");
            }
        }
    }
}
