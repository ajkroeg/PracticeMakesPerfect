# PracticeMakesPerfect

Mod consists of two modules, XP and Specializations.



Original Module: [XP Module](#xp-module) allows Commander and pilots to gain bonus XP from kills and/or damage dealt during contract. In addition, allows bonus XP to be awareded by combat actions as defined below. Also connects PilotDef kill stats (as displayed in barracks) to Pilot stats accessible by events.

New Module: [Specializations](#specializations) - Allows pilots and commander to develop specializations (bonuses) for various mission types and opfor factions. Can be pre-set for specific pilots via tag, or earned by completing missions and getting kills on a given faction.

<b>Important Change</b>

As of version 1.0.1.0., settings are located in settings.json, <b>not</b> mod.json.

## XP Module

### XP Settings
Current XP settings in settings.json:
```
{
	"useMissionXPforBonus": true,
	"bonusXP_MissionMechKills": 0.05,
	"bonusXP_MissionOtherKills": 0.025,
	"bonusXP_MechKills": 100,
	"bonusXP_OtherKills": 50,
	"bonusXP_StrDamageMult": 0.0,
	"bonusXP_ArmDamageMult": 0.0,
	"bonusXP_CAP": -1
	"activeProbeXP_PerTarget": true,
	"activeProbeXP": 25,
	"sensorLockXP": 25,
	"missionXPEffects": 0.025,
	"missionXPeffectBonusDivisor": 35,
	"reUseRestrictedbonusEffects_XP": {
		"AbilityDefPPC": 50
		},	
	"degradingbonusEffects_XP": {
		"TAG-Effect": 50
		},					
	"bonusEffects_XP": {
		"DamagePerShot": 25
}
```

`useMissionXPforBonus` - bool, switch for whether bonus XP is awarded as a function of the contract XP or as flat bonus.

`bonusXP_MissionMechKills` - multiplier for bonus XP as function of Mission XP, per mech kill. Using settings above, if contract awards 1000XP and pilot gets 2 mech kills, they recieve 100 bonus XP (1000 * .05 * 2 = 100)

`bonusXP_MissionOtherKills` - as above, but for vehicles/turrets.

`bonusXP_MechKills` - int, bonus XP given per Mech kill. Set to 0 to disable. ~~Disabled if `useMissionXPforBonus` true.~~ 

`bonusXP_OtherKills` - int, bonus XP given per Other kill (vehicles/turrets). Set to 0 to disable. ~~Disabled if `useMissionXPforBonus` true.~~

`bonusXP_StrDamageMult` - float, multiplier for bonus XP given per Structure damage inflicted (e.g., for 200 structure damage inflicted, award 10 XP). Set to 0 to disable. <b>not affected by `useMissionXPforBonus`</b>.

`bonusXP_ArmDamageMult` - float, multiplier for bonus XP given per Armor damage inflicted (e.g., for 200 armor damage inflicted, award 10 XP). Set to 0 to disable. <b>not affected by `useMissionXPforBonus`</b>.

`bonusXP_CAP` - int, maximum amount of bonus XP awardable for kills, damage, or effects/actions. Set to -1 to disable cap, set to 0 to disable <i>all</i> bonus XP.

NEW STUFF:

`activeProbeXP_PerTarget` - bool, controls whether activeProbeXP bonus is awarded per-target in probe radius. For example, if `activeProbeXP` is set to 25, and 3 targets are affected when a player unit actives Active Probe, that unit would receive 75 XP.

`activeProbeXP` - int, XP awarded to the player for using Active Probe.

`sensorLockXP` - int, XP awarded to player for using Sensor Lock ability.

`missionXPEffects` - float, multiplier for bonus XP awarded for effects as a function of Mission XP. Functions in tandem with `missionXPeffectBonusDivisor` below to approximate a per-effect bonus.

`missionXPeffectBonusDivisor` - int, divisor of <i>total bonus XP awarded via the dictionaries below, or `effectXP`</i> in order to approximate a per-effect XP bonus based on contract XP. The resulting bonus follows the formula `ContractXP * missionXPEffects * effectXP / missionXPeffectBonusDivisor`. Thus, for 3 effects defined in the dictionaries with flat-rate bonuses of 35, 50, and 75, a `missionXPeffectBonusDivisor` value of 50 to 55 would give be recommended.

`reUseRestrictedbonusEffects_XP` - Dictionary<String, int> - Effects and corresponding XP rewards for the player unit applying those effects. XP bonus for effects in this dictionary are limited to being applied once per source-target pair. For example, if the TAG effect is listed in this dictionary, PlayerMech1 can only recieve a single TAG XP bonus for TAG-ing EnemyMech1 <i>until that effect expires</i>, at which point they would be able to recieve TAG XP for EnemyMech1 once again. However, PlayerMech1 <i>would</i> be able to recieve TAG XP for applying TAG to, for example, EnemyMech<b>2</b>. However, PlayerMech<b>2</b> could also receive TAG XP bonus for tagging EnemyMech1, even if EnemyMech1 had an active TAG effect from PlayerMech1.

`degradingbonusEffects_XP` - Dictionary<String, int> - similar to above. Effects in this dictionary award the full amount when first applied, and then a penalized amount if re-applied, changing as a function of rounds remaining until effect expiration. For example, if the TAG effect has a Duration of 3 activations, the first time TAG is applied, it awards the amount defined in the dictionary. If it is then reapplied the following activation, the XP awarded is base/3. If it was instead reapplied 2 turns later, the XP award would be would be base/2, etc.

`bonusEffects_XP` - Dictionary<String, int> - Effects and corresponding XP rewards for player unit applying those effects. Effects in this dictionary are <b>not</b> subject to same limitations as above; full XP is rewarded every time the effect is applied, regardless of source and target.



To clarify use of `reUseRestrictedbonusEffects_XP`, `degradingbonusEffects_XP`, and `bonusEffects_XP`, the following is an excerpt of the vanilla WeaponDef for `Weapon_PPC_PPC_0-STOCK`

```
"effectType" : "StatisticEffect",
            "Description" : {
                "Id" : "AbilityDefPPC",
                "Name" : "SENSORS IMPAIRED",
                "Details" : "[AMT] Difficulty to all of this unit's attacks until its next activation.",
                "Icon" : "uixSvgIcon_status_sensorsImpaired"
            },
            "nature" : "Debuff",
            "statisticData" : {
                "appliesEachTick" : false,
                "effectsPersistAfterDestruction" : false,
                "statName" : "AccuracyModifier",
                "operation" : "Float_Add",
                "modValue" : "1.0",
                "modType" : "System.Single",
                "additionalRules" : "NotSet",
                "targetCollection" : "NotSet",
                "targetWeaponCategory" : "NotSet",
                "targetWeaponType" : "NotSet",
                "targetAmmoCategory" : "NotSet",
                "targetWeaponSubType" : "NotSet"
            },
```
Bonus XP can be defined by either the effectType.Description.Id (`AbilityDefPPC` above), OR by the effectType.statisticData.statName (`AccuracyModifier`). The mod will check the dictionary first for a matching `Id` before checking for a matching `statName`. This was done to allow some flexibility, as some stats will be unique to certain mods, while others like AccuracyModifier are used in many places where you may not want XP awarded. <b>Important: aura effects like ECM will re-award the XP bonus every time the effect is removed/restored, even for the same target.</b>

Valid settings for `Weapon_PPC_PPC_0-STOCK` could therefore be
```
			"bonusEffects_XP": {
				"AbilityDefPPC": 25
				},	
```
OR

```
			"bonusEffects_XP": {
				"AccuracyModifier": 25
				},	
```


__________________________________________________________

New pilot stats accessible through events, etc. as follows:

`TotalMechKills` - Number of mechs pilot has killed in total

`TotalOtherKills` - Number of vehicles/turrets pilot has killed in total

`TotalKills` - because math is hard

`TotalInjuries` - total number of injuries pilot has ever sustained

## Specializations

The Specializations module makes use of 3 new data types: OpForSpecs, MissionSpecs, and StratComs.

### OpForSpec

OpForSpecs or OpFor Specializations are unique bonuses awarded to pilots that only apply against specific OpFor factions. Depending on settings they may apply specifically against units of that OpFor faction, or they may apply for the entirety of a contract where that OpFor faction is the contract "target" faction.

example json structure for OpForSpecs is as follows:

```
{
	"OpForSpecID": "TC_Destructor",
	"OpForSpecName": "Taurian Concordat: Demolisher",
	"killsRequired": 0,
	"factionID": "TaurianConcordat",
	"applyToFaction": true,
	"description": "This pilot is an expert at killing Taurians, and has increased damage against them.",
	"effectDataJO": [
		{
			"durationData": {
				"duration": 1,
				"ticksOnEndOfRound": true,
				"useActivationsOfTarget": true,
				"stackLimit": 1
			},
			"targetingData": {
				"effectTriggerType": "OnWeaponFire",
				"effectTargetType": "Creator",
				"showInStatusPanel": true
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "TCDemolisher",
				"Name": "Demolisher",
				"Details": "This pilot is an expert at killing Taurians, and has increased damage against them.",
				"Icon": "seeingstars"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "DamagePerShot",
				"operation": "Float_Multiply",
				"modValue": "1.5",
				"modType": "System.Single",
				"additionalRules": "NotSet",
				"targetCollection": "Weapon",
				"targetWeaponCategory": "NotSet",
				"targetWeaponType": "NotSet",
				"targetAmmoCategory": "NotSet",
				"targetWeaponSubType": "NotSet"
			}
		},
		{
			"durationData": {},
			"targetingData": {
				"effectTriggerType": "Passive",
				"effectTargetType": "Creator",
				"showInStatusPanel": true
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "TCDemolisherPassive",
				"Name": "Demolisher",
				"Details": "This pilot is an expert at killing Taurians, and has 50% increased damage against them.",
				"Icon": "seeingstars"
			},
			"nature": "Buff",
			"statisticData": {}
		}
	]
}
```

`OpForSpecID` - string, ID for this OpForSpec

`OpForSpecName` - string, human-friendly name for this OpForSpec

`killsRequired` - int, number of kills required vs the appropriate faction for this OpForSpec to be awarded. <b>IMPORTANT:</b> If set to `0` as above, this OpForSpec will <i>not</i> be awarded to pilots via kills, and can only be awarded via tags, using the setting `taggedOpForSpecs`, e.g. for `"taggedOpForSpecs": {"name_t-bone": "TC_Destructor"	},`, pilots with the tag `name_t-bone` would get the OpForSpec `TC_Destructor`.

`factionID` - string, faction ID for which progress towards with OpForSpec is tracked, <b>AND</b> against which this OpForSpec applies. Works in conjunction with `applyToFaction`.

`applyToFaction` - bool, determines how OpForSpec effects are applied during contracts. If `false`, the OpForSpec effects will be applied at contract start, <i>if the contract Target faction == `factionID`</i>.  So in a three-way battle contract where the target faction was Taurians, if the effect was increased damage vs Taurians, and the Capellans turn up...you'll  do increased damage against both the Taurians and the Capellans.  Intended targeting data for effects when false is:
	```
	"targetingData": {
			"effectTriggerType": "Passive",
			"effectTargetType": "Creator",
			"showInStatusPanel": true
			},
	```
`effectTargetType` can also be `AllAllies`, `AllLanceMates`, or `AllEnemies` when false.

If `true`, OpForSpec effects are applied <i>only to that specific OpFor faction</i>, meaning in a three-way battle...only the faction matching `factionID` will have the effect applied. So if the effect was increased damage vs Taurians, and the Capellans turn up...you'll only do increased damage against the Taurians. In addition, duration data should be set as above The UI does NOT presently update to show things like increased weapon damage when targeting the faction, so it is recommended to create a 2nd "dummy" effect with `"effectTriggerType": "Passive",` `"effectTargetType": "Creator",`  and `"showInStatusPanel": true` to display an effect tooltip to let the player know that they will deal/recieve the effects vs the appropriate faction (as in above example).

Intended targeting data for effects when `true` is:
	```
	"targetingData": {
			"effectTriggerType": "OnWeaponFire",
			"effectTargetType": "Creator", 
			"showInStatusPanel": true
			},
	```
`effectTargetType` can also be `AllEnemies` when `true`.

`description` - human-legible description of the OpForSpec. For `OpForSpec` in `OpForDefaultList`, the string {faction} will be replaced with `FactionDef.Demonym`s: 
e.g., for `"description": "This pilot inflicts increased damage against {faction}.",` , the description in-game would read "This pilot inflicts increased damage against Taurians" for TaurianConcordat, and "This pilot inflicts increased damage against Chainelanians" for Chainelane.

`effectDataJO` - List of standard format `effectData` for this specialization.


### MissionSpec

MissionSpecs, or Mission Specializations are unique bonuses awarded to pilots that only apply during their respective mission types.

example json structure for MissionSpecs is as follows:
```
{
	"MissionSpecID": "DamageResistance",
	"MissionSpecName": "Damage Resistance",
	"missionsRequired": 10,
	"contractTypeID": "UNIVERSAL",
	"description": "This pilot loves the simplicity of a good brawl, and takes less damage during {contract} contracts.",
	"effectDataJO": []
}
```

`MissionSpecID` - string, ID for this MissionSpec

`MissionSpecName` - string, human-friendly name for this MissionSpec

`missionsRequired` - int, number of completed contracts of this type for the specialization to be awarded. As with OpForSpecs, will never be awarded if set to 0 except through tags via `taggedMissionSpecs`

`contractTypeID` - string, contract type for which progress is tracked and for which effects are applied when specialization is awarded; e.g. SimpleBattle, DuoDuel.

`description` - human-legible description for this specialization. Similar to OpforSpecs, `MissionSpec` in `MissionDefaultList`, the string {contract} will be replaced with `ContractTypeValue.Name`: 
e.g., for `"description": "This pilot loves the simplicity of a good brawl, and takes less damage during {contract} contracts.",` , the description in-game would read "This pilot loves the simplicity of a good brawl, and takes less damage during SimpleBattle contracts."

`effectDataJO` - List of standard format `effectData` for this specialization.

Intended targeting data for MissionSpecs:
```
	"targetingData": {
			"effectTriggerType": "Passive",
			"effectTargetType": "Creator",
			"showInStatusPanel": true
			},
```

`effectTargetType` can also be `AllAllies`, `AllLanceMates`, or `AllEnemies` 

### StratCom

StratComs are unique bonuses that apply to your pilots <i>only when your commander is not on the field</i>. We all know Darius is pretty useless, so it might make sense for there to be strategic-level advantage for the commander to focus on...well, commanding, instead of just blasting the OpFor.

example json structure for StratComs is as follows:
```
{
"StratComID": "StrategicCommand",
"StratComName": "Strategic Command",
"description": "The commander is sitting this contract out, providing strategic command over the battle. Resolve generation is increased.",
"effectDataJO": [
	{
	}
```

`StratComID` - string, ID for this StratCom

`StratComName` - string, human-friendly name for this StratCom

`description` - string, human-friendly description for this StratCom

`effectDataJO` - List of standard format `effectData` for this StratCom.


### Specialization Settings
Specializations settings in settings.json:

```
{
	"MaxOpForSpecializations": 3,
	"OpForTiersCountTowardMax": false,
	"MaxMissionSpecializations": 3,
	"MissionTiersCountTowardMax": false,
	"WhiteListOpFor": [
		"Davion",
		"Liao",
		"Kurita",
		"Marik",
		"Steiner"
	],
	"WhiteListMissions": [
		"SimpleBattle",
		"DestroyBase",
		"CaptureBase",
		"DefendBase",
		"CaptureEscort"
	],
	"OpForDefaultList": [],
	"MissionDefaultList": [],
	"OpForSpecList": [],
	"MissionSpecList": [],
	"StratComs": [],
	"taggedOpForSpecs": {
		"name_t-bone": "TC_Destructor"
	},
	"taggedMissionSpecs": {},
	"DebugCleaning": false
}
```

`MaxOpForSpecializations` - int, maximum number of OpFor specializations pilots can have.

`OpForTiersCountTowardMax` - bool, defines whether "tiers" of specializations count toward max (e.g., if you define 2 specializations to be awarded for killing AuriganPirates, one at 25 kills and one at 50 kills; if `OpForTiersCountTowardMax: true` both specializations would count towards the cap. If `OpForTiersCountTowardMax: false`, only one would count towards the cap (allowing you to specialize against `MaxOpForSpecializations` factions total).

`MaxMissionSpecializations` - int, maximum number of Mission specializations pilots can have.

`MissionTiersCountTowardMax` - bool, same as `OpForTiersCountTowardMax`, just for Mission specializations.

`WhiteListOpFor` - List<string>, List of OpFor factions (e.g., Liao, AuriganPirates, ClanWolf) for which specializations will be created from `OpForDefaultList` and for which progress towards specializations will be tracked.
	
`WhiteListMissions` - List<string>, List of contract types (e.g., SimpleBattle, CaptureBase, DuoDuel) for which specializations will be created from `MissionDefaultList` and for which progress towards specializations will be tracked.
	
`OpForDefaultList` - List<OpforSpec> - list of default OpFor specializations. These OpForSpecs are created for every faction in `WhiteListOpFor`.

`MissionDefaultList` - List<MissionSpec> - list of default Missions specializations. These MissionSpecs are created for every Mission type in `WhiteListMissions`.
	
`OpForSpecList` - List<OpForSpec> - list of specific OpFor specializations. These should be unique to specific factions. OpForSpec in this list with `killsRequired: 0` will only be awarded via tags.
	
`MissionSpecList` - List<MissionSpec> - list of specific Mission specializations. These should be unique to specific Mission types. MissionSpec in this list with `missionsRequired: 0` will only be awarded via tags.
	
`StratComs` - List<StratCom> - list of StratCom bonuses your pilots will recieve when commander is <i>not</i> deployed.

`taggedOpForSpecs` - Dictionary<string,string> - KeyValuePairs where Key = pilot tag and Value = `OpForSpec.OpForSpecID` of the OpForSpec to be awarded to pilots with the tag.

`taggedMissionSpecs` - Dictionary<string,string> - KeyValuePairs where Key = pilot tag and Value = `MissionSpec.MissionSpecID` of the MissionSpec to be awarded to pilots with the tag.

`DebugCleaning` - bool. Debugging only! if set to `true`, will wipe all specializations on save load.
