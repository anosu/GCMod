using TMPro;
using System;
using BepInEx;
using Gc;
using DMM.OLG.Unity.Engine.Internal;
using DMM.OLG.Unity.Extensions.Novel;
using GcObfuscate.Service;
using HarmonyLib;
using UnityEngine;

namespace GCMod
{
	public class Patch
	{
        public static int CurrentNovelId;
		public static TMP_FontAsset TranslationFont;

		public static void Initialize()
		{
			Harmony.CreateAndPatchAll(typeof(Patch), null);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(EventText), "Parse")]
		public static void SetMessageText(EventText __instance, ref string message)
		{
			string text;
			if (Config.Translation && Translation.novels.ContainsKey(Patch.CurrentNovelId) && Translation.novels[Patch.CurrentNovelId].TryGetValue(message, out text))
			{
				message = (text.IsNullOrEmpty() ? message : text);
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(EventMessage), "SetName")]
		public static void SetMessageName(EventMessage __instance, ref string text)
		{
			string text2;
			if (Config.Translation && Translation.novels.ContainsKey(Patch.CurrentNovelId) && Translation.names.TryGetValue(text, out text2))
			{
				text = (text2.IsNullOrEmpty() ? text : text2);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(EventMessage), "SetName")]
		public static void SetMessageNameFont(EventMessage __instance)
		{
			if (Config.Translation && Translation.novels.ContainsKey(Patch.CurrentNovelId))
			{
				if (Patch.TranslationFont == null)
				{
					AssetBundle assetBundle = AssetBundle.LoadFromFile(Paths.PluginPath + "/font/" + Config.TranslationFontName);
					UnityEngine.Object @object = assetBundle.LoadAsset(Config.TranslationFontName + " SDF");
					Patch.TranslationFont = ((@object != null) ? @object.TryCast<TMP_FontAsset>() : null);
					assetBundle.Unload(false);
				}
				__instance.MessageName.font = Patch.TranslationFont;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(EventTitle), "ShowBlurEffect")]
		public static void SetMessageTitleFont(EventTitle __instance)
		{
			if (Config.Translation && Translation.novels.ContainsKey(Patch.CurrentNovelId))
			{
				if (Patch.TranslationFont == null)
				{
					AssetBundle assetBundle = AssetBundle.LoadFromFile(Paths.PluginPath + "/font/" + Config.TranslationFontName);
					UnityEngine.Object @object = assetBundle.LoadAsset(Config.TranslationFontName + " SDF");
					Patch.TranslationFont = ((@object != null) ? @object.TryCast<TMP_FontAsset>() : null);
					assetBundle.Unload(false);
				}
				__instance._TitleMain.font = Patch.TranslationFont;
				__instance._TitleSub.font = Patch.TranslationFont;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(EventText), "SetRuby")]
		public static void SetMessageTextFont(GameObject go, EventText.Letter letter, ref TextMeshProUGUI text)
		{
			if (Config.Translation && Translation.novels.ContainsKey(Patch.CurrentNovelId))
			{
				if (Patch.TranslationFont == null)
				{
					AssetBundle assetBundle = AssetBundle.LoadFromFile(Paths.PluginPath + "/font/" + Config.TranslationFontName);
					UnityEngine.Object @object = assetBundle.LoadAsset(Config.TranslationFontName + " SDF");
					Patch.TranslationFont = ((@object != null) ? @object.TryCast<TMP_FontAsset>() : null);
					assetBundle.Unload(false);
				}
				text.font = Patch.TranslationFont;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(EventTitle), "ShowBlurEffect")]
		public static void SetMessageTitle(EventTitle __instance)
		{
			string text;
			if (Config.Translation && Translation.novels.ContainsKey(Patch.CurrentNovelId) && Translation.novels[Patch.CurrentNovelId].TryGetValue(__instance._TitleMain.text, out text))
			{
				__instance._TitleMain.text = (text.IsNullOrEmpty() ? __instance._TitleMain.text : text);
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(NovelApiService), "Read")]
		public static void SetupTranslation(NovelType novelType, int novelId)
		{
			Plugin.Global.Log.LogInfo("NovelId: " + novelId.ToString() + ", NovelType: " + novelType.ToString());
			if (Config.Translation)
			{
				Patch.CurrentNovelId = novelId;
				if (!Translation.novels.ContainsKey(Patch.CurrentNovelId))
				{
					Translation.FetchNovelTranslationAsync(Patch.CurrentNovelId);
				}
			}
		}
	}
}
