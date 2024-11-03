using UnityEditor;

namespace Functional
{
    internal static class AssetsReserialize
    {
        [MenuItem("Tools/Force Reserialize Assets")]
        private static void ForceReserializeAssets()
        {
            AssetDatabase.ForceReserializeAssets();
        }
    }
}
