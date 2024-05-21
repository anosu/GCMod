using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GCMod
{
	public class Translation
	{
        public static Dictionary<string, string> names = new Dictionary<string, string>();
		public static Dictionary<int, Dictionary<string, string>> novels = new Dictionary<int, Dictionary<string, string>>();

		public static void Init()
		{
			if (!Config.Translation)
			{
				return;
			}
			HttpResponseMessage httpResponseMessage = Translation.HttpRequester(Config.TranslationUrl + "/gc/names/zh_Hans.json");
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				Translation.names = JsonSerializer.Deserialize<Dictionary<string, string>>(httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult(), null);
				Plugin.Global.Log.LogInfo("Names translation loaded. Total: " + Translation.names.Count.ToString());
				return;
			}
			Plugin.Global.Log.LogWarning("Names translation failed to load !");
		}

		public static HttpResponseMessage HttpRequester(string url)
		{
			HttpClient client = new HttpClient();
			Task<HttpResponseMessage> task = Task.Run<HttpResponseMessage>(() => client.GetAsync(url));
			task.Wait();
			return task.Result;
		}

		public static void FetchNovelTranslation(int novelId)
		{
			HttpResponseMessage httpResponseMessage = Translation.HttpRequester(Config.TranslationUrl + "/gc/novels/" + novelId.ToString() + "/zh_Hans.json");
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				string result = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				Translation.novels[novelId] = JsonSerializer.Deserialize<Dictionary<string, string>>(result, null);
				Plugin.Global.Log.LogInfo("Novel translation loaded. Total: " + Translation.novels[novelId].Count.ToString());
				return;
			}
			Plugin.Global.Log.LogWarning("Novel translation failed to load !");
		}

		public static async Task FetchNovelTranslationAsync(int novelId)
		{
			try
			{
				HttpResponseMessage httpResponseMessage = await new HttpClient().GetAsync(Config.TranslationUrl + "/gc/novels/" + novelId.ToString() + "/zh_Hans.json");
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					string json = await httpResponseMessage.Content.ReadAsStringAsync();
					Translation.novels[novelId] = JsonSerializer.Deserialize<Dictionary<string, string>>(json, null);
					Plugin.Global.Log.LogInfo("Novel translation loaded. Total: " + Translation.novels[novelId].Count.ToString());
				}
				else
				{
					Plugin.Global.Log.LogWarning("Novel translation failed to load!");
				}
			}
			catch (Exception ex)
			{
				Plugin.Global.Log.LogError("Error loading novel translation: " + ex.Message);
			}
		}
	}
}
