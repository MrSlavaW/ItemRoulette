using BepInEx;
using ItemRoulette.Artifacts;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using ItemRoulette.Core.Modules;

namespace ItemRoulette
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api.content_management", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ZetAspectsCompat.ModGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(QualityCompat.ModGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class ItemRoulettePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.user.itemroulette";
        public const string PluginName = "ItemRoulette";
        public const string PluginVersion = "1.5.2";

        private void Awake()
        {
            // Register all modular architecture components in priority order
            ModuleManager.RegisterModule(new ConfigurationModule(Config));
            ModuleManager.RegisterModule(new LocalizationModule(Info.Location));
            ModuleManager.RegisterModule(new NetworkModule());
            ModuleManager.RegisterModule(new ServerEventsModule());
            ModuleManager.RegisterModule(new ServerSpinServiceModule());
            ModuleManager.RegisterModule(new CasinoServerServiceModule());
            ModuleManager.RegisterModule(new RunStateModule());
            ModuleManager.RegisterModule(new DropSuppressionModule());
            ModuleManager.RegisterModule(new RouletteUIModule());

            // Initialize all modules and subscribe unified lifecycle dispatchers
            ModuleManager.InitializeAll();

            // Register Artifact of the Roulette and lifecycle gating
            RouletteArtifact.Register();
            RouletteGate.Init();

            Logger.LogInfo($"[{PluginName} v{PluginVersion}] All modules successfully loaded! Roulette: [{ModConfig.HotkeyKey}], Casino: [{ModConfig.CasinoToggleHotkeyKey}], Admin: [{ModConfig.AdminMenuHotkeyKey}]");
        }

        private void OnDestroy()
        {
            // Clean unhooking and destruction of all modules in reverse priority order
            ModuleManager.CleanupAll();
        }
    }
}
