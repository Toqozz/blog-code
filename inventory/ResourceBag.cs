using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Items/ResourcePouch", fileName = "ResourcePouch.asset")]
public class ResourceBag : Item {
    public GemType gemType;

    public override string GetItemInfo() {
        // TODO.
        return "";
    }
}
