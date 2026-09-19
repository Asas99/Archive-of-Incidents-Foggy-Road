using com.IvanMurzak.McpPlugin.Common;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FlashlightOnOff : MonoBehaviour
{
    public GameObject spotLight;
    private bool Isactive;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Isactive = !Isactive;
            spotLight.SetActive(Isactive);
        }
    }
}
