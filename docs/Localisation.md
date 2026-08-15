# Server Localisation

Status: **Implemented.** Four locales are supported: Japanese (`ja`), English (`en`), Simplified Chinese (`zh-Hans`), and Traditional Chinese (`zh-Hant`). Players choose their language on the Portal account page. That preference is stored on `User.Language` and applied by Auth, Msg, and Area when sending server-authored text.

## Scope

Localise only strings serialized into a game packet or returned on the player-facing Portal account API.

Do **not** localise:

- Server-script step/state names, event keys, packet names, log messages, or database codes
- Player-authored names, chat, profiles, circles, or room names
- Client CSV dialogue (`EventScriptPlayNotify` only sends a path; the CSV lives in client assets)

## Seed JSON

Translated fields live on the owning seed object as a `LocalisedString`:

```json
{
  "npcObjectId": 123,
  "name": {
    "ja": "真珠",
    "en": "Shinju",
    "zh-Hans": "真珠",
    "zh-Hant": "真珠"
  },
  "eventKey": "shinju_registration"
}
```

A plain string is still accepted and treated as Japanese. Entity `Name`/`Description` columns keep the canonical (Japanese/fallback) value. All supplied locales are upserted into `LocalisedTexts` as missing rows only, so operator edits are preserved.

Standalone client-bound text (dialogue, menus, notices, emotions, maintenance) lives in `aisp.Common/seedData/localisation.json`.

Key format: `{domain}.{subject}.{field}`, lowercase and stable. Dynamic IDs use factories on `L` (`L.Item.Name(itemId)`, `L.Npc.Name(npcObjectId)`, `L.Shop.DisplayName(code)`).

## Resolution

`ITextLocaliser.Get` uses the requested language when that row exists, otherwise Japanese. Japanese is required for every key.

Auth, Msg, and Area copy `User.Language` onto `IPlayerSession.Language` at login. Portal changes persist to the user row and apply on the next full reconnect.

## Packet encodings

The client stores packet strings as `vce::utf8` and converts with `MultiByteToWideChar(CP_UTF8)`. Display text is UTF-8 on the wire.

| Surface | Encoding | Notes |
|---------|----------|-----------|
| Item/NPC/emotion names, dialogue, shops, worlds, island selector, enquete, room names, chat | UTF-8 | Packet writers truncate to the field byte limit |
| IP addresses, OTPs, Nico movie IDs | ASCII | Identifiers, not display text |
| Client CSV event scripts | Shift_JIS (cp932) | Client asset files; the server only sends the path |

Chinese in-game fonts may still need a client asset patch to render.

## Translation workflow

1. Prefer inline `LocalisedString` objects on the owning seed JSON (`ja` required; add `en` / `zh-Hans` / `zh-Hant` incrementally).
2. Put dialogue, notices, and other unowned client-bound text in `aisp.Common/seedData/localisation.json` with all four locales.
3. Startup upserts missing `(Key, Language)` rows only. Operator edits in `LocalisedTexts` are kept.
4. Coverage is logged at startup (`missing N keys for Language`) and listed at Debug. Tests report English catalog gaps without blocking startup.
5. New server-authored player text must go through `LocKey` / `ITextLocaliser`. Do not add parallel string tables.

Unsupported client surfaces: client CSV dialogue is a client-assets Localisation stream and is not translated by the server. Chinese glyphs may still need a client font/asset patch.

## Adding a new player-facing string

1. Add `L.Domain.Subject.Field` (or a factory for dynamic ids)
2. Add `ja` / `en` / `zh-Hans` / `zh-Hant` on the owning seed object, or in `localisation.json`
3. Call `localiser.Get(session, L.Domain.Subject.Field)` and pass it to the packet. `WriteFixedString` / `Write` truncate to the field's byte limit.

No parallel string systems.
