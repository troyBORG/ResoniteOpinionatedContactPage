# OpinionatedContactPage

[resonitemodloader](https://github.com/resonite-modding-group/ResoniteModLoader) mod that provides an opinionated sorting to the contacts page.

**Note**: This is a fork of the original mod by yosh. Original repository: https://git.unix.dog/yosh/ResoniteOpinionatedContactPage/

The sorting order is:

1) unread messages/invites
2) your own account (automatically prioritized)
3) Resonite bot (automatically prioritized)
4) **pinned contacts** (see below)
5) users you can join, regardless of status
6) users you can't join, sorted by online status
7) headless users
8) contact requests
9) offline users

ties are broken alphabetically rather than last message time

## Configuration

This mod supports configuration options that can be changed in Resonite's mod configuration menu (accessible from the main menu):

- **PrioritizeOwnAccount** (default: `true`): Prioritize your own account at the top of the contacts list (after unread messages)
- **PrioritizeResoniteBot** (default: `true`): Prioritize the Resonite bot at the top of the contacts list (after own account)
- **SortingStyle** (default: `"Optimized"`): Choose the sorting style:
  - `"Default"`: Use vanilla Resonite sorting (mod does nothing) - **Note: Currently not fully implemented, uses Optimized instead**
  - `"Optimized"`: Custom optimized sorting with pins, joinable priority, online status, etc.

## Pinning Contacts

This mod supports pinning contacts to keep them at the top of your contacts list. Pinned contacts appear right after contacts with unread messages.

To pin/unpin contacts, edit the configuration file:
- **Windows**: `%LocalAppData%\Resonite\ModConfigs\OpinionatedContactPage\pinned_contacts.json`
- **Linux/Mac**: `~/.local/share/Resonite/ModConfigs/OpinionatedContactPage/pinned_contacts.json`

The file contains a JSON array of ContactUserIds (not usernames). Example:
```json
[
  "U-user-id-1",
  "U-user-id-2",
  "U-user-id-3"
]
```

**Note**: ContactUserIds are unique identifiers (e.g., "U-xxxxx-xxxxx"), not usernames. You can find a contact's UserId by inspecting the contact details in Resonite.

**UI**: When you select a contact in the contacts list, a "Pin" or "Unpin" button will appear in the actions menu. Click it to toggle the pin state. You can also manually edit the JSON file if needed.
