using UnityEditor;

[System.Serializable]
public class InclusiveTestFilterConfig
{
    public SceneAsset[] FilteredScenes;
    public BuildTarget BuildPlatform = BuildTarget.NoTarget;
}
