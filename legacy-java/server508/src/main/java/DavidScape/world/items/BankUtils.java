package DavidScape.world.items;

import DavidScape.Engine;

/**
 * @author Gravediggah
 */
public class BankUtils {
    public BankUtils() {

    }

    public boolean isNote(int itemId) {
        String description = Engine.items.getItemDescription(itemId);
        return description.toLowerCase().startsWith("swap");
    }

    public boolean canBeNoted(int itemId) {
        return (findNote(itemId) > -1);
    }

    public int findNote(int itemId) {
        // SECURITY FIX: Bounds checking for item array access
        if (Engine.items == null || Engine.items.itemLists == null) {
            return -1;
        }
        
        for (int idx = 0; idx < Engine.items.itemLists.length; idx++) {
            ItemList i = Engine.items.itemLists[idx];
            if (i != null && i.itemDescription != null && i.itemName != null) {
                if (i.itemDescription.toLowerCase().startsWith("swap") &&
                        i.itemName.equals(Engine.items.getItemName(itemId))) {
                    return i.itemId;
                }
            }
        }
        return -1;
    }

    public int findUnNote(int itemId) {
        // SECURITY FIX: Bounds checking for item array access
        if (Engine.items == null || Engine.items.itemLists == null) {
            return -1;
        }
        
        for (int idx = 0; idx < Engine.items.itemLists.length; idx++) {
            ItemList i = Engine.items.itemLists[idx];
            if (i != null && i.itemDescription != null && i.itemName != null) {
                if (!i.itemDescription.toLowerCase().startsWith("swap") &&
                        i.itemName.equals(Engine.items.getItemName(itemId))) {
                    return i.itemId;
                }
            }
        }
        return -1;
    }
}