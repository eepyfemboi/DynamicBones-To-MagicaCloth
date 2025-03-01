using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MagicaCloth2;
using Unity.VisualScripting;
using System.IO.Enumeration;
using System;

namespace eepyfemboi
{
    public class ConvertDynamicBoneToMagicaCloth2 : EditorWindow
    {
        private GameObject targetObject;
        private bool smartConvert;
        private Dictionary<string, string> magicaClothPresets = new Dictionary<string, string>();
        private bool presetsLoaded = false;

        [MenuItem("eepy.io/DB2MC Converter")]
        public static void ShowWindow()
        {
            GetWindow<ConvertDynamicBoneToMagicaCloth2>("DB2MC Converter");
        }

        private void OnGUI()
        {
            GUILayout.Label("DB2MC Converter", EditorStyles.boldLabel);
            targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
            smartConvert = (bool)EditorGUILayout.Toggle(value: smartConvert, label: "Smart(dumb)Convert (try to use presets based on the names of the gameobjects, currently recommended because i cant figure out how to directly convert db values to mc)");

            if (GUILayout.Button("convert"))
            {
                if (targetObject != null)
                {
                    ConvertDynamicBones(targetObject);
                }
                else
                {
                    Debug.LogError("please put your avatar object to convert dynamicbones on in here");
                }
            }
        }

        private void LoadMagicaPresets()
        {
            var guidArray = AssetDatabase.FindAssets($"MC2_Preset t:{nameof(TextAsset)}");
            if (guidArray == null)
                return;

            foreach (var guid in guidArray)
            {
                var filePath = AssetDatabase.GUIDToAssetPath(guid);

                if (filePath.EndsWith(".json") == false)
                    continue;

                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                if (text)
                {
                    string fileName = filePath.Split('/')[^1].ToLower().Replace("mc2_preset_", "").Replace(".json", "").Trim().Replace(" ", "");
                    magicaClothPresets.Add(fileName, text.text);
                    //Debug.Log(fileName);
                }
            }

            presetsLoaded = true;
        }

        private void ConvertDynamicBones(GameObject obj)
        {
            LoadMagicaPresets();

            if (obj == null)
            {
                Debug.LogError("please select an avatar to convert dynamicbones on");
                return;
            }

            DynamicBone[] dynamicBones = obj.GetComponentsInChildren<DynamicBone>();

            if (dynamicBones.Length == 0)
            {
                Debug.LogWarning("theres no dynamicbones on this avatar");
                return;
            }

            GameObject duplicatedObject = Instantiate(obj);
            duplicatedObject.name = obj.name + "__DB2MC_Converted";
            duplicatedObject.transform.SetParent(obj.transform.parent);

            obj.SetActive(false);

            foreach (var dynamicBone in duplicatedObject.GetComponentsInChildren<DynamicBone>())
            {
                ConvertDynamicBone(dynamicBone);
            }

            Debug.Log($"done converting {dynamicBones.Length} dynamicbones.");
        }

        private void NormalConvertDynamicBone(DynamicBone dynamicBone, MagicaCloth magicaCloth)
        {
            // uhhh this doesnt rly do anything lol, i dont even know where to begin with making this work
        }

        private bool _Inc(string n, string t)
        {
            if (n.Contains(t, StringComparison.OrdinalIgnoreCase)) { return true; }
            return false;
        }

        private string GuessTypeFromName(string name) // i know this is shitty, i know, you dont have to tell me, i know its terrible, but its better than nothing. please dont judge me i am very sensitive and will cry
        {
            string currentGuess = "tail";

            if (_Inc(name, "hair"))
            {
                if (_Inc(name, "front"))
                {
                    return "fronthair";
                }
                if (_Inc(name, "side"))
                {
                    currentGuess = "shorthair";
                }
                if (_Inc(name, "back"))
                {
                    currentGuess = "longhair";
                }
                if (_Inc(name, "short"))
                {
                    currentGuess = "shorthair";
                }
                if (_Inc(name, "long"))
                {
                    currentGuess = "longhair";
                }
            }
            if (_Inc(name, "tail"))
            {
                return "tail";
            }
            if (_Inc(name, "ahoge"))
            {
                currentGuess = "softspring";
            }
            if (_Inc(name, "ear"))
            {
                currentGuess = "middlespring";
            }
            if (_Inc(name, "string") | _Inc(name, "cloth") | _Inc(name, "accessory"))
            {
                currentGuess = "accessory";
            }
            if (_Inc(name, "skirt"))
            {
                currentGuess = "skirt";
            }
            if (_Inc(name, "cape"))
            {
                currentGuess = "cape";
            }

            return currentGuess;
        }

        private string CalculateMagicaPresetToConvert(DynamicBone dynamicBone)
        {
            string gameObjName = dynamicBone.gameObject.name;
            string rootName = dynamicBone.m_Root.gameObject.name;

            string guessFromGameObjName = GuessTypeFromName(gameObjName);
            string guessFromRootName = GuessTypeFromName(rootName);

            if (guessFromGameObjName == guessFromRootName)
            {
                return guessFromGameObjName;
            } else if (guessFromGameObjName != null && guessFromRootName == "tail")
            {
                return guessFromGameObjName;
            } else
            {
                return guessFromRootName;
            }
        }

        private void SmartConvertDynamicBone(DynamicBone dynamicBone, MagicaCloth magicaCloth)
        {
            string magicaPreset = CalculateMagicaPresetToConvert(dynamicBone);
            if (magicaClothPresets.TryGetValue(magicaPreset, out string magicaPresetValue))
            {
                magicaCloth.SerializeData.ImportJson(magicaPresetValue);
                Debug.Log($"Loaded preset {magicaPreset}");
            } else
            {
                Debug.Log($"FAILED TO LOAD PRESET! {magicaPreset}");
            }
        }

        private void ConvertDynamicBone(DynamicBone dynamicBone)
        {
            if (dynamicBone.m_Root == null)
            {
                Debug.LogWarning($"{dynamicBone.gameObject.name} doesnt have a root and cannot be converted");
                return;
            }

            GameObject obj = dynamicBone.gameObject;

            MagicaCloth magicaCloth = obj.AddComponent<MagicaCloth>();
            magicaCloth.SerializeData.clothType = ClothProcess.ClothType.BoneCloth;

            magicaCloth.SerializeData.rootBones = new List<Transform>();
            magicaCloth.SerializeData.rootBones.Add(dynamicBone.m_Root);

            if (smartConvert)
            {
                SmartConvertDynamicBone(dynamicBone, magicaCloth);
            } else
            {
                //NormalConvertDynamicBone(dynamicBone, magicaCloth);
                Debug.LogError("im kinda stupid so right now you can only convert dynamicbones to magica cloth by using my shitty guessing thingie that guesses which magica preset should be used. sowwy :c  -- Sleepy");
                return;
            }



            /*List<MagicaCapsuleCollider> colliders = new List<MagicaCapsuleCollider>();
            foreach (var col in dynamicBone.m_Colliders)
            {
                if (col != null)
                {
                    MagicaCapsuleCollider magicaCollider = col.gameObject.GetComponent<MagicaCapsuleCollider>();
                    if (magicaCollider != null)
                    {
                        colliders.Add(magicaCollider);
                    }
                }
            }
            magicaCloth.SerializeData.colliderCollisionConstraint.colliderList = colliders;*/


            dynamicBone.enabled = false;

            Debug.Log($"Converted DynamicBone on {obj.name} to MagicaBoneCloth.");
        }
    }
}
