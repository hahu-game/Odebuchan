using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;  // Å© êV Input System

public class UIRaycastDebugger : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            PointerEventData ped = new PointerEventData(EventSystem.current);
            ped.position = Mouse.current.position.ReadValue();

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            Debug.Log("---- UI Raycast Results ----");
            foreach (var r in results)
            {
                Debug.Log($"Hit: {r.gameObject.name}, SortingLayer: {r.sortingLayer}, Order: {r.sortingOrder}");
            }
        }
    }
}
