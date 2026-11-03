using RoR2;
using UnityEngine.SceneManagement;

namespace ItemRoulette.Core.Modules
{
    /// <summary>
    /// Base contract for all modular subsystems in ItemRoulette.
    /// Supports deterministic prioritized startup and reverse cleanup.
    /// </summary>
    public interface IModModule
    {
        string Name { get; }

        /// <summary>
        /// Higher priority modules initialize first and clean up last.
        /// </summary>
        int Priority => 0;

        void Initialize();
        void Cleanup();
    }

    /// <summary>
    /// Module listener for Run-level lifecycle events.
    /// </summary>
    public interface IRunLifecycleListener
    {
        void OnRunStart(Run run);
        void OnRunDestroy(Run run);
    }

    /// <summary>
    /// Module listener for Stage-level and scene transitions.
    /// </summary>
    public interface IStageLifecycleListener
    {
        void OnStageStart(Stage stage);
        void OnServerStageComplete(Stage stage);
        void OnSceneExitBegin(SceneExitController sec);
        void OnSceneLoaded(Scene scene, LoadSceneMode mode);
    }
}
