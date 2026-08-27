using UnityEngine;

/// <summary>
/// Anything the player can aim at and press the interact key on.
/// `Pickup` and `Door` both implement it, so one key handles both.
/// </summary>
/// E နှိပ်လို့ရတဲ့ အရာအားလုံးရဲ့ စံပါ။ Pickup ရော Door ရော ဒါကို implement လုပ်ထားလို့
/// PlayerInteractor က တစ်ခုတည်းနဲ့ နှစ်မျိုးလုံးကို ကိုင်တွယ်နိုင်ပါတယ်။
/// အသစ်တစ်မျိုး (ဥပမာ ခလုတ်၊ အံဆွဲ) ထပ်လုပ်ချင်ရင်လည်း ဒါကို implement လုပ်လိုက်ရုံပါပဲ။
public interface IInteractable
{
    /// <summary>Shown next to the key hint, e.g. "OPEN   DOOR". English only.</summary>
    string Prompt { get; }

    /// <summary>False hides the prompt entirely, as if nothing were there.</summary>
    bool CanInteract { get; }

    /// <summary>
    /// Do the thing. Return a short message to flash on screen, or null for none.
    /// </summary>
    /// <param name="interactor">The player object that pressed the key.</param>
    string Interact(GameObject interactor);
}
