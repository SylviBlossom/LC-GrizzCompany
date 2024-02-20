using BepInEx;
using BepInEx.Logging;
using System.Reflection;
using UnityEngine;

namespace GrizzCompany.Enemies
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "SylviBlossom.GrizzCompany-Enemies";
        public const string NAME = "GrizzCompany-Enemies";
        public const string VERSION = "0.1.0";

        public static Plugin Instance { get; private set; }
        public static new ManualLogSource Logger { get; private set; }

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

			Patches.Load();
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