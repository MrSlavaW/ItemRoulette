using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using BepInEx;
using BepInEx.Configuration;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using ItemRoulette.Hooks;
using ItemRoulette.UI;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItemRoulette.Core.Modules
{
    /// <summary>
    /// Module 1: Initializes configuration, RiskOfOptions, and mod compatibility layers.
    /// </summary>
    public sealed class ConfigurationModule : IModModule
    {
        private readonly ConfigFile _config;

        public ConfigurationModule(ConfigFile config)
        {
            _config = config;
        }

        public string Name => "Configuration";
        public int Priority => 100;

        public void Initialize()
        {
            ModConfig.Init(_config);
            RiskOfOptionsCompat.Init();
            ZetAspectsCompat.Init();
            QualityCompat.Init();
        }

        public void Cleanup() { }
    }

    /// <summary>
    /// Module 2: Manages official RoR2.Language folder registration, fallback token parsing, and translation sub-mod auto-discovery.
    /// </summary>
    public sealed class LocalizationModule : IModModule
    {
        private readonly string _pluginAssemblyLocation;

        public LocalizationModule(string pluginAssemblyLocation)
        {
            _pluginAssemblyLocation = pluginAssemblyLocation;
        }

        public string Name => "Localization";
        public int Priority => 110;

        public void Initialize()
        {
            RoR2.Language.collectLanguageRootFolders += RegisterLanguageFolders;
            RoR2.Language.onCurrentLanguageChanged += OnCurrentLanguageChanged;
            EnsureTokensLoadedAndInjected();
        }

        public void Cleanup()
        {
            RoR2.Language.collectLanguageRootFolders -= RegisterLanguageFolders;
            RoR2.Language.onCurrentLanguageChanged -= OnCurrentLanguageChanged;
        }

        private void OnCurrentLanguageChanged()
        {
            try
            {
                var enTokens = LoadTokensForLanguage("en");
                var ruTokens = LoadTokensForLanguage("ru");
                InjectIntoCurrentLanguage(enTokens, ruTokens);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to update tokens on language change: {ex.Message}");
            }
        }

        private void EnsureTokensLoadedAndInjected()
        {
            try
            {
                var enTokens = LoadTokensForLanguage("en");
                var ruTokens = LoadTokensForLanguage("ru");

                Localization.SetFallbackTokens("en", enTokens);
                Localization.SetFallbackTokens("ru", ruTokens);

                // Inject into English fallback dictionary if available
                if (RoR2.Language.english != null)
                {
                    foreach (var kvp in enTokens)
                    {
                        RoR2.Language.english.SetStringByToken(kvp.Key, kvp.Value);
                    }
                }

                // Inject into Russian dictionary if available
                var ruLang = RoR2.Language.FindLanguageByName("ru");
                if (ruLang != null)
                {
                    foreach (var kvp in ruTokens)
                    {
                        ruLang.SetStringByToken(kvp.Key, kvp.Value);
                    }
                }

                InjectIntoCurrentLanguage(enTokens, ruTokens);
                Debug.Log($"[ItemRoulette] Localization ready: {enTokens.Count} EN and {ruTokens.Count} RU tokens registered.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to ensure localization tokens: {ex.Message}");
            }
        }

        private void InjectIntoCurrentLanguage(Dictionary<string, string> enTokens, Dictionary<string, string> ruTokens)
        {
            var current = RoR2.Language.currentLanguage;
            if (current == null) return;

            // Always inject English tokens first as universal base fallback
            foreach (var kvp in enTokens)
            {
                current.SetStringByToken(kvp.Key, kvp.Value);
            }

            // If active language is Russian, overwrite with Russian tokens
            if (Localization.IsRussian)
            {
                foreach (var kvp in ruTokens)
                {
                    current.SetStringByToken(kvp.Key, kvp.Value);
                }
            }
        }

        private Dictionary<string, string> LoadTokensForLanguage(string langCode)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? jsonContent = null;

            try
            {
                // 1. Try loading from disk in plugin Language folder
                string? pluginDir = Path.GetDirectoryName(_pluginAssemblyLocation);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    string diskPath = Path.Combine(pluginDir, "Language", langCode, "ItemRoulette.json");
                    if (File.Exists(diskPath))
                    {
                        jsonContent = File.ReadAllText(diskPath);
                    }
                    else if (langCode.Equals("ru", StringComparison.OrdinalIgnoreCase))
                    {
                        string flatPath = Path.Combine(pluginDir, "ItemRoulette.json");
                        if (File.Exists(flatPath))
                        {
                            jsonContent = File.ReadAllText(flatPath);
                        }
                    }
                }

                // 2. Fallback to embedded assembly resource
                if (string.IsNullOrEmpty(jsonContent))
                {
                    var asm = typeof(LocalizationModule).Assembly;
                    string resName = $"ItemRoulette.Language.{langCode}.ItemRoulette.json";
                    using var stream = asm.GetManifestResourceStream(resName);
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        jsonContent = reader.ReadToEnd();
                    }
                }

                // 3. Parse JSON strings node
                if (!string.IsNullOrEmpty(jsonContent))
                {
                    var rootNode = SimpleJSON.JSON.Parse(jsonContent);
                    if (rootNode != null && rootNode["strings"] != null)
                    {
                        var stringsNode = rootNode["strings"];
                        foreach (string key in stringsNode.Keys)
                        {
                            dict[key] = stringsNode[key].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Error parsing {langCode} language tokens: {ex.Message}");
            }

            return dict;
        }

        private void RegisterLanguageFolders(List<string> folders)
        {
            try
            {
                // 1. Add our own mod's Language root
                string? pluginDir = Path.GetDirectoryName(_pluginAssemblyLocation);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    string myLang = Path.Combine(pluginDir, "Language");
                    if (Directory.Exists(myLang) && !folders.Contains(myLang))
                    {
                        folders.Add(myLang);
                    }
                }

                // 2. Discover any external translation packs or sub-mods in BepInEx/plugins/
                string pluginsRoot = Paths.PluginPath;
                if (Directory.Exists(pluginsRoot))
                {
                    foreach (string dir in Directory.EnumerateDirectories(pluginsRoot, "*", SearchOption.AllDirectories))
                    {
                        string dirName = Path.GetFileName(dir);
                        if (dirName.Equals("Language", StringComparison.OrdinalIgnoreCase) ||
                            dirName.Equals("Languages", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!folders.Contains(dir))
                            {
                                folders.Add(dir);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to register language root folders: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Module 3: Low-level HLAPI networking registration.
    /// </summary>
    public sealed class NetworkModule : IModModule, IRunLifecycleListener, IStageLifecycleListener
    {
        public string Name => "Networking";
        public int Priority => 80;

        public void Initialize()
        {
            NetworkHelper.Init();
        }

        public void Cleanup() { }

        public void OnRunStart(Run run) => NetworkHelper.EnsureServerHandlerRegistered();
        public void OnRunDestroy(Run run) { }
        public void OnStageStart(Stage stage) => NetworkHelper.EnsureServerHandlerRegistered();
        public void OnServerStageComplete(Stage stage) { }
        public void OnSceneExitBegin(SceneExitController sec) { }
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }
    }

    /// <summary>
    /// Module 4: Typed HLAPI client event synchronization.
    /// </summary>
    public sealed class ServerEventsModule : IModModule, IRunLifecycleListener, IStageLifecycleListener
    {
        public string Name => "ServerEvents";
        public int Priority => 75;

        public void Initialize()
        {
            ServerEvents.Init();
        }

        public void Cleanup() { }

        public void OnRunStart(Run run) => ServerEvents.EnsureClientHandlerRegistered();
        public void OnRunDestroy(Run run) { }
        public void OnStageStart(Stage stage) => ServerEvents.EnsureClientHandlerRegistered();
        public void OnServerStageComplete(Stage stage) { }
        public void OnSceneExitBegin(SceneExitController sec) { }
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }
    }

    /// <summary>
    /// Module 5: Server-authoritative roulette spin transactions and decisions.
    /// </summary>
    public sealed class ServerSpinServiceModule : IModModule, IRunLifecycleListener, IStageLifecycleListener
    {
        public string Name => "ServerSpinService";
        public int Priority => 70;

        public void Initialize()
        {
            ServerSpinService.Init();
        }

        public void Cleanup() { }

        public void OnRunStart(Run run)
        {
            ServerSpinService.EnsureServerHandlers();
            ServerSpinService.EnsureClientHandlers();
        }

        public void OnRunDestroy(Run run) { }

        public void OnStageStart(Stage stage)
        {
            ServerSpinService.EnsureServerHandlers();
            ServerSpinService.EnsureClientHandlers();
        }

        public void OnServerStageComplete(Stage stage) { }
        public void OnSceneExitBegin(SceneExitController sec) { }
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }
    }

    /// <summary>
    /// Module 5.5: Server-authoritative Casino Roulette transactions, payouts, and life bets.
    /// </summary>
    public sealed class CasinoServerServiceModule : IModModule, IRunLifecycleListener, IStageLifecycleListener
    {
        public string Name => "CasinoServerService";
        public int Priority => 65;

        public void Initialize()
        {
            CasinoServerService.Init();
        }

        public void Cleanup() { }

        public void OnRunStart(Run run)
        {
            CasinoServerService.EnsureServerHandlers();
            CasinoServerService.EnsureClientHandlers();
        }

        public void OnRunDestroy(Run run)
        {
            CasinoServerService.ClearPendingAwardsServer();
        }

        public void OnStageStart(Stage stage)
        {
            CasinoServerService.EnsureServerHandlers();
            CasinoServerService.EnsureClientHandlers();
        }

        public void OnServerStageComplete(Stage stage)
        {
            CasinoServerService.FlushAllPendingAwardsServer();
        }

        public void OnSceneExitBegin(SceneExitController sec)
        {
            CasinoServerService.FlushAllPendingAwardsServer();
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }
    }

    /// <summary>
    /// Module 6: Run and stage state lifecycle manager (free rolls, gravity, artifacts, curse stacks).
    /// </summary>
    public sealed class RunStateModule : IModModule, IRunLifecycleListener, IStageLifecycleListener
    {
        public string Name => "RunState";
        public int Priority => 60;

        public void Initialize() { }
        public void Cleanup() { }

        public void OnRunStart(Run run)
        {
            RouletteManager.Instance.ResetForNewRun();
            ModConfig.ResetSyncedConfig();
        }

        public void OnRunDestroy(Run run)
        {
            RouletteManager.Instance.ResetForNewRun();
            ModConfig.ResetSyncedConfig();
        }

        public void OnStageStart(Stage stage)
        {
            RouletteManager.Instance.ResetForNewStage();
        }

        public void OnServerStageComplete(Stage stage)
        {
            RunStateStore.Instance.ClearStageTemporaryArtifacts();
            RunStateStore.Instance.RestoreStageGravity();
            RunStateStore.Instance.ClearPlayerTierMultipliers();
        }

        public void OnSceneExitBegin(SceneExitController sec) { }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Run.instance != null)
            {
                RouletteManager.Instance.CheckAndHandleStageTransition();
            }
        }
    }

    /// <summary>
    /// Module 7: Orchestrates drop suppression hooks and debouncing.
    /// </summary>
    public sealed class DropSuppressionModule : IModModule, IRunLifecycleListener, IStageLifecycleListener
    {
        public string Name => "DropSuppression";
        public int Priority => 50;

        public void Initialize()
        {
            DropSuppressionHooks.Init();
        }

        public void Cleanup()
        {
            DropSuppressionHooks.Cleanup();
        }

        public void OnRunStart(Run run)
        {
            DropStateManager.ResetForNewStage();
        }

        public void OnRunDestroy(Run run) { }

        public void OnStageStart(Stage stage)
        {
            DropStateManager.ResetForNewStage();
            if (UnityEngine.Networking.NetworkServer.active)
            {
                DropSuppressionHooks.BroadcastHostConfigSync();
            }
        }

        public void OnServerStageComplete(Stage stage) { }
        public void OnSceneExitBegin(SceneExitController sec) { }
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }
    }

    /// <summary>
    /// Module 8: IMGUI User Interface root controller and animations lifecycle.
    /// </summary>
    public sealed class RouletteUIModule : IModModule, IRunLifecycleListener, IStageLifecycleListener
    {
        private GameObject? _uiRoot;

        public string Name => "RouletteUI";
        public int Priority => 10;

        public void Initialize()
        {
            if (_uiRoot != null) return;
            _uiRoot = new GameObject("ItemRoulette_UIRoot");
            _uiRoot.AddComponent<RouletteUI>();
            _uiRoot.AddComponent<AdminMenuUI>();
            _uiRoot.AddComponent<CasinoRouletteUI>();
            _uiRoot.AddComponent<NotificationToastUI>();
            _uiRoot.AddComponent<NotificationJournalUI>();
            UnityEngine.Object.DontDestroyOnLoad(_uiRoot);
        }

        public void Cleanup()
        {
            if (_uiRoot != null)
            {
                UnityEngine.Object.Destroy(_uiRoot);
                _uiRoot = null;
            }
            UiCommon.Cleanup();
        }

        public void OnRunStart(Run run)
        {
            RouletteUI.Instance?.ResetUI(preserveAutoSpin: false);
        }

        public void OnRunDestroy(Run run) { }

        public void OnStageStart(Stage stage)
        {
            RouletteUI.Instance?.ResetUI(preserveAutoSpin: true);
        }

        public void OnServerStageComplete(Stage stage)
        {
            RouletteUI.Instance?.HandleSceneExitTransition();
            CasinoRouletteUI.Instance?.ForceFinishSpinImmediate();
        }

        public void OnSceneExitBegin(SceneExitController sec)
        {
            RouletteUI.Instance?.HandleSceneExitTransition();
            CasinoRouletteUI.Instance?.ForceFinishSpinImmediate();
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }
    }
}
