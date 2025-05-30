using System;
using UnityEngine;

namespace GCMod
{
	public class PluginBehaviour : MonoBehaviour
	{
		private void Update()
		{
            if (Input.GetKeyDown(KeyCode.F5))
			{
				Config.ModifyText.Value = !Config.ModifyText.Value;
                Notification.Show($"[{Config.ModifyText.Definition.Section}]", $"{Config.ModifyText.Definition.Key} => {Config.ModifyText.Value}");
            }

			if (Input.GetKeyDown(KeyCode.F6))
			{
				float alpha = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
					? Config.NormalAlpha.Value + 0.1f
					: Config.NormalAlpha.Value - 0.1f;
                Config.NormalAlpha.Value = Mathf.Clamp(MathF.Round(alpha, 1), 0f, 1f);
                if (Patch.NormalFrame != null && Patch.BaseNameFrame != null)
				{
					Color color1 = Patch.NormalFrame.color;
					Color color2 = Patch.BaseNameFrame.color;
                    color1.a = (color2.a = Config.NormalAlpha.Value);
					Patch.NormalFrame.color = color1;
					Patch.BaseNameFrame.color = color2;
				}
			}

			if (Input.GetKeyDown(KeyCode.F7))
			{
                float alpha = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    ? Config.CgModeAlpha.Value + 0.1f
                    : Config.CgModeAlpha.Value - 0.1f;
                Config.CgModeAlpha.Value = Mathf.Clamp(MathF.Round(alpha, 1), 0f, 1f);
                if (Patch.CgModeFrame != null)
                {
                    Color color = Patch.CgModeFrame.color;
                    color.a = Config.CgModeAlpha.Value;
                    Patch.CgModeFrame.color = color;
                }
			}

			if (Input.GetKeyDown(KeyCode.F8))
			{
				Config.Translation.Value = !Config.Translation.Value;
                Notification.Show($"[{Config.Translation.Definition.Section}]", $"{Config.Translation.Definition.Key} => {Config.Translation.Value}");
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                Plugin.Config.Reload();
                Plugin.Log.LogInfo("Config reloaded");
                Notification.Show("Config", "Reloaded successfully");
            }
        }
    }
}
