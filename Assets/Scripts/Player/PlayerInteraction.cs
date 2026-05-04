using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public Key interactKey = Key.E;

    [Header("UI")]
    public Text hintText;

    private Camera _camera;

    void Start()
    {
        _camera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        CheckForInteractable();
        TryInteract();
    }

    void CheckForInteractable()
    {
        if (hintText == null) return;

        FarmPlot plot = GetLookedAtPlot();

        if (plot != null)
        {
            hintText.text = plot.currentState switch
            {
                PlotState.Empty => "[E] Plant",
                PlotState.Growing => "[E] Growing...",
                PlotState.ReadyToHarvest => "[E] Bring in the harvest!",
                _ => ""
            };
        }
        else
        {
            hintText.text = "";
        }
    }

    void TryInteract()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[interactKey].wasPressedThisFrame)
        {
            FarmPlot plot = GetLookedAtPlot();
            plot?.Interact();
        }
    }

    FarmPlot GetLookedAtPlot()
    {
        if (_camera == null) return null;

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            return hit.collider.GetComponent<FarmPlot>();

        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (_camera == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(_camera.transform.position,
                       _camera.transform.forward * interactDistance);
    }
}