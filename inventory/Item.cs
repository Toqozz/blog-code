using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A "template" for items.
[System.Serializable]
public abstract class Item : ScriptableObject {
    public enum ItemType {
        Ore, Brick, Shell, Gem, Jewel, ChargedJewel, Shonky, ResourcePouch
    }

    public enum RuneType {
        Rune1, Rune2, Rune3
    }

    public enum GemType {
        Ruby, Diamond, Sapphire, Emerald
    }

    public string itemName;
    public GameObject physicalRepresentation;
    public int stackLimit;
    public bool mergeable;
}

// A class that holds a real instance of a ScriptableObject item.
// Allows us to have copies with mutable data.
[System.Serializable]
public class ItemInstance {
    public Item item;
    public int quantity = 1;
    public Quality.QualityGrade quality;
    public bool isNew;

    public ItemInstance(Item item, int quantity, Quality.QualityGrade quality, bool isNew) {
        this.item = item;
        this.quantity = quantity;
        this.quality = quality;
        this.isNew = isNew;
    }

    public void AddQuantity(int amount) {
        quantity = Mathf.Min(item.stackLimit, quantity + amount);
    }

    public string GetItemName() {
        return item.itemName;
    }

    public string GetItemInfo() {
        string grade = Quality.GradeToString(quality);
        string gradeCol = "#" + ColorUtility.ToHtmlStringRGB(Quality.GradeToColor(quality));
        string str = string.Format("Quality: <color={0}>{1}</color>\n" +
                                   "Quantity: <color=white>{2}</color>\n" +
                                   (isNew ? "<color=#ffc605fc>NEW</color>\n" : ""), gradeCol, grade, quantity);
        return str;
    }
}
