
using UnityEngine;

public class BootstrapLoader : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Nocturne Cocoa initialized!");

        SceneFlowController.Instance.LoadScene(
            "Cafe_Exploration"
        );
    }
}
