# PracticeMakesPerfect
Mod allows Commander and pilots to gain bonus XP from kills and/or damage dealt during contract. In addition, allows bonus XP to be awareded by combat actions as defined below.

Also connects PilotDef kill stats (as displayed in barracks) to Pilot stats accessible by events.

Current settings in mod.json:
```
	"Settings": {
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

`activeProbeXP_PerTarget` - bool, controls whether activeProbeXP bonus is awarded per-target in probe radius. For example, if `activeProbeXP` is set to 25, and 3 targets are affected when a player unit actives Active Probe, that unit would receive 75 XP.

`activeProbeXP` - int, XP awarded to the player for using Active Probe.

`sensorLockXP` - int, XP awarded to player for using Sensor Lock ability.

`missionXPEffects` - float, multiplier for bonus XP awarded for effects as a function of Mission XP. Functions in tandem with `missionXPeffectBonusDivisor` below to approximate a per-effect bonus.

`missionXPeffectBonusDivisor` - int, divisor of <i>total bonus XP awarded via the dictionaries below, or `effectXP`</i> in order to approximate a per-effect XP bonus based on contract XP. The resulting bonus follows the formula `ContractXP * missionXPEffects * effectXP / missionXPeffectBonusDivisor`. Thus, for 3 effects defined in the dictionaries with flat-rate bonuses of 35, 50, and 75, a `missionXPeffectBonusDivisor` value of 50 to 55 would give be recommended.

`reUseRestrictedbonusEffects_XP` - Dictionary<String, int> - Effects and corresponding XP rewards for the player unit applying those effects. XP bonus for effects in this dictionary are limited to being applied once per source-target pair. For example, if the TAG effect is listed in this dictionary, PlayerMech1 can only recieve a single TAG XP bonus for TAG-ing EnemyMech1 <i>until that effect expires</i>, at which point they would be able to recieve TAG XP for EnemyMech1 once again. However, PlayerMech1 <i>would</i> be able to recieve TAG XP for applying TAG to, for example, EnemyMech<b>2</b>. However, PlayerMech<b>2</b> could also receive TAG XP bonus for tagging EnemyMech1, even if EnemyMech1 had an active TAG effect from PlayerMech1.

`degradingbonusEffects_XP` - Dictionary<String, int> - similar to above. Effects in this dictionary award the full amount when first applied, and then a penalized amount if re-applied, changing as a function of rounds remaining until effect expiration. For example, if the TAG effect has a Duration of 3 activations, the first time TAG is applied, it awards the amount defined in the dictionary. If it is then reapplied the following activation, the XP awarded is base/3. If it was instead reapplied 2 turns later, the XP award would be would be base/2, etc.

`bonusEffects_XP` - Dictionary<String, int> - Effects and corresponding XP rewards for player unit applying those effects. Effects in this dictionary are <b>not</b> subject to same limitations as above; full XP is rewarded every time the effect is applied, regardless of source and target.



To clarify use of `reUseRestrictedbonusEffects_XP` and `bonusEffects_XP`, the following is an excerpt of the vanilla WeaponDef for `Weapon_PPC_PPC_0-STOCK`

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
Bonus XP can be defined by either the effectType.Description.Id (`AbilityDefPPC` above), OR by the effectType.statisticData.statName (`AccuracyModifier`). The mod will check the dictionary first for a matching `Id` before checking for a matching `statName`. This was done to allow some flexibility, as some stats will be unique to certain mods, while others like AccuracyModifier are used in many places where you may not want XP awarded. If a match is found in both `Id` and `statName`, XP rewards will be duplicated.

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
