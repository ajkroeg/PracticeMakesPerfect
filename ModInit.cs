using Harmony;
using System;
using System.Reflection;
using Newtonsoft.Json;

namespace PracticeMakesPerfect
{

    public static class ModInit
    {
        public static PracticeMakesPerfectSettings Settings = new PracticeMakesPerfectSettings();
        public const string HarmonyPackage = "us.tbone.PracticeMakesPerfect";
        public static void Init(string directory, string settingsJSON)
        {
            try
            {
                ModInit.Settings = JsonConvert.DeserializeObject<PracticeMakesPerfectSettings>(settingsJSON);
            }
            catch (Exception)
            {

                ModInit.Settings = new PracticeMakesPerfectSettings();
            }
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }
    public class PracticeMakesPerfectSettings
    {
        public bool useMissionXPforBonus = true;
        public float bonusXP_MissionMechKills = 0.05f;
        public float bonusXP_MissionOtherKills = 0.025f;

        public int bonusXP_MechKills = 100;
        public int bonusXP_OtherKills = 50;
        public float bonusXP_StrDamageMult = 0.0f;
        public float bonusXP_ArmDamageMult = 0.0f;
        public int bonusXP_CAP = -1;

    }
}