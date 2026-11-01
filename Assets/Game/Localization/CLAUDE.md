# Localization

Assets only, no code. `LocalizationSettings.asset` is the active settings (Project Settings > Localization), with English as the project locale (`Locales/`).

- String tables live in `Tables/`. `UI` holds menu and HUD text, keyed `<screen>.<element>` (`menu.play`). Add entries with `python -I Tools/loc-add.py UI <entries.tsv>` (a UTF-8 file of `key<TAB>value` lines; it also updates existing keys).
- Tables load through Addressables (`Assets/AddressableAssetsData/`, groups made by the Localization package); the player build builds that content.
- Weapon and ability names come from `UI` as `weapon.<asset>.name` / `ability.<asset>.name` (`Core.Text.LocalizedNames`, cached per locale), with the asset's `_displayName` as the English fallback.
- `Locales/Pseudo-Locale.asset` (TV-87) transforms the English tables: accented, about 30% longer (padding uses letters only, since other characters broke format strings) and bracketed. It's not in the Settings language list; turn it on with F5 HUD > Pseudo-locale to check layouts. Its accents also hit `{0:F2}`-style format codes, so formatted values (nest stat effects) show blank in pseudo only.
- `python -I Tools/loc-scan.py` lists hard-coded player-facing strings in runtime C# and UXML (exit 1 when any). `python -I Tools/loc-export.py [folder]` exports every table to CSV for translators (default `Builds/Localization/`).
- Fonts (TV-87, developer decisions; Noto, OFL, Asset Ledger row): `UI/Fonts/NotoSans-SDF` is the default UI font (set in `UI/RuntimeTheme.tss` `:root`), `NotoSansMono-SDF` is for the lore reader, and the CJK fonts (`UI/Fonts/Resources/Fonts/NotoSansCJK-sc|tc|jp|kr-SDF`) are fallbacks that `UI/Fonts/LocaleFonts` adds to the shared `UITextSettings` (on every PanelSettings) only for Chinese, Japanese or Korean locales (`LocaleFontRules`, unit tested). All are dynamic font assets that clear their atlas on build.
