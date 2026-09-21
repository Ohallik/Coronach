# T2 pilot: Taren Natural

P3_PILOT_OK. Nathan approved the species before paid generation. Meshy T2 image-to-3D cost 15 credits and humanoid rigging cost 5, recorded in gen-manifest.json. The cleaned model has 11,728 triangles, one skinned body, UVs, 24 rig bones and a 1024-pixel albedo with a derived amber emission mask.

Actual Unity prefab renders were opened: `Builds/logs/renders/TarenNatural-idle-front.png`, `-idle-side.png`, `-walk-15.png`, `-walk-65.png`, `-attack.png`, and `true-scale-row.png`. Idle has relaxed lowered arms; the two walks clearly alternate legs and arms; the sword attack bends knees and swings the torso and arms without detached geometry. The natural wraps deform acceptably at gameplay scale. Individual fingers are modeled but the 24-bone rig has no finger animation.

The avatar is explicitly mapped and `isHuman`. Bone-weighted vertices, using bone world matrices multiplied by bind poses, measure exactly 1.900 m in rest pose; Idle is about 1.913 m. The ancestor of the skeleton is scaled. Renderer bounds and a head-to-ankle allowance were rejected as measurements. Animation verification measures local joint rotation as well as bone and visible vertex travel, so root-only movement cannot pass.

The first synchronous renders were rejected: Unity reused GPU skinning data for several captures in one editor frame. The review now advances editor frames between poses, retains the manual animation graph through capture and renders twice. `scripts/exec.ps1 -Async` keeps the editor alive until the review marker. This was a review-instrument defect, not a reason to spend on a replacement model.

UAL skeleton and five animation clips are CC0; no donor mesh is staged. Emission tint is taken from the subject's key color. Hero faces retain the approved production references; portrait sheets provide greater facial detail than the small in-world mesh.
