# Localization

Assets only, no code. `LocalizationSettings.asset` is the active settings (Project Settings > Localization), with English as the project locale (`Locales/`).

- String tables live in `Tables/`. `UI` holds menu and HUD text, keyed `<screen>.<element>` (`menu.play`). Add entries with `python -I Tools/loc-add.py UI <entries.tsv>` (a UTF-8 file of `key<TAB>value` lines; it also updates existing keys).
- Tables load through Addressables (`Assets/AddressableAssetsData/`, groups made by the Localization package); the player build builds that content.
