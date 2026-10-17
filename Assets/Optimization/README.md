# Station shadow optimization

The scene keeps its original visible meshes, materials, textures, colliders, and door hierarchy. Fifteen derived meshes render shadows for 75 station objects. Their sampled surface error is at most 3 mm at the largest tested instance scale. The front door frame failed that limit and keeps its original caster.

Shadow-map resolution, cascade count, shadow distance, vegetation density, and render resolution are unchanged. Shadow geometry is approximate: the tested forecourt comparison changed 0.053% of pixels by more than 8/255, while the alternate light direction changed 0.012%. These are measured views, not a promise of identical shadows at every angle.

StationShadowCaster follows the original renderer's enable state, material, layer, and forceRenderingOff state. Its child transform follows the source mesh, including doors. It falls back to the original caster if the source mesh is replaced, its vertex count or bounds change, or the proxy mesh is missing. Changes inside unchanged bounds with an unchanged vertex count require a manual rebuild.

Use **Tools > Foggy Road > Station Shadows > Revert to original shadows** to remove the bindings. The original meshes remain in the project. **Apply validated proxies** reinstalls this bake from Bindings.tsv.

Rebuild and validate proxies after editing the source models or changing their scale beyond the tested instances. Current bake inputs, the Blender generation script, image comparisons, timings, and the pre-change scene backup are under Library/PerfAudit. SurfaceValidation.tsv records the original candidate results, including the rejected door frame.

The performance report is Library/PerfAudit/Optimization-2026-10-17.md.

