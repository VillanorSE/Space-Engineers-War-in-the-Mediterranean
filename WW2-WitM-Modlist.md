# WW2 Encounters — Modlist

## Flags

- **[CORE]** — foundational framework, everything else depends on it
- **[REQUIRED]** — confirmed needed for the mod as designed
- **[PENDING]** — direction confirmed, one specific detail still needs verifying
- **[REVIEW]** — you flagged this yourself as possibly redundant/unnecessary
- **[RETIRE]** — being actively replaced, do not carry forward once the rebuild is done
- **[ID?]** — Workshop number not independently confirmed this session; don't treat any number here as real unless it's marked found

---

## Core Framework

1. **Modular Encounters Systems (MES)** `[CORE]` — Workshop ID: **1521905890**
2. **WeaponCore 3.0** `[CORE]` — Workshop ID: **3154371364**
3. **Water Mod** `[CORE]` — Workshop ID: **2200451495**
4. **Text HUD API** `[CORE]` — dependency for other mods' HUD elements. Workshop ID: **758597413**

## Weapons — confirmed per domain

5. **Fletcher Armaments (WeaponCore Edition)** `[REQUIRED]` — naval. Workshop ID: **2844434226**
6. **Consty Aircraft Pack – Ordnance (WeaponCore) 1.0** `[REQUIRED]` — air, locked to period-appropriate subset. Workshop ID: **2881339118**
7. **KONTAKT Ground Systems [WeaponCore] v1.0** `[REQUIRED]` — ground vehicles. Workshop ID: **2930049835**
8. **Terran Titans - WW2 Weapons Pack (BETA 0.0.4h)** `[REQUIRED]` - Torpedos and some gun turrets. Workshop ID: **3043566172**

## Structural / Aircraft

9. **[CAP] Consty's Aircraft Pack (Now with wings!)** `[REQUIRED]` — Workshop ID: **2758773086**
10. **Thin Wings, Plane Parts & Control Surfaces** `[REQUIRED]` — explicitly designed as CAP's companion mod (CAP's own description recommends it by name). Workshop ID: **2612368868**
11. **Plane Parts** `[REQUIRED]` — Contains thick wing parts and rigging used for decorative purposes. Workshop ID: **837058476**
12. **Aerodynamic Control Surfaces** `[REQUIRED]` — Blocks for aerodynamic control surfaces. Workshopp ID: **800500312**
13. **Aerodynamic Control Surface Mod Component** `[REQUIRED]` - Script component of the Control Surfaces mod. Workshop ID: **2831593561**
14. **Carrier Landing - Arrestor Cable and Tailhook** `[REQUIRED]` — Used extensively for decorative cables on ships. #9's arrestor hooks are explicitly described as "simply a decorative block if the mod isn't present," meaning this is the mod that makes them functional rather than cosmetic. Workshop ID: **2219683817**
15. **AWG Convenient Rotation Pack (20+ rotors, hinges and parts, separate from CWP)** `[REQUIRED]` - Useful rotors, hinges, and converter blocks. Workshop ID: **2714418378**

## Naval Structural

16. **Boat Parts** `[REQUIRED]` — Propellers and non-deforming block that is used in ship elevators. Confirmed as half of the real CWP-replacement combination (see below). Workshop ID: **2685161727**
17. **Terran Titans - Naval Blocks** `[REQUIRED]` — confirmed real and distinct: smoke stacks, bridge windows, maneuvering props. Same author as the WW2 Weapons Pack but a genuinely separate, non-weapon product. Confirmed via search as part of the actual combination (alongside Boat Parts) used by real naval-focused servers to drop CWP entirely — the "Dygamic" naval/military server, most likely what you were recalling. Workshop ID: **2899106264**
18. **AQD - Armor Expansion** `[REQUIRED — partially superseded, not fully]` — the mod's own changelog confirms 10 specific block shapes (Bevelled Corner, Corner Split, Half Inverted Corner Base/Tip, Inverted Corner Split, Slab Corner, Slab Half Corner, Slab Inverted Corner, Slab Raised Corner, Slab Slope) were folded into vanilla and hidden from the G-menu, but the mod remains active (current version 2.2a) and still provides other armor content beyond those 10. Not fully redundant — worth confirming whether the *specific* pieces you actually use are among the 10 mothballed ones or not. Workshop ID: **1939935505**
19. **Gyro Ship Rudders (Mod Component)** `[REQUIRED]` - The script side of a mod that uses gryoscopes to control ships and also keeps them from rolling or pitching too much. Workshop ID: **2916173692**

## Ground Vehicles

20. **SETB Community Tank Parts** `[REQUIRED]` — Tons of tank functional parts. Workshop ID: **3579674659**
21. **Tank Tracks Framework & API** `[REQUIRED]` — by Digi, the underlying framework other track mods plug into. Workshop ID: **3208995513**
22. **Tank Track Pack: Morue** `[REQUIRED]` — confirmed actively maintained (regular updates through October, adding new KGS/AWG wheel support). Confirmed compatible with AWG, KGS, *and* Consolidation Propulsion wheel ecosystems, consistent with what SETB's own modlist showed earlier in this project. Workshop ID: **3225398014**
23. **Yakobe's Machinations** `[REQUIRED]` — Fixed weapons for ground vehicles. Workshop ID: **3431829641**
24. **ArmourEssentials** `[REVIEW]` - Turret ring rotors, gunsights, ammoracks. Workshop ID: **3660034986**

## Progression / Restriction

25. **Ship Core Framework** `[CORE]` — Stage 1's core progression system; not yet added to the world mod list per the Roadmap's own to-do. Workshop ID: **3552595651**
26. **Block Restrictions** `[REQUIRED]` — confirmed by prior use on this project, works well. Scope (2026-08-21): remove non-period-correct blocks from the G-menu across vanilla + dependency mods — anachronistic weapons (overlaps with the WeaponCore lockdown scope in the Prerequisite section, but this is a hard G-menu removal rather than the soft unlock-currency gating used there), reactors, thrusters, and likely other categories TBD. Workshop ID: **2053202808**

## First-Party (this project's own mods, not Workshop dependencies)

27. **WW2-WitM-Framework** `[CORE]` — Ship Core role/progression definitions. Local mod, source at `WW2-WitM-Framework\`.
28. **WW2-WitM-MES** `[CORE]` — MES encounter pack (spawns, behaviors, factions). Local mod, source at `WW2-WitM-MES\`.
29. **WW2-WitM-Planet** `[CORE]` — custom Mediterranean planet. Local mod, source at `WW2-WitM-Planet\`.

## Being retired

- **AWG CWP Legacy Pack**, **AWG WeaponCore Pack (v205)**, **AWG's Convenient Weapon Pack – Movement Fix Patch** `[RETIRE]` — superseded by #17–23 above.

---

