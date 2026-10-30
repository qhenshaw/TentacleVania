# VFX

VFX Graph assets (no C#). `CreatureTentacles/CreatureTentacles.vfx` is the developer's: strips along the creature's chains, fed by `Creature/Rig/ChainTexture` (see the Creature CLAUDE.md). Don't edit it without the developer's go-ahead.

## Tentacle tube shader

- `CreatureTentacles/TentacleTube` (Lit Shader Graph with VFX support, built from `TentacleTube.shadergraph.json` + `.hlsl`): makes a flat strip read as a glossy tube by bending the tangent-space normal across the strip width (UV v), so it needs strips or lines whose u runs along the length and v across. Wet black base with high smoothness, teal veins flowing along the length (`_VeinColor`, `_VeinAmount`, `_VeinScale`, `_VeinSpeed`) and the same `_StatusColor` / `_StatusAmount` / `_StatusPulse` channel as the character master, brighter at the tube edges.
- `TentacleTube.mat` uses it on meshes and `LineRenderer`s (`textureMode` Stretch).
- `CreatureTentacles.vfx` draws its strips with an `Output ParticleStrip|Shader Graph|Quad` context (`VFXComposedParticleStripOutput`) using it, casting shadows; the exposed `StatusColor`/`StatusAmount`/`StatusPulse` feed the graph's status inputs (set through `CharacterMaterials`). The legacy URP Lit strip output refuses a Shader Graph ("convert this context to dedicated ShaderGraph Output") and compiles no shader, so it draws nothing. From an editor eval, build the new context from `VFXLibrary.GetContexts()`, move the blocks and flow link over by hand and `Detach` the old one (`VFXModel.ReplaceModel` dropped the blocks and links), then `VisualEffectResource.WriteAsset()`; if an eval leaves the in-memory graph broken, discard the file and restart the editor, since reimporting doesn't reload it.
- Swapping a `VisualEffect`'s asset at runtime resets its exposed property overrides (`StripsPerChain`, `NoiseSpeed`) and the chain texture binding, which `ChainTexture` only sets when it rebuilds.
