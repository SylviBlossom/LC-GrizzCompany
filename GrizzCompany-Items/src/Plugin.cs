using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace GrizzCompany.Items
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "SylviBlossom.GrizzCompany-Items";
        public const string NAME = "GrizzCompany-Items";
        public const string VERSION = "1.0.2";

        public static Plugin Instance { get; private set; }
		public static new Config Config { get; private set; }
        public static new ManualLogSource Logger { get; private set; }

        private void Awake()
        {
            Instance = this;
			Config = new(base.Config);
            Logger = base.Logger;

			var harmony = new Harmony(GUID);
			harmony.PatchAll(typeof(Config));

			Assets.Load();

			InitializeModNetworking();

			Logger.LogInfo($"Plugin {NAME} is loaded!");
        }

        private static void InitializeModNetworking()
        {
			var types = Assembly.GetExecutingAssembly().GetTypes();
			foreach (var type in types)
			{
				var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				foreach (var method in methods)
				{
					var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
					if (attributes.Length > 0)
					{
						method.Invoke(null, null);
					}
				}
			}
		}
    }
}