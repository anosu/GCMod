using System;
using System.Text;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace GCMod
{
	[BepInPlugin("GCMod", "GCMod", "11.45.14")]
	public class Plugin : BasePlugin
	{
		public override void Load()
		{
			try
			{
				Console.OutputEncoding = Encoding.UTF8;
				Plugin.Global.Log = base.Log;
				ManualLogSource log = base.Log;
				bool flag = true;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(18, 1, ref flag);
				if (flag)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Plugin ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>("GCMod");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" is loaded!");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
			catch (Exception)
			{
				Plugin.Global.Log = base.Log;
			}
			GCMod.Config.Read();
			Translation.Init();
			Patch.Initialize();
		}
		public class Global
		{
			public static ManualLogSource Log { get; set; }
		}
	}
}
