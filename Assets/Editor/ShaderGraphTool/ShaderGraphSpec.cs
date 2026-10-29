using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Editor.ShaderGraphTool
{
    [Serializable]
    public sealed class ShaderGraphSpec
    {
        public string target = "Unlit";
        public string hlsl;
        public string vertexFunction;
        public string fragmentFunction;
        public bool castShadows = true;
        public string renderFace = "Front";
        public bool transparent;
        public string depthTest;
        public List<string> keywords = new();
        public List<PropertySpec> properties = new();

        public bool IsLit => string.Equals(target, "Lit", StringComparison.OrdinalIgnoreCase);
        public bool IsFullscreen => string.Equals(target, "Fullscreen", StringComparison.OrdinalIgnoreCase);
    }

    [Serializable]
    public sealed class PropertySpec
    {
        public string name;
        public string type = "Float";
        public float value;
        public Color color = Color.white;
        public Vector4 vector;
    }
}
