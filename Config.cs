using System;
using System.Text.Json;
using Il2CppSystem.IO;
using Il2CppSystem.Text;

namespace GCMod
{
	public class Config
	{
		public static bool OffLine;
		public static string ApiUrl;
		public static bool Translation;
		public static string TranslationUrl;
		public static string TranslationFontName;
        
		public static void Read()
		{
			if (File.Exists("./BepInEx/plugins/GCMod.json"))
			{
				JsonElement rootElement = JsonDocument.Parse(File.InternalReadAllText("./BepInEx/plugins/GCMod.json", Encoding.UTF8), default(JsonDocumentOptions)).RootElement;
				JsonElement jsonElement;
				if (rootElement.TryGetProperty("offline", out jsonElement))
				{
					Config.OffLine = jsonElement.GetBoolean();
				}
				JsonElement jsonElement2;
				if (rootElement.TryGetProperty("offline_api", out jsonElement2))
				{
					Config.ApiUrl = jsonElement2.GetString();
				}
				JsonElement jsonElement3;
				if (rootElement.TryGetProperty("translation", out jsonElement3))
				{
					Config.Translation = jsonElement3.GetBoolean();
				}
				JsonElement jsonElement4;
				if (rootElement.TryGetProperty("translation_api", out jsonElement4))
				{
					Config.TranslationUrl = jsonElement4.GetString();
				}
				JsonElement jsonElement5;
				if (rootElement.TryGetProperty("translation_font", out jsonElement5))
				{
					Config.TranslationFontName = jsonElement5.GetString();
				}
				Plugin.Global.Log.LogInfo(string.Format("Offline: {0}", Config.OffLine));
				Plugin.Global.Log.LogInfo("ApiUrl: " + Config.ApiUrl);
				Plugin.Global.Log.LogInfo(string.Format("Translation: {0}", Config.Translation));
				Plugin.Global.Log.LogInfo("TranslationUrl: " + Config.TranslationUrl);
				Plugin.Global.Log.LogInfo("TranslationFont: " + Config.TranslationFontName);
				return;
			}
			Plugin.Global.Log.LogWarning("Config.json NOT Found!");
			Config.OffLine = false;
			Config.ApiUrl = null;
			Config.Translation = false;
			Config.TranslationUrl = null;
			Config.TranslationFontName = null;
		}
	}
}
