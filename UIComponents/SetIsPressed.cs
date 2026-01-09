using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KrunchyStickmanMod.UIComponents {

    public class SetIsPressed : MonoBehaviour {

        // Material to apply when the button or toggle is pressed
        public Material PressedMaterial;

        // Delay in seconds before resetting the material to its original state
        public float ResetDelay = 0.3f;

        // The original material to reset to after the delay
        private Material targetMaterial;

        // References to the Button and Toggle components (if applicable)
        private Button button;

        private Toggle toggle;

        // Timer to track the countdown for resetting the material
        private float timer = 0.0f;

        // Flag to indicate if this component is attached to a Toggle
        private bool isToggle = false;

        // List of Image components whose materials will be modified
        public List<Image> Images = [];

        // Dictionary to store the original materials of the Images, keyed by their names
        public Dictionary<string, Material> Materials = [];

        // Unity's Start method, called when the script is first initialized
        private void Start() {
            // No initialization logic here, but this method is available for future use
        }

        // Unity's Update method, called once per frame
        private void Update() {
            // Skip the timer logic if this is a Toggle
            if (isToggle) return;

            // Decrease the timer and reset the material when the timer reaches zero
            if (timer > 0) {
                timer -= Time.deltaTime;
                if (timer <= 0) {
                    ResetMaterial();
                }
            }
        }

        // Sets the target material to reset to after the delay
        public void SetTargetMaterial(Material mat) {
            targetMaterial = mat;
        }

        // Configures the component to work with a Button
        public void SetButton(Button btn) {
            // Set the target material to the Button's current material
            SetTargetMaterial(btn.image.material);

            // Store the Button reference and add a listener for its click event
            button = btn;
            button?.onClick.AddListener(ToggleIsPressed);
        }

        // Handles the logic for when the Button is pressed
        public void ToggleIsPressed() {
            // Apply the pressed material to all Images
            foreach (Image item in Images) {
                if (item.material != null) {
                    item.material = PressedMaterial;
                }
            }

            // Start the reset timer if this is not a Toggle
            if (!isToggle) {
                timer = ResetDelay;
            }
        }

        // Configures the component to work with a Toggle
        public void SetToggle(Toggle tog) {
            isToggle = true; // Mark this as a Toggle
            toggle = tog;

            // Add a listener for the Toggle's value change event
            toggle?.onValueChanged.AddListener(ToggleMaterial);
        }

        // Handles the logic for when the Toggle's state changes
        private void ToggleMaterial(bool isOn) {
            foreach (Image item in Images) {
                if (item.material != null) {
                    // Apply the pressed material if the Toggle is on, otherwise reset to the original material
                    if (Materials.TryGetValue(item.name, out Material mat)) {
                        item.material = isOn ? PressedMaterial : mat;
                    } else {
                        // Log a message if the material is not found in the dictionary
                        ModMain.GetInstance().Log.LogMessage($"ToggleMaterials TryGetValue failed!! No message found in dictionary!");
                    }
                }
            }
        }

        // Resets the material of all Images to the target material
        private void ResetMaterial() {
            foreach (Image item in Images) {
                if (item.material != null)
                    item.material = targetMaterial;
            }
        }
    }
}