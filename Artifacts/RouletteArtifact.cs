using R2API;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Artifacts
{
    /// <summary>
    /// Defines and registers the "Artifact of the Roulette".
    /// When selected in lobby or run rules, enables ItemRoulette mechanics.
    /// When unselected, the run plays completely as vanilla Risk of Rain 2.
    /// </summary>
    public static class RouletteArtifact
    {
        public const string NameToken = "ITEMROULETTE_ARTIFACT_NAME";
        public const string DescriptionToken = "ITEMROULETTE_ARTIFACT_DESC";

        public static ArtifactDef? Def { get; private set; }

        public static void Register()
        {
            if (Def != null) return;

            var def = ScriptableObject.CreateInstance<ArtifactDef>();
            def.cachedName = "ItemRoulette_RouletteArtifact";
            def.nameToken = NameToken;
            def.descriptionToken = DescriptionToken;
            def.smallIconSelectedSprite = ArtifactIcon.Create(selected: true);
            def.smallIconDeselectedSprite = ArtifactIcon.Create(selected: false);

            try
            {
                if (ContentAddition.AddArtifactDef(def))
                {
                    Def = def;
                }
                else
                {
                    Debug.LogWarning("[ItemRoulette] ContentAddition.AddArtifactDef returned false; Artifact of the Roulette not registered.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemRoulette] Failed to register RouletteArtifact with R2API: {ex}");
            }
        }
    }
}
