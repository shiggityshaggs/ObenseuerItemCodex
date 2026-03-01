using UnityEngine;

namespace ItemCodex.Utility
{
    internal class InputHandler : MonoBehaviour
    {
        bool IsMenu => GameUIController.instance?.IsMenuVisible ?? false;
        bool IsInventory => GameUIController.instance?.InventoryIsVisible() ?? false;
        bool IsBuildMenu => BuildingSystemMenu.instance?.menuIsOpen ?? false;
        bool IsPaused => GameController.instance?.GameIsPaused ?? false;

        void Update()
        {
            if (!GameUIController.instance || !ItemDatabase.instance) return;
            if ( (IsMenu && !IsInventory) || IsBuildMenu || IsPaused) return;

            if (Input.GetKeyDown(KeyCode.U))
            {
                if (Plugin.GUIComponent.enabled)
                    Close();
                else
                    Open();
            }

            if (InputManager.instance.GetKeyDown("Pause Menu") || InputManager.instance.GetKeyDown("Menu"))
            {
                Close();
            }
        }

        System.Collections.IEnumerator ControlsEnabled(bool enable)
        {
            yield return new WaitForEndOfFrame();
            if (enable)
                GameController.instance.ControlsEnabled(this.gameObject);
            else
                GameController.instance.ControlsDisabled(gameObject: this.gameObject, ShowCursor: true);
        }

        void Open()
        {
            if (GameController.instance != null && GameController.instance.keysDisabled)
                return;

            Plugin.GUIComponent.enabled = true;
            StartCoroutine(ControlsEnabled(false));
        }

        void Close()
        {
            Plugin.GUIComponent.enabled = false;
            StartCoroutine(ControlsEnabled(true));
        }
    }
}
