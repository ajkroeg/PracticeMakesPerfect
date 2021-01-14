using BattleTech;
using System.Collections.Generic;
using System.Linq;

namespace PracticeMakesPerfect.Framework
{
    class Descriptions
    {
        internal static string getMissionSpecializationDescription(string contractID)
        {
            var rtrn = "";
            if (SpecManager.ManagerInstance.MissionSpecList.Any(x=>x.contractTypeID == contractID))
            {
                var list = new List<MissionSpec>(
                    SpecManager.ManagerInstance.MissionSpecList.Where(x => x.contractTypeID == contractID)).OrderBy(x => x.missionsRequired);
                var title = $"\n\n<b>Available Mission Specializations</b>\n\n";
                foreach (var missionSpec in list)
                {
                    var description =
                        $"{missionSpec.MissionSpecName} [{missionSpec.missionsRequired}]: {missionSpec.description}\n\n";
                    rtrn += description;
                }
                return title += rtrn;
            }
            return null;
        }

        internal static string getOpForSpecializationDescription(string factionID)
        {
            var rtrn = "";
            if (SpecManager.ManagerInstance.OpForSpecList.Any(x => x.factionID == factionID))
            {
                var list = new List<OpForSpec>(
                    SpecManager.ManagerInstance.OpForSpecList.Where(x => x.factionID == factionID)).OrderBy(x => x.killsRequired);
                var title = $"\n\n<b>Available OpFor Specializations</b>\n\n";
                foreach (var opforSpec in list)
                {
                    var description =
                        $"{opforSpec.OpForSpecName} [{opforSpec.killsRequired}]: {opforSpec.description}\n\n";
                    rtrn += description;
                }
                return title += rtrn;
            }
            return null;
        }

        internal static string getPilotSpecializationsOrProgress(Pilot pilot)
        {
            var pilotID = pilot.FetchGUID();
            var rtrn = "";

            if (SpecHolder.HolderInstance.MissionSpecMap[pilotID].Count > 0)
            {
                rtrn+= $"\n<b>Mission Specializations</b>\n\n";

                var mspecsOrdered = SpecManager.ManagerInstance.MissionSpecList.Where(x =>
                    SpecHolder.HolderInstance.MissionSpecMap[pilotID].Any(y => y == x.MissionSpecID)).OrderBy(c=>c.contractTypeID).ThenBy(m=>m.missionsRequired);
                foreach (var missionSpec in mspecsOrdered)
                {
                    var description =
                        $"<b>{missionSpec.MissionSpecName} [{missionSpec.missionsRequired}]:</b> {missionSpec.description}\n\n";
                    rtrn += description;
                }
            }

            if (SpecHolder.HolderInstance.OpForSpecMap[pilotID].Count > 0)
            {
                rtrn += $"\n<b>OpFor Specializations</b>\n\n";

                var opspecsOrdered = SpecManager.ManagerInstance.OpForSpecList.Where(x =>
                    SpecHolder.HolderInstance.OpForSpecMap[pilotID].Any(y => y == x.OpForSpecID)).OrderBy(c => c.factionID).ThenBy(m => m.killsRequired);
                foreach (var opforSpec in opspecsOrdered)
                {
                    var description =
                        $"<b>{opforSpec.OpForSpecName} [{opforSpec.killsRequired}]:</b> {opforSpec.description}\n\n";
                    rtrn += description;
                }
            }

            rtrn += $"\n<b>Mission Stats</b>\n\n";
            foreach (var stat in SpecHolder.HolderInstance.MissionsTracker[pilotID])
            {
                rtrn += $"<b>{stat.Key}:</b> {stat.Value} complete.\n";
            }

            rtrn += $"\n<b>Opfor Stats</b>\n\n";
            foreach (var stat in SpecHolder.HolderInstance.OpForKillsTracker[pilotID])
            {
                rtrn += $"<b>{stat.Key}:</b> {stat.Value} kills.\n";
            }

            return rtrn;
        }
    }
}
