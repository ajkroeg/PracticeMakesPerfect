using Harmony;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PracticeMakesPerfect.Framework;

namespace PracticeMakesPerfect
{

    public static class ModInit
    {
        internal static Logger modLog;
        internal static string modDir;


        internal static Settings modSettings;
        public const string HarmonyPackage = "us.tbone.PracticeMakesPerfect";
        public static void Init(string directory, string settingsJSON)
        {
            modDir = directory;
            modLog = new Logger(modDir, "bigPMPin", true);
            try
            {
                using (StreamReader reader = new StreamReader($"{modDir}/settings.json"))
                {
                    string jsData = reader.ReadToEnd();
                    ModInit.modSettings = JsonConvert.DeserializeObject<Settings>(jsData);
                }

            }
            catch (Exception ex)
            {
                ModInit.modLog.LogException(ex);
                ModInit.modSettings = new Settings();
            }
            //HarmonyInstance.DEBUG = true;
            ModInit.modLog.LogMessage($"Initializing PracticeMakesPerfect - Version {typeof(Settings).Assembly.GetName().Version}");
            SpecManager.ManagerInstance.Initialize();
            SpecHolder.HolderInstance.Initialize();
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

        }

    }

    class Settings
    {
        public bool enableLogging = true;

        public bool useMissionXPforBonus = true;
        public float bonusXP_MissionMechKills = 0.05f;
        public float bonusXP_MissionOtherKills = 0.025f;

        public int bonusXP_MechKills = 100;
        public int bonusXP_OtherKills = 50;
        public float bonusXP_StrDamageMult = 0.0f;
        public float bonusXP_ArmDamageMult = 0.0f;
        public int bonusXP_CAP = -1;

        public bool activeProbeXP_PerTarget = false;
        public int activeProbeXP = 25;
        public int sensorLockXP = 25;

        public float missionXPEffects = 0.05f;
        public float missionXPeffectBonusDivisor = 1000000f;

        public Dictionary<string, int> reUseRestrictedbonusEffects_XP = new Dictionary<string, int>();

        public Dictionary<string, int> degradingbonusEffects_XP = new Dictionary<string, int>();

        public Dictionary<string, int> bonusEffects_XP = new Dictionary<string, int>();

        public int MaxOpForSpecializations = 0;
        public bool OpForTiersCountTowardMax = false;

        public int MaxMissionSpecializations = 0;
        public bool MissionTiersCountTowardMax = false;

        public bool TaggedOpforSpecsCountTowardMax = false;
        public bool TaggedMissionSpecsCountTowardMax = false;

        public int MissionSpecSuccessRequirement = 0; //0 = no req, 1 = GoodFaith, 2 = SuccessOnly

        public List<string> WhiteListOpFor= new List<string>();
        public List<string> WhiteListMissions= new List<string>();

        public List<OpForSpec> OpForSpecList = new List<OpForSpec>();
        public List<OpForSpec> OpForDefaultList = new List<OpForSpec>();

        public List<MissionSpec> MissionSpecList = new List<MissionSpec>();
        public List<MissionSpec> MissionDefaultList = new List<MissionSpec>();

        public Dictionary<string, List<string>> taggedOpForSpecs = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> taggedMissionSpecs = new Dictionary<string, List<string>>();

        public List<StratCom> StratComs = new List<StratCom>();

        public string dummyOpForStat = "dummyOpForStat";

        public string argoUpgradeToReset = null;

        public bool removeDeprecated = false;
        public bool DebugCleaning = false;
    }
}