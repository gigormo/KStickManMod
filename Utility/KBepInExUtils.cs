using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace KrunchyStickmanMod.Utility {

    public class KBepInExUtils {

        public static Texture2D LoadTextureFromFile(string fileName) {
            string dllPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string fullPath = Path.Combine(dllPath, fileName);

            if (File.Exists(fullPath)) {
                byte[] fileData = File.ReadAllBytes(fullPath);

                Texture2D tex = new(2, 2);

                if (ImageConversion.LoadImage(tex, fileData)) {
                    return tex;
                }
            }

            Debug.LogError($"Could not find or load texture at: {fullPath}");
            return null;
        }

        public static void SetTimeScale(float scale) {
            Time.timeScale = scale;
        }

        public static GameObject FindDeepChild(Transform parent, string name) {
            foreach (Transform child in parent) {
                if (child.name == name) return child.gameObject;
                GameObject result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        public static List<Transform> GetAllObjects(Transform parent) {
            List<Transform> allChildren = [parent];
            foreach (Transform child in parent) {
                allChildren.AddRange(GetAllObjects(child));
            }
            return allChildren;
        }

#nullable enable

        public static int GetChildCount(Transform? parentTransform, GameObject? parentObject) {
            // Ensure at least one parameter is not null
            if (parentTransform == null && parentObject == null) {
                throw new ArgumentNullException("Both parentTransform and parentObject cannot be null.");
            }

            if (parentObject != null) {
                parentTransform = parentObject.transform;
            }

            Transform rootTransform = parentTransform ?? parentObject!.transform;

            int count = 0;

            foreach (Transform child in rootTransform) {
                count++;
                count += GetChildCount(child, null);
            }

            return count;
        }

#nullable disable

        public static void FixTMP() {
            if (TMP_Settings.instance == null) {
                // Create a new TMP_Settings instance if it doesn't exist -- _ discards the returned object
                _ = ScriptableObject.CreateInstance<TMP_Settings>();
            }
        }

        public static void FixText(TMP_StyleSheet styleSheet, GameObject parent) {
            foreach (TMP_Text tmp in parent.GetComponentsInChildren<TMP_Text>()) {
                if (styleSheet != null) tmp.styleSheet = styleSheet;
            }
        }
    }
}