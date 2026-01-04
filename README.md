# OpinionatedContactPage

[resonitemodloader](https://github.com/resonite-modding-group/ResoniteModLoader) mod that provides an opinionated sorting to the contacts page.

**Note**: This is a fork of the original mod by yosh. Original repository: https://git.unix.dog/yosh/ResoniteOpinionatedContactPage/

The sorting order is:

1) unread messages/invites
2) **pinned contacts** (see below)
3) users you can join, regardless of status
4) users you can't join, sorted by online status
5) headless users
6) contact requests
7) offline users

ties are broken alphabetically rather than last message time

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
