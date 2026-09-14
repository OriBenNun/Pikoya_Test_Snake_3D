using UnityEditor;
using UnityEngine;

namespace GardenSnake.Editor
{
    /// <summary>Creates reusable tuning assets without replacing existing artist settings.</summary>
    public static class GardenTuning
    {
        public const string Folder = "Assets/Tuning";

        public static void Bind(SnakeManager snake)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Tuning");
            var serialized = new SerializedObject(snake);
            Assign<SnakeSkinSettings>(serialized, "skinSettings", "Snake Skin");
            Assign<SnakeMouthSettings>(serialized, "mouthSettings", "Snake Mouth");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign<T>(SerializedObject target, string field, string name) where T : ScriptableObject
        {
            var property = target.FindProperty(field);
            if (property.objectReferenceValue != null) return;
            string path = Folder + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            property.objectReferenceValue = asset;
        }
    }
}
