using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Globals
{
    public static Dictionary<int, Color> NumberColorDic = new Dictionary<int, Color>()
    {
        { 2, new Color(0.9215686f, 0.3960784f, 0.5568628f,.5f) },
        { 4,  new Color(0.8901961f, 0.3490196f, 0.2196078f,.5f) },
        { 8,  new Color(1,0.7294118f,0.1882353f,.5f) },
        { 16,  new Color(0.9058824f,0.509804f,0.2196078f,.5f) },
        { 32,  new Color(0.4588235f,0.7960784f,0.3176471f,.5f) },
        { 64,  new Color(0.3019608f,0.827451f,0.6509804f,.5f) },
        { 128,  new Color(0.4117647f,0.7450981f,0.9215686f,.5f) },
        { 256,  new Color(0.427451f,0.572549f,0.8588235f,.5f) },
        { 512,  new Color(0.8156863f,0.6196079f,0.8117647f,.5f) },
        { 1024,  new Color(0.6868359f,0.3326806f,0.7924528f,.5f) },
        { 2048,  new Color(0.7830189f,0.5462034f,0.365655f,.5f) },
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
