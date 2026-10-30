# VFX

VFX Graph assets (no C#). `CreatureTentacles/CreatureTentacles.vfx` is the developer's: strips along the creature's chains, fed by `Creature/Rig/ChainTexture` (see the Creature CLAUDE.md). Don't edit it without the developer's go-ahead.

## Tentacle tube shader

- `CreatureTentacles/TentacleTube` (Lit Shader Graph with VFX support, built from `TentacleTube.shadergraph.json` + `.hlsl`): makes a flat strip read as a glossy tube by bending the tangent-space normal across the strip width (UV v), so it needs strips or lines whose u runs along the length and v across. Wet black base with high smoothness, teal veins flowing along the length (`_VeinColor`, `_VeinAmount`, `_VeinScale`, `_VeinSpeed`) and the same `_StatusColor` / `_StatusAmount` / `_StatusPulse` channel as the character master, brighter at the tube edges.
- `TentacleTube.mat` uses it on meshes and `LineRenderer`s (`textureMode` Stretch).
- To use it on a VFX strip output, pick the graph in the output's Shader Graph field in the VFX Graph editor, which adds the graph's properties as output inputs. Patching the `.vfx` YAML's `shaderGraph` reference by hand doesn't create those and the output draws nothing.
- Swapping a `VisualEffect`'s asset at runtime resets its exposed property overrides (`StripsPerChain`, `NoiseSpeed`) and the chain texture binding, which `ChainTexture` only sets when it rebuilds.
