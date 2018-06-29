using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Items/Jewel", fileName = "Jewel.asset")]
public class Jewel : Item {
    public GemType gemType;

    public override string GetItemInfo() {
        // TODO.
        return "";
    }
}