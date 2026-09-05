using StarterAssets;
using UnityEngine;

/// <summary>
/// Disables player look/move and unlocks the cursor for a full-screen UI, then
/// restores both. Shared by every reader panel so the freeze logic lives in
/// one place instead of being copied into each one.
/// </summary>
/// Full-screen UI တစ်ခု ဖွင့်ထားစဉ် ကင်မရာလှည့်တာ/လမ်းလျှောက်တာကို ရပ်ပြီး mouse ကို
/// ပြန်ပေးတာပါ. Reader panel အားလုံးက ဒီတစ်ခုတည်းကို ခေါ်သုံးလို့ logic ထပ်ရေးစရာ မလိုပါ.
public static class PlayerFreeze
{
    public static void Apply(bool restore)
    {
        FirstPersonController controller = Object.FindFirstObjectByType<FirstPersonController>();
        if (controller != null)
        {
            controller.enabled = restore;
        }

        StarterAssetsInputs inputs = controller != null
            ? controller.GetComponent<StarterAssetsInputs>()
            : null;

        if (inputs != null)
        {
            inputs.cursorInputForLook = restore;
            inputs.cursorLocked = restore;

            if (!restore)
            {
                inputs.move = Vector2.zero;
                inputs.look = Vector2.zero;
                inputs.sprint = false;
                inputs.jump = false;
            }
        }

        Cursor.lockState = restore ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !restore;
    }
}
