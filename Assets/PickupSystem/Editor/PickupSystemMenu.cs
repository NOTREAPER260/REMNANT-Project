using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click wiring for the pickup system, so turning a new prop into a
/// collectable never means remembering which components it needs.
/// </summary>
/// ပစ္စည်းအသစ်တစ်ခုကို ကောက်လို့ရအောင် လုပ်ချင်တိုင်း component ဘာတွေလိုလဲ
/// မှတ်နေစရာမလိုအောင် menu တစ်ချက်နှိပ်ရုံနဲ့ ပြီးအောင် လုပ်ပေးထားတာပါ။
///
/// Unity menu bar → Tools → Pickup System
public static class PickupSystemMenu
{
    [MenuItem("Tools/Pickup System/Make Selected Pickable", false, 10)]
    private static void MakeSelectedPickable()
    {
        GameObject[] selection = Selection.gameObjects;
        int changed = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Make Pickable");

        for (int i = 0; i < selection.Length; i++)
        {
            GameObject go = selection[i];

            if (go.GetComponentInChildren<Renderer>() == null)
            {
                Debug.LogWarning("[Pickup System] '" + go.name +
                                 "' has no Renderer, so it cannot be photographed. Skipped.", go);
                continue;
            }

            // The player aims with a raycast, so something has to be hittable.
            if (go.GetComponentInChildren<Collider>() == null)
            {
                Undo.AddComponent<BoxCollider>(go);
            }

            Pickup pickup = go.GetComponent<Pickup>();
            if (pickup == null)
            {
                pickup = Undo.AddComponent<Pickup>(go);

                // Reset() is not guaranteed to run for a scripted AddComponent.
                SerializedObject so = new SerializedObject(pickup);
                so.FindProperty("itemName").stringValue = go.name.ToUpperInvariant();
                so.ApplyModifiedProperties();
            }

            changed++;
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        if (changed > 0)
        {
            Debug.Log("[Pickup System] " + changed + " object(s) are now pickable. " +
                      "Fill in the Description on each, then save the scene.");
        }
    }

    [MenuItem("Tools/Pickup System/Make Selected Pickable", true)]
    private static bool MakeSelectedPickableValidate()
    {
        return Selection.gameObjects.Length > 0;
    }

    [MenuItem("Tools/Pickup System/Add Interactor To Scene", false, 20)]
    private static void AddInteractorToScene()
    {
        PlayerInteractor existing = Object.FindFirstObjectByType<PlayerInteractor>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            Debug.Log("[Pickup System] The scene already has a PlayerInteractor.", existing.gameObject);
            return;
        }

        // Keep it next to the inventory so all the UI systems live together.
        HorrorInventory inventory = Object.FindFirstObjectByType<HorrorInventory>();
        GameObject host = inventory != null ? inventory.gameObject : null;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Add Player Interactor");

        if (host == null)
        {
            host = new GameObject("Player Interactor");
            Undo.RegisterCreatedObjectUndo(host, "Add Player Interactor");
            Debug.LogWarning("[Pickup System] No HorrorInventory found, so the interactor " +
                             "was put on a new GameObject. Assign the inventory yourself.", host);
        }

        PlayerInteractor interactor = Undo.AddComponent<PlayerInteractor>(host);

        if (inventory != null)
        {
            SerializedObject so = new SerializedObject(interactor);
            so.FindProperty("inventory").objectReferenceValue = inventory;
            so.ApplyModifiedProperties();
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        Selection.activeGameObject = host;
        EditorGUIUtility.PingObject(host);
        Debug.Log("[Pickup System] Interactor added to '" + host.name + "'. Save the scene to keep it.", host);
    }

    [MenuItem("Tools/Pickup System/Make Selected Doors", false, 30)]
    private static void MakeSelectedDoors()
    {
        GameObject[] selection = Selection.gameObjects;
        int changed = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Make Doors");

        for (int i = 0; i < selection.Length; i++)
        {
            GameObject go = selection[i];

            if (go.GetComponentInChildren<Renderer>() == null)
            {
                Debug.LogWarning("[Pickup System] '" + go.name +
                                 "' has no Renderer, so it is probably not a door leaf. Skipped.", go);
                continue;
            }

            // The aim ray has to hit the leaf itself.
            if (go.GetComponentInChildren<Collider>() == null)
            {
                Undo.AddComponent<BoxCollider>(go);
            }

            if (go.GetComponent<Door>() == null)
            {
                Door door = Undo.AddComponent<Door>(go);

                SerializedObject so = new SerializedObject(door);
                so.FindProperty("displayName").stringValue = "DOOR";
                so.ApplyModifiedProperties();
            }

            changed++;
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        if (changed > 0)
        {
            Debug.Log("[Pickup System] " + changed + " door(s) are now openable. " +
                      "Check that each door's pivot sits on its hinge edge, then save the scene.");
        }
    }

    [MenuItem("Tools/Pickup System/Make Selected Doors", true)]
    private static bool MakeSelectedDoorsValidate()
    {
        return Selection.gameObjects.Length > 0;
    }

}
