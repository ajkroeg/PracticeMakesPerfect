# PracticeMakesPerfect
Mod allows Commander and pilots to gain bonus XP from kills and/or damage dealt during contract. Also connects PilotDef kill stats (as displayed in barracks) to Pilot stats accessible by events.

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
			},
```

`useMissionXPforBonus` - bool, switch for whether bonus XP is awarded as a function of the contract XP or as flat bonus.

`bonusXP_MissionMechKills` - multiplier for bonus XP as function of Mission XP, per mech kill. Using settings above, if contract awards 1000XP and pilot gets 2 mech kills, they recieve 100 bonus XP (1000 * .05 * 2 = 100)

`bonusXP_MissionOtherKills` - as above, but for vehicles/turrets.

`bonusXP_MechKills` - int, bonus XP given per Mech kill. Set to 0 to disable. Disabled if `useMissionXPforBonus` true.

`bonusXP_OtherKills` - int, bonus XP given per Other kill (vehicles/turrets). Set to 0 to disable. Disabled if `useMissionXPforBonus` true.

`bonusXP_StrDamageMult` - float, multiplier for bonus XP given per Structure damage inflicted (e.g., for 200 structure damage inflicted, award 10 XP). Set to 0 to disable. <b>not affected by `useMissionXPforBonus`</b>.

`bonusXP_ArmDamageMult` - float, multiplier for bonus XP given per Armor damage inflicted (e.g., for 200 armor damage inflicted, award 10 XP). Set to 0 to disable. <b>not affected by `useMissionXPforBonus`</b>.

`bonusXP_CAP` - int, maximum amount of XP awardable for kills and/or damage. Set to -1 to disable cap, set to 0 to disable <i>all</i> bonus XP.
__________________________________________________________

New pilot stats accessible through events, etc. as follows:

`TotalMechKills` - Number of mechs pilot has killed in total

`TotalOtherKills` - Number of vehicles/turrets pilot has killed in total

`TotalKills` - because math is hard

`TotalInjuries` - total number of injuries pilot has ever sustained
