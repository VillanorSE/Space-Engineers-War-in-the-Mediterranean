# Gibraltar Merge — Hand-off

**Status:** Discussed only, nothing built or recorded yet. This file exists because the plan
lived purely in conversation and wasn't written down anywhere in the repo — a grep across the
whole project for "Gibraltar", "Ceuta", "Turkey", and "Spain" (planet-data context) turned up
nothing prior to this file.

## The plan (as discussed)

Replace the area of the custom planet's heightmap that currently represents **central Turkey**
with a merge of real-world **Southern Spain** terrain data, so that a **Gibraltar-style choke
point** ends up positioned near the existing **Ceuta point** on the planet. The goal is a
navigable strait chokepoint in that region, mirroring the real Strait of Gibraltar's role
between the Mediterranean and the Atlantic — but relocated to sit next to Ceuta rather than at
Turkey's current spot.

## What's already established (verified from the repo, safe to build on)

- **Planet mod:** `WW2-WitM-Planet`, PlanetGeneratorDefinition SubtypeId `WW2WitM_Mediterranean`,
  defined in [PlanetGeneratorDefinitions.sbc](Data/PlanetGeneratorDefinitions.sbc).
- **Heightmap source files:** 6 cube faces, each 2048×2048 PNG, at
  `Data/PlanetDataFiles/WW2Mediterranean/{front,back,left,right,up,down}.png`, each paired with
  a `_mat.png` (material/biome map — red channel = north/south hemisphere split, green channel =
  biome/environment-item placement, blue channel = ore map).
- **Confirmed cube-face projection rules** (from the file's own comments — keep these exact for
  any new texture work, they were hard-won via in-game verification):
  - Side faces (front/right/back/left) use standard gnomonic projection with the Y component
    negated relative to the naive formula (fixes an "Italy backwards" mirror bug encountered
    earlier).
  - `front` = internal-longitude 0, `right` = 90, `back` = 180 (**back is confirmed to be where
    Italy sits**), `left` = 270, increasing eastward.
  - Pole faces are swapped (north-pole data → `down.png`, south-pole data → `up.png`) **and**
    both get a left-right mirror flip (`np.fliplr`) on top of the swap. Confirmed correct via
    in-game checks (Corsica north of Apulia, Crete attached to Greece).
  - This is almost certainly irrelevant to the Gibraltar work itself (Mediterranean content
    lives on the side faces, not the poles) but is included here since any heightmap edit must
    not break this convention for neighboring content.
- **Planet scale:** 60km diameter / 30km radius (confirmed elsewhere in the Roadmap) — this
  matters a lot for the merge: real-world Gibraltar-to-Ceuta is roughly 14km across the strait,
  which is a significant fraction of this planet's entire diameter. Real-world Southern Spain
  data will need real thought about how much to compress/simplify, not just naive real-world
  scale.
- Since `back` = Italy at internal-longitude 180, and the mapping goes eastward
  front(0)→right(90)→back(180)→left(270): by that logic, points **east** of Italy (e.g. Turkey)
  should fall on the **`left`** face or the back/left seam, and points **west** of Italy (e.g.
  Spain/Gibraltar/Ceuta) should fall on the **`right`** face or the back/right seam. This is a
  logical inference from the documented rule, not something I visually confirmed on the
  textures — treat it as a starting hypothesis to verify, not a given.

## What is NOT yet known / not yet in any file — needs you to fill in

1. **Exact current location of "central Turkey"** on the `left.png` (or `back.png`) heightmap —
   pixel region or UV coordinates. I looked at `left.png` directly; a grayscale heightmap alone
   doesn't let me reliably identify "this is Turkey" without either the original generation
   script/source data or your own reference annotation. You've evidently already identified this
   visually — worth screenshotting/annotating it once, here or in a follow-up file, so it isn't
   re-derived from memory again later.
2. **Exact current location of "our Ceuta point"** — same issue. Nothing in any SpawnGroup,
   port, or prefab file in the repo currently mentions Ceuta by that name (confirmed via repo
   grep), so this must be a terrain feature you've identified on the map itself, not a built
   structure. Needs the same pixel/UV pinpointing.
3. **Real-world data source for Southern Spain / Gibraltar terrain** — nothing chosen yet (e.g.
   an SRTM/DEM heightmap export, a specific bounding box of real coordinates, or a manually
   painted approximation). Given the scale mismatch above, a literal 1:1 real-world height/coast
   import is unlikely to be right — decide whether this is a faithful-but-compressed import or a
   stylized approximation that just captures "narrow strait + rock promontory."
4. **Merge/blend technique** — how the imported Spain data gets stitched into the existing
   heightmap without a visible seam: direct pixel overwrite with a feathered blend edge,
   re-running the whole material/biome/ore pass for the affected region afterward (all three
   depend on the height data and the confirmed projection geometry above), etc.
5. **Downstream effects to re-check after the terrain edit**: the `_mat.png` hemisphere split,
   biome green channel, and ore blue channel all need to be regenerated or manually patched for
   whatever region gets overwritten — per the file's own comments, these were generated **from**
   the heightmap using the confirmed projection, so editing the base heightmap without touching
   the paired maps will leave them out of sync with the new terrain.
6. Nothing has been decided about what happens to whatever ports/spawn content (if any) currently
   sits in the "central Turkey" area being overwritten — worth checking before deleting terrain
   out from under anything already built there.

## Suggested next step

Before any texture editing: pin down items 1 and 2 above concretely (screenshot + rough pixel
coordinates, or in-game GPS coordinates translated to face/UV space) so the actual merge work
has a precise target instead of relying on memory of the conversation.
