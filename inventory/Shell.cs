using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Items/Shell", fileName = "Shell.asset")]
public class Shell : Item {
    public RuneType runeType;

    public override string GetItemInfo() {
        // TODO.
        return "";
    }
}