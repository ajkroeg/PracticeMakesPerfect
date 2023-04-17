# PracticeMakesPerfect

**Versions 1.2.0.0 and higher depend on CustomAmmoCategories!**
**Versions 1.3.0.0 and higher requires modtek v3 or higher**

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
	"enableSpecializations": true,
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
	"AMSKillsXP": 0.5,
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
		},
}
```

`enableSpecializations` - bool, if true or missing (defaults to true), specializations will be enabled. if false, all specializations and related patches are disabled, even if configured in later settings.

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

`AMSKillsXP`- float, XP awarded for missile "kills" for AMS. Added together per-AMS attack sequence and rounded to integer.

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

Pilot specializations (and progress towards them) can be seen by hovering over their large portrait on the right side of the Barracks screen:

![TextPop](https://github.com/ajkroeg/PracticeMakesPerfect/blob/master/doc/progress.png)

Players can reset specializations and clear progress (allowing new specializations to be earned) by holding Control while clicking on the "Service Record" tab in the Barracks. Optionally this ability can be tied to an argo upgrade:

![TextPop](https://github.com/ajkroeg/PracticeMakesPerfect/blob/master/doc/resetSpec.png)


### OpForSpec

OpForSpecs or OpFor Specializations are unique bonuses awarded to pilots that only apply against specific OpFor factions. Depending on settings they may apply specifically against units of that OpFor faction, or they may apply for the entirety of a contract where that OpFor faction is the contract "target" faction. In addition, OpForSpecs can have "simgame" effects apart from the standard in-contract effects using effectData.

Available OpForSpecs for a given faction can be seen in the faction tooltips in the Commanders Quarters and in Command Center screens. The number in brackets indicates the number of kills required for the OpForSpec to be awarded:

![TextPop](https://github.com/ajkroeg/PracticeMakesPerfect/blob/master/doc/availableOpFor.png)

#### OpForSpec - In-Contract effects
example json structure for OpForSpec with in-contract effects is as follows:

```
{
	"OpForSpecID": "TC_Destructor",
	"OpForSpecName": "Taurian Concordat: Demolisher",
	"killsRequired": 0,
	"factionID": "TaurianConcordat",
	"applyToFaction": ["TaurianConcordat"],
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
				"Details": "This pilot is an expert at killing Taurians, and deals 10% increased damage to them.",
				"Icon": "seeingstars"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "DummyBuff",
				"operation": "Set",
				"modValue": "true",
				"modType": "System.Boolean"
			}
		}
	]
}

```

`OpForSpecID` - string, ID for this OpForSpec

`OpForSpecName` - string, human-friendly name for this OpForSpec

`killsRequired` - int, number of kills required vs the appropriate faction for this OpForSpec to be awarded. <b>IMPORTANT:</b> If set to `0` as above, this OpForSpec will <i>not</i> be awarded to pilots via kills, and can only be awarded via tags, using the setting `taggedOpForSpecs`, e.g. for `"taggedOpForSpecs": {"name_t-bone": "TC_Destructor"	},`, pilots with the tag `name_t-bone` would get the OpForSpec `TC_Destructor`.

`factionID` - string, faction ID for which progress towards with OpForSpec is tracked, <b>AND</b> against which this OpForSpec applies. Works in conjunction with `applyToFaction`. e.g. AuriganPirates, TaurianConcordat, Liao, etc.

`applyToFaction` - List<string>, determines against what factions an OpForSpec is applied. For most in-contract effects, should usually be only the faction against which you have a specialization. However, you may include other factions if, for example, you want a specialization against AuriganPirates to also apply against Circinians and Tortugans. 
	
For purposes of PMP Specializations, there are broadly two types of effects; Passive and "Active". Passive effects are applied at contract start, and are applied as long as the contract target faction is one of `applyToFaction`. Passive effects have the following `targetingData`.
	
```
"targetingData": {
		"effectTriggerType": "Passive",
		"effectTargetType": "Creator",
		"showInStatusPanel": true
		},
```

`effectTargetType` can also be `AllAllies`, `AllLanceMates`, or `AllEnemies`.

For "Active" effects, OpForSpec effects are applied <i>only to that specific OpFor faction</i>, meaning in a three-way battle...only the factions matching `factionID` will have the effect applied. So if the effect was increased damage vs Taurians, and the Capellans turn up...you'll only do increased damage against the Taurians. In addition, duration data should be set as above The UI does NOT presently update to show things like increased weapon damage when targeting the faction, so it is recommended to create a 2nd "dummy" effect with `"effectTriggerType": "Passive",` `"effectTargetType": "Creator",`  and `"showInStatusPanel": true` to display an effect tooltip to let the player know that they will deal/recieve the effects vs the appropriate faction (as in above example).

<b>ALSO IMPORTANT for "Active effects":</b> Note durationData in above example! Necessary to ensure that the effect expires at the end of the round and prevent it from potentially applying to units of the wrong faction!

Intended targeting data for "Active" effects is:

```
"targetingData": {
		"effectTriggerType": "OnWeaponFire",
		"effectTargetType": "Creator", 
		"showInStatusPanel": true
		},
```

`effectTargetType` can also be `AllAllies`, `AllLanceMates`, or `AllEnemies`.


`description` - human-legible description of the OpForSpec. For `OpForSpec` in `OpForDefaultList`, the string {faction} will be replaced with `FactionDef.Demonym`s: 
e.g., for `"description": "This pilot inflicts increased damage against {faction}.",` , the description in-game would read "This pilot inflicts increased damage against Taurians" for TaurianConcordat, and "This pilot inflicts increased damage against Chainelanians" for Chainelane.

`effectDataJO` - List of standard format `effectData` for this specialization.


#### OpForSpec - Simgame effects
example json structure for OpForSpec with "simgame" effects:

```
{
	"OpForSpecID": "Local_Rabble",
	"OpForSpecName": "Locals: Damn Rabble",
	"killsRequired": 0,
	"factionID": "Locals",
	"applyToFaction": [
		"Locals"
	],
	"description": "This pilot is an expert at killing locals, and does fancy things.",
	"repMod": {
		"{OWNER}": 2,
		"Liao": 1,
		"Davion": 3,
		"{TARGET}": 8
		},
	"storeDiscount": {
		"ComStar": -0.2
		},
	"storeBonus": {
		"ComStar": 0.2
		},
	"cashMult": {
		"{EMPLOYER}": 0.25
		},
	"killBounty": {
		"Locals": 2500
		},
	"effectDataJO": []
}

```

`repMod` - Dictionary <string, int> - Added to reputation gained/lost for contracts against this faction (or if faction is listed in `applyToFaction`). For listed factions that are <i>not</i> the contract employer or target, reputation will be gained as a function of the contract employer reputation gain. The fixed strings "{OWNER}", {"EMPLOYER"}, and "{TARGET}" can also be used to dynamically change reputation for the system owner, your employer, and target factions respectively. 

**Special Case** `"repMod": {  "MercenaryReviewBoard": X},` will add X to the normally gained MRB rating from the mission

Using the above settings, if a contract would result in you gaining 5 reputation with the Aurigan Restoration and losing 4 with the Locals, you would also:
	1. Gain 7 reputation with the system owner if the system owner is also your employer.
	2. Gain 5 reputation with Liao.
	3. Gain 8 reputation with Davion.
	4. Lose no reputation with Locals due to `"{TARGET}": 8`. In this case `8` (from the repMod) + `-4` (from the contract target rep loss) = 4, which is rounded back down to 0 to prevent gaining reputation vs targets.
	
If there is a multiplier for `{"EMPLOYER"}` or `"{OWNER}"` AND that faction is specifically listed in `repMult`, the final employer reputation multiplier will be the largest of the three values.

If there is a multiplier for `"{TARGET}"` AND the target faction is specifically listed in `repMult`, the final target reputation multiplier will be the smaller of  the multipliers (e.g., if you had `"{TARGET}": 0.5, "Locals: 0.9`, the final reputation change multiplier would be 0.5).

`storeDiscount` - Dictionary <string, float> - Adjustments to store purchase discount for listed factions. Stacks with reputation discounts/markups, so values should be negative to decrease prices and positive to increase prices. Changes to discount/markup are reflected in the shop screen.

`storeBonus` - Dictionary <string, float> - Adjustments to store selling prices. Stacks with difficulty setting (e.g. sell price = 15% of item value), so values should be positive to increase selling prices. Changes to sell price are reflected in the shop screen.

`cashMult` - Dictionary <string, float>, Multiplier added to contract payout if `applyToFaction` contains the contract target and the employer is either explicitely listed as the key, or `{"EMPLOYER"}` is listed as a key (in which case, `cashMult` is applied for all contracts against the target faction). Using above settings, if base contract payout vs Locals was ¢10,000, final contract payout would be ¢12,000 (1 + 0.2 from `cashMult`), and would apply for all employers.

`killBounty` - Dictionary <string, int>, per-kill c-bill bounty awarded at contract resolution if `applyToFaction` contains the contract target and the employer is either explicitely listed as the key, or `{"EMPLOYER"}` is listed as a key (in which case, `killBounty` is applied for all contracts against the target faction). Using the above settings, getting 5 kills against Locals would award you a ¢12500 bonus <b>if</b> the contract employer is Liao.

In all cases, simgame effects of multiple specializations (e.g. from multiple pilots with the same specialization) <b>do stack</b>.

### MissionSpec

MissionSpecs, or Mission Specializations are unique bonuses awarded to pilots that only apply during their respective mission types.

Available MissionSpecs for a given contract type can be seen in the contract tooltip in the Command Center. The number in brackets indicates the number of missions required for the MissionSpec to be awarded:

![TextPop](https://github.com/ajkroeg/PracticeMakesPerfect/blob/master/doc/availableMission.png)

example json structure for MissionSpecs is as follows:

```

{
	"MissionSpecID": "DamageIncrease",
	"MissionSpecName": "Damage Increased",
	"missionsRequired": 10,
	"contractTypeID": "UNIVERSAL",
	"description": "This pilot loves the simplicity of a good brawl, and deals more damage to Mechs and Vehicles during {contract} contracts.",
	"cashMult": 0.1,
	"AdvTargetInfoUnits": [	"Mech", "Vehicle"],
	"effectDataJO": []
}

```

`MissionSpecID` - string, ID for this MissionSpec

`MissionSpecName` - string, human-friendly name for this MissionSpec

`missionsRequired` - int, number of completed contracts of this type for the specialization to be awarded. As with OpForSpecs, will never be awarded if set to 0 except through tags via `taggedMissionSpecs`

`contractTypeID` - string, contract type for which progress is tracked and for which effects are applied when specialization is awarded; e.g. SimpleBattle, DuoDuel.

`description` - human-legible description for this specialization. Similar to OpforSpecs, `MissionSpec` in `MissionDefaultList`, the string {contract} will be replaced with `ContractTypeValue.Name`: e.g., for `"description": "This pilot loves the simplicity of a good brawl, and takes less damage during {contract} contracts.",` , the description in-game would read "This pilot loves the simplicity of a good brawl, and takes less damage during SimpleBattle contracts."

`cashMult` - float, as with MissionSpecs, provides a bonus modifier added to contract payouts for this contract type.

`AdvTargetInfoUnits` - List<string>, Defines "Advanced Targeting info" that only applies in-contract effects to certain units in conjunction with effect `targetingData`. Allowed values are: `Primary, NotPlayer, Mech, Vehicle, Turret, Building`

`Primary` - effects apply for Primary mission targets for the contract type. E.g., for assassination contracts, the effects would be applied only to the assassination target if the `"effectTriggerType": "Passive"` and `"effectTargetType": "AllEnemies"`. Similar to "Active" vs "Passive" effects as discussed for OpForSpecs, using `"effectTriggerType": "OnWeaponFire"` applies the effect only when weapons are fired, and should be used in conjunction with durationData to ensure the effect expires immediately after firing.

`NotPlayer` - effects apply to non-player factions. Only implemented in conjunction with `"effectTargetType": "AllEnemies"` (impacts all factions in three-way battles, including friendlies!).

`Mech, Vehicle, Turret, Building` - effects apply to the unit type in question. Buildings are a special case, as they are not considered AbstractActors; as such they do not have the same statistics such as `DamageReductionMultiplierAll`. In order to conditionally adjust building damage, it is necessary to adjust the damage <i>dealt by the attacking unit</i>, as in the following example.

```
{
			"MissionSpecID": "DefendBase_Bulwark",
			"MissionSpecName": "Defend Base: Bulwark Writ Large",
			"missionsRequired": 10,
			"contractTypeID": "DefendBase",
			"description": "Friendly building damage reduction",
			"AdvTargetInfoUnits": [
				"Building"
			],
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
						"effectTargetType": "AllEnemies",
						"showInStatusPanel": true
					},
					"effectType": "StatisticEffect",
					"Description": {
						"Id": "DB_DR",
						"Name": "Damage Decreased",
						"Details": "Friendly building damage reduction",
						"Icon": "hooded-assassin"
					},
					"nature": "Buff",
					"statisticData": {
						"statName": "DamagePerShot",
						"operation": "Float_Multiply",
						"modValue": "0.8",
						"modType": "System.Single",
						"additionalRules": "NotSet",
						"targetCollection": "Weapon",
						"targetWeaponCategory": "NotSet",
						"targetWeaponType": "NotSet",
						"targetAmmoCategory": "NotSet",
						"targetWeaponSubType": "NotSet"
					}
				}
			]
		},
```

In the above example, friendly buildings effectively take 80% damage from opponents due to the effect applying ```"targetingData": {
						"effectTriggerType": "OnWeaponFire",
						"effectTargetType": "AllEnemies",
						"showInStatusPanel": true
					},```

 If you wanted to <i>increase</i> damage your pilot deals to buildings, you could use `"effectTriggerType": "OnWeaponFire",` and `"effectTargetType": "Creator",` with  ```"statisticData": {
	"statName": "DamagePerShot",
	"operation": "Float_Multiply",
	"modValue": "1.2",```
 
 
`effectDataJO` - List of standard format `effectData` for this specialization.

### StratCom

StratComs are unique bonuses that apply to your pilots <i>only when your commander is not on the field</i>. We all know Darius is pretty useless, so it might make sense for there to be strategic-level advantage for the commander to focus on...well, commanding, instead of just blasting the OpFor.

Players can select the active StratCom by holding Control while clicking the "Service Record" tab in the Barracks when the Commander pilot is selected.

The first popup will ask the player if they want to Change StratComes or Reset Specs:
![TextPop](https://github.com/ajkroeg/PracticeMakesPerfect/blob/master/doc/changeorreset.png)

If "Change StratComs" was selected, the following popup lets the player choose the active stratcom:
![TextPop](https://github.com/ajkroeg/PracticeMakesPerfect/blob/master/doc/selectActive.png)

Otherwise, if "Reset Specs" was selected, the popup is as for a normal pilot.

The currently selected StratCom can be seen by hovering over the commanders portrait in the barracks:

![TextPop](https://github.com/ajkroeg/PracticeMakesPerfect/blob/master/doc/stratcom.png)

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
	"DebugCleaning": false,
	"OpforSubfactionsMap": {
		"ClanWolf": [
			"CWEpsilonGalaxy"
		]
	}
}
```

`MaxOpForSpecializations` - int, maximum number of OpFor specializations pilots can have.

`OpForTiersCountTowardMax` - bool, defines whether "tiers" of specializations count toward max (e.g., if you define 2 specializations to be awarded for killing AuriganPirates, one at 25 kills and one at 50 kills; if `OpForTiersCountTowardMax: true` both specializations would count towards the cap. If `OpForTiersCountTowardMax: false`, only one would count towards the cap (allowing you to specialize against `MaxOpForSpecializations` factions total).

`MaxMissionSpecializations` - int, maximum number of Mission specializations pilots can have.

`MissionTiersCountTowardMax` - bool, same as `OpForTiersCountTowardMax`, just for Mission specializations.

`TaggedOpforSpecsCountTowardMax` - bool, do OpForSpecs awarded via pilot tags count toward the max.

`TaggedMissionSpecsCountTowardMax` - bool, do MissionSpecs awarded via pilot tags count toward the max.

`MissionSpecSuccessRequirement` - int, controls whether a mission needs to be successful to count toward MissionSpec progress. if 0, all missions outcomes count (even bad faith effort or total failure). if 1, good faith efforts count.  if 2, only a mission success counts.

`WhiteListOpFor` - List<string>, List of OpFor factions (e.g., Liao, AuriganPirates, ClanWolf) for which specializations will be created from `OpForDefaultList` and for which progress towards specializations will be tracked.
	
`WhiteListMissions` - List<string>, List of contract types (e.g., SimpleBattle, CaptureBase, DuoDuel) for which specializations will be created from `MissionDefaultList` and for which progress towards specializations will be tracked.
	
`OpForDefaultList` - List<OpforSpec> - list of default OpFor specializations. These OpForSpecs are created for every faction in `WhiteListOpFor`.

`MissionDefaultList` - List<MissionSpec> - list of default Missions specializations. These MissionSpecs are created for every Mission type in `WhiteListMissions`.
	
`OpForSpecList` - List<OpForSpec> - list of specific OpFor specializations. These should be unique to specific factions. OpForSpec in this list with `killsRequired: 0` will only be awarded via tags.
	
`MissionSpecList` - List<MissionSpec> - list of specific Mission specializations. These should be unique to specific Mission types. MissionSpec in this list with `missionsRequired: 0` will only be awarded via tags.
	
`StratComs` - List<StratCom> - list of StratCom bonuses your pilots will recieve when commander is <i>not</i> deployed.

`taggedOpForSpecs` - Dictionary<string, List<string>> - KeyValuePairs where Key = pilot tag and Values = list of `OpForSpec.OpForSpecID` of the OpForSpecs to be awarded to pilots with the tag.

`taggedMissionSpecs` - Dictionary<string, List<string>> - KeyValuePairs where Key = pilot tag and Values = list of `MissionSpec.MissionSpecID` of the MissionSpecs to be awarded to pilots with the tag.

`argoUpgradeToReset` - string, company tag associated with argo upgrade required to reset specializations, e.g "argo_trainingModule3". If null, specializations can always be reset.

`dummyOpForStat` - string, name of dummy stat used to create fake status effect for pilots with OpForSpecs that use OnWeaponFire

`DebugCleaning` - bool. Debugging only! if set to `true`, will wipe all specializations on save load.

`OpforSubfactionsMap` - dictionary <string, List<string>> - defines subfactions which will count toward OpForSpecs for the "key" faction. Such OpforSpecs will also apply to these subfactions regardless of `factionID` and `applyToFaction` settings within the OpforSpec