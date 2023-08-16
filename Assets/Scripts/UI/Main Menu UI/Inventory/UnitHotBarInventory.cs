using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitHotBarInventory : MonoBehaviour
{
    [SerializeField] List<UnitSO> units = new List<UnitSO>();
    List<UnitHotBarSlot> slots= new List<UnitHotBarSlot>();

    int nextFreeIndex = 0;
    [SerializeField] int maxUnits;

    [SerializeField] UnitInventory unitInventory;
    [SerializeField] LobbyAuth lobbyAuth;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < transform.GetChild(0).childCount; i++)
        { 
            // Gets the child (0) which holds all the inv slot children
            slots.Add(transform.GetChild(0).GetChild(i).GetComponent<UnitHotBarSlot>());
            slots[i].AssignSlot(i, this);
        }
    }

    public bool CheckInventory()
    {
        return units.Count < 3;
    }

    public void AddUnit(UnitSO unit) 
    {
        if (units.Contains(unit))
        {
            Debug.Log($"Unit is already achieved.");
            return;
        }

        units.Add(unit);
        slots[nextFreeIndex].EquipUnit(unit);

        lobbyAuth.UpdateUnitInventory(units);

        nextFreeIndex++;
    }

    public void RemoveUnit(UnitSO unit, int slotIndex) 
    {
        unitInventory.AddUnit(unit);

        slots[slotIndex].ThisShitSucks();

        units.Remove(unit);
        OrganizeUnitSlots();

        lobbyAuth.UpdateUnitInventory(units);

        nextFreeIndex = units.Count;
    }

    void OrganizeUnitSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].ResetSlot();

            if (i >= units.Count) continue;

            slots[i].EquipUnit(units[i]); 
        }
    }
}
