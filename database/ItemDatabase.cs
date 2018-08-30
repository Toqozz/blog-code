[CreateAssetMenu(menuName = "Items/Database", fileName = "ItemDatabase.asset")]
public class ItemDatabase : ScriptableObject {
    // Item objects.
    public Item Ruby;
    public Item Sapphire;
    public Item Emerald;
    public Item Amethyst;

    public Item GetActual(string name) {
        if (string.IsNullOrEmpty(name)) {
            //Debug.Log("GetActual(): name is null or empty.  You're either checking an empty slot or using this function incorrectly.");
            return null;
        }

        switch (name.ToLower()) {
            case "ruby": return Ruby;
            case "sapphire": return Sapphire;
            case "emerald": return Emerald;
            case "amethyst": return Amethyst;

            default: 
                Debug.Log("Could not find an Item for key \"" + name + "\", is it typed correctly?");
                return null;
        }
    }
}
