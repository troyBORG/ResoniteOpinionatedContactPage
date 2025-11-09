using FrooxEngine;
using System.Reflection.Emit;
using SkyFrost.Base;

using ResoniteModLoader;
using HarmonyLib;

namespace OpinionatedContactPage;

public class OpinionatedContactPage : ResoniteMod
{
	public override string Name => "OpinionatedContactPage";
	public override string Author => "yosh";
	public override string Version => typeof(OpinionatedContactPage).Assembly.GetName().Version?.ToString() ?? "0.0.0";
	public override string Link => "https://git.unix.dog/yosh/ResoniteOpinionatedContactPage/";

	private static readonly Harmony harmony = new("org.yosh.OpinionatedContactPage");

	//// INIT ////

	public override void OnEngineInit()
	{
		InitMod();
	}

	public static void InitMod()
	{
		harmony.PatchAll();
	}

	//// PATCHES ////

	[HarmonyPatch(typeof(ContactsDialog), "OnCommonUpdate")]
	public static class Patch_Method
	{
		public static int Compare(Slot a, Slot b)
		{
			ContactItem ci1 = a.GetComponent<ContactItem>();
			ContactItem ci2 = b.GetComponent<ContactItem>();
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
				case (null, null): return ci1.Contact.ContactUsername.CompareTo(ci2.Contact.ContactUsername);
				case (ContactData, null): return -1;
				case (null, ContactData): return 1;
			}

			// check 0: has messages
			int msgc = ci2.HasMessages.CompareTo(ci1.HasMessages);
			if (msgc != 0) {
				return msgc;
			}

			// check 1: offline or online
			int cd1stat = (int?)cd1.CurrentStatus.OnlineStatus ?? 1;
			int cd2stat = (int?)cd2.CurrentStatus.OnlineStatus ?? 1;
			switch (cd1stat, cd2stat) {
				case (0, >0): return 1;
				case (>0, 0): return -1;
				case (0, 0):
					return cd1.Contact.ContactStatus.CompareTo(cd2.Contact.ContactStatus) switch {
						0 => ci1.Contact.ContactUsername.CompareTo(ci2.Contact.ContactUsername),
						int s => s
					};
			}

			// check 2: headless
			int hlc = cd1.CurrentStatus.SessionType.CompareTo(cd2.CurrentStatus.SessionType);
			if (hlc != 0) {
				return hlc;
			}

			// check 3: joinable
			SessionInfo cd1s = cd1.CurrentSessionInfo;
			SessionInfo cd2s = cd2.CurrentSessionInfo;
			switch (cd1s, cd2s, cd1stat == cd2stat) {
				case (SessionInfo, null, _):  return -1;
				case (null, SessionInfo, _):  return 1;
				case (null, null, false):     return cd2stat.CompareTo(cd1stat);
			};

			return ci1.Contact.ContactUsername.CompareTo(ci2.Contact.ContactUsername);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var insts = instructions.ToList();
			var rtm = typeof(Patch_Method).GetMethod("Compare");

			// modify the last ldftn instruction, which is loading the anonymous sorting function
			for (int i = insts.Count - 1; i >= 0; i--) {
				if (insts[i].opcode == OpCodes.Ldftn) {
					insts[i].operand = rtm;
					break;
				}
			}
			return insts;
		}
	}
}
