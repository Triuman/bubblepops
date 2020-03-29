using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Globals
{
    public static Dictionary<int, Color> NumberColorDic = new Dictionary<int, Color>()
    {
        { 2, new Color() },
        { 4,  new Color() },
        { 8,  new Color() },
        { 16,  new Color() },
        { 32,  new Color() },
        { 64,  new Color() },
        { 128,  new Color() },
        { 256,  new Color() },
        { 512,  new Color() },
        { 1024,  new Color() },
        { 2048,  new Color() },
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
