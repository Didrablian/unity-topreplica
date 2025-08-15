using UnityEngine;

public interface ISlot
{
    Item GetItem();
    InventoryUIHandler GetInventoryUIHandler(); // If slot is part of inventory UI
    EquipmentUIHandler GetEquipmentUIHandler(); // If slot is part of equipment UI
    GameObject GetGameObject(); // To get the transform
    // Add other common methods here as needed by InventoryItemDrag
}
