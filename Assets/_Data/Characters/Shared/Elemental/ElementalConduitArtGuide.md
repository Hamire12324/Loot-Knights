# Elemental Conduit Art Guide

## Gameplay Flow

1. Enemy dies and drops ElementalShardPickup objects.
2. Btn_ElementAbsorb pulls nearby shards toward the hero.
3. ElementCoreMeter shows stored elements and stacks.
4. Btn_Skill_ElementConduit releases the stored element or reaction.

## Required Graphics

### 1. Elemental Shard Pickup Prefab

Create one prefab for the dropped shard and assign it to EnemySpawner > Elemental Shard Prefab.

Required components:
- ElementalShardPickup
- Collider2D with Is Trigger enabled
- SpriteRenderer for the shard icon/core

Recommended child objects:
- Glow sprite or small aura
- Idle ParticleSystem
- TrailRenderer or particle trail for absorb movement

The script recolors SpriteRenderer and ParticleSystem by element, so one neutral white/cyan sprite can support Fire, Frost, Lightning, and Poison.

### 2. Absorb Feedback VFX

This is played when a shard reaches the hero.

Assign to HeroSkillElementalConduitEffect:
- Feedback > Absorb Vfx for default
- Feedback > Element Vfx Overrides > Absorb Vfx for per-element visuals
- Feedback > Absorb Sfx or per-element Absorb Sfx

Visual idea:
- Small ring flash around the hero core
- Short inward sparkle burst
- Element-colored particles

### 3. Release Impact VFX

This is played when the Elemental Conduit skill hits targets.

Assign to HeroSkillElementalConduitEffect:
- Feedback > Impact Vfx for default
- Feedback > Element Vfx Overrides > Impact Vfx for Fire/Frost/Lightning/Poison
- Feedback > Reaction Vfx Overrides > Impact Vfx for reactions

Visual idea:
- Fire: orange/red cone burst, ember particles
- Frost: cyan shard burst, mist ring
- Lightning: violet arc snap, sharp flash
- Poison: green splash cloud, lingering motes

### 4. Element Core UI

Keep this as hand-placed scene UI. Do not rely on runtime rebuild for final layout.

Expected object name:
- ElementCoreMeter

Recommended children:
- CoreBackground
- CoreAccent
- ElementMeterTitle
- ElementMeterText
- ElementSlot_1, ElementSlot_2, ElementSlot_3, ElementSlot_4
- StackText under each slot

The script only refreshes content/state. Position and size should be edited by hand in the scene.

### 5. Buttons

Required gameplay buttons:
- Btn_ElementAbsorb: pulls shards into the hero.
- Btn_Skill_ElementConduit: releases stored element/reaction.

Recommended visuals:
- Absorb button: magnet/vortex/core-pull icon.
- Release button: elemental core blast icon, not a sword/normal attack icon.

## Final Art Checklist

- Assign Elemental Shard Prefab on every EnemySpawner that should drop shards.
- Assign absorb/release VFXDefinition assets on HeroSkillElementalConduitEffect.
- Assign SFXDefinition assets if sound is ready.
- Disable Play Procedural Element Vfx when custom VFX are final.
- Keep ElementCoreMeter and buttons manually positioned in the scene.
