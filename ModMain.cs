using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;
using KrunchyStickmanMod.UIComponents;
using KrunchyStickmanMod.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KrunchyStickmanMod {

    [BepInPlugin("krunchy.KStickManMod", "KrunchyStickManMod", "1.0")]
    public class ModMain : BaseUnityPlugin {

        // Reference to mod logger
        public ManualLogSource Log => base.Logger;

        // Reference to mods config
        public ConfigFile ModConfig => base.Config;

        // Previous Cursor States
        private bool prevCursorVis;

        private CursorLockMode prevLockState;

        // UI Objects
        public GameObject ModUI;

        public List<Transform> ModMenuTransforms { get; private set; }
        public Slider SpeedSlider => GetSpeedSlider();

        // Text Style Sheet
        public TMP_StyleSheet StyleSheet { get; private set; }

        // Menu Drag Handler
        private GUIDragHandler _menuDragHandler;

        // Config Entries
        public ConfigEntry<bool> HideGUIOnStart;

        public ConfigEntry<bool> IsMenuDraggable;
        public ConfigEntry<bool> ShouldHideGUI;
        public ConfigEntry<bool> HotkeysEnabled;
        public ConfigEntry<KeyCode> ToggleMenuHotkey;
        public ConfigEntry<float> GameSpeed;

        // Singleton Instance
        private static ModMain s_instance;

        // Version String
        private string _versionString;

        private string _modName;

        private BattleManager _battleManager;

        private float timer = 0f;
        private readonly float timerDuration = 0.1f;

        // Asset Bundle Path
        private readonly string _bundlePath = Path.Combine(Path.GetDirectoryName(typeof(ModMain).Assembly.Location), "kmodbundle");

        private void Awake() {
            if (s_instance != null && s_instance != this) {
                Destroy(gameObject);
                return;
            }

            // Initialize singleton
            s_instance = this;

            // Set version string
            _versionString = "KMod Version: " + base.Info.Metadata.Version.ToString();
            _modName = base.Info.Metadata.Name;

            // Persist through scenes
            DontDestroyOnLoad(gameObject);

            // Apply Harmony Patches
            Harmony harmony = new("krunchy.KTaintedMod");
            harmony.PatchAll();

            try {
                // Setup Config
                InitConfig();

                // Initialize Mod UI
                InitModMenu(_bundlePath);

                // Setup Mod Panel/Must run after InitModMenu!!!
                SetupModPanel();

                // Setup buttons
                SetupButtons();

                // Setup toggles
                SetupToggleButtons();

                // Setup sliders
                SetupSliders();

                // Create draggable event
                IsMenuDraggable.SettingChanged += (sender, args) => {
                    SetMenuIsDraggable(IsMenuDraggable.Value);
                };
            } catch (FileNotFoundException ex) {
                Log.LogError($"File not found: {ex.Message}");
                Log.LogError(ex.StackTrace);
            } catch (NullReferenceException ex) {
                Log.LogError($"Null reference encountered: {ex.Message}");
                Log.LogError(ex.StackTrace);
            } catch (Exception ex) {
                Log.LogError($"Unexpected error: {ex.Message}");
                Log.LogError(ex.StackTrace);
            } finally {
                if (HideGUIOnStart.Value) {
                    ModUI.SetActive(false);
                    Log.LogInfo("KStickManMod has loaded and patches applied!");
                    _battleManager = Singleton<BattleManager>.Instance;
                }
            }
        }

        private void Update() {
            timer += Time.deltaTime;

            if (HotkeysEnabled.Value)
                HandleInput();

            //if (timer >= timerDuration) {
            //    timer = 0f;
            //    if (_battleManager != null) {
            //        if (_battleManager.player != null && _battleManager.player.owner is BattleHuman human) {
            //            human.comboHitDelay = 0.1f;
            //            human.blockTime = 0.08f;
            //            human.GetUpDelay = 0.2f;
            //            human.comboLate = 0.1f;
            //        }
            //    } else {
            //        _battleManager = Singleton<BattleManager>.Instance;
            //    }
            //}
        }

        public void OnDestroy() {
            GameSpeed.Value = 1f;
        }

        // UI Getters
        public GameObject GetMenuObjectByName(string name) {
            return ModMenuTransforms.Find(t => t.name == name).gameObject;
        }

        public Slider GetSpeedSlider() {
            return ModMenuTransforms.Find(t => t.name == "SpeedSlider").GetComponent<Slider>();
        }

        private void InitConfig() {
            // Config Entries
            GameSpeed = ModConfig.Bind("General", "GameSpeed", 1.0f, "Game speed");
            IsMenuDraggable = ModConfig.Bind("UI", "IsMenuDraggable", true, "Allow the menu to be moved?");
            HideGUIOnStart = ModConfig.Bind("UI", "HideGUIOnStart", true, "Should mod menu be shown at game start?");
            ShouldHideGUI = ModConfig.Bind("UI", "ShouldHideGUI", true, "Should mod menu be hidden?");
            HotkeysEnabled = ModConfig.Bind("Hotkeys", "HotkeysEnabled", true, "Are hotkeys enabled?");

            // Hotkey Config Entries
            ToggleMenuHotkey = ModConfig.Bind("Hotkeys", "ToggleMenu", KeyCode.CapsLock, "Hotkey to toggle the mod menu.");
        }

        private void HandleInput() {
            if (Input.GetKeyDown(ToggleMenuHotkey.Value)) {
                ToggleModMenu();
            }
        }

        public void SetMenuIsDraggable(bool isDraggable = true) {
            _menuDragHandler.IsDraggable = isDraggable;
        }

        public void InitModMenu(string path) {
            AssetBundle bundle = AssetBundle.LoadFromFile(path);

            if (bundle == null) {
                Log.LogError("Failed to load AssetBundle!");
                return;
            }

            // Load the UI Prefab
            GameObject prefab = bundle.LoadAsset<GameObject>("KModCanvas");

            // Load the Style Sheet
            StyleSheet = bundle.LoadAsset<TMP_StyleSheet>("Default Style Sheet");

            // Instantiate the UI
            GameObject modUI = Instantiate(prefab, transform, false);

            ModUI = modUI;

            // Get all transforms in the mod menu
            ModMenuTransforms = KBepInExUtils.GetAllObjects(modUI.transform);

            // Fix Text Styles
            KBepInExUtils.FixText(StyleSheet, modUI);

            // Unload the bundle
            bundle.Unload(false);
        }

        private void SetupModPanel() {
            GameObject modPanel = GetMenuObjectByName("KModPanel");

            // Set Version Text
            modPanel.transform.Find("VersionText").GetComponent<TMP_Text>().text = _versionString;

            // Set Mod Title Text
            modPanel.transform.Find("ModTitle").GetComponent<TMP_Text>().text = _modName;

            // Setup Footer Button
            modPanel.transform.Find("CloseBtn").GetComponent<Button>().onClick.AddListener(ToggleModMenu);

            // Add Drag Handler
            _menuDragHandler = GetMenuObjectByName("KModPanel").AddComponent<GUIDragHandler>();

            // Set initial draggable state
            SetMenuIsDraggable(IsMenuDraggable.Value);
        }

        private void SetupButtons() {
            Button[] buttons = ModUI.GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons) {
                if (button.name == "ApplySpeedBtn") {
                    button.GetComponent<Button>().onClick.AddListener(() => Time.timeScale = SpeedSlider.value);
                } else if (button.name == "ResetSpeedBtn")
                    button.GetComponent<Button>().onClick.AddListener(() => {
                        // Reset the game speed to 1x and update the slider and text
                        Time.timeScale = 1f;
                        SpeedSlider.GetComponentInChildren<TMP_Text>().text = $"Speed: {Time.timeScale:0.00}x";
                        SpeedSlider.value = 1f;
                    });
            }
        }

        private void SetupToggleButtons() {
            Toggle[] toggles = ModUI.GetComponentsInChildren<Toggle>(true);

            foreach (Toggle t in toggles) {
                ConfigEntry<bool> configVal;

                if (t.name == "MenuDragToggle") {
                    configVal = IsMenuDraggable;
                } else if (t.name == "HotkeyToggle") {
                    configVal = HotkeysEnabled;
                } else if (t.name == "OnStartToggle") {
                    configVal = HideGUIOnStart;
                } else {
                    configVal = null;
                    Log.LogError("Toggle has no config value!!");
                }

                // Initialize the toggle's state based on the configuration value
                t.isOn = configVal.Value;

                // If the toggle is for hiding the GUI on start, hide the mod UI if enabled
                if (HideGUIOnStart.Value) {
                    ModUI.SetActive(false);
                }

                // Update the configuration value when the toggle's state changes
                t.onValueChanged.AddListener(val => configVal.Value = val);
            }
        }

        private void SetupSliders() {
            SpeedSlider.value = Time.timeScale;
            TMP_Text text = SpeedSlider.GetComponentInChildren<TMP_Text>();
            text.text = $"Speed: {SpeedSlider.value:0.00}x";

            SpeedSlider.onValueChanged.AddListener(val => text.text = $"Speed: {val:0.00}x");
        }

        private void ToggleModMenu() {
            ShouldHideGUI.Value = !ShouldHideGUI.Value;

            if (ShouldHideGUI.Value) {
                RestoreCursorState();
                ModUI.SetActive(false);
            } else {
                SaveCursorState();
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                ModUI.SetActive(true);
            }
        }

        private void SaveCursorState() {
            prevCursorVis = Cursor.visible;
            prevLockState = Cursor.lockState;
        }

        private void RestoreCursorState() {
            Cursor.visible = prevCursorVis;
            Cursor.lockState = prevLockState;
        }

        public static ModMain GetInstance() {
            if (s_instance == null) {
                s_instance = new ModMain();
            }
            return s_instance;
        }
    }
}