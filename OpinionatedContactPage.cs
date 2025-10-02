using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using Renderite.Shared;

using ResoniteModLoader;
using HarmonyLib;

#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace OpinionatedContactPage;

public class OpinionatedContactPage : ResoniteMod
{
	public override string Name => "OpinionatedContactPage";
	public override string Author => "yosh";
	public override string Version => "1.0.0";
	public override string Link => "https://git.unix.dog/yosh/ResoniteOpinionatedContactPage/";

	private static Harmony harmony = new Harmony("org.yosh.OpinionatedContactPage");

	//// CONFIG ////

	private static ModConfiguration? config;

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<int> ExampleKey = new(
		"ExampleKey",
		"Example configuration key",
		computeDefault: () => 4,
		valueValidator: (v) => 1 <= v && v <= 9
	);

	//// INIT ////

	public override void OnEngineInit()
	{
#if DEBUG
		HotReloader.RegisterForHotReload(this);
#endif
		config = GetConfiguration();
		InitMod();
	}

	public static void InitMod()
	{
		harmony.PatchAll();
	}

	//// RELOAD ////

#if DEBUG
	static void BeforeHotReload()
	{
		harmony.UnpatchAll(harmony.Id);
	}

	static void OnHotReload(ResoniteMod modInstance)
	{
		config = modInstance.GetConfiguration();
		InitMod();
	}
#endif

	//// PATCHES ////

	[HarmonyPatch(typeof(FrooxEngineClass), nameof(FrooxEngineClass.Method))]
	public static class Patch_Method
	{
		static bool Prefix()
		{
			return true;
		}
	}
}
