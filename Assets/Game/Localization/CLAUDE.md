# Localization

Assets only, no code. `LocalizationSettings.asset` is the active settings (Project Settings > Localization), with English as the project locale (`Locales/`).

- String tables live in `Tables/`. `UI` holds menu and HUD text, keyed `<screen>.<element>` (`menu.play`). Add entries through `LocalizationEditorSettings.GetStringTableCollection("UI")` in an eval, `GetTable(locale.Identifier)` then `AddEntry`, and mark the table and its `SharedData` dirty before saving.
- Tables load through Addressables (`Assets/AddressableAssetsData/`, groups made by the Localization package); the player build builds that content.
