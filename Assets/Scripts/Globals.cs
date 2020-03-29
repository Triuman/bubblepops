using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Globals
{
    public static Dictionary<int, Color> NumberColorDic = new Dictionary<int, Color>()
    {
        { 2, new Color(1,0,1,.5f) },
        { 4,  new Color(1,0,1,.5f) },
        { 8,  new Color(1,0,1,.5f) },
        { 16,  new Color(1,0,1,.5f) },
        { 32,  new Color(1,0,1,.5f) },
        { 64,  new Color(1,0,1,.5f) },
        { 128,  new Color(1,0,1,.5f) },
        { 256,  new Color(1,0,1,.5f) },
        { 512,  new Color(1,0,1,.5f) },
        { 1024,  new Color(1,0,1,.5f) },
        { 2048,  new Color(1,0,1,.5f) },
    };

    public static Dictionary<int, int> NumberIndexDic = new Dictionary<int, int>()
    {
        { 2, 0 },
        { 4, 1 },
        { 8, 2 },
        { 16, 3 },
        { 32, 4 },
        { 64, 5 },
        { 128, 6 },
        { 256, 7 },
        { 512, 8 },
        { 1024, 9 },
        { 2048, 10 },
    };
}
