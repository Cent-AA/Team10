# Prototype Arena Mechanics

These scripts are attached only to `Assets/Scenes/TestArena_PrototypeMVP.unity`.

- `PrototypeCampfireHealth` gives the campfire HP and lets nearby zombies damage it.
- `PrototypeCardRewardManager` pauses after each completed wave and offers 3 upgrade cards.
- `PrototypeEnemyVariantManager` turns regular zombies into Grunt, Runner, Tank, Exploder, and wave-5 MiniBoss variants.
- `PrototypeClassRoleTuner` gives the current two-player setup clearer combat roles.
- `PrototypeRunStats` tracks waves, kills, picked cards, and shows the game-over result.
- `PrototypeEngineerBuilder` gives Engineer turret/dispenser building: Shoot builds a turret, Block+Shoot builds a dispenser.
- `PrototypeTurret` auto-fires at nearby zombies.
- `PrototypeDispenser` heals nearby players.
- `PrototypeReviveManager` / `PrototypeReviveTarget` implement Nightreign-style revive by damaging a downed ally. Revive cost increases after every death, and revival restores 30% HP.

Keep this folder separate until the mechanics are approved for the main arena.
