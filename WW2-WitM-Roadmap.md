# WW2 Encounters — Expansion Roadmap & To-Do List

A staged plan for growing the mod from its current state (Naval + Air + Installations, Gray/Green factions) into a fuller PvPvE experience with Ship-Core-driven progression, borrowing proven systems from Ares at War, GV Deserts of Kharak Season 10, and MES Shared Behaviors (MSB — enenra's public MES behavior library). Ordered so small, achievable, playable milestones come first, and grid-design-heavy work comes only once the systems around it are proven. Custom C# scripting is in scope for this project where it's the right tool, but deliberately staged in after simpler XML-only versions are built and playable — see Stage 3's territory section and Stage 5 for where scripting is first introduced.

---

## Prefab naming convention (confirmed)

Two parallel prefab families per hull, distinguished by purpose rather than just faction/class:

- **`Player-WW2-[name]`** — configured for player use (includes landing gear). Used at hangars, garages, factories, and stores — the buyable/stealable grid tier described in Stage 3's expansion.
- **`NPC-WW2-[name]`** — used for NPC-controlled encounters, matching the existing convention already reflected throughout the weapon-rebuild checklist below.

---

## Prerequisite — Weapon System Rebuild

Not a stage so much as a foundation everything else sits on. The current naval weapon mod is effectively dead, so every existing ship and plane needs rebuilding onto a new weapon system before Ship Core work can be meaningfully tested — the whole premise of Stage 1 is gating access to ships you already have, and that doesn't hold if those ships don't load.

- **Naval weapons:** [Fletcher Armaments – WeaponCore Edition](https://steamcommunity.com/sharedfiles/filedetails/?id=2844434226) (Workshop 2844434226) — WeaponCore-based WW2 naval weapon pack.
- **Air weapons:** [Consty Aircraft Pack – Ordnance (WeaponCore) 1.0](https://steamcommunity.com/sharedfiles/filedetails/?id=2881339118) by Const (Workshop 2881339118), companion to Const's Tech-Focused Aircraft Pack. Confirmed via its changelog to mix genuinely period-appropriate content (.50 cal guns, 30mm aircraft cannon, unguided bombs, rocket pods) with clearly modern/anachronistic content (AIM-7/54/120 guided missiles, 5.56mm burst rifles, 9mm SMGs, .45 handguns). The lockdown plan is sound and necessary, not just thematic tidiness — gate/exclude the modern-coded weapons, keep the period-coded ones, using the same tech-item mechanism as Stage 1's unlock currency.
- **Ground vehicle weapons:** [KONTAKT Ground Systems [WeaponCore] v1.0](https://steamcommunity.com/sharedfiles/filedetails/?id=2930049835) Mostly fixed weapons. Same identification and BlockGroup-building process as Fletcher Armaments/Consty's Ordnance once you're ready to pull its block SubtypeIds.
- **CWP replacement candidates**
  - **SETB Community Tank Parts** — replace all existing armor blocks, enabling the ground vehicle rebuild off AWG CWP armor specifically.
  - **ArmourEssentials** — Mostly turret ring rotors, gunsight cameras, and rangefinders.
  - **Yakobe's Machinations** — wheels/suspension sourced from CWP, plus additional guns.
  - **SETB - Multicrew - MODPACK** — the full SETB bundle. Not intended for wholesale adoption, but worth keeping as a reference source.
- **Turret philosophy shift:** moving away from custom rotor/hinge-built turrets toward general WeaponCore turret blocks. This should make future `BlockGroup`/`BlockLimit` definitions for the Core system far more stable, since they'll reference known WeaponCore block types instead of a dead mod's custom subtypes.
- **Dependency additions:** WeaponCore itself, plus whichever specific weapon-pack mods end up used. Worth checking MES's documented WeaponCore compatibility notes once rebuild work starts, since combining MES/RivalAI-driven NPCs with WeaponCore weapons is a well-trodden combination but has its own configuration quirks (weapon targeting profiles, ammo replenishment behavior).

**To-do:**
- [x] Configure BlockRestrictions to disable all blocks that are not to be available to players.
- [x] Configure ShipCores for block limits
- [ ] Update G menu to not show empty groups or blank spaces
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
- [ ] Connect basic war level and territory aspects to ports
- [x] Create GPS Routes for travel between existing port locations
- [x] Assign component costs to cores (does not include advanced Naval or Ground cores yet)
- [x] Assign component costs to advanced propellers
- [ ] Assign component costs to advanced wheels (5x5)
- [x] Assign component costs to weapons
- [ ] Figure out store block problems
- [x] Get respawn rovers to show up in respawn menu
- [ ] Add industrial component cost to the upgrade modules for production blocks
- [x] Check Aquila for old guns (had blueprint in console block)
- [x] Remove old ammo from prefab cargoes
- [ ] Merge in Gibraltar Topography
- [x] Make mountains, especially in Italy and Greece less "spire"like
- [ ] Get Static spawns to properly spawn, figure out why they are despawning.

**Rebuilds:**

  **Naval:**
  - [x] NPC-WW2-Golo_Italian (Cargo Ship, Gray)
  - [x] NPC-WW2-Golo_French (Cargo Ship, Green)
  - [x] NPC-WW2-Gabbiano (Corvette, Gray)
  - [x] NPC-WW2-Spica (Torpedo Boat, Gray)
  - [x] NPC-WW2-Francesco_Crispi (Destroyer, Gray)
  - [x] NPC-WW2-Comandante_Margottini (Destroyer, Gray)
  - [x] NPC-WW2-La_Malouine (Corvette, Green)
  - [x] NPC-WW2-Bougainville (Aviso/Destroyer-behavior, Green)
  - [x] NPC-WW2-Le_Triomphant (Destroyer, Green)
  - [x] NPC-WW2-Algerie (Cruiser, Green)
  - [x] NPC-WW2-Emile_Bertin (Cruiser, Green)
  - [x] NPC-WW2-Aquila (Carrier, Gray)
  - [x] NPC-WW2-Bearn (Carrier, Green)
  - [x] Rebuild Loire 130 on the Emile Bertin and Algier and re-export/replace.

  **Air:**
  - [x] NPC-WW2-Re2001 (Fighter, Gray)
  - [x] NPC-WW2-Re2000 (Fighter, Gray)
  - [x] NPC-WW2-FC20 (Attacker, Gray)
  - [x] NPC-WW2-SM79_Bomber (Attacker, Gray)
  - [x] NPC-WW2-SM79_Torpedo (Attacker, Gray)
  - [x] Player-WW2-SM79_Bomber (Attacker, Gray)
  - [x] Player-WW2-SM79_Torpedo (Attacker, Gray)
  - [x] NPC-WW2-Loire130 (Recon, Green)
  - [x] NPC-WW2-MS406 (Fighter, Green)
  - [x] NPC-WW2-Potez630 (Attacker, Green)
  - [x] NPC-WW2-V-156-F-Bomber (Attacker, Green)
  - [x] NPC-WW2-V-156-F-Torpedo (Attacker, Green)
  - [x] NPC-WW2-Ju52 (Cargo Plane, Gray)
  - [x] NPC-WW2-F222 (Cargo Plane, Green)

 **Installations:**
  - [ ] NPC-WW2-Ammo-Depot-1
  - [x] NPC-WW2-Hangar
  - [x] NPC-WW2-Factory-Plane
  - [ ] NPC-WW2-Garage (create, never actually made)


- [x] Remove F4F from the mod (spawn groups, prefabs, any references) — pulled from the roster, see Air roster note.
- [x] Confirm SM.79 (Bomber and Torpedo, NPC and Player) is fully wired into SpawnGroups/Behaviors/Loot now that the prefab rebuild is done.
- [ ] Test all MES components end-to-end to confirm baseline features (spawning, behaviors, triggers) work after the recent rebuild work.
- [x] Build BlockRestrictions definitions removing non-period-correct blocks (anachronistic weapons, reactors, thrusters, etc.) from the G-menu across vanilla and dependency mods — new tool decision, see Modlist entry #26; full block audit not yet done.

---

## Stage 1 — Core Progression: Vehicles and Bases

**Goal:** everything a player builds — ships, planes, tanks, and bases — is capped by a placeable Core block (via Ship Core Framework, Workshop 3552595651), starting small and unlocking bigger/better through play. No new grids required beyond what the weapon rebuild already touches.

### Confirmed tier ladders

| Category | Civilian track | Military track |
|---|---|---|
| Naval | **Cargo Ship** (Golo-scale) | **Corvette** → Destroyer → Cruiser → Heavy Cruiser → Battleship → Carrier |
| Air | **Cargo Plane** (Ju52/F222-scale) | **Fighter** → Attacker/Bomber → Heavy Bomber |
| Wheeled | **Truck** (Fiat 626, ANH) | **Armored Car** (new) → Light Tank → Medium Tank → Heavy Tank |
| Base | *(no civilian/military split)* | **Base** (few per faction, HQ-scale) and **Outpost** (more per faction, smaller) — both available from the start, not sequential |

**Stage 1 scope specifically:** only the starting rung of each track, plus Base and Outpost (both needed since they're parallel, not sequential). That's Civilian Naval, Corvette Naval, Cargo Plane, Fighter, Transport, Armored Car, Base, Outpost — eight core definitions — plus the unlock-gate system working end-to-end for exactly one proof-of-concept upgrade (Corvette → Destroyer). Everything past that first rung on each ladder belongs to Stage 5.

### Ground vehicle scale and weapon dependency (confirmed)

**No large grid wheels — the entire Wheeled category is small grid only.** This is a firm constraint, not a default: no large-grid wheeled vehicles exist anywhere in the ladder.

**Scale factor: two-tier, driven by functional-block real estate, not wheel proportions.** Correctly-sized wheels exist, so 1:1 is viable on that front — the actual driver is fitting functional blocks (cockpit, respawn kit, cargo, weapon mount) inside a genuinely small real-world hull at small-grid's 0.5m block size. This hits Armored Cars hardest — historically some of the smallest vehicles in the whole roster, and the tightest fit for the same reason. Same two-tier structure still applies, just for the corrected reason: weapon blocks are fixed dimensions that don't rescale with the hull, so military vehicles stay conservative; civilian vehicles have no such anchor and can flex further if it helps fit functional blocks comfortably.
- **Military track (Armored Car → Heavy Tank):** capped around **110–120%** of real-world scale.
- **Civilian track (Transport):** allowed up to **150%**.

**Note for your own builds, not a mod-content concern:** vanilla SE's AI Flight block doesn't support wheeled vehicle propulsion at all — confirmed via Keen's own support forum, a longstanding unaddressed gap, not a bug on your end. Doesn't affect your NPC Armored Cars/tanks, which run on RivalAI/MES (already proven for ground units), but rules out the vanilla Automatons toolkit specifically if you ever want a player-built AI escort — that'd need a dedicated third-party rover-AI script instead.

**Existing assets affected:** the Fiat 626 (Italian) and Renault AHR-or-similar (French) are both built at 1:1 and are the natural Transport-tier hulls. If civilian scale-up is adopted, both need rescaling before use — worth deciding before further detail work goes into them at the wrong scale.

### Naval roster (confirmed)

| Tier | Gray (Italy) | Green (France) |
|---|---|---|
| **Civilian** | Golo (Italian variant) | Golo (French variant) |
| **Corvette** | Gabbiano | La Malouine |
| **Destroyer** | Francesco Crispi, Comandante Margottini, Spica (Torpedo Boat) | Bougainville (Aviso/Destroyer-behavior), Le Triomphant |
| **Cruiser** | Bartolomeo Colleoni (Giussano-class, famously lost at the Battle of Cape Spada) | Emile Bertin (France's only genuine light Cruiser under treaty classification) |
| **Heavy Cruiser** | Trento, Zara | Algérie (confirmed France's only heavy cruiser under treaty limits — no sister ship, an honest asymmetry to keep rather than fix, same shape as the Italian heavy-tank gap) |
| **Battleship** | Vittorio Veneto (Littorio-class — picked over sister ships Littorio and Roma for the broadest active-service record: present at both Taranto and Cape Matapan, rather than fame tied to a single event) | Strasbourg (Dunkerque-class; existing blueprint from an established builder, to be adapted) |
| **Carrier** | Aquila | Bearn |

**Correction on record (confirmed via research):** Trento, Zara, and Algérie are all officially classed Heavy Cruisers (Washington Treaty tonnage), not plain Cruisers — Emile Bertin alone is a genuine light Cruiser, and Bartolomeo Colleoni fills the Gray Cruiser-tier slot that opened up as a result.

---

### Naval roster — Submarines (confirmed, new category outside the Civilian–Carrier ladder)

| Type | Gray (Italy) | Green (France) |
|---|---|---|
| **Standard submarine** | Adua class (17 built, 1936–38; Regia Marina's numerically most important coastal submarine class of the interwar/war period, part of the broader "600 Series" family) | *(no equivalent identified this session)* |
| **Cruiser submarine** | *(no equivalent — Italy did not build cruiser submarines)* | Surcouf (largest submarine in the world at the time; twin 203mm/8-inch cruiser-caliber guns plus torpedoes; carried its own reconnaissance floatplane, a Besson MB.411, in a hangar — same shipborne-floatplane concept as the new Air Recon tier, submarine-mounted) |

**Direct link to existing content:** several Adua-class boats — most notably *Scirè* — served as mother submarines for the Decima Flottiglia MAS "Maiale" human torpedoes (see the Alexandria raid, Naval roster context above). Not a coincidence being forced here — it's a genuine historical connection between two additions confirmed in the same research pass.

**Not yet placed in the mod's tier/progression structure.** Submarines (standard or cruiser type) don't fit the existing Civilian→Corvette→Destroyer→Cruiser→Heavy Cruiser→Battleship→Carrier ladder, and neither does Decima Flottiglia MAS as a special-forces/raider concept. Roster-confirmed here as real, verified historical content; how (or whether) they slot into Ship Core tiers, spawn behavior, or a wholly separate mechanic is a mechanics-thread decision, not addressed here.

---

### Air roster (confirmed)

| Tier | Gray (Italy) | Green (France) |
|---|---|---|
| **Civilian** | Ju52 *(open item)* | F.222 (civilian variant) |
| **Fighter** | Re2001, Re2000; Macchi C.202 Folgore | MS406; Dewoitine D.520 *(F4F pulled, see note below)* |
| **Attacker** | FC20; Ba.88, Ba.65 *(Stage 5 additions)* | Potez630; Bréguet 693, ANF Les Mureaux 115 *(Stage 5 additions)* |
| **Bomber** | Caproni Ca.311 *(light)*; SM.79 *(medium/torpedo)*; P.108 *(heavy)* | V-156-F *(light dive bomber, reclassified from Attacker)*; MB.210, LeO 451 *(medium)*; F.222 (military variant, heavy) |
| **Recon** | IMAM Ro.43 | Loire 130 |

**Attacker tier, three per faction (confirmed).** FC20 (Gray) and Potez630 (Green) are the existing built assets. Ba.88 and Ba.65 round out Gray's tier — Ba.65 confirmed as Italy's only dedicated ground-attack aircraft to see active service in that role, lightly armed but kept for thematic and historical accuracy. Bréguet 693 and ANF Les Mureaux 115 round out Green's tier — the 693 a genuine twin-engine ground-attack counterpart to Ba.88/Ba.65, and the Mureaux 115 an older, more obsolescent reconnaissance/light-attack hybrid (119 built, 1935–38) that fills the same "outclassed early-war plane" role Ba.65 fills for Gray, right down to both being real but underwhelming in actual combat use. The Mureaux's most notable historical distinction is being the first French aircraft shot down by the Luftwaffe, in September 1939.

**Bomber tier corrections (confirmed).** V-156-F reclassified from Attacker to Bomber — confirmed as the French export version of the Vought SB2U Vindicator dive-bomber, ordered for carrier air groups, not an attack aircraft. MB.210 confirmed as a medium bomber, not attacker — twin-engine, ~257 built, entered service 1936. Caproni Ca.311 added as Gray's light-bomber entry — 335 built, twin-engine light bomber/reconnaissance hybrid, entered service 1939; notably it also replaced Ba.65 in some ground-attack roles historically, a mild irony worth knowing but not a real conflict since the two filled different jobs (recon-bomber vs. ground-attack).

**Macchi C.202 Folgore and Dewoitine D.520 added to Fighter tier (confirmed, prior session).** Both are stronger historical picks than the existing entries in their tiers — C.202 widely regarded as Italy's best fighter of the war, D.520 the only French fighter able to meet the Bf 109E on roughly equal terms. Neither has an existing build; both are next up in the build queue.

**Recon tier (confirmed, prior session).** IMAM Ro.43 (Gray) and Loire 130 (Green), both real catapult-launched shipborne reconnaissance floatplanes. Mechanics deferred to a separate thread.

**Open item: Gray Civilian-tier air.** Ju52 is German-built, not Italian was used substantially by Italians, supplement with additional cargo plane variant eventually.

**Ba.88** fills the lighter attack role — genuinely real and well-documented, but its actual historical reputation is "notoriously one of WW2's worst operational aircraft," which fits an entry-tier plane thematically rather well.

**P.108** is Italy's only operational four-engine heavy bomber. Existing impressive model already built, needs rebuild onto the new weapon systems.

**LeO 451** is widely regarded as the best French bomber of the war — the closest French equivalent to SM.79 in national significance, even though it arrived too late and in too few numbers to matter much before the armistice. **MB.210** fills the entry-tier attack role — older, more obsolescent by 1940, the same "weaker starting" role Ba.88 fills for Gray.

**F.222** is confirmed via research to be far more significant than expected: explicitly classified as a heavy bomber, "the only four-engined bomber in front line service with any Allied air force" at the start of the Battle of France, flew 63 night-bombing sorties over Germany in May–June 1940, and a related variant flew the first-ever Allied bombing raid on Berlin. Civilian use was the historical *secondary* role, not primary — the reverse of how the existing asset is currently used (Civilian tier). **Build approach confirmed: the existing F.222 build will be modified into the military-spec Bomber-tier version rather than built from scratch.**

**Civilian tier scope, confirmed as open-ended:** the Civilian tier isn't locked to historical trucks alone. Non-historical utility vehicles players will want — mining, welding, grinding rigs, possibly articulated-arm designs — are anticipated future additions to this tier, not ruled out by the historical framing used for the rest of the roster. Not scoped yet; revisit when utility-vehicle design work actually starts.

**Ruled out during research, worth remembering so it doesn't resurface as an assumption:**
- Tiger/Panther for Italy — Germany never transferred either to Italian units; confirmed dead end, not a style choice.
- M7 Priest for France — real, well-documented Lend-Lease option, but declined on purpose (indirect fire, out of scope for now).
- Churchill for France — searched, found no confirmation either way; treat as unverified, not assumed real.
- Captured R35 for Italy — genuinely real (German-supplied, 1941, two real battalions), but set aside over faction-color confusion risk (an R35 in Gray sitting next to the actual Green-faction R35).

### Mechanical design (confirmed against the actual Ship Core Framework v3 schema)

- `MobilityType`: `Mobile` for vehicle cores, `Static` for Base/Outpost. One enum field, not two booleans.
- **Civilian vs. Military differentiation:** two levers —
  1. `Modifiers` — Military cores get `RefineSpeed`/`RefineEfficiency`/`AssemblerSpeed` reduced (~0.5) even where a few production blocks are allowed; Civilian cores stay at baseline or above.
  2. `BlockLimits` — cap weapon block counts low on Civilian cores, cap production/cargo block counts low on Military cores, via `BlockGroup` definitions. Blocked on the weapon rebuild above for the weapon-side groups; production/cargo/tool-side groups can be written now since they're vanilla block types.
- **Speed/boost:** `MaxSpeed`/`MaxBoost` are fractions of the world's `MaxPossibleSpeedMetersPerSecond`, not absolute values — changing the world setting rescales every core's absolute speed automatically without touching individual core files. Currently set to 150 m/s; **raising to 250 m/s under consideration, blocked on a physics test.** A known subgrid pitch-down bug (planes tipping toward the ground) has previously appeared around 150 m/s+ in testing — general subgrid pitch/tilt bugs are well-documented in the wider SE community (dampeners not accounting for subgrid mass, phantom forces from unshared inertia tensors), but the specific 150 m/s threshold isn't something I could independently confirm as a documented trigger, so it's this project's own prior finding, not a verified external one. Test incrementally on an actual subgrid-bearing plane before committing to 250. If adopted, revisit the tuning (not just the mechanics) of every already-written `MaxSpeed`/`MaxBoost` fraction — they'll still function unchanged, but the resulting absolute speeds may no longer match the intended feel. Boost itself is duration/cooldown-limited only — the framework has no native fuel-drain boost mode. Naval "flank speed" will use the same duration/cooldown mechanism as air/wheeled for now (tuned with a longer duration to read as "sustained" rather than "burst"); a true fuel-cost mechanic is a possible custom-trigger addition later if the simple version doesn't feel right.
- **Upgrade path (confirmed, deferred to later):** the "upgrade a Corvette core to allow more guns/propellers" idea maps directly onto `UpgradeModule.BlockLimitModifiers` — a real, first-class feature of the framework, not something to build custom. Stage 1 doesn't need this yet; Stage 1 is core-swap only (place a better core block outright). Converting to upgrade-modules-on-top-of-one-core is a clean later refinement once tier numbers are proven, not a Stage 1 requirement.

### Unlock economy

**Confirmed: territory/salvage-based, mostly salvage to start.** Fed by what a spawn *is* — military or civilian-themed — not by whether it was killed or found already wrecked. Every spawn type should exist in both an active (fight it) and wrecked (salvage it) form where that makes sense, but the loot-sourcing split runs along the military/civilian axis, not the alive/dead one:

1. **Military spawns** (active or wrecked) — combat ships, planes, weapon emplacements. Primary source of Military and Advanced Military Components.
2. **Civilian spawns** (active or wrecked) — cargo ships, transports, civilian installations. Primary source of Industrial Components.

Both routes reward exploring and engaging with the world rather than a pure kill-counter. Territory control (Stage 3) becomes a further feed once captured installations exist — not part of Stage 1, since territory doesn't exist yet.

**Confirmed component names, structure, and sourcing rules.** Three different components, not one generic currency:

- **Industrial Components** — gates Advanced Civilian production blocks. Sourced from: advanced production blocks (a player can *manufacture* these, not just find them), trade-location purchases, and loot — weighted heavily toward civilian-themed spawns, present in smaller amounts on military-themed spawns too.
- **Military Components** — gates first-tier combat hulls at low quantity. Sourced from: non-basic weapon blocks, advanced production output, trade-location purchases, and loot — weighted heavily toward military-themed spawns, present in smaller amounts on civilian-themed spawns too.
- **Advanced Military Components** — gates top-tier combat hulls. Sourced from: advanced weapons, advanced production output, trade-location purchases (**gated behind good faction standing**, not just currency), and loot — at very low quantity on spawn classes that wouldn't themselves need Advanced Military Components to build as a player core, and at meaningfully higher quantity on spawn classes that would. This ties loot value directly to the same tier scale as build cost, rather than being a separate system that happens to coexist with it.

**Basic/starting-tier everything uses plain vanilla components, no specialized item at all** — the unlock materials only enter the picture once a player is unlocking *beyond* the starting rung. This applies to weapons too: **basic weapons cost vanilla components, non-basic/advanced weapons cost Military Components** (or Advanced Military Components at the top end) — the same tiering logic already applied to hulls, applied consistently to weapon blocks as well. Practical implication for the weapon rebuild: the `WeaponBlocks` BlockGroup splits already done (Guns/Bombs/Torpedoes/Smoke) will need a further basic/advanced sub-classification once real costing starts — flagged as a to-do, not resolved yet.

**Trade locations** (Stage 5's planned economy stations) will eventually sell Industrial, Military Components, and Advanced Military Components all locked behind good faction standing — the first concrete gameplay payoff for Stage 2's reputation system, which otherwise is purely behavioral/flavor. Reputation stops being just "how NPCs act toward you" and becomes something that gates real economic access.

**Fleet-size limiting: fixed cost + finite income, not escalating price.** A core costs a flat amount of a genuinely scarce component; since the component is earned at a tuned (slow) rate, a player's standing fleet is naturally capped by how much they've banked and haven't spent — no special mechanic needed for this, it's just a normal crafting cost against a limited income rate. An escalating-cost variant (each *additional* core of a type costing more, based on how many are currently built and standing — not lifetime built, resets on loss) is a genuinely different and harder feature, since it requires live-tracking currently-placed grids and dynamically adjusting a cost, which needs real scripting rather than static XML. Interesting for later, but it's a Stage 5+ idea, not Stage 1.

**Production-block spam is already a solved problem, not a new one.** `BlockLimits` on the core itself already caps "how many Assembler blocks total" independent of what gates the upgrade — no separate anti-spam mechanic needed if Advanced Civilian production blocks end up costing Industrial Components.

### Two smaller open items

- **Static defensive-only grids** (a "gun outpost" concept) — already covered by the existing Base/Outpost `WeaponCap` BlockLimits (4 and 2 respectively), no new core type needed. Revisit only if that turns out to feel insufficient in practice.
- **Small utility grids with no functional blocks** (ramps, bridges, similar) — most core-limiting mods of this style allow a small default block count on any grid with no core placed at all, specifically so minor builds aren't blocked. Confirmed for Ship Core Framework specifically. Non core grids will also have a production block limit of 0.

### Core costs by tier (confirmed)

| Category | Starting tier (vanilla components) | Mid tier (Military Components) | Top tier (Advanced Military Components) |
|---|---|---|---|
| **Naval** | Civilian, Corvette | Destroyer (low), Cruiser (many) | Heavy Cruiser (low), Battleship (very many), Carrier (medium) |
| **Air** | Cargo Plane, Fighter | Attacker/Bomber (low–medium) | Heavy Bomber (low) |
| **Wheeled** | Transport, Armored Car | Light Tank (very few), Medium Tank (low) | Heavy Tank (low) |
| **Base** | Base, Outpost — hard `MaxPerFaction`/`MaxPerPlayer` limit instead of component gating (Outpost intentionally allowed higher than Base) | — | — |

Air and Wheeled costs should sit **cheaper overall** than the equivalent Naval tier at the same component quantity — both are small-grid-only and far more likely to be lost/destroyed in normal play than a ship, so the same nominal "low amount" of Military Components should represent a smaller real cost for a plane or tank than for a destroyer.

**Production/tool blocks:** Basic tier uses vanilla components; Advanced tier uses Industrial Components. Upgrade modules for assemblers/refineries potentially also cost Industrial Components — spam risk here is already covered by `BlockLimits`, not something to solve separately.

**Practical Stage 1 scope:** only the vanilla-cost starting tier plus the Destroyer proof-of-concept (low Military Components) actually need to exist for Stage 1's playable milestone. Everything else in the table above — Cruiser and up, Attacker and up, Light Tank and up — is real, confirmed design, but belongs with Stage 5's deeper roster.

### Salvage loop

Non-hostile, pre-damaged versions of existing hulls (a hulked Francesco Crispi, a stripped Golo) as static, explorable salvage sites — existing prefabs with `IsPirate:false` and some blocks pre-removed/damaged. Should cover both military-themed wrecks (Francesco Crispi, Le Triomphant) and civilian-themed wrecks (Golo) to match the military/civilian loot-sourcing split above, not just one or the other. Towed or ground down at a Base/Outpost, feeding the unlock economy. No new grids required, so this belongs in Stage 1 rather than waiting on Stage 4.

### Do NPCs need Ship Cores?

**Working answer: no.** Ship Core Framework's `MaxPerFaction`/`MaxPerPlayer` design is oriented around limiting player-built grids — an MES-spawned NPC ship isn't "progressing," it's pre-built encounter content, same as today. This matches the broader ecosystem convention (Block Restrictions explicitly differentiates `AllowedForNPC`/`AllowedForPlayer`/`AllowedForUnowned`) of exempting NPC ownership from player-progression systems by default. Not fully settled, though — found a real, unresolved forum comment from someone hitting this exact ambiguity in practice with a different but related core mod ("tried spawning ships with the core[s] on them there was no level set to them"). Treat as a real to-do, not an assumption. (Note: your own correction — SDX2 runs MES underneath, with AI Enabled/Crew Enabled as supplementary systems rather than a replacement — makes SDX2 more relevant evidence here than originally credited, not less, since it's a real MES-based server coexisting with a core-progression mod.)

**To-do:**
- [x] Once Ship Core Framework is actually integrated, test spawning an NPC ship with no core at all — confirm it isn't rejected, capped, or otherwise affected before assuming NPCs can stay core-free..
- [ ] Add type-appropriate ammo to NPC cargo loot — naval gun ammo on ships, aircraft gun ammo on planes — extending the existing `WW2-Loot-*` profiles per ship class. Blocked on confirmed ammo magazine SubtypeIds from Fletcher Armaments/Consty's Ordnance (separate from the weapon *block* IDs already gathered — ammo magazine IDs are typically distinct strings, need their own lookup pass once the weapon rebuild settles).

**To-do:**
- [ ] Add Industrial/Military/Advanced Military Components to existing `WW2-Loot-*` container profiles at tuned drop frequencies, per the military/civilian sourcing split confirmed above.
- [x] Sub-classify the existing `WeaponBlocks` `BlockGroup` split (Guns/Bombs/Torpedoes/Smoke) into basic vs. advanced, to support vanilla-vs-Military-Component weapon costing.
- [x] Test subgrid pitch-down behavior on an actual plane incrementally from 150 up toward 250 m/s before committing to the world speed increase. World speed set to 150. Nose down became problematic at 150m/s.
- [ ] Write `ShipCoreConfig_World.xml` (confirm `MaxPossibleSpeedMetersPerSecond` — 150 or 250 pending the physics test above — plus `MassTypeMode`, `FrictionSpeedValueMode`).
- [x] Write `BlockGroup` definitions: `ProductionBlocks`, `CargoBlocks`, `ToolBlocks` (can start now, vanilla types) and `WeaponBlocks` (blocked on weapon rebuild).
- [x] Add Ship Core Framework as a mod dependency to the world/mod list.
- [x] Smoke test before committing further: write one minimal `ShipCore` definition (no component cost, just a block limit or two) and place it on a throwaway test grid. Confirm the block limit actually enforces, `/core` commands respond as documented, and the correct `MaxPossibleSpeedMetersPerSecond` value is taking effect — before writing any of the real definitions below on top of an unverified foundation.
- [x] Write the eight Stage 1 `ShipCore` XML definitions (Civilian Naval, Corvette Naval, Cargo Plane, Fighter, Transport, Armored Car, Base, Outpost) — vanilla-component cost.
- [x] Write the Destroyer `ShipCore` definition — low Military Components cost — as the Stage 1 proof-of-concept unlock tier.
- [ ] Spawn-condition a small number of salvageable wreck variants of existing hulls, covering both military and civilian spawn themes.

---

## Stage 2 — Squadron, Reputation, and Morale Depth

No new grids required. Design sourced from a three-way comparison of Ares at War (AaW), MES Shared Behaviors (MSB — enenra's public MES behavior library, github.com/enenra/mes-shared-behaviors), and GVK Deserts of Kharak S10, reconciled below. Confirmed decisions, not open questions.

**Squadron/escort tracking: MSB's `CommandChain` system, adopted wholesale.** AaW's own squadron pattern was considered first (each plane in a fixed numbered flight — Squadron1/2/3 — broadcasts its own death via a command code, survivors watch boolean flags for their specific wingmen and break off once all others are down) and works, but it's hand-authored per fixed slot count, not generic. MSB's `CommandChain` does the same job — leader/escort join-up, renaming, and death notification via `CommandCode` broadcast (`MSB_LeaderDead`) and boolean state (`LeaderInactive`) — but uses RivalAI's native `AssignEscortFromCommand` for slot assignment, so it scales to however many escorts actually spawn rather than needing a separately-authored trigger/condition pair per numbered slot. **Decision: use MSB's mechanism as the underlying system; flight size of 2–3 planes remains a content choice (how many fighters get spawned into a given encounter), not a constraint baked into the tracking logic itself.**
- Add `MSB_System_CommandChain_Leader_TriggerGroup` to flight leaders, `MSB_System_CommandChain_Escort_TriggerGroup` to wingmen, per MSB's usage notes.
- Both require one of `MSB_DynamicCommon_TriggerGroup`/`MSB_StaticCommon_TriggerGroup` present on the encounter first, per MSB's base requirements.

**Layered reputation: binary contribution-threshold gate, not tiered credit.** Sourced from AaW's `_FAC` REPSystem (`FAC-Context-REPAHE.sbc` and per-faction siblings): a hostile unit's death (`Type:Compromised`) checks `CheckCustomCounters:CountPlayerDamage >= 15` before awarding reputation with the beneficiary faction (i.e., killing something a faction also considers hostile improves standing with them — "enemy of my enemy"), radius-shared so nearby faction-mates of the killer get credit too. MSB's `_DynamicCommon.sbc` separately tracks three finer-grained kill-credit outcomes (`Compromised_PlayerHelped` / `Compromised_PlayerMadeFinalShot` / `Compromised_PlayerNotHelped`) which could in principle tier the reward. **Decision: don't tier it.** Two players who both clear the damage-contribution minimum should get equal full credit — a partial-credit tier risks one contributor feeling shortchanged relative to the other for what reads, in the moment, as equivalent effort. Use AaW's binary threshold shape; MSB's finer states aren't needed for this.
- Replace the flat ±1 `ReputationDamage` trigger with `CheckCustomCounters:CountPlayerDamage` gating a `ChangeReputationWithPlayers` action on relevant hostile-to-Gray/hostile-to-Green kills.
- Radius-share the reputation gain to nearby faction members of the credited player, matching AaW's `ReputationChangesForAllRadiusPlayerFactionMembers`.

**Morale: adopted, in scope for this stage.** Sourced from MSB's `System_Morale.sbc` (marked WIP/unfinished upstream, but functional): a decaying `CustomCounter` (starts ~100, ticks down under sustained combat, drops sharply on low health / weapons lost / leader death / nearby losses) that swaps `TargetProfile` at low-morale thresholds — a shaken unit doesn't hard-retreat, it just stops proactively hunting at range (detection range effectively shrinks). Entirely pure-XML, no scripting, self-contained — doesn't gate or get gated by anything else in this stage. Pairs conceptually with the already-built (separately tracked, not part of this roadmap item) plane disable/spiral-crash mechanic — low morale plus a real chance of visibly losing control, rather than either alone, is the intended combined effect; that mechanic will be refined and expanded on its own timeline, noted here only for context.

**To-do:**
- [ ] Add MSB's `_Common` TriggerGroup (Dynamic or Static as appropriate) as a base requirement to fighter behaviors before layering CommandChain on top.
- [ ] Wire `MSB_System_CommandChain_Leader_TriggerGroup` / `_Escort_TriggerGroup` into existing fighter flights; confirm flights still read as 2–3 plane groups in practice even though the mechanism itself is N-agnostic.
- [ ] Build the `CountPlayerDamage`-gated reputation actions for Gray/Green "enemy of my enemy" kills, radius-shared to nearby faction members.
- [ ] Add MSB's `MoraleSystem_TriggerGroup` to fighter (and eventually other) behaviors; tune decay/threshold values against actual playtests rather than assuming MSB's defaults fit WW2 Encounters' pacing.
- [ ] Playtest: confirm squadrons visibly thin out/disengage as CommandChain reports losses, confirm reputation responds to *who* you fight, confirm morale-driven target-profile shrinkage is noticeable without being confusing.

---

## Stage 3 — Capturable Territory

No new grids required, and no Custom Planet dependency for this base version (see the corrected territory-growth mechanism below — the earlier assumption that true radius-based expansion needed hand-placed regions on mod-controlled terrain turned out to be wrong; see next section). Adapt AaW's Capturable/CapturableController pattern to one installation type (Ammo Depot is simplest) — on capture: recolor grid, flip block ownership, disable old owner's supply triggers, enable new owner's. Hooks into existing `WW2-Manipulation-AmmoDepots` and faction-override plumbing. Confirmed via AaW's actual files: ownership state is a per-installation `SandboxBoolean` (`{Faction}{SpawnGroupName}`) re-checked at world load (`Type:Session` trigger) and re-applied via `RecolorGrid` + `ChangeBlockOwnership` — this is the pattern to replicate.

**General "stay out" zones: MSB's `AreaRestriction` system, adopted broadly — not scoped to capturable installations only.** Sourced from MSB's `System_AreaRestriction.sbc`: pre-built radius tiers (100/1000/2500/5000m) that warn a neutral/hostile player on entry, then apply periodic reputation loss (`-25` every 10s in MSB's default) for as long as they linger, via a `PlayerNeutral`-gated `Manual` trigger pair (in-range/out-of-range) plus a repeating `StillInRange` timer. Functionally similar to AaW's separate `CapturableController` loiter-penalty layer, but general-purpose rather than capture-specific. **Decision: use this generically** — around capturable installations (discouraging enemy loitering pre-capture), around non-capturable but sensitive locations (a permanent naval base that should never be taken but should still be respected), and specifically to discourage players from building bases too close to non-friendly NPC installations. Treat it as a reusable tool to reach for anywhere "don't hang around here uninvited" is the desired signal, not a one-off built just for capture.

**To-do:**
- [ ] Build capture trigger chain for Ammo Depot, using AaW's session-load boolean-check pattern.
- [ ] Add MSB's `AreaRestriction` TriggerGroup (radius tier TBD per installation type) to the Ammo Depot and to at least one non-capturable installation, to prove the generic use case alongside the capture-specific one.
- [ ] Wire captured territory in as a third feed into the Stage 1 unlock currency (confirmed design — see Stage 1).
- [ ] Playtest: confirm a captured depot actively works for its new owner, and confirm the AreaRestriction warning/reputation-loss cadence feels like a nudge rather than a punishment.

### Stage 3 expansion — territory growth, ownership model, and War Level (confirmed design, corrected and expanded twice from earlier versions)

**Correction to a prior assumption in this roadmap:** an earlier version of this section assumed true radius-based territory expansion "would need MES to compute which installations currently fall inside a changing radius — not confirmed to exist as a feature," and designed around that limitation using hand-placed regions sequenced after the Custom Planet track. Reviewing GVK Deserts of Kharak S10's actual files disproves that assumption. GVK implements exactly this, in pure MES/RivalAI XML, no scripting or custom terrain required, via `CustomSandboxCounter` thresholds driving `ChangeZoneAtPosition` actions on nested pre-defined radius tiers (`ZoneRadiusChangeType:Set`, `ZoneToggleActiveMode`, both growth and shrinkage native via paired Enable/Disable conditions per tier).

**Superseded again: multi-anchor per-faction zones, not one zone per faction.** A single circle per faction (Rome for Gray, Toulon for Green) produced awkward overlap and didn't read as a real frontline once mapped against the actual custom planet's geography. Confirmed replacement: **each faction gets several independent anchor points**, each running its own full copy of the GVK mechanism (own nested radius tiers, own counter, own growth/shrink triggers) — not one shared faction-wide zone, and not a "nearest anchor" computation either (see below, that turned out to be unnecessary).

**Confirmed anchors:**
- **Gray:** La Spezia, Rome, Tripoli — **updated 2026-08-23 per direct request:** Gray's African anchor moved from Tunis to Tripoli.
- **Green:** Toulon, Oran, Alexandria — Green stands in as a broader Allied Mediterranean presence for now (French-themed first, per the project's existing "France is under-covered for this period" framing), not literally France alone. This resolves the Alexandria/Oran historical-ownership question: not an error, an intentional placeholder ahead of the confirmed later UK/Green sub-faction split.
- **Likely contested, no fixed lean:** Algiers, Tunis, Benghazi, Sardinia/Corsica — Benghazi in particular is historically apt here, since it changed hands repeatedly in the real desert campaign. Tunis moved into this list from the confirmed-Gray list above (see 2026-08-23 update).
- **Thematically leaning Gray, not mechanically anchored:** Yugoslavia, Greece — matches Italy's real occupation zones in the period, but no anchor is placed there; this is a narrative note for future content, not a to-do.
- **Not a coincidence: La Spezia and Toulon do double duty.** Both are already confirmed as the Gray and Green **domain-anchor Port locations** respectively (see the buyable/stealable section below — Gray Port loosely inspired by La Spezia, Green Port loosely inspired by Toulon). The same two points serve as both a permanent buyable/sellable location and a territory-growth anchor. Rome, Tripoli, Oran, and Alexandria are territory-growth anchors only, with no domain-anchor building tied to them.

**Radius tiers: 5 tiers per anchor, 8/13/19/26/34 km.** Checked against the now-confirmed planet size (60km diameter / 30km radius, corrected below in the Custom Planet section from an earlier session's wrong 60km-*radius* assumption): well inside the proven-working range, since GVK's own outer zone tier reaches 55km on the identical-sized Pertam-based planet (55km is a great-circle surface distance, not a straight-line diameter measurement — see the Custom Planet section's full reasoning, which also corrects an internal error of its own on this exact point). In-game measurement of actual anchor-to-anchor distances on this planet is still an open to-do, since the tier numbers above are a reasonable starting scale rather than something derived from measured distances yet.

**Point award mechanism: ring-weighted, no nearest-anchor logic needed.** Originally scoped as requiring some approximation of "nearest anchor" (since true distance-comparison-across-anchors isn't something static triggers do well), but the confirmed design sidesteps that entirely: **every anchor evaluates every relevant event against its own rings independently**, and awards its own counter based on where the event falls relative to *that anchor specifically* — event within that anchor's rings 1-3 = 4x base value to that anchor's counter; within rings 4-5 = 2x; outside ring 5 = 1x (flat, faction-wide baseline). An event near La Spezia naturally scores high on La Spezia's counter (falls in La Spezia's inner rings), and only baseline on Rome's or Tripoli's counters (falls outside their rings), purely because each anchor is checking its own distance bands — no cross-anchor comparison is computed anywhere. Every event good for a faction nudges every anchor of that faction, weighted by that event's proximity to each one individually.

**Overlap handling — two different scopes, not one blanket rule:**
- **Regular dynamic spawns/combat encounters:** no special handling needed. Each faction's own-zone `ZoneConditions` check passes independently in an overlap band, so both factions naturally spawn and act normally there — this is what actually produces the "frontline" feel, not a rule that has to be separately authored.
- **Significant fixed installations specifically** (not regular spawns): stricter placement gate — eligible to newly spawn only where "my zone AND NOT opponent's zone" holds. A location stops being eligible for a *new* significant installation the moment it becomes contested; this only governs new placement, it doesn't retroactively remove one that was already there before an overlap developed (that's handled by ownership below, not this gate).

**Fixed-installation ownership: sticky, not a live faction-strength tie-break.** Considered and dropped a three-layer "Strength" value specifically to resolve who owns a facility sitting in overlapping territory — simpler sticky-ownership rule covers it fully in plain XML with a per-installation `SandboxBoolean` current-owner flag and a zone-containment check against both factions:
1. Neutral until any faction's territory first covers it.
2. Once owned by Faction X, ownership sticks even if Y's territory also grows to cover it — overlap alone doesn't flip it.
3. Flips to Y only once Y covers it **and** X no longer does (X's territory has receded past that point entirely).

No faction-vs-faction comparison value needed anywhere in this — "first to arrive, held until exclusively displaced" is a per-installation check, not an aggregate. AaW's scripted Strength/Holdings model remains a separate, real Stage 5+ idea (see below) — just not the tool for this particular problem.

**Neutral installations:** the "Nobody faction" precedent an earlier version of this section cited from Ares at War doesn't hold up under the actual files (`Nobody` is just a wreck-prefab folder name, not a real neutral-faction pattern) — noted here again since the underlying idea (unthemed refineries, reputation-gated purchases) is unaffected by that correction and still stands on its own.

**War Level — one single shared value across both factions, not per-faction.** Corrects an earlier version of this section, which implied War Level might be tracked separately per faction; confirmed it's one dial, read the same way by Gray and Green both — a measure of how "hot" the war has become overall, not a per-side stat. Confirmed feed: NPC-attacked events raise it; it decays gradually over time on its own. Two confirmed effects, both about spawn *composition*, not just difficulty:
1. **Fixed-installation functionality/activity level** — a domain anchor or maintenance yard's stock, services, or active defenses scale with War Level, layered on top of whichever faction currently owns it via the ownership rule above.
2. **Dynamic spawn escalation, hard-gated at some thresholds** — easy/small spawns become less common and stronger spawns more common as War Level rises; some content is hard-gated to only appear above a specific War Level rather than just becoming statistically more likely. Confirmed example: air cargo convoys spawn as a single unescorted plane at low War Level; at higher War Level the same convoy type can include fighter escorts, additional defensive call-ins, and/or more cargo planes per spawn. Generalizes to other dynamic spawn types (naval convoys, patrol composition) as a pattern, layering onto the existing `ThreatScoreMinimum` spawn-gating already used for the carrier's threat threshold.

**Future upgrade path, deliberately deferred (Stage 5+): Ares at War's scripted faction-strength model.** AaW's actual implementation (`Factions.cs`) is real custom C# — a per-faction `Strength_Counter`/`Aggression_Counter` with a `Holdings` list summing captured-installation production into strength, gating a `ReadyForExpansion` flag. Genuinely richer than anything in this section, but needs scripting to aggregate live state across many grids. Confirmed as a named Stage 5+ item, not needed for Stage 3's ownership tie-break (see above) or for anything else in this section's current scope.

**Players fighting both factions stop being an edge case:** since ownership is driven by faction-vs-faction zone containment rather than direct player capture, a player hostile to both sides just slows both factions' growth rather than creating a logical contradiction about who's capturing what.

**To-do:**
- [ ] Measure actual in-game distances between the confirmed anchor points on the custom planet to sanity-check the 8/13/19/26/34km tier progression against real anchor spacing now that the planet's terrain is essentially finished.
- [ ] Build each anchor's nested radius-tier zone definitions and paired Enable/Disable timer-trigger-condition sets (6 anchors × 5 tiers each).
- [ ] Build the ring-weighted `CustomSandboxCounter` point-award actions per anchor (4x/2x/1x by ring band) for each relevant encounter/event type.
- [ ] Add `ZoneConditions` gating to existing dynamic SpawnConditions so Gray/Green spawns are restricted to their own current territory (any-anchor-of-that-faction, not anchor-specific).
- [ ] Build the stricter "friendly AND NOT enemy" placement gate for significant fixed installations specifically, distinct from the looser rule for regular dynamic spawns.
- [ ] Build the sticky-ownership boolean-flag logic per fixed installation (first-covered, held until exclusively displaced).
- [ ] Build the single shared War Level counter: NPC-attacked increment actions, gradual passive decay, and the hard-gate thresholds for higher-tier spawn content.
- [ ] Build the convoy-escalation proof-of-concept: single-plane air cargo convoy at low War Level, escorted/multi-plane version unlocked at a higher tier — smallest concrete test of the spawn-composition-scaling idea before generalizing it further.
- [ ] Decide neutral-installation scope: which installation types (refineries likely, others TBD) stay unthemed and reputation-gated rather than faction-owned from the start.
- [ ] Define "significant fixed installation" precisely enough to know which buildings the stricter overlap-placement gate applies to, versus which stay under the looser regular-spawn rule.
- [ ] Playtest: confirm the multi-anchor system visibly produces a frontline-like shape rather than uniform faction blobs, confirm sticky ownership feels intuitive rather than arbitrary, confirm the shared War Level's hard-gated thresholds read as real escalation points.

### Stage 3 expansion — buyable/stealable grids and three location tiers (confirmed design)

Built on the territory and War Level system defined above — no separate tracking system, no Custom Planet dependency (that dependency is removed along with the region-based approach it was tied to).

**Four location tiers, each a different point on the permanent/capturable/dynamic spectrum — and each maps directly onto the two-category ownership model above:**

- **Domain anchors (Ports)** — three locations per faction (Gray: La Spezia, Rome, Tripoli; Green: Toulon, Oran, Alexandria). Permanent, faction-fixed forever, never capturable — these are fixed-location installations whose *ownership* never changes, though their functionality/stock still scales with War Level per the mechanism above. Ground and naval vehicles are available for purchase; each anchor sells its faction's complete current ground/naval catalog, gated by War Level rather than by regional stock. **Aircraft are not sold here** — Airports are a separate, decoupled tier (see below), not part of the domain-anchor set.

  **Port anchor design, confirmed:** not intended as historically accurate reconstructions of real places — real installations are used as loose inspiration only, for layout logic and proportion, not as a build target to replicate. **Gray Port is loosely inspired by La Spezia** (Italy's largest naval dockyard); **Green Port is loosely inspired by Toulon** (France's main Mediterranean naval arsenal). **Design philosophy, confirmed: full (1:1) scale buildings and mooring/construction structures, but far fewer of them than the historical installations had.** The goal is an installation that reads as genuinely full-scale next to a 1:1 Aquila or Vittorio Veneto, not a compressed miniature — the compression happens in *count* of repeated facilities, not in the scale of any individual structure. (An earlier session's specific footprint figures and a primary-vs-secondary construction-slip distinction were dropped as incorrect — confirmed 2026-08-24. The actually-built La Spezia, Toulon, Tripoli, and Oran Port grids, built to read as similar in scale/layout to their real-world namesakes, are the real reference now, not any prior number.)

  **Terrain fitting, confirmed technique:** MES's voxel-spawning capability will be used to carve/scoop the correct berth/slip shapes and level the surrounding ground so the Port anchor's pier, construction slip, and quay structures sit correctly into planet terrain, rather than requiring hand-sculpted terrain to already exist at the anchor's fixed coordinates before the installation is placed.

- **Airports — decoupled from domain anchors, confirmed 2026-08-24.** Unlike Ports, Airports are **not** permanent/faction-fixed — ownership flips via the same territory-ownership sticky zone-containment mechanism already defined above for significant fixed installations, closer in kind to a Maintenance Yard than to a Port. Sells aircraft, gated to whichever faction currently owns that Airport — same "inventory reflecting whoever currently holds it" pattern already established for Maintenance Yards, applied here to aircraft specifically. This resolves the sell-location gap flagged in the prior session: once an Airport is built and placed, its stock is whatever the current owner's aircraft catalog is, not a fixed per-location roster. Candidate locations identified, build order/priority not yet decided:
  - **Gray:** Foggia, Tripoli-Castel Benito, Benina (Benghazi)
  - **Green:** Gibraltar, La Senia, Tunis/Bizerte, Cairo West, Casablanca

- **Maintenance yards** — new tier, sitting between the anchors and the hangars/garages: a fixed-location installation, permanent position, but capturable via the territory-ownership-flip mechanism above, with inventory reflecting whoever currently holds it.
- **Hangars/garages/factories** — one general prefab per faction per installation type (not per plane/vehicle variant, see the Prerequisite section's Installations note), functioning as regular dynamic MES spawns: these follow the *dynamic-encounter* half of the ownership model — they stay with their design-time faction and are gated to spawn only within that faction's current territory, rather than changing hands themselves. Which specific vehicle variant appears inside is an MES SpawnGroup selection made at spawn time, not baked into the installation prefab itself.

  **Factory production-line build states (confirmed, Fighter tier):** a Factory's interior spawn points fill with sub-spawns representing a production line rather than uniformly finished hulls. Fighter-tier factories use 6 slots: 2 fully built (100%, the untouched blueprint, no manipulation needed), 2 at a randomized 50–75% build state, 2 at a randomized 25–50% build state, using MES's `ReduceBlockBuildStates` manipulation (confirmed to only affect non-essential blocks like armor/glass, not functional blocks) via two reusable Manipulation Profiles (`WW2-Manipulation-BuildState-Stage2`, `WW2-Manipulation-BuildState-Stage1`) applied uniformly across each affected hull rather than to a random subset of its blocks. Applied identically across every Fighter variant rather than authored per-plane. Attacker tier (3–4 slots, exact count and percentage scheme TBD pending checking previous builds for what physically fits) needs its own profile(s) once confirmed.

  Each Factory instance randomly commits to one class per spawn cycle — a 6-slot Fighter production line (all 6 the same randomly-selected Fighter variant) or a 3–4-slot Attacker line (all slots the same randomly-selected Attacker variant) — never mixed within one factory instance.

**Steal-or-buy, same location, two paths to the same grid.** Anywhere a player can steal a vehicle, they can also buy that same vehicle — friendly/reputable players take the peaceful route, everyone else takes the risk-it route, both ending at the same actual hull. Applies at the hangar/garage tier specifically, where stealing already lives.

**War Level effects specific to this section** (the general War Level mechanism and its two confirmed effect categories are defined in the territory-growth section above; this is the domain-anchor-specific application of effect #1, fixed-installation functionality/activity scaling):
- **Domain anchor stock** — higher War Level unlocks better hulls/materials in that faction's complete catalog. This is the more legible of War Level's signals to a player — not "spawn tables quietly shifted," but "the shelf at the anchor visibly got better."

**Restocking has real, working precedent already shipped in this mod — it's not a new problem.** The general per-faction Factory installations already use a persistent structure periodically spawning new grids nearby (`OffenseSpawn-Factory-Plane`, `DeliverySpawn-Factory-Plane`), now generalized to spawn whichever plane variant a SpawnGroup selects rather than one hardcoded plane per Factory prefab. Maintenance yards need the same capability aimed at ground vehicles instead of aircraft. Ownership-change restocking (when a yard flips faction) doesn't need separate infrastructure either — it's one more action added to the same capture trigger chain already designed above (recolor, flip ownership, swap supply triggers, **now also: reroll stock**).

**One real, unverified gap in the restocking plan: precise spawning inside a tight interior bay.** Factory-Plane's precedent is proven for aircraft, which tolerate minor spawn-point imprecision by simply flying away; a ground vehicle spawned slightly off inside a garage bay can clip through walls or get stuck. Recommended sequencing: prove the restocking mechanic on **open platforms in an outdoor supply yard first** (no tight geometry to clip through), and only attempt precise interior-bay spawning as a stretch goal once the basic mechanic is confirmed reliable — building the harder version first risks discovering it doesn't work after already committing to it.

**One open question, deliberately not chased down yet, held per your own instinct:** whether hostility toward a specific player can be dynamically gated by that player's own threat score or prior aggression, independent of whether bigger NPCs simply exist in the world at a given War Level. This is what would let a slow/non-combat player see the war escalate around them without personally being targeted by it. Threat score is confirmed to exist and gate spawning; whether it (or reputation/counter logic) can additionally gate an NPC's *initial aggression state toward a specific player* dynamically, versus only being checked at spawn time, is unverified — worth an empirical check before this becomes load-bearing, same treatment as the P.108 gun question.

**SDX2's mission system — no longer pursuable as a reference.** SDX2's GitHub isn't public and no MES-related files could be located on the Steam Workshop (likely unlisted). Flagged as interesting in an earlier session but there's nothing concrete to evaluate against MES's actual Event system, and no further avenue to get one — dropped as a reference source rather than left open.

**To-do (blocked on the territory-growth to-dos above; Custom Planet dependency removed):**
- [x] Build the first two domain anchors (Toulon, La Spezia).
- [x] Build the second two domain anchors (Oran, Tripoli).
- [ ] Build the third two domain anchors (Rome, Alexandria).
- [ ] Build a Gray Airport — candidates: Foggia, Tripoli-Castel Benito, Benina (Benghazi); not yet prioritized/sequenced.
- [ ] Build a Green Airport — candidates: Gibraltar, La Senia, Tunis/Bizerte, Cairo West, Casablanca; not yet prioritized/sequenced.
- [ ] Design the "complete catalog" concept — which hulls are sellable at all, and how War Level gates the list.
- [x] Build cored/purchasable versions of the planes per faction.
- [ ] Build cored/purchasable versions of the ground vehicles per faction.
- [ ] Build cored/purchasable versions of some of the ships (Gabbiano, La Malouine, at least one destroyer each also)
- [ ] Wire buy-capability into existing hangar/garage locations alongside the existing steal mechanic.
- [ ] **Store blocks are currently broken** (2026-08-24) — needs investigation before any buy-capability wiring (hangars/garages, Ports, or future Airports) can be considered working. Root cause not yet diagnosed.
- [ ] Design and place maintenance yards as a new prefab tier, distinct from both anchors and hangars (Ideally make these able to repair player grids for credits).
- [ ] Create trade posts to place as neutral installations that change ownership with territory. (Buy all items and ores, sell modest quantities of lootable components and large quantities of ammo.)
- [ ] Create refinery installations to serve as neutral installations that change ownership with territory.
- [ ] Prototype restocking on an open-air supply yard before attempting interior-bay spawning.
- [ ] Add "reroll stock" as an action in the capture trigger chain for maintenance yards.
- [ ] Verify whether per-player dynamic hostility gating (threat score/reputation-based) is actually achievable in MES, before committing the "big stuff exists but isn't aggressive unless provoked" design to it.

### Stage 3 expansion — Port-based naval patrols (confirmed 2026-08-24)

**Additive layer, does not replace random dynamic combat spawns.** Combat ships continue spawning via the existing random/stationary mechanism across each faction's territory unchanged (this is what already produces the "frontline" feel per the overlap-handling section above). Patrols are a separate, second source of naval combat encounters layered on top, not a substitute — the coverage risk of relying on patrols alone (large stretches of the map between routes seeing no naval action) was the deciding factor against a full replacement.

- **Route structure: multiple lanes, not a single corner-to-corner line.** Adjacent-anchor legs per faction (Gray: La Spezia–Rome, Rome–Tripoli; Green: Toulon–Oran, Oran–Alexandria) plus at least one long-haul cross-Mediterranean lane per faction (e.g. La Spezia–Tripoli, Toulon–Alexandria), so patrol traffic crosses several different parts of the map rather than one predictable corridor.
- **Lane selection randomized per spawn cycle** — each patrol spawn rolls which anchor-pair it runs between, so which lanes are active shifts over time rather than being a fixed, learnable shape.
- **Behavior, distinct from the cargo convoy mechanic:** patrols loop (A→B→A, repeating) rather than making a one-way trip and despawning, and carry normal aggressive combat `SpawnConditions`/engagement behavior rather than the cargo convoy's passive despawn-suspension pattern. Reuses the same fixed-coordinate Waypoint Profile approach confirmed for the cargo convoy, but the patrol/looping/aggressive behavior is a materially different build, not a reskin.
- **Unverified, needs a real check before implementation:** MES's specific capability for looping fixed-waypoint patrol behavior with hostile engagement en route — believed to exist, but no confirmed trigger/behavior names checked against this project's MES version yet.
- **War Level tie-in:** patrol frequency/strength scales with War Level, same confirmed pattern as air cargo convoy escorts — more/stronger patrol traffic as the war heats up, not just static route traffic.

**To-do:**
- [ ] Verify MES's actual capability for looping fixed-waypoint patrol behavior with hostile engagement en route (specific trigger/behavior names, not just general plausibility).
- [ ] Define the full lane list per faction (adjacent-anchor legs + long-haul lane(s)) once all 6 domain-anchor Ports are actually built and placed.
- [ ] Build the randomized per-cycle lane-selection logic for patrol spawns.
- [ ] Build patrol ship combat/engagement behavior distinct from the cargo convoy's passive despawn-suspension pattern.
- [ ] Wire patrol frequency/strength to War Level, mirroring the air cargo convoy escort scaling.
- [ ] Playtest: confirm patrols read as an added layer of naval presence without making the existing random-spawn coverage feel redundant or crowded.

---

## Stage 4 — First New Grids: Basic-Tier Starters

New grid design finally becomes necessary. Deliberately small:
- Naval: one small patrol boat per faction, sized to Corvette Core.
- Wheeled: one Armored Car per faction — this is also where land combat gets its first real content instead of just installations.
- Civilian: one truck per faction to serve as a respawn vehicle. Include survival kit, basic refinery, basic assembler, cargo.

Air can wait — the existing Fighter roster already covers the starting tier.

**To-do:**
- [x] Create 150% scale Fiat 626 truck.
- [x] Create 150% scale AHN truck.
- [ ] Design Gray Armored Car (AB 41).
- [ ] Design Green Armored Car (Panhard).
- [ ] Wire all four into SpawnGroups/Behaviors/Loot per existing pipeline.
- [x] Create respawn mod with the trucks.

---

## Stage 5 — Elite Tier, Escalation, and Polish

By now the loop is proven end to end. Roughly in order of effort:

- **Rest of each ladder:** Destroyer → Cruiser → Heavy Cruiser → Battleship → Carrier (Naval); Attacker/Bomber → Heavy Bomber (Air); Light Tank → Medium Tank → Heavy Tank (Wheeled). **Correction, confirmed via research:** Trento, Zara, and Algérie are all officially classed Heavy Cruisers (Washington Treaty tonnage), not plain Cruisers — Emile Bertin alone is a genuine light Cruiser. Corrected roster: **Cruiser tier — Bartolomeo Colleoni** (Gray, Giussano-class, famously lost at the Battle of Cape Spada) and **Emile Bertin** (Green). **Heavy Cruiser tier — Trento and Zara** (Gray) and **Algérie** (Green, confirmed France's only heavy cruiser under treaty limits — no sister ship, an honest asymmetry to keep rather than fix, same shape as the Italian heavy-tank gap). **Battleship — Strasbourg** (Green, Dunkerque-class; existing blueprint from an established builder, to be adapted) and **Vittorio Veneto** (Gray, Littorio-class; a real, purchasable 1/1800 reference model exists on MyMiniFactory, and the class is well-represented across Sketchfab/CGTrader/physical scale kits too — picked over sister ships Littorio and Roma for having the broadest active-service record: present at both Taranto and Cape Matapan, rather than fame tied to a single event). Note: Algerie and Emile Bertin both carry the AWG piston/hinge catapult-floatplane assembly responsible for the grid-loading bug (see Stage prefab checklist) — factor the rebuild into their timelines, not just Emile Bertin's rebuild-list entry.

### Air roster — Attacker/Bomber and Heavy Bomber tiers (confirmed)

**Gray (Italy):**
- **Attacker/Bomber tier — Breda Ba.88 and Savoia-Marchetti SM.79.** SM.79 ("il Gobbo Maledetto," the damned hunchback) is one of the most famous Italian aircraft of the war, especially in its torpedo-bomber ("Silurante") role — a dedicated torpedo variant is planned specifically to threaten player ships. Ba.88 fills the lighter/entry role — genuinely real and well-documented, but its actual historical reputation is "notoriously one of WW2's worst operational aircraft," which fits an entry-tier plane thematically rather well rather than being a downside.
- **Heavy Bomber tier — Piaggio P.108.** Italy's only operational four-engine heavy bomber. Existing impressive model already built, needs rebuild onto the new weapon systems.

**Green (France):**
- **Attacker/Bomber tier — Lioré et Olivier LeO 451.** Widely regarded as the best French bomber of the war — the closest French equivalent to SM.79 in national significance, even though it arrived too late and in too few numbers to matter much before the armistice. Optional third pick for full symmetry with Gray's entry-tier: **Bloch MB.210** (older, more obsolescent by 1940, same "weaker starting bomber" role Ba.88 fills for Gray) — not decided, just flagged as available if wanted later.
- **Heavy Bomber tier — Farman F.222.** Confirmed via research to be far more significant than expected: explicitly classified as a heavy bomber, "the only four-engined bomber in front line service with any Allied air force" at the start of the Battle of France, flew 63 night-bombing sorties over Germany in May–June 1940, and a related variant flew the first-ever Allied bombing raid on Berlin. Civilian use was the historical *secondary* role, not primary — the reverse of how the existing asset is currently used (Cargo Plane tier). Strong case for reusing/varianting the existing F.222 build into a military-spec version rather than starting from scratch — build approach (shared base hull vs. two separate builds) still an open call.

**Reference material found for new builds** (BA.88, SM.79, LeO 451 all appear to come from the same modeler's consistent "Historic Aircraft (1914–1974)" series on CGTrader — worth building from one consistent source rather than mixing styles):
- SM.79: multiple free/paid 3D models on Sketchfab and CGTrader, including several explicitly modeled as the torpedo-bomber variant.
- Ba.88: 3D model on CGTrader (same series as above), plus a dedicated multi-view blueprint on drawingdatabase.com.
- LeO 451: 3D model on CGTrader (same series), three-view drawings also findable via general image search.
- P.108: no new search needed — existing asset already covers this.


- **Upgrade Modules:** convert core-swap progression to modules-on-top-of-one-core where it improves the feel (per the confirmed schema support above).
- **Retaliation/escalation** (from AaW): tiered "strike back" raids against captured territory, cooldown-limited. Reuses Stage 3's capture plumbing.
- **Fleet/Escort formalization:** generalize the existing `CarrierSpawn` escort pattern into something reusable across ship classes. Now that Stage 2 adopts MSB's `CommandChain` for squadron/escort tracking (see Stage 2 — this superseded the originally-planned AaW `EscortSystem` port, since MSB's version is already N-agnostic rather than needing generalization from a fixed-slot pattern), this item is really "apply the existing CommandChain mechanism to carriers and their escorts," not a new system.
- **Faction Strength/Holdings scripting** (from AaW, deferred from Stage 3): the scripted upgrade path already named in Stage 3's territory section — aggregate captured-installation production into a real per-faction Strength value once the flat GVK-style counter has been proven. This is the natural point to introduce custom scripting into the project, staged deliberately after Stage 3's simpler XML-only version is playable.
- **Narrative flavor:** ScenarioTools for MES Events (Workshop 2998575759) for NewsFeed broadcasts and dynamic GPS markers, no custom C# required.
- **Economy stations** (AaW + GVK both): superseded by the fuller domain-anchor/maintenance-yard/hangar design confirmed in Stage 3's expansion — see there for the current version rather than this placeholder.

---

## Parallel track — Custom Planet

Doesn't block or get blocked by the numbered stages themselves — new terrain doesn't need Stage 4's new hulls to exist, and Stage 4's hulls don't need new terrain either. Pick this up whenever, including alongside the weapon rebuild, since it's a genuinely different skill (terrain/image work, not XML/grid design) and switching between them may be a welcome break rather than a distraction.

**Correction to a prior version of this roadmap:** this section previously claimed Stage 3's territory expansion had a hard dependency on the Custom Planet track, since the design at the time relied on hand-placed capture regions needing reliable spacing and terrain under the mod's own control. That design has since been replaced — GVK Deserts of Kharak S10's actual files show true radius-based territory growth is achievable in pure MES/RivalAI XML via `ChangeZoneAtPosition`, with no terrain requirements at all (see Stage 3). The Custom Planet track is once again fully non-blocking, same as every other parallel track — pick it up purely on its own merits (a Mediterranean coastline is still a strong fit for this mod's setting), not because anything else is waiting on it.

**Planet size: confirmed at 60km diameter (30km radius) — the same size as GVK's own Kharak planet, not just using it as a size reference.** Verified via two independent sources: GVK's own community guide states Kharak's terrain is "based on the vanilla planet Pertam, which has been highly modified with smooth roads, higher peaks, traversable canyons, and fun slopes" — no resizing mentioned, so it inherits Pertam's native diameter — and the official Space Engineers wiki's planet-size table lists Pertam at 60.00km diameter (vs. 120km for EarthLike/Mars/Alien, 19km for moons). GVK's own Zone 3 boundary ("52km+" from their Crossroads hub, per their server guide) is consistent with this: that's a great-circle surface distance, and the maximum possible surface distance on a 30km-radius sphere is roughly 94km (half the circumference), so a 52km zone edge fits comfortably without implying a larger planet. **Correction to an internal inconsistency from an earlier pass:** this same reasoning was previously followed immediately by a claim that GVK's 55km outer zone "does not fit a 30km-radius planet" — that claim contradicts the great-circle math directly above it. It was wrong on both counts: 55km fits the same way 52km does, and in any case WW2 Encounters' own confirmed territory-zone tiers (8/13/19/26/34km, see Stage 3) sit comfortably inside that proven range regardless, so this was never actually a live constraint on this project's own numbers.

**Status: heightmap/material/ore pipeline complete and working, built as its own third mod (`WW2-WitM-Planet`).** What follows is a summary of confirmed decisions; full technical detail (formulas, scripts, the cube-face geometry mapping) lives in `WW2-Reference-PlanetCreation.md`.

**Confirmed design:**
- Real elevation sourced via OpenTopography (SRTM15Plus), spanning mid-Iberia to just past Suez — terrain.party couldn't handle the needed area, so the originally-planned tool changed.
- Wraparound design: the full west-to-east real extent (~3,300km) wraps onto the planet's full equatorial circumference (188.4km) as a closed loop, with land occupying the outer two-thirds (nearer each pole) and water the middle third — giving roughly 62% land / 38% water, close to the 1/3-1/3-1/3 target.
- Differential horizontal compression (~15.9:1 for the main Iberia-to-Crete span, ~24:1 for the emptier Crete-to-Suez stretch) and uniform vertical compression (23.5:1).
- Elevation relief retuned to match Pertam's real scale (~6.2:1 land compression, giving comparable peak heights to vanilla) after an earlier, much flatter first pass read as lifeless in-game — the confirmed 60km/30km-radius size match with Pertam (see above) is what made this direct scale reference possible.
- A real hazard specific to this engine was found and fixed: Water Mod's water is a fixed-radius sphere from planet center, not a terrain-following mesh, so any inland depression below sea level would flood regardless of connectivity to the ocean. 758 such disconnected pockets were found and raised (two large enough to be real closed basins, plausibly the Qattara Depression among them).
- Deliberately did **not** floor coastal land to a safety margin above Water Mod's default ±2m wave range — an earlier attempt at this produced an artificial terraced "shelf" at every shoreline; natural gentle slopes into the wave zone were judged better than that artifact.
- Cube-face geometry fully debugged and confirmed correct via extensive in-game testing: side faces use a Y-negated gnomonic projection; pole faces need their north/south content **swapped** and then **both** given a left-right mirror flip. This exact mapping is documented directly in the `.sbc` comments and must be reused for any future texture work on this planet (biome map, etc.).
- Material system: `ComplexMaterials` height/slope rules (no texture needed) handle basic sand/grass/rock/peak banding; a real material texture (red channel) adds a north/south hemisphere split so the African coast reads as arid (CrackedSoil/DesertRocks/DustyRocks) versus the European coast's grass palette — the one thing height/slope rules alone couldn't do, since this planet's wrap means SE's built-in Latitude concept can't distinguish the two poles' hemispheres.
- Ore system: real cited EarthLike ore-type proportions (Silicon/Magnesium/Iron/Cobalt/Nickel/Gold/Silver), Ice's proportion replaced with Platinum + Uranium (for a planned Platinum/Uranium-gated advanced-component crafting restriction), scaled to 4% land coverage, biased toward coastlines and away from the exact poles, with extra clustering around real WW2-significant North African ports (Tunis, Bizerte, Tripoli, Benghazi, Tobruk, Alexandria) to bias player activity toward historically active areas. Distribution within that budget is otherwise random — deliberately deferred, not a gap.
- "Ice" material used for all water deeper than -7.5m real depth, so the planet reads as having oceans from orbit even when Water Mod's water isn't actively rendering.

**Known open items:**
- [ ] Biome GREEN channel (foliage/environment items) not built yet.
- [ ] `Uraninite_01` (used for Uranium ore) confirmed real only against an asteroid materials reference, not a planet one — watch the load log for this specific line.
- [x] Water Mod's working radius for this planet is empirically `/wradius 1.02862`, not the ~1.0 the HillParams math would suggest — result of wradius 1.0 being based on the lowest point of the surface, not "sea level".
- [ ] Ore distribution is random-within-budget; revisit if specific historical/gameplay-driven placement becomes worth the effort.
- [ ] Spawn in a fresh test world and confirm WeaponCore/MES don't exhibit raycast issues (the ≤2048px constraint was respected throughout, but hasn't been explicitly re-verified against live NPC/weapon behavior).
- [x] Once terrain is fully settled: place port locations (existing Base/Outpost/Hangar prefabs) and plan convoy GPS routes using the existing SpawnGroup/patrol pipeline.
- [x] Sky doesn't render blue correctly (2026-08-21) — atmosphere/sky settings need investigation, root cause confirmed that planet was first spawned with atmosphere=false and does not re-check. Fixed by placing new planet.
- [x] Pull tree density and grass distribution parameters from vanilla Orcus into this planet's environment-item definitions (2026-08-21) — Orcus already used as a voxel-material reference on this project (`VoxelMaterials_Orcus.sbc`), now also wanted for its foliage density/placement, not just its materials.

---

## Parallel track — Map Tool (deferred decision)

Not scoped yet on purpose — see the ROI discussion above. Revisit once Stage 3/5's capturable territory is live and actually contested; a map has nothing dynamic to show before then. When that point arrives, work through the three tiers (existing Workshop map script → custom LCD/GPS script → bespoke external tool) in that order, stopping at whichever tier actually earns its cost.

---

## Long-term vision

**Sub-factions.** Not scoped yet, named so early architecture doesn't box it out. Splitting Gray/Green into historical sub-factions (Germans/Italians under Gray; French/UK/US/Soviets under Green, or some other split) matches AaW's structure of many factions under fewer broad alliances. Practical implication for everything above: avoid hardcoding "GRAY"/"GREEN" any more than necessary, prefer patterns that generalize to a new faction being copy-and-retarget rather than a rewrite.

**First wave, confirmed: Germany (Gray) and UK (Green).** UK over US for the first wave — Britain was Italy's primary Mediterranean opponent for nearly the entire period the existing roster already centers on (Taranto, Cape Matapan, Malta convoys, the early North African campaign all predate US Mediterranean involvement, which didn't begin until Operation Torch in November 1942). UK also brings more genuinely new content to players, since it's less commonly built in the existing SE Workshop ecosystem than US equipment — the tradeoff being fewer existing builds to reference/borrow from, which is a real cost but a manageable one given the mod's existing pattern of building from scratch (Fiat 626, Renault). US remains a strong later addition, natural to pair with a Sicily/Italy-invasion-era expansion rather than this first wave.

**More ore/ingot variety, SDX2-inspired.** SDX2 doesn't add a resource layer beneath their components — it just adds more raw ores and ingots alongside vanilla's existing set (lead, copper, etc., sitting next to iron, silicon, cobalt). So the actual tradeoff here is simpler than "add a new economic tier": it's whether refining/component-building recipes get more varied and specific, or stay on vanilla's existing ore set. Still a real accessibility-vs-depth call — more ore types means more mining/refining variety but also more for a new player to track — but it's additive breadth, not structural complexity. Parked for the same reason as the custom planet and map tool: not needed to prove Stage 1's core loop, easy to add later by defining new ore/ingot pairs and wiring them into whichever recipes should use them, without needing to touch the three-component system's own structure at all.

---

## Why this order

The weapon rebuild has to happen first, full stop — nothing else is testable on top of a dead weapon mod. After that, Stages 1–3 are entirely trigger/XML work on the (now-rebuilt) existing roster and get a genuinely different-feeling mod — full core progression across ships/planes/tanks/bases, squadron behavior, reputation, capturable territory, salvage economy — before a single new hull needs designing. Stage 4 is deliberately small so the payoff of "new grids" gets tested cheaply before Stage 5 asks for more of them. Each stage's playable milestone stands on its own.

The two parallel tracks (Custom Planet, Map Tool) sit fully outside this sequence — they don't block the stages and the stages don't block them. An earlier version of this roadmap had Stage 3's fuller territory expansion waiting on the Custom Planet track; that dependency is gone now that GVK's radius-growth mechanism (see Stage 3) proved terrain control was never actually required. Pick up the planet work whenever the XML/grid work needs a break; leave the map tool alone until there's live territory worth mapping.