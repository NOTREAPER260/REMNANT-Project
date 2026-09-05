/// <summary>
/// True while a full-screen reader panel (a paper note or the book) is open,
/// so the inventory's Tab key does not fight it for the screen.
/// </summary>
/// Paper note ဒါမှမဟုတ် book panel ဖွင့်ထားစဉ် true ဖြစ်နေပါတယ် — ဒါဆို
/// inventory ရဲ့ Tab key က screen ကို လာမလုယူတော့ပါဘူး.
public static class ReaderLock
{
    public static bool IsAnyOpen;
}
