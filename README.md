# PracticeMakesPerfect
Mod allows Commander and pilots to gain bonus XP from kills and/or damage dealt during contract. Also connects PilotDef kill stats (as displayed in barracks) to Pilot stats accessible by events.

Current settings in mod.json:
```
"Settings": {
			"bonusXP_MechKills": 100,
			"bonusXP_OtherKills": 50,
			"bonusXP_StrDamageMult": 0.05,
			"bonusXP_ArmDamageMult": 0.05,
			"bonusXP_CAP": -1
			},
```

`bonusXP_MechKills` - bonus XP given per Mech kill. Set to 0 to disable.

`bonusXP_OtherKills` - bonus XP given per Other kill (vehicles/turrets). Set to 0 to disable.

`bonusXP_StrDamageMult` - multiplier for bonus XP given per Structure damage inflicted (e.g., for 200 structure damage inflicted, award 10 XP). Set to 0 to disable.

`bonusXP_ArmDamageMult` - multiplier for bonus XP given per Armor damage inflicted (e.g., for 200 armor damage inflicted, award 10 XP). Set to 0 to disable.

`bonusXP_CAP` - maximum amount of XP awardable for kills and/or damage. Set to -1 to disable cap, set to 0 to disable <i>all</i> bonus XP.

New pilot stats accessible through events, etc. as follows:

`TotalMechKills` - Number of mechs pilot has killed in total

`TotalOtherKills` - Number of vehicles/turrets pilot has killed in total

`TotalKills` - because math is hard

`TotalInjuries` - total number of injuries pilot has ever sustained
