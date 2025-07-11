using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils;
using DMM.OLG.Unity.Engine.Internal;
using DMM.OLG.Unity.Extensions.Novel;
using Gc;
using Gc.Battle.SkillWidget;
using Gc.Home;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GCMod
{
	public class Patch
	{
        public static int novelId;
        public static Image NormalFrame;
        public static Image CgModeFrame;
        public static Image BaseNameFrame;
        public static Color DefaultNameColor = new Color(0.957f, 0.957f, 0.914f);
        public static TMP_FontAsset originalFontAsset;

        public static void Initialize()
		{
			Harmony.CreateAndPatchAll(typeof(Patch), null);
		}

        public static void ModifyText(TextMeshProUGUI text, Color color)
        {
            text.color = color;
            text.fontMaterial.EnableKeyword("OUTLINE_ON");
            text.fontMaterial.SetFloat("_FaceDilate", Config.FaceDilate.Value);
            text.fontMaterial.SetColor("_OutlineColor", Config.OutlineColor);
            text.fontMaterial.SetFloat("_OutlineWidth", Config.OutlineWidth.Value);
            text.fontMaterial.SetFloat("_OutlineSoftness", Config.OutlineSoftness.Value);
        }

        public static void CancelModifyText(TextMeshProUGUI text, Color color)
        {
            text.color = color;
            text.fontMaterial.SetFloat("_FaceDilate", 0f);
            text.fontMaterial.SetColor("_OutlineColor", Color.black);
            text.fontMaterial.SetFloat("_OutlineWidth", 0f);
            text.fontMaterial.SetFloat("_OutlineSoftness", 0f);
            text.fontMaterial.DisableKeyword("OUTLINE_ON");
        }

        // Offline
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ConfigData), nameof(ConfigData.Set))]
        public static void SetApiDomain(string key, ref string value)
        {
            if (!Config.offline && !Config.Offline.Value)
            {
                return;
            }
            if ("ApiDomain".Equals(key))
            {
                value = Config.OfflineCDN.Value;
                Plugin.Log.LogInfo($"ApiDomain: {value}");
            }
        }

        // Setup
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScriptObjectManager), nameof(ScriptObjectManager.Setup))]
        public static void SetupTranslation(string prefix, string id)
        {
            if (!Config.Translation.Value)
            {
                return;
            }
            Plugin.Log.LogInfo($"Prefix: {prefix}, Id: {id}");
            novelId = int.Parse(id);
            if (!Translation.novels.ContainsKey(novelId))
            {
                Task task = Translation.GetNovelTranslationAsync(novelId);
                if (!Config.AsyncMode.Value)
                {
                    task.Wait();
                }
            }
            if (Translation.fontAsset == null)
            {
                Plugin.Instance.StartCoroutine(Translation.LoadFontAsset());
            }
        }

        // Title
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EventTitle), nameof(EventTitle.ShowBlurEffect))]
        public static void SetMessageTitle(EventTitle __instance)
        {
            if (!Config.Translation.Value)
            {
                return;
            }
            if (Translation.novels.ContainsKey(novelId))
            {
                if (Translation.novels[novelId].TryGetValue(__instance._TitleMain.text, out string title))
                {
                    __instance._TitleMain.text = title;
                }
            }
        }

        // Name
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EventMessage), nameof(EventMessage.SetName))]
        public static void SetMessageName(ref string text)
        {
            if (!Config.Translation.Value)
            {
                return;
            }
            if (Translation.novels.ContainsKey(novelId))
            {
                if (Translation.names.TryGetValue(text, out string name))
                {
                    text = name;
                }
            }
        }

        // Text
        [HarmonyPrefix]
		[HarmonyPatch(typeof(EventText), nameof(EventText.Parse))]
		public static void SetMessageText(EventText __instance, ref string message)
		{
            if (Config.Translation.Value && Translation.novels.ContainsKey(novelId))
			{
                if (Translation.novels[novelId].TryGetValue(message, out string text))
                {
					message = text;
                }
			}
            if (Config.ModifyText.Value)
            {
                __instance.fontSpacing = Config.CharacterSpacing.Value;
            }
        }

        // Title font
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EventTitle), nameof(EventTitle.ShowBlurEffect))]
        public static void SetMessageTitleFont(EventTitle __instance)
        {
            if (!Config.Translation.Value)
            {
                return;
            }
            if (Translation.novels.ContainsKey(novelId))
            {
                __instance._TitleMain.font = Translation.fontAsset;
            }
        }

		// Name font
        [HarmonyPostfix]
		[HarmonyPatch(typeof(EventMessage), nameof(EventMessage.SetName))]
		public static void SetMessageNameFont(EventMessage __instance)
		{
			if (Config.Translation.Value && Translation.novels.ContainsKey(novelId))
			{
				__instance.MessageName.font = Translation.fontAsset;
			}
            if (Config.ModifyText.Value)
            {
                ModifyText(__instance.MessageName, Config.NameTextColor);
            }
            else
            {
                CancelModifyText(__instance.MessageName, DefaultNameColor);
            }
		}

		// Text font
		[HarmonyPostfix]
		[HarmonyPatch(typeof(EventText), nameof(EventText.SetRuby))]
		public static void SetMessageTextFont(GameObject go, EventText.Letter letter, ref TextMeshProUGUI text)
		{
            if (Config.Translation.Value && Translation.novels.ContainsKey(novelId))
			{
				text.font = Translation.fontAsset;
			}
			if (Config.ModifyText.Value)
			{
                ModifyText(text, Config.MessageTextColor);
            }
		}

        // Alpha
		[HarmonyPostfix]
		[HarmonyPatch(typeof(EventMessage), nameof(EventMessage.Init))]
		public static void SaveTextBackgound(EventMessage __instance)
		{
			foreach (Image image in __instance.GetComponentsInChildren<Image>())
			{
				if (image.name == "Normal")
				{
					Color color = image.color;
					color.a = Config.NormalAlpha.Value;
					image.color = color;
					NormalFrame = image;
				}
				if (image.name == "MessageWindow")
				{
					Color _color = image.color;
                    _color.a = Config.CgModeAlpha.Value;
					image.color = _color;
					CgModeFrame = image;
				}
			}
			foreach (Image image in __instance.NameImage.GetComponentsInChildren<Image>())
			{
				if (image.name == "BaseName")
				{
					Color color = image.color;
                    color.a = Config.NormalAlpha.Value;
                    image.color = color;
					BaseNameFrame = image;
				}
			}
		}

        // Words
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitWordMasterBase), nameof(UnitWordMasterBase.Word), MethodType.Getter)]
        public static void SetHomeWord(ref string __result)
        {
            if (!Config.Translation.Value)
            {
                return;
            }
            if (Translation.words.TryGetValue(__result, out string text))
            {
                __result = text;
            }
        }

        // Words font
        [HarmonyPrefix]
		[HarmonyPatch(typeof(HomeSpineObject), nameof(HomeSpineObject.PlayWord))]
		public static void SetHomeWordFont(HomeSpineObject __instance)
		{
			if (Config.Translation.Value)
			{
				if (Translation.fontAsset == null)
				{
                    Plugin.Instance.StartCoroutine(Translation.LoadFontAsset());
                }
				if (originalFontAsset == null)
				{
					originalFontAsset = __instance._wordText.font;
				}
				__instance._wordText.font = Translation.fontAsset;
				__instance._wordText.lineSpacing = 24f;
				__instance._wordText.paragraphSpacing = 8f;
			}
            else
            {
                if (originalFontAsset != null)
                {
                    __instance._wordText.font = originalFontAsset;
                }
                __instance._wordText.lineSpacing = 0f;
                __instance._wordText.paragraphSpacing = -16f;
            }
		}

        // Skip cutin
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CutInMoviePlayer), nameof(CutInMoviePlayer.PlayAsync))]
        public static void SkipCutin(CutInMoviePlayer __instance)
        {
            if (Config.IsSkipCutin.Value)
            {
                __instance.Skip();
            }
        }

        // Change FPS
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GcOptionData), nameof(GcOptionData.SetPowerSaving))]
        public static void ChangeFrameRate()
        {
            int fps = Config.FrameRate.Value;
            if (fps > 0)
            {
                GcOptionData.ChangeApplicationTargetFrameRate(fps);
            }
        }

    }
}
