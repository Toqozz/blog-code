using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Items/Charged Jewel", fileName = "Charged Jewel.asset")]
public class ChargedJewel : Item {
    public GemType gemType;

    public override string GetItemInfo() {
        // TODO.
        return "";
    }
}