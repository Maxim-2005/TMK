using UnityEngine;
// Если вы используете ItemPickup, вы можете также использовать Game.Items, 
// но для простоты интерфейс оставим в Game.Interfaces
// using Game.Items; 

namespace Game.Interfaces
{
    public interface IItemTaker
    {
        bool TryGiveItem(GameObject item);
    }
}