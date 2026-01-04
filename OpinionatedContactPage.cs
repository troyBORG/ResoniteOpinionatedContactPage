using FrooxEngine;
using FrooxEngine.UIX;
using System.Reflection;
using System.Reflection.Emit;
using SkyFrost.Base;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

using ResoniteModLoader;
using HarmonyLib;

namespace OpinionatedContactPage;

public class OpinionatedContactPage : ResoniteMod
{
	public override string Name => "OpinionatedContactPage";
	public override string Author => "yosh";
	public override string Version => typeof(OpinionatedContactPage).Assembly.GetName().Version?.ToString() ?? "0.0.0";
	public override string Link => "https://github.com/troyBORG/ResoniteOpinionatedContactPage";

	private static readonly Harmony harmony = new("org.yosh.OpinionatedContactPage");
	internal static PinnedContactsStorage? pinnedStorage;

	//// INIT ////

	public override void OnEngineInit()
	{
		pinnedStorage = new PinnedContactsStorage();
		InitMod();
	}

	public static void InitMod()
	{
		harmony.PatchAll();
	}

	//// PINNED CONTACTS STORAGE ////

	internal class PinnedContactsStorage
	{
		private readonly HashSet<string> pinnedUserIds = new();
		private readonly string configPath;

		public PinnedContactsStorage()
		{
			// Store config in Resonite user data directory
			var userDataPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Resonite",
				"ModConfigs",
				"OpinionatedContactPage"
			);
			Directory.CreateDirectory(userDataPath);
			configPath = Path.Combine(userDataPath, "pinned_contacts.json");
			Load();
		}

		public bool IsPinned(string userId)
		{
			lock (pinnedUserIds)
			{
				return pinnedUserIds.Contains(userId);
			}
		}

		public void TogglePin(string userId)
		{
			lock (pinnedUserIds)
			{
				if (!pinnedUserIds.Add(userId))
				{
					pinnedUserIds.Remove(userId);
				}
				Save();
			}
		}

		private void Load()
		{
			try
			{
				if (File.Exists(configPath))
				{
					var json = File.ReadAllText(configPath);
					var userIds = JsonSerializer.Deserialize<List<string>>(json);
					if (userIds != null)
					{
						lock (pinnedUserIds)
						{
							pinnedUserIds.Clear();
							foreach (var id in userIds)
							{
								pinnedUserIds.Add(id);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Msg($"Error loading pinned contacts: {ex.Message}");
			}
		}

		private void Save()
		{
			try
			{
				List<string> userIds;
				lock (pinnedUserIds)
				{
					userIds = pinnedUserIds.ToList();
				}
				var json = JsonSerializer.Serialize(userIds, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(configPath, json);
			}
			catch (Exception ex)
			{
				Msg($"Error saving pinned contacts: {ex.Message}");
			}
		}
	}

	//// PATCHES ////

	[HarmonyPatch(typeof(ContactsDialog), "OnCommonUpdate")]
	public static class Patch_Method
	{
		// Cache MethodInfo to avoid repeated reflection calls in transpiler
		private static readonly MethodInfo CompareMethod = typeof(Patch_Method).GetMethod(nameof(Compare))!;

		public static int Compare(Slot a, Slot b)
		{
			ContactItem? ci1 = a.GetComponent<ContactItem>();
			ContactItem? ci2 = b.GetComponent<ContactItem>();
			
			// Early null checks
			if (ci1 == null && ci2 == null) return 0;
			if (ci1 == null) return 1;
			if (ci2 == null) return -1;

			Contact? c1 = ci1.Contact;
			Contact? c2 = ci2.Contact;
			ContactData? cd1 = ci1.Data;
			ContactData? cd2 = ci2.Data;

			switch (c1, c2) {
				case (null, null): return 0;
				case (Contact, null): return -1;
				case (null, Contact): return 1;
			}
			
			switch (cd1, cd2) {
				case (null, null): return c1!.ContactUsername.CompareTo(c2!.ContactUsername);
				case (ContactData, null): return -1;
				case (null, ContactData): return 1;
			}

			// check 0: has messages
			int msgc = ci2.HasMessages.CompareTo(ci1.HasMessages);
			if (msgc != 0) {
				return msgc;
			}

			// check 0.5: pinned contacts (using ContactUserId as identifier, like FlexibleContactsSort)
			bool c1Pinned = OpinionatedContactPage.pinnedStorage?.IsPinned(c1!.ContactUserId) ?? false;
			bool c2Pinned = OpinionatedContactPage.pinnedStorage?.IsPinned(c2!.ContactUserId) ?? false;
			int pinC = c2Pinned.CompareTo(c1Pinned);
			if (pinC != 0) {
				return pinC;
			}

			// check 1: offline or online
			int cd1stat = (int?)cd1!.CurrentStatus.OnlineStatus ?? 1;
			int cd2stat = (int?)cd2!.CurrentStatus.OnlineStatus ?? 1;
			switch (cd1stat, cd2stat) {
				case (0, >0): return 1;
				case (>0, 0): return -1;
				case (0, 0):
					return cd1.Contact.ContactStatus.CompareTo(cd2.Contact.ContactStatus) switch {
						0 => c1.ContactUsername.CompareTo(c2.ContactUsername),
						int s => s
					};
			}

			// check 2: headless
			int hlc = cd1.CurrentStatus.SessionType.CompareTo(cd2.CurrentStatus.SessionType);
			if (hlc != 0) {
				return hlc;
			}

			// check 3: joinable
			SessionInfo? cd1s = cd1.CurrentSessionInfo;
			SessionInfo? cd2s = cd2.CurrentSessionInfo;
			switch (cd1s, cd2s, cd1stat == cd2stat) {
				case (SessionInfo, null, _):  return -1;
				case (null, SessionInfo, _):  return 1;
				case (null, null, false):     return cd2stat.CompareTo(cd1stat);
			};

			return c1.ContactUsername.CompareTo(c2.ContactUsername);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var insts = instructions.ToList();

			// modify the last ldftn instruction, which is loading the anonymous sorting function
			for (int i = insts.Count - 1; i >= 0; i--) {
				if (insts[i].opcode == OpCodes.Ldftn) {
					insts[i].operand = CompareMethod;
					break;
				}
			}
			return insts;
		}
	}

	[HarmonyPatch(typeof(ContactsDialog), "UpdateSelectedContactUI")]
	public static class ContactsDialog_UpdateSelectedContactUI_Patch
	{
		static void Postfix(ContactsDialog __instance, UIBuilder ___actionsUi)
		{
			if (__instance.SelectedContact is null || 
			    __instance.SelectedContactId == __instance.Cloud.Platform.AppUserId || 
			    __instance.SelectedContact.IsSelfContact)
				return;

			if (OpinionatedContactPage.pinnedStorage == null)
				return;

			bool isPinned = OpinionatedContactPage.pinnedStorage.IsPinned(__instance.SelectedContactId);
			var pinButton = ___actionsUi.Button(isPinned ? "Unpin" : "Pin");
			
			pinButton.LocalPressed += (button, data) =>
			{
				OpinionatedContactPage.pinnedStorage.TogglePin(__instance.SelectedContactId);
				bool nowPinned = OpinionatedContactPage.pinnedStorage.IsPinned(__instance.SelectedContactId);
				((Text)pinButton.LabelTextField.Parent).Content.Value = nowPinned ? "Unpin" : "Pin";
			};
		}
	}
}
