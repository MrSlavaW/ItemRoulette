using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItemRoulette.Core.Modules
{
    /// <summary>
    /// Central registry and lifecycle coordinator for all ItemRoulette mod modules.
    /// Manages single global subscriptions to RoR2 lifecycle events and dispatches them safely.
    /// </summary>
    public static class ModuleManager
    {
        private static readonly List<IModModule> _modules = new List<IModModule>();
        private static bool _initialized = false;

        public static void RegisterModule(IModModule module)
        {
            if (module == null) return;
            if (_initialized)
            {
                Debug.LogWarning($"[ItemRoulette] Attempted to register module '{module.Name}' after ModuleManager has already initialized.");
                return;
            }
            _modules.Add(module);
        }

        public static T? GetModule<T>() where T : class, IModModule
        {
            return _modules.OfType<T>().FirstOrDefault();
        }

        public static void InitializeAll()
        {
            if (_initialized) return;

            // Sort by priority (highest priority first)
            _modules.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            // Hook RoR2 lifecycle events once globally
            Run.onRunStartGlobal += DispatchRunStart;
            Run.onRunDestroyGlobal += DispatchRunDestroy;
            Stage.onStageStartGlobal += DispatchStageStart;
            Stage.onServerStageComplete += DispatchServerStageComplete;
            SceneExitController.onBeginExit += DispatchSceneExitBegin;
            SceneManager.sceneLoaded += DispatchSceneLoaded;

            foreach (var module in _modules)
            {
                try
                {
                    module.Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ItemRoulette] Exception initializing module '{module.Name}': {ex}");
                }
            }

            _initialized = true;
        }

        public static void CleanupAll()
        {
            if (!_initialized) return;

            // Unhook global events
            Run.onRunStartGlobal -= DispatchRunStart;
            Run.onRunDestroyGlobal -= DispatchRunDestroy;
            Stage.onStageStartGlobal -= DispatchStageStart;
            Stage.onServerStageComplete -= DispatchServerStageComplete;
            SceneExitController.onBeginExit -= DispatchSceneExitBegin;
            SceneManager.sceneLoaded -= DispatchSceneLoaded;

            // Cleanup modules in reverse priority order
            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _modules[i].Cleanup();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ItemRoulette] Exception during cleanup of module '{_modules[i].Name}': {ex}");
                }
            }

            _modules.Clear();
            _initialized = false;
        }

        // =====================================================================
        // Event Dispatchers
        // =====================================================================

        private static void DispatchRunStart(Run run)
        {
            foreach (var l in _modules.OfType<IRunLifecycleListener>())
            {
                try { l.OnRunStart(run); }
                catch (Exception ex) { Debug.LogError($"[ItemRoulette] Error in OnRunStart listener: {ex}"); }
            }
        }

        private static void DispatchRunDestroy(Run run)
        {
            foreach (var l in _modules.OfType<IRunLifecycleListener>())
            {
                try { l.OnRunDestroy(run); }
                catch (Exception ex) { Debug.LogError($"[ItemRoulette] Error in OnRunDestroy listener: {ex}"); }
            }
        }

        private static void DispatchStageStart(Stage stage)
        {
            foreach (var l in _modules.OfType<IStageLifecycleListener>())
            {
                try { l.OnStageStart(stage); }
                catch (Exception ex) { Debug.LogError($"[ItemRoulette] Error in OnStageStart listener: {ex}"); }
            }
        }

        private static void DispatchServerStageComplete(Stage stage)
        {
            foreach (var l in _modules.OfType<IStageLifecycleListener>())
            {
                try { l.OnServerStageComplete(stage); }
                catch (Exception ex) { Debug.LogError($"[ItemRoulette] Error in OnServerStageComplete listener: {ex}"); }
            }
        }

        private static void DispatchSceneExitBegin(SceneExitController sec)
        {
            foreach (var l in _modules.OfType<IStageLifecycleListener>())
            {
                try { l.OnSceneExitBegin(sec); }
                catch (Exception ex) { Debug.LogError($"[ItemRoulette] Error in OnSceneExitBegin listener: {ex}"); }
            }
        }

        private static void DispatchSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var l in _modules.OfType<IStageLifecycleListener>())
            {
                try { l.OnSceneLoaded(scene, mode); }
                catch (Exception ex) { Debug.LogError($"[ItemRoulette] Error in OnSceneLoaded listener: {ex}"); }
            }
        }
    }
}
