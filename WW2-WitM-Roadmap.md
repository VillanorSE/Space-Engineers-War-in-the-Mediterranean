# WW2 War in the Mediterranean — Roadmap & To-do List

War in the Mediterranean is a scenario for Space Engineers themed on the World War 2 Mediterranean conflict.
The scenario is primarily based on three major components: 
1. A custom planet that wraps the Mediterranean and nearby land around a 60km diameter planet with roads between point of interest locations. 
2. A Modular Encounters System (MES) mod containing planes, ships, installations, and ground vehicles that are WW2 recreations. These are primarily Italian and French but intended to be expanded on.
3. A framework mod that includes respawn vehicles, mod adjustments, block restrictions, ship core profiles, and a wing rebalance. 

This project is heavily inspired by mechanics in GV Deserts of Kharak by Mike Dude and Ares at War by Cpt Arthur.

The project is designed to be phased so that expansion can be done with discrete playable milestones along the way.

The scenario is designed primarily as a player vs. environment (PvE) experience but has had consideration all throughout for player vs. player vs. environment (PvPvE).

---

## Prefab naming convention (confirmed)

Two parallel prefab families per hull, distinguished by purpose rather than just faction/class:

- **`Player-WW2-[name]`** — configured for player use (includes landing gear). Used at hangars, garages, factories, and stores — the buyable and stealable grids
- **`NPC-WW2-[name]`** — used for NPC-controlled encounters.

---

## Prerequisite — Existing Grid Rebuilds

All grids were rebuilt from the older version of this project to update away from legacy mods that are no longer supported.

## Current Primary Mod Dependencies 

There is a full modlist for this project but the primary functional mods are listed below:

- **Naval weapons:** [Fletcher Armaments – WeaponCore Edition](https://steamcommunity.com/sharedfiles/filedetails/?id=2844434226) (Workshop 2844434226) — WeaponCore-based WW2 naval weapon pack.
- **Air weapons:** [Consty Aircraft Pack – Ordnance (WeaponCore) 1.0](https://steamcommunity.com/sharedfiles/filedetails/?id=2881339118) by Const (Workshop 2881339118)
- **Ground vehicle weapons:** [Yakobe's Machinations] (steam link needed) (Workshop id)

 - **SETB Community Tank Parts** — Small grid armor blocks and decorative components.
  - **ArmourEssentials** — Mostly turret ring rotors, gunsight cameras, and rangefinders.
  - **Yakobe's Machinations** — wheels/suspension sourced from CWP, plus additional guns.
 - **Plane Parts** — Several plane component mods for wings, propellers, landing gear, and more.

- **Dependency additions:** 
- WeaponCore
- Water Mod
- Ship Core: Workshop 3552595651
- Mod Adjuster

**To-do:**
- [x] Configure BlockRestrictions to disable all blocks that are not to be available to players.
- [x] Configure ShipCores for block limits
- [x] Update G menu to not show empty groups or blank spaces
- [x] Test all Factory spawns for placement
- [x] Resolve naval spawns not moving along paths after spawn
- [ ] Resolve CrashAir not working as intended
- [x] Create Fiat 626 player respawn truck (150% scale)
- [x] Create AHN player respawn truck (150% scale)
- [x] Configure basic features on port installations
  - [x] Selling grids of that faction
  - [x] Spawning cargo ships that travel to the other port
  - [x] Defensive spawn
- [x] Adjust threat level and similar spawn controls on Bearn and Aquila, spawning when not expected
- [x] Create GPS Routes for travel between existing port locations
- [x] Assign component costs to cores (does not include advanced Naval or Ground cores yet)
- [x] Assign component costs to advanced propellers
- [x] Assign component costs to advanced wheels (5x5)
- [x] Assign component costs to weapons
- [ ] Figure out store block problems and add all basic items plus progression components to the stores.
- [x] Get respawn rovers to show up in respawn menu
- [x] Add industrial component cost to the upgrade modules for production blocks
- [x] Check Aquila for old guns (had blueprint in console block)
- [x] Remove old ammo from prefab cargoes
- [x] Merge in Gibraltar Topography
- [x] Make mountains, especially in Italy and Greece less "spire"like
- [x] Get Static spawns to properly spawn, figure out why they are despawning.
- [ ] Create neutral trade post installation, port at Valencia. 

**Rebuilds:**

  **Naval:**
  - [ ] NPC-WW2-Tramontane (Destroyer/Le Fantasque-class, Green)
  

 **Installations:**
  - [x] NPC-WW2-Ammo-Depot
  - [x] NPC-WW2-Hangar
  - [x] NPC-WW2-Factory-Plane
  - [ ] NPC-WW2-Garage (create, never actually made)

- [ ] Test all MES components end-to-end to confirm baseline features (spawning, behaviors, triggers) work after the recent rebuild work.

---

## Overview

Two factions spawn planes, ships, ground vehicles, and installations. These factions are Green, for all allied forces, and Gray for all axis forces. Currently Green is only French and Gray is only Italians.
There are six primary locations that are invulnerable and are the anchors for territory control for their respective factions. 
**Green:**
1. Toulon: Main headquarters
2. Oran: Major port
3. Alexandria: Major port
**Gray:**
1. La Spezia: Main headquarters
2. Tripoli: Major port
3. Rome: Major port

Permanent installations will be located across other major points of interest and their ownership is determined by faction territory and can change over time.

Interactions between NPCs and between players and NPCs will effect the overall war level (a custom counter in MES) and territory size. NPC activity and overall strength of spawns will be driven by war level. 

All grids are defined by ship cores which determine the maximum block and PCU amounts as well as limits functional blocks across numerous categories. Grids are either static (installations), air, naval, or ground. Only static and naval grids are large grids. Static grid cores, utility/civilian grid cores, and the first combat grid core in each category only costs basic components to construct. Advanced combat cores, and eventually upgrades to cores, require special components that are acquired primarily through loot and purchase. 

## Special Components

War in the Mediterranean has three special component types that restrict player access to various blocks. 

**Industrial Components**
Industrial components are used for good non-combat blocks. Yield and speed upgrade modules, the strongest air and naval propellers, and the largest wheels all require industrial components. 

Industrial components may be used to allow players to make advanced tools or automated construction in later stages. Large and small grid hangar block may be added in later stages to allow utility ground vehicles to carry a single plane, carriers to carry many aircraft, and installations to hold all small grids. These would use industrial components too.

**Military Components**
Military components are used to construct attacker plane cores, light and medium tank cores, and destroyer cores. Military components are also used to make stronger weapons in the "Basic Weapons" category and the weaker weapons in the "Weapons" category. 

**Advanced Military Components**
Advanced military components are used for cruiser, battleship, and carrier naval cores, heavy tank cores, and bomber cores. Advanced military components are also used for the strongest weapons in the "Basic Weapons" and "Weapons" categories. 

## War Level

War level is a custom counter variable in the Modular Encounter System that represents the overall intensity of the war. Every NPC attacked or destroyed, by player or other NPC, contributes to an increase in a counter. War level is set by different intervals of that counter. Every NPC that despawns without being disabled or attacked reduces that counter.

More dangerous NPCs spawn at higher war levels. Cargo ships begin having escorts, fighters start spawning in squadrons, cruisers become more common, battleships and carriers begin to appear.

## Territory

Each of the six primary locations is an anchor for territory for its faction. The territory of a faction functions the same everywhere but grows and contracts separately per primary location.

Territory has six different levels that each correspond to a different radius applied along the surface of the planet. The six levels are 8/13/19/26/34/43 km. The first territory to cover an installation controls it. Control changes when a new faction territory covers the installation and the previous owner's territory no longer does. 

## Reputation

Damaging grids from one faction reduces a player's reputation with that faction and improved it to a lesser amount with the other faction. Destroying an NPC grid has a large impact in the same way. Nearby players of the same faction share these reputation changes and their faction gets reputation changes the same way.

Players can ally with either faction with sufficient reputation. Being allied to a faction allows players to buy and sell at their locations, use their functional installations, and eventually take on contracts with that faction.
Sourced from AaW's `_FAC` REPSystem (`FAC-Context-REPAHE.sbc` and per-faction siblings): a hostile unit's death (`Type:Compromised`) checks `CheckCustomCounters:CountPlayerDamage >= 15` before awarding reputation with the beneficiary faction (i.e., killing something a faction also considers hostile improves standing with them, "enemy of my enemy"), radius-shared so nearby faction-mates of the killer get credit too.
- Radius-share the reputation gain to nearby faction members of the credited player, matching AaW's `ReputationChangesForAllRadiusPlayerFactionMembers`.

**Faction Currency**
- [ ] Design and wire a currency-reward mechanic tied to successful cargo ship/plane deliveries (or similar), so GRAY/GREEN can meaningfully grow richer through play rather than just sitting at their starting balance.

## Stage 1 — Baseline Scenario

**Goal:** Get the basic scenario built and functioning. 

Major components: 
1. Fully built planet with a settled heightmap (mountains will get further tweaking eventually) so players don't get disrupted by future planet improvements.
2. Baseline MES mod functioning with:
    1. Randomly spawning hangars that contain stealable planes. 
    2. Randomly spawning plane factories that contain stealable planes, store blocks that sell the corresponding plane, defensive plane spawns, and offensive fighter spawns that occur on a variable timer. They also serve as a destination for cargo planes. When cargo planes arrive, projected blocks are built.
    3. Four static ports that are invulnerable; Toulon, Oran, La Spezia, and Tripoli. These ports spawn cargo ships that transit to the other ports of their faction. They have a store block to sell all player variants available for the faction. They have defensive air spawns.
    4. Randomly spawning naval encounters that are stationary until a target is close enough to trigger their engage behavior. Includes Corvettes, destroyers, cruisers, and aircraft carriers. 
    5. Randomly spawning cargo planes that fly a route and despawn. They divert to installations that call to them. Source of components for players.
    6. Randomly spawning combat planes. Fly a few waypoints and will engage nearby targets. 
    7. Randomly spawning ammo depot containing randomized ammo types with defensive guns.
3. Ship Cores with no specialized costs for Bases, Outposts, Static miners, civilian/utility class trucks, planes, and ships.
4. Combat Ship Cores for Armored Cars, Corvettes, and Fighters with basic costs. Combat ship cores with advanced costs for destroyers, attackers, and bombers.
5. Configured framework consisting of block restrictions to limit blocks to WW2 appropriate, including removal of thrusters and reactors, specialized components to gate progression blocks, adjusted costs for progression items, respawn vehicles, and a wing rebalance.

## Primary Installations (Domain anchors)

Primary Installations are six permanent, invulnerable, fixed ownership locations. Three locations per faction (Gray: La Spezia, Rome, Tripoli; Green: Toulon, Oran, Alexandria). Their functionality/stock scales with War Level. Ground and naval vehicles are available for purchase; each anchor sells its faction's complete current ground/naval catalog, gated by War Level rather than by regional stock. **Aircraft are not sold here** Airports are a separate tier of installation, not part of the domain-anchor set.

**Store Functions** each of the six primary locations runs a full, vanilla-style Store block that allows players with 500+ reputation with the faction to buy and sell ores, ingots, components, tools, and other physical items there the same way they would at a vanilla NPC Trading Outpost. These use the same underlying pricing mechanism as vanilla (multiplier chains through ore→ingot→component, Economy Tick updates).

**Component availability**
Industrial, Military, and Advanced Military Components are deliberately NOT sold at any Store - that progression tier is loot/craft-only (refine Battle Wreckage into Salvage Alloy, then build the components), never purchasable with credits.
 
**Neutral trade station**
There is a neutral installation usable by anyone, with no reputation threshold at all. Owned by a new faction, **Traders** (tag **TRADER**), whose relation to both Gray and Green is set to **Neutral** not allied with either, matching vanilla's Neutral behavior (no automatic turret hostility toward either faction). Located on the east coast of Spain.

## Secondary Installations (Airports, Vehicle Depots, Garrisons)

Secondary installations are permanent and invulnerable but **not** faction-fixed, ownership is determined by the territory-ownership mechanics.
Secondary installations have restricted zones around them for neutral and hostile players and factions. They sell aircraft or ground vehicles based on installation type, restricted to whichever faction currently owns that installation and current war level.
Candidate locations identified, build order/priority not yet decided:
**Airports**
  - Foggia, Tripoli-Castel Benito, Benina (Benghazi), Gibraltar, La Senia, Tunis/Bizerte, Cairo West, Casablanca
**Vehicle Depots**
- Mantua, Abruzzo (Combined with Rome), Benghazi, Tel-el-kebir (combined with Alexandria), Tunis, Mascara
**Garrisons**
- Tunis, Mersa Matruh, Tobruk, Tripoli

**Airports**
Have restricted areas where neutral and hostile players lose reputation for lingering. Spawn defensive AI bots when players approach. Spawn defensive spawns when neutral or hostile players get close. Spawn cargo planes periodically. Spawn hostile combat planes periodically that roam and target players and NPCs.

War Level integration: Stronger offensive and defensive spawns with level and more numerous squadrons. More cargo planes per spawn and with escorts.

**Vehicle Depots**
Have restricted areas where neutral and hostile players lose reputation for lingering. Spawn defensive AI bots when players approach. Spawn defensive spawns when neutral or hostile players get close. Send signal that spawns cargo trucks at the nearest port periodically that drive to the depot. Spawn hostile ground vehicles periodically that patrol around the depot and target players and NPCs.

War Level integration: Stronger offensive and defensive spawns with level and more numerous squadrons. More trucks per spawn and with escorts.

**Garrisons**
Have restricted areas where neutral and hostile players lose reputation for lingering. Spawn defensive AI bots when players approach. Spawn defensive spawns when neutral or hostile players get close.

War Level integration: Larger counts of defensive bots with level. 

  **Terrain fitting, confirmed technique:** MES's voxel-spawning capability can be used to carve/scoop the correct berth/slip shapes and level the surrounding ground so the Port anchor's pier, construction slip, and quay structures sit correctly into planet terrain, rather than requiring hand-sculpted terrain to already exist at the anchor's fixed coordinates before the installation is placed.

## Logistics Installations

Logistics installations are dynamically spawning, non-permanent, non-capturable installations that only spawn within a faction's territory. They include plane and vehicle factories, gas supply stations, supply depots, ammo depots, hangars, garages, and barracks. 

**Hangars & Garages**
Hangars and garages are a basic installation that contain one or two vehicles and have a light guard complement of Ai enabled bots. They primarily serve as a place for players to steal ready-built grids rather than designing their own.

War Level integration: Higher war levels allow stronger fighters, attackers, and bombers to spawn. Each vehicle will have a corresponding minimum war level.

 **Factories**
Factories contain several vehicles of the same type and store blocks that sell the same vehicle. They have AI bots for defense, spawn defensive units when neutral or hostile players approach and persist for a long time or until heavily damaged. 
Factories enter a "production mode" when players first approach. In this mode they spawn offensive fighters on a variable timer. Factories also call to friendly cargo planes and build projected blocks when cargo planes successfully deliver to them.

War Level integration: Higher war level allows factories for stronger planes to spawn (attackers, bombers, better fighters). Weaker stackers may spawn at lower levels and stronger attackers and bombers at higher war level. Each factory will have a corresponding minimum war level to spawn.

- Note: move offensive air spawns to airfields and remove from factories. Also resolves challenges related to factories despawning quickly or persisting too long and preventing other spawns.

**Supply Depots**
Supply depots have cargo containers with large volumes of basic components and small amounts of ammo. They have store blocks (or NPC dummies as "quartermasters" with industrial components for sale. They occasionally have small amounts of military components for sale too. They have AI defender bots, call defensive spawns of planes or vehicles and call cargo planes.

War Level integration: Higher war levels will increase volume of components, number of AI defenders, and quality/quantity of defensive spawns.

- Note: see about making supply depots have more components for sale and refill cargo containers after cargo planes successfully drop off.

**Ammo Depots**
Ammo depots have cargo containers with large volumes of ammunition. They have store blocks (or NPC dummies as "quartermasters" with military components and small amounts of advanced military components for sale. They have larger numbers of AI defender bots, have emplaced defenses and call cargo planes.

War Level integration: Higher war levels will increase volume of ammunition, number of AI defenders, and quality/quantity of defensive spawns.

- Note: see about making ammo depots have more components for sale and refill cargo containers after cargo planes successfully drop off.

### Naval Roster 

| Tier | Gray (Italy) | Green (France) |
|---|---|---|
| **Utility** | Golo (Italian variant) | Golo (French variant) |
| **Corvette** | Gabbiano | La Malouine |
| **Destroyer** | Francesco Crispi, Comandante Margottini, Spica (Torpedo Boat), Turbine, Carabiniere (Soldati class) | Bougainville (Aviso/Destroyer-behavior), Léopard (Chacal class) | Le Triomphant, Tramontane (Bourrasque class) |
| **Cruiser** | Bartolomeo Colleoni (Giussano-class,), Luigi Cadormo | Emile Bertin, Duguay-Trouin, La Galissonnière |
| **Heavy Cruiser** | Trento, Zara | Algérie, Colbert (Suffren class) |
| **Battleship** | Vittorio Veneto (Littorio-class) | Strasbourg (Dunkerque-class) |
| **Carrier** | Aquila | Bearn |
| **Submarine** | Adua class | Surcouf |

---

### Air Roster

| Tier | Gray (Italy) | Green (France) |
|---|---|---|
| **Utility** | Ju52 | F.222 (cargo variant) |
| **Fighter** | Re2001, Re2001 (naval), Re2000, Macchi C.202 Folgore | MS406, Dewoitine D.520 |
| **Attacker** | Ba.65, Ba.88, FC20 | ANF Les Mureaux 115, Bréguet 693, Potez 630 |
| **Bomber** | Caproni Ca.311, SM.79 (bomber & torpedo), P.108 | V-156-F (bomber & torpedo), MB.210, LeO 451, F.222 (bomber) |
| **Recon** | IMAM Ro.43 | Loire 130 |

---

## Ground Roster 

| Tier | Gray (Italy) | Green (France) |
|---|---|---|
| **Utility** | Fiat 626, SPA Dovunque 35 | Renault AHN, Laffly S15 (S15T/S15R family); Laffly S20 |
| **Armored Car** | AB 41; L3/35 | Panhard 178 (AMD 35) |
| **Light Tank** | L6/40, Semovente L40 da 47/32 | Renault R35, Hotchkiss H35/H39, FCM 36 |
| **Medium Tank** | M13/40, M14/41, Semovente da 75/18 | SOMUA S35, Char D2 |
| **Heavy Tank** | P26/40, Semovente da 105/25 | Char B1 (bis), M10 Wolverine |

### Ground Vehicle Scale

**No large grid wheels — the entire Wheeled category is small grid only.**

**Scale factor** Utility ground vehicles are 150% scale. All ground vehicles will likely be due to the very small size of the historical vehicles involved in the Mediterranean.

---

### Mechanical design

- `MobilityType`: `Mobile` for vehicle cores, `Static` for Base/Outpost. One enum field, not two booleans.
- **Civilian vs. Military differentiation:** two levers —
  1. `Modifiers` — Military cores get `RefineSpeed`/`RefineEfficiency`/`AssemblerSpeed` reduced (0.5) on the few closes where production blocks are allowed; Civilian cores stay at baseline (1).
  2. `BlockLimits` — every major type of functional block has an allowance set by the core and the block count and PCU are determined by the replica builds of the same class. For example, the block and PCU limit for the air utility core is determined by the F.222 and giving a small buffer above that.

**To-do:**
- [ ] Add Industrial/Military/Advanced Military Components to existing `WW2-Loot-*` container profiles at tuned drop frequencies, per the military/civilian sourcing split confirmed above.
- [ ] Spawn-condition a small number of salvageable wreck variants of existing hulls, covering both military and civilian spawn themes.
- [ ] Make larger hangar variant for large attackers and bombers. 
- [ ] Build Alexandria and Rome and add them to the static encounters. 
- [ ] Wire in cargo ship paths for Rome and Alexandria.
- [x] Build cored/purchasable versions of the planes per faction.
- [ ] Build cored/purchasable versions of the ground vehicles per faction.
- [ ] Build cored/purchasable versions of some of the ships (Gabbiano, La Malouine, at least one destroyer each also)
- [x] **Store blocks are currently broken** (2026-08-24) — root cause confirmed 2026-09-07: MES's `ApplyStoreProfiles` Timer/Action (used by `WW2-Store-Behavior-PortSales.sbc`) crashes the client on interact, a real MES regression after Keen's Economy 2 update, independently reported on the MES Discord for the same StoreBlock/Contract terminal family (money/reputation rewards zeroed on Acquisition contracts, plus a separate confirmed client crash on Shipyard/Suit Upgrade-style terminal interaction). Fixed for the 4 Ports by dropping the custom MES stocking action entirely and using `[InitializeStoreBlocks:true]` on each Port's SpawnGroup instead — this calls MES's `EconomyHelper.InitNpcStoreBlock` once at spawn, which just converts whatever's physically in the Store block's own cargo into vanilla store offers via the real `IMyStoreBlock` API. Ports now function as plain vanilla Store Blocks: native reputation-gated access/pricing (>500 reputation = buy discount/sell bonus, hostile = denied entry, both built into vanilla, no scripting) and native random restock, no MES involvement at all. See `SpawnGroups-Ports.sbc` for the full writeup.
- [ ] **Follow-up, not yet done:** the Factory installations' own per-plane sale profiles (`WW2-Store-Behavior-FactoryPlaneSales.sbc`) use the identical `ApplyStoreProfiles` mechanism and very likely carry the same crash risk — apply the same `InitializeStoreBlocks` fix there once confirmed.
- [ ] Confirm each Port's Store block's own cargo inventory actually contains the "normal items" (ores/ingots/components/ammo/tools) intended for sale, in prefab, now that `InitNpcStoreBlock` reads directly from that inventory rather than a StoreProfile list — populate/edit that inventory in-game and re-export the 4 Port prefabs if it's currently empty or wrong.
- [ ] Longer-term (explicitly deferred): missions (vanilla Contract Block — check whether the Port prefabs already have one placed, same as the Store/ATM/Services Terminal) and grid-selling. Vanilla Store Blocks can sell pre-built vehicles too (a different offer type than physical inventory items) — worth its own investigation before assuming `InitNpcStoreBlock` covers it.
- [ ] Each Port prefab already has a vanilla **Services Terminal** block placed (confirmed in La Spezia's prefab) — natively provides Grid Storage, Repair, and Salvage/scrap for any grid the interacting player owns >50% of, all with the same native reputation-scaled bonuses, zero scripting required. This is a real, already-built alternative to the earlier Faction Hangar/Grid Garage research thread, and a native implementation of the Maintenance Yard's "repair for credits" and Grinder Pit's salvage concepts — confirm it's wired/functional and cross-reference those two roadmap items rather than solving them twice.

---

### Expansion 1 - Territory, Territory Growth, Ownership, & Area Restrictions

Add territory to each faction anchored on the primary locations. Create the mechanisms for territory to grow and shrink. Create the secondary installations and build the mechanics necessary to change their ownership. Create restricted areas near controlled installations that damage neutral and hostile player reputation over time.

**Territory**
Each primary point runs its own full copy of the GVK territory mechanism (own nested radius tiers, own counter, own growth/shrink triggers).

`CustomSandboxCounter` thresholds drive `ChangeZoneAtPosition` actions on nested pre-defined radius tiers (`ZoneRadiusChangeType:Set`, `ZoneToggleActiveMode`, both growth and shrinkage native via paired Enable/Disable conditions per tier).

**Primary Locations:**
- **Gray:** La Spezia, Rome, Tripoli
- **Green:** Toulon, Oran, Alexandria

**Radius tiers: 6 tiers per anchor, 8/13/19/26/34/43 km.** These are great circle radii, so they are applied along the surface curve of the planet.

**Territory Growth**

**Point award mechanism: ring-weighted** every anchor evaluates every relevant event against its own territory rings independently, and awards its own counter based on where the event falls relative to *that anchor specifically* — events within that anchor's rings 1 = 4x base value to that anchor's counter; within ring 2-3 = 3x; within ring 4-5 = 2x outside ring 5 = 1x (flat, faction-wide baseline). An event near La Spezia naturally scores high on La Spezia's counter (falls in La Spezia's inner rings), and only baseline on Rome's or Tripoli's counters (falls outside their rings), purely because each anchor is checking its own distance bands, no cross-anchor comparison is computed anywhere. Every event good for a faction nudges every anchor of that faction, weighted by that event's proximity to each one individually.

**Facton Ownership:**
A simple sticky-ownership system fully in XML with a per-installation `SandboxBoolean` current-owner flag and a zone-containment check against both factions:
1. Neutral until any faction's territory first covers it.
2. Once owned by Faction X, ownership sticks even if Y's territory also grows to cover it — overlap alone doesn't flip it.
3. Flips to Y only once Y covers it **and** X no longer does (X's territory has receded past that point entirely).

Upon capture: recolor grid, flip block ownership, disable old owner's supply triggers, enable new owner's. Confirmed via AaW's actual files: ownership state is a per-installation `SandboxBoolean` (`{Faction}{SpawnGroupName}`) re-checked at world load (`Type:Session` trigger) and re-applied via `RecolorGrid` + `ChangeBlockOwnership` this is the pattern to replicate.

**Area Restrictions**

These are general "stay out" zones: MSB's `AreaRestriction` system, adopted for neutral and hostile primary and secondary locations. Using MSB's `System_AreaRestriction.sbc`: pre-built radius tiers (100/1000/2500/5000m) that warn a neutral/hostile player on entry, then apply periodic reputation loss (`-25` every 10s in MSB's default) for as long as they linger, via a `PlayerNeutral`-gated `Manual` trigger pair (in-range/out-of-range) plus a repeating `StillInRange` timer. Functionally similar to AaW's separate `CapturableController` loiter-penalty layer, but general-purpose rather than capture-specific.
This is specifically intended to discourage players from building bases too close to non-friendly NPC installations.

**To-Do**
- [ ] Measure actual in-game distances between the confirmed anchor points on the custom planet to sanity-check the 8/13/19/26/34/43 km tier progression against real anchor spacing now that the planet's terrain is essentially finished.
- [ ] Build each anchor's nested radius-tier zone definitions and paired Enable/Disable timer-trigger-condition sets (6 anchors × 6 tiers each).
- [ ] Define what actions will count towards territory growth and against it.
- [ ] Define what threshold values each ring is set by.
- [ ] Build the ring-weighted `CustomSandboxCounter` point-award actions per anchor (4x/3x/2x/1x by ring band) for each relevant encounter/event type.
- [ ] Build Gibraltar and Foggia airports and place them as static encounters. 
- [ ] Build a non Gibraltar airport and use it and Foggia for all remaining airport locations. 
- [ ] Build a barracks and use it at all relevant locations with behavior as described in the location section.
- [ ] Build ownership change triggers and actions and add them to behavior for secondary installations.
- [ ] Add MSB's `AreaRestriction` TriggerGroup to all installations, 1,500m radius on primary points and 500m on all other installations.
- [ ] Playtest: confirm the AreaRestriction warning/reputation-loss cadence feels like a nudge rather than a punishment.
- [ ] Playtest: confirm control of installation changes appropriately and triggers update to the new owner faction.

---

## Expansion 2 - War Level

War level is a universal value that represents the level of intensity of the conflict. As war level increases, more NPC spawns unlock and NPC patrols and defenses get stronger. 

War level increases every time an NPC is first attacked or destroyed.

**War Level Gates (tentative)**
War Level is 1-5 and starts at 1.
War Level 2: 50 war activity
War Level 3: 150 war activity
War Level 4: 350 war activity
War Level 5: 650 war activity

War activity come from the destruction of any NPC at the following tentative rates: 
| Grid Type |  | War Activity change|
|---|---|
| Utility Ground | +3 |
| Armored Car | +1 |
| Light Tank | +2 |
| Medium Tank | +3 |
| Heavy Tank | +3 |
| Utility Air | +5 |
| Fighter | +3 |
| Attacker | +4 |
| Bomber | +5 |
| Utility Naval | +10 |
| Corvette | +5 |
| Destroyer | +10 |
| Cruiser | +20 |
| Heavy Cruiser | +25 |
| Battleship | +40 |
| Carrier | +50 |
| Hangar/Garage | +2 |
| Factory | +10 |


**To-Do**
- [ ] Define defensive spawns per war level per installation type.
- [ ] Define minimum war level per spawn.
- [ ] Create store listings by war level based on "war level minimum" requirements determination.
- [ ] Update spawn conditions to include war level checks. 
- [ ] Test war level increase rates.

---

## Expansion 3 - Vehicle Depots & Garrison Functions

Create vehicle depots, at least two ground vehicles per faction, and implement functionality for vehicle depots to have convoys from the nearest port

**To-do:**
- [ ] Create AB 41 and Panhard armored cars at 150% scale.
- [ ] Create one light tank per faction at 150% scale. 
- [ ] Create a basic vehicle depot prefab either per faction or that can be used for both.
- [ ] Create a basic barracks and command building that can be used for both factions.
- [ ] Create behavior for vehicle depot that calls for vehicle convoys from the nearest port that scale with war level and includes defensive AI bot spawns and general triggers. Include sale of vehicles at the vehicle depots.
- [ ] Create behavior for garrisons with general triggers and bot spawns and include sale of utility vehicle.
- [ ] Look into missions/contracts that could be offered at the garrisons. 
- [ ] Configure garrisons and vehicle depots as permanent and invulnerable static spawns.
- [ ] Create refinery/fuel point grids.
- [ ] Add "reroll stock" as an action in the capture trigger chains.
- [ ] Add AI enabled spawn sequences to installations.
- [ ] Test whether RivalAI/MES ground-vehicle autopilot supports a multi-waypoint patrol pattern between two fixed points (Garrison Post to Garrison Post).

---

## Expansion 4 - Territory Spawning

Structure spawns to only occur within the territory of their faction. All logistics buildings, naval encounters, and non reconnaissance air spawns will be constrained to only spawn in their faction territory.

**To-do:**
- [ ] Add `ZoneConditions` gating to existing dynamic SpawnConditions so Gray/Green spawns are restricted to their own current territory (any-anchor-of-that-faction, not anchor-specific).

---

## Expansion 5 - Squadrons and Morale Systems

Create a squadron system to spawn convoys, planes, and possibly ground vehicles in groups that work together based on war level. The design is sourced from a three-way comparison of Ares at War (AaW), MES Shared Behaviors (MSB — enenra's public MES behavior library, github.com/enenra/mes-shared-behaviors), and GVK Deserts of Kharak S10, reconciled below.

Create a morale system for all encounters that makes engagements feel more realistic and engaging.

**Squadron/escort tracking: MSB's `CommandChain` system, adopted wholesale.** MSB's `CommandChain` does leader/escort join-up, renaming, and death notification via `CommandCode` broadcast (`MSB_LeaderDead`) and boolean state (`LeaderInactive`) and uses RivalAI's native `AssignEscortFromCommand` for slot assignment, so it scales to however many escorts actually spawn. **Decision: use MSB's mechanism as the underlying system; flight size of 2–3 planes remains a content choice (how many fighters get spawned into a given encounter), not a constraint baked into the tracking logic itself.**
- Add `MSB_System_CommandChain_Leader_TriggerGroup` to flight leaders, `MSB_System_CommandChain_Escort_TriggerGroup` to wingmen, per MSB's usage notes.
- Both require one of `MSB_DynamicCommon_TriggerGroup`/`MSB_StaticCommon_TriggerGroup` present on the encounter first, per MSB's base requirements.

**Morale:** Sourced from MSB's `System_Morale.sbc` (marked WIP/unfinished upstream, but functional): a decaying `CustomCounter` (starts ~100, ticks down under sustained combat, drops sharply on low health / weapons lost / leader death / nearby losses) that swaps `TargetProfile` at low-morale thresholds. A shaken unit doesn't hard-retreat, it just stops proactively hunting at range (detection range effectively shrinks). Entirely XML controlled. Pairs conceptually with the plane disable/spiral-crash mechanic. Low morale plus a real chance of visibly losing control, rather than either alone, is the intended combined effect.

**To-do:**
- [ ] Add MSB's `_Common` TriggerGroup (Dynamic or Static as appropriate) as a base requirement to fighter behaviors before layering CommandChain on top.
- [ ] Wire `MSB_System_CommandChain_Leader_TriggerGroup` / `_Escort_TriggerGroup` into existing fighter flights; confirm flights still read as 2–3 plane groups in practice even though the mechanism itself is N-agnostic.
- [ ] Add MSB's `MoraleSystem_TriggerGroup` to plane and naval behaviors; tune decay/threshold values against actual playtests rather than assuming MSB's defaults fit WW2 WitM's pacing.
- [ ] Playtest: confirm squadrons visibly thin out/disengage as CommandChain reports losses, confirm reputation responds to *who* you fight, confirm morale-driven target-profile shrinkage is noticeable without being confusing.

--- 

## Expansion 6 - Logistics Installations

Implement the dynamically spawning, non permanent, faction owned installations. These inside vehicle factories, supply depots, ammo depots, and refueling points.

**To-Do**
- [ ] Create supply depots to spawn dynamically. (Buy all items and ores)
- [ ] Create ammo depots to spawn dynamically. Stocked with ammunition and also buy and sell ammunition. 
- [ ] Create ground vehicle factory mimicking plane factories but for armored cars, light tanks, etc.
- [ ] Create refueling points. Gas stations that players can connect to and refuel and get refreshed every ~30 minutes.

---

### Expansion (undecided prioritiy) Port-based Naval Patrols

Implement ports spawning patrols on variable timers for combat ships that travel randomly selected pre-defined patrol routes. These patrols should escalate in strength and number of ships with war level.

**Additive Layer** Combat ships continue spawning via the existing random/stationary mechanism across each faction's territory unchanged. Patrols are a separate, second source of naval combat encounters layered on top of these spawns.

- **Route structure:** Adjacent-anchor legs per faction (Gray: La Spezia–Rome, Rome–Tripoli; Green: Toulon–Oran, Oran–Alexandria) plus at least one long-haul cross-Mediterranean lane per faction (e.g. La Spezia–Tripoli, Toulon–Alexandria), so patrol traffic crosses several different parts of the map rather than one predictable corridor.
Have ships run out and back before despawn.
- **Lane selection randomized per spawn cycle** — each patrol spawn rolls which anchor-pair it runs between, so which lanes are active shifts over time rather than being a fixed, learnable shape.
- **Behavior, distinct from the cargo convoy mechanic:** patrols go out and back rather than making a one-way trip and despawning, and carry normal aggressive combat `SpawnConditions`/engagement behavior rather than the cargo convoy's passive despawn-suspension pattern. Reuses the same fixed-coordinate Waypoint Profile approach confirmed for the cargo convoy, but the patrol/aggressive behavior is a materially different build, not a reskin.
- **War Level tie-in:** patrol frequency/strength scales with War Level, same pattern as air cargo convoy escorts. More/stronger patrol traffic as the war heats up, not just static route traffic.

**To-do:**
- [ ] Define the full lane list per faction (adjacent-anchor legs + long-haul lane(s)) once all 6 domain-anchor Ports are actually built and placed.
- [ ] Build the randomized per-cycle lane-selection logic for patrol spawns.
- [ ] Build patrol ship combat/engagement behavior distinct from the cargo convoy's passive despawn-suspension pattern. (Should be able to reuse existing combat ship behavior and just add the travel portion)
- [ ] Wire patrol frequency/strength to War Level, mirroring the air cargo convoy escort scaling.
- [ ] Playtest: confirm patrols read as an added layer of naval presence without making the existing random-spawn coverage feel redundant or crowded.

---

## Basic-Tier Starters

Build starter grids per domain and make them available in ports, airports, vehicle depots, hangars, and garages.
- [ ] Naval: one small patrol boat per faction, sized to Corvette Core. (Can start by just making a player version of the Gabbiano and La Malouine, turn on gravity align script and add ship core)
- [ ] Ground: one Armored Car per faction. This is also where land combat gets its first real content instead of just installations.
- [x] Ground Utility: one truck per faction to serve as a respawn vehicle. Include survival kit and cargo. Include components for wind turbine, basic refinery, and basic assembler.
- [ ] Air Utility: Create a larger hangar so the Ju 52 and F.222 can be spawned and found.

--- 

## Other Stuff

- **Upgrade Modules:** add upgrade modules to allow customizing each core.
- Ideas include upgrading armored car to have more weapon slots to allow for anti-air style builds, upgrading any tank type to allow special weapons for howitzers etc.

- **Retaliation/escalation** (from AaW): tiered "strike back" raids against captured territory, cooldown-limited.

- **Fleet/Escort formalization:** generalize the existing `CarrierSpawn` escort pattern into something reusable across ship classes. MSB's `CommandChain` for squadron/escort tracking means this item is really "apply the existing CommandChain mechanism to carriers and their escorts," not a new system.

- **Faction Strength/Holdings scripting** (from AaW) the scripted upgrade path to aggregate captured-installation production into a real per-faction Strength value once the flat GVK-style counter has been proven.

- **Narrative flavor:** ScenarioTools for MES Events (Workshop 2998575759) for NewsFeed broadcasts and dynamic GPS markers, no custom C# required. Could be used for capture notifications, major sinking events, and boss encounters.

---

## Custom Planet

**Planet size: confirmed at 60km diameter (30km radius)** 

**Status: heightmap/material/ore pipeline complete and working, built as its own third mod (`WW2-WitM-Planet`).** What follows is a summary of confirmed decisions; full technical detail (formulas, scripts, the cube-face geometry mapping) lives in `WW2-Reference-PlanetCreation.md`.

**Confirmed design:**
- Real elevation sourced via OpenTopography (SRTM15Plus), spanning portugal to just past Suez.
- Wraparound design: the full west-to-east real extent (~3,300km) wraps onto the planet's full equatorial circumference (188.4km) as a closed loop, with land occupying the outer two-thirds (nearer each pole) and water the middle third — giving roughly 62% land / 38% water, close to the 1/3-1/3-1/3 target.
- Differential horizontal compression (~15.9:1 for the main Iberia-to-Crete span, ~24:1 for the emptier Crete-to-Suez stretch) and uniform vertical compression (23.5:1).
- Elevation relief retuned to match Pertam's real scale (~6.2:1 land compression, giving comparable peak heights to vanilla) after an earlier, much flatter first pass read as lifeless in-game — the confirmed 60km/30km-radius size match with Pertam (see above) is what made this direct scale reference possible.
- A real hazard specific to this engine was found and fixed: Water Mod's water is a fixed-radius sphere from planet center, not a terrain-following mesh, so any inland depression below sea level would flood regardless of connectivity to the ocean. 758 such disconnected pockets were found and raised (two large enough to be real closed basins, plausibly the Qattara Depression among them).
- Deliberately did **not** floor coastal land to a safety margin above Water Mod's default ±2m wave range — an earlier attempt at this produced an artificial terraced "shelf" at every shoreline; natural gentle slopes into the wave zone were judged better than that artifact.
- Cube-face geometry fully debugged and confirmed correct via extensive in-game testing: side faces use a Y-negated gnomonic projection; pole faces need their north/south content **swapped** and then **both** given a left-right mirror flip. This exact mapping is documented directly in the `.sbc` comments and must be reused for any future texture work on this planet (biome map, etc.).
- Material system: `ComplexMaterials` height/slope rules (no texture needed) handle basic sand/grass/rock/peak banding; a real material texture (red channel) adds a north/south hemisphere split so the African coast reads as arid (CrackedSoil/DesertRocks/DustyRocks) versus the European coast's grass palette — the one thing height/slope rules alone couldn't do, since this planet's wrap means SE's built-in Latitude concept can't distinguish the two poles' hemispheres.
- Ore system: real cited EarthLike ore-type proportions (Silicon/Magnesium/Iron/Cobalt/Nickel/Gold/Silver), Ice's proportion replaced with Platinum + Uranium (for a planned Platinum/Uranium-gated advanced-component crafting restriction), scaled to 4% land coverage, biased toward coastlines and away from the exact poles, with extra clustering around real WW2-significant North African ports (Tunis, Bizerte, Tripoli, Benghazi, Tobruk, Alexandria) to bias player activity toward historically active areas. Distribution within that budget is otherwise random, deliberately deferred, not a gap.

**Known open items:**
- [ ] Biome GREEN channel (foliage/environment items) not built yet.
- [x] Water Mod's working radius for this planet is empirically `/wradius 1.02862`. This is the result of wradius 1.0 being based on the lowest point of the surface, not "sea level".
- [ ] Ore distribution is random-within-budget; revisit if specific historical/gameplay-driven placement becomes worth the effort.
- [ ] Spawn in a fresh test world and confirm WeaponCore/MES don't exhibit raycast issues (the ≤2048px constraint was respected throughout, but hasn't been explicitly re-verified against live NPC/weapon behavior).

---

## Map Tool (deferred decision)

Not scoped yet on purpose. This is a large effort with lower return on investment at this point in the project. Revisit once capturable territory is live and actually contested; a map has nothing dynamic to show before then. When that point arrives, work through the three tiers (existing Workshop map script → custom LCD/GPS script → bespoke external tool) in that order, stopping at whichever tier actually earns its cost.

---

## Long-term vision

**Sub-factions.** Not scoped yet, named so early architecture doesn't box it out. Splitting Gray/Green into historical sub-factions (Germans/Italians under Gray; French/UK/US under Green) matches AaW's structure of many factions under fewer broad alliances. Practical implication for everything above: avoid hardcoding "GRAY"/"GREEN" any more than necessary, prefer patterns that generalize to a new faction being copy-and-retarget rather than a rewrite.

**First wave, confirmed: Germany (Gray) and UK (Green).** UK over US for the first wave because Britain was Italy's primary Mediterranean opponent for nearly the entire period the existing roster already centers on (Taranto, Cape Matapan, Malta convoys, the early North African campaign all predate US Mediterranean involvement, which didn't begin until Operation Torch in November 1942). UK also brings more genuinely new content to players, since it's less commonly built in the existing SE Workshop ecosystem than US equipment. The tradeoff being fewer existing builds to reference/borrow from, which is a real cost but a manageable one given my preference for building my way. US remains a strong later addition, natural to pair with a Sicily/Italy-invasion-era expansion rather than this first wave.

UK Expansion: 
Carrier: Formidable (Illustrious class) 
Battleship: Warsprite (Queen Elizabeth class) 



**More ore/ingot variety, SDX2-inspired.** SDX2 adds more raw ores and ingots alongside vanilla's existing set (lead, copper, etc., sitting next to iron, silicon, cobalt). So the actual tradeoff here is whether refining/component-building recipes get more varied and specific, or stay on vanilla's existing ore set. Still a real accessibility-vs-depth call. More ore types means more mining/refining variety but also more for a new player to track, but it's additive breadth, not structural complexity. This is not needed to prove the core loop and is easy to add later by defining new ore/ingot pairs and wiring them into whichever recipes should use them, without needing to touch the three-component system's own structure at all.

---