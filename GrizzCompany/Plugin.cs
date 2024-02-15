using BepInEx;
using BepInEx.Logging;

namespace GrizzCompany
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "SylviBlossom.GrizzCompany";
        public const string NAME = "GrizzCompany";
        public const string VERSION = "0.1.0";

        public static Plugin Instance { get; private set; }
        public static new ManualLogSource Logger { get; private set; }

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

			Patches.Load();
			Assets.Load();

            Assets.Griller.Register();
            
            Logger.LogInfo($"Plugin {GUID} is loaded!");
        }
    }
}