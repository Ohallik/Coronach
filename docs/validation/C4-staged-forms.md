# C4 staged forms — September 27

**C4 OPEN. Focused source checks pass; ordinary-player integration remains pending.** Bodies now keep their full scale. The outgoing humanoid tucks its arms, folds its attached generated vanes and tips around its hips for launch. Ship wings fold around their longitudinal roots using a temporary copy of the existing generated mesh. A brief exchange effect covers the topology change; the incoming body opens before its actions resume. Ground-to-ground shaping uses a smaller torso inclination.

Closing takes 0.23 s, exchange uses 0.05 s either side of replacement, and opening takes 0.27 s, subject to frame quantization. Repeated requests coalesce and reversal reopens the current pose. Pause retains that pose. Exactly one body remains active. Lethal interruption restores a full body for defeat presentation. A form change cancels pending attacks and motor travel; both motors reject movement while folding.

Normal docking now folds the departing party before fade-out and opens the arriving party during fade-in. The world interaction supplies a completion predicate to scene loading, keeping Core independent of Combat. Spawn names, destination IDs and autosave behavior are retained. Flight CPU readback is enabled only on the two existing ship imports so runtime wing deformation can read their actual vertices. Original meshes are restored exactly; no generated geometry or textures are replaced.

## Rejected controls and correction

| Evidence under `Builds/quality/C4/` | Result |
|---|---|
| `form-stages-red01/playmode-results.xml` | **0/5**: whole-body scale falls to 0.44; ship vertices are unreadable in runtime; lethal interruption leaves shaping active; dock skips folding; a pending attack survives body change. |
| `form-stages-red01/editmode-results.xml` | **0/2**: both flight mesh imports lack readback. |
| `form-stages-green02/playmode-results.xml` | **10/11**. Dock assertions pass, then fixture cleanup accesses its scene-unloaded pad. Cleanup now checks whether it still exists. |
| `form-stages-travel-red01/playmode-results.xml` | **0/1**: pending dash/new input moves the folding actor 0.363057 m. |
| `form-stages-green03/playmode-results.xml` | **12/12**, zero skips, 49.000 s: six staged-form checks and six repeat/reverse/pause continuity checks. |
| `form-stages-green03/editmode-results.xml` | **6/6**, zero skips: both readback cases plus existing hull/socket geometry checks. |

The first implementation compile also rejected a Core-to-Combat dependency. That dependency was removed rather than adding a circular assembly reference. The first render captured only Taren Natural, then rejected a lookup that confused state names with clip names. The renderer now resolves the actual Idle state's motion; that complete 54-image capture is `form-stages-02`.

## Actual image review

The agent opened **30 images from `form-stages-02`**: side views at 0/50/100% and fully folded front/top views for both heroes in Natural, Shaped and Flight forms. Two preceding Taren Natural side views were also opened from the incomplete first capture. Exact viewed files/hashes are in the JSON; the other 24 images are not claimed as reviewed.

The opened phases show full-size bodies, attached vane hinges, tucked arms and distinct folded ship wings with an intact central hull. No separation or tearing is visible in these views. Taren's ship width decreases from 2.975 m to 1.243 m as its wings rise; Sela's decreases from 2.650 m to 1.648 m. Length remains 3.2 m. These observations support the candidate folding geometry. They do not establish that the brief exchange masks the replacement convincingly in continuous gameplay, or that docking reads well at the actual camera distance.

Both human save hashes remain unchanged. Both player packages still contain `feac4b9`; full integrated suites and new player captures follow this source checkpoint. Flight disable/overlap, rejected long circuits, outstanding maps and later gates remain open. Continuous normal/slow motion, sound audition and physical-controller feel remain UNVERIFIED.
