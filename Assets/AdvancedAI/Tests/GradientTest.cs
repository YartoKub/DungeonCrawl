using System.Collections.Generic;
using UnityEngine;


public class GradientTest : MonoBehaviour
{
    public int subdivisions;
    [SerializeField]private DebugUtilities.GradientOption option;
    public Vector3 color1; public Vector3 color2;
    //private enum GradientTestOption { RYG, Lerp, HSVGradient, Rainbow_Looped, Rainbow_Red2Violet};
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float step = (float)(1.0f / (subdivisions - 1));

        for (int i = 0; i < subdivisions; i++)
        {
            Vector2 p1 = new Vector2(0 + step * i, 0); Vector2 p2 = new Vector2(0 + step * i, 1);
            switch (option)
            {
                case DebugUtilities.GradientOption.RYG:
                    DebugUtilities.DebugDrawLine(p1, p2, DebugUtilities.RYG_Gradient(i, subdivisions));
                    break;
                case DebugUtilities.GradientOption.Lerp:
                    DebugUtilities.DebugDrawLine(p1, p2, DebugUtilities.LerpGradient(new Color(color1.x, color1.y, color1.z), new Color(color2.x, color2.y, color2.z), i, subdivisions));
                    break;
                case DebugUtilities.GradientOption.HSVGradient:
                    DebugUtilities.DebugDrawLine(p1, p2, DebugUtilities.HSVGradient(new Color(color1.x, color1.y, color1.z), new Color(color2.x, color2.y, color2.z), i, subdivisions));
                    break;
                case DebugUtilities.GradientOption.Rainbow_Looped:
                    DebugUtilities.DebugDrawLine(p1, p2, DebugUtilities.RainbowGradient_Looped(i, subdivisions));
                    break;
                case DebugUtilities.GradientOption.Rainbow_Red2Violet:
                    DebugUtilities.DebugDrawLine(p1, p2, DebugUtilities.RainbowGradient_Red2Violet(i, subdivisions)); 
                    break;
                default:
                    break;

            }
        }


    }
}
