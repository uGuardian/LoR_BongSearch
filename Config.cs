using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using GameSave;
using Mod;

// Your namespace here
namespace BongSearch {
	public static class ConfigInitializer {
		public static void OnInitializeMod() {
			// Init config
			if (Array.Exists(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name.Contains("ConfigAPI"))) {
				InitConfig.Init();
			} else {
				StandaloneConfig.Load();
				StandaloneConfig.EchoAll();
			}
			if (BongSearch_Config.Instance.LegacyMode && BongSearch_Config.Instance.CultureSensitive) {
				Singleton<ModContentManager>.Instance.AddErrorLog(
					"BongSearch: Legacy mode isn't compatible with Culture Sensitive mode");
			}
			// TODO Culture Sensitive Mode
			if (BongSearch_Config.Instance.CultureSensitive) {
				Singleton<ModContentManager>.Instance.AddErrorLog("BongSearch: Culture Sensitive Mode isn't ready yet");
				BongSearch_Config.Instance.CultureSensitive = false;
			}
		}
	}
	internal static class InitConfig {
		internal static void Init() {
			ConfigAPI.Init("BongSearch", BongSearch_Config.Instance);
		}
	}

	[Serializable]
	public class BongSearch_Config {
		public int version = 1;
		public bool LegacyMode = false;
		public bool CompatibilityMode = false;
		public bool CultureSensitive = false;

		public bool overrideAutoRangeFilter = true;
		public bool overrideAutoExclusiveFilter = false;

		public static BongSearch_Config Instance = new BongSearch_Config();
	}

	internal static class StandaloneConfig {
		internal static void Load() {
			var directory = Directory.CreateDirectory(SaveManager.GetFullPath("ModConfigs"));
			var configFile = new FileInfo($"{directory.FullName}/BongSearch.ini");
			var exists = configFile.Exists;
			string config;
			using (var stream = configFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
				if (exists) {
					try {
						var reader = new StreamReader(stream);
						config = reader.ReadToEnd();
						JsonUtility.FromJsonOverwrite(config, BongSearch_Config.Instance);
						reader.DiscardBufferedData();
					} catch (Exception ex) {
						Debug.LogError("Error reading config file");
						Debug.LogException(ex);
						Singleton<ModContentManager>.Instance.AddErrorLog("BongSearch: ini file invalid, resetting it");
					}
				}
				stream.Seek(0, SeekOrigin.Begin);
				config = JsonUtility.ToJson(BongSearch_Config.Instance, true);
				var writer = new StreamWriter(stream);
				writer.Write(config);
				writer.Flush();
				stream.SetLength(stream.Position);
			}
		}
		internal static void EchoAll() {
			Debug.Log($"BongSearch: {JsonUtility.ToJson(BongSearch_Config.Instance, true)}");
		}
	}
}