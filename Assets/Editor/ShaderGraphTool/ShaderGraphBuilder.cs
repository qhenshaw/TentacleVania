using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace TentacleVania.Editor.ShaderGraphTool
{
    public static class ShaderGraphBuilder
    {
        private const string UrpEditorAssembly = "Unity.RenderPipelines.Universal.Editor";
        private const string UrpNamespace = "UnityEditor.Rendering.Universal.ShaderGraph.";
        private const int GeometryOutputSlot = 0;

        public static string BuildFromSpec(string specPath)
        {
            var spec = JsonUtility.FromJson<ShaderGraphSpec>(File.ReadAllText(specPath));
            var graphPath = specPath.EndsWith(".json") ? specPath[..^".json".Length] : Path.ChangeExtension(specPath, ".shadergraph");
            if (!graphPath.EndsWith(".shadergraph"))
                graphPath += ".shadergraph";
            Build(graphPath, spec);
            return graphPath;
        }

        public static void Build(string graphPath, ShaderGraphSpec spec)
        {
            var hlslGuid = AssetDatabase.AssetPathToGUID(spec.hlsl);
            if (string.IsNullOrEmpty(hlslGuid))
                throw new FileNotFoundException($"HLSL file {spec.hlsl} is not an imported asset");

            GraphData graph = new() { path = "TentacleVania" };
            graph.AddContexts();
            graph.InitializeOutputs(new[] { CreateTarget(spec) }, BlockDescriptors(spec).ToArray());
            graph.AddCategory(CategoryData.DefaultCategory());

            foreach (var keyword in spec.keywords)
            {
                var shaderKeyword = new ShaderKeyword(KeywordType.Boolean)
                {
                    keywordDefinition = KeywordDefinition.MultiCompile,
                    keywordScope = KeywordScope.Global,
                    displayName = keyword.TrimStart('_'),
                    overrideReferenceName = keyword,
                    generatePropertyBlock = false,
                };
                graph.AddGraphInput(shaderKeyword);
            }

            var properties = new List<PropertyNode>();
            var y = 0f;
            foreach (var propertySpec in spec.properties)
            {
                var property = CreateProperty(propertySpec);
                graph.AddGraphInput(property);
                var node = new PropertyNode { property = property };
                Place(node, -900f, y += 60f);
                graph.AddNode(node);
                properties.Add(node);
            }

            if (!string.IsNullOrEmpty(spec.vertexFunction))
                AddVertexFunction(graph, spec, hlslGuid, properties);
            if (!string.IsNullOrEmpty(spec.fragmentFunction))
                AddFragmentFunction(graph, spec, hlslGuid, properties);

            FileUtilities.WriteShaderGraphToDisk(graphPath, graph);
            AssetDatabase.ImportAsset(graphPath, ImportAssetOptions.ForceUpdate);
        }

        private static void AddVertexFunction(GraphData graph, ShaderGraphSpec spec, string hlslGuid, List<PropertyNode> properties)
        {
            var function = CreateFunction(spec.vertexFunction, hlslGuid, -300f, -300f);
            var slot = 0;
            ConnectInput(graph, Geometry<PositionNode>(graph, CoordinateSpace.Object, -600f, -400f), GeometryOutputSlot, function, Vector3In(slot++, "PositionOS", ShaderStageCapability.Vertex));
            ConnectInput(graph, Geometry<NormalVectorNode>(graph, CoordinateSpace.Object, -600f, -340f), NormalVectorNode.kOutputSlotId, function, Vector3In(slot++, "NormalOS", ShaderStageCapability.Vertex));
            ConnectInput(graph, Add(graph, new VertexIDNode(), -600f, -280f), GeometryOutputSlot, function, new Vector1MaterialSlot(slot++, "VertexID", "VertexID", SlotType.Input, 0f, ShaderStageCapability.Vertex));
            ConnectInput(graph, Add(graph, new UVNode(), -600f, -220f), UVNode.OutputSlotId, function, new Vector4MaterialSlot(slot++, "UV0", "UV0", SlotType.Input, Vector4.zero, ShaderStageCapability.Vertex));
            slot = AddPropertyInputs(graph, function, properties, spec, slot, ShaderStageCapability.Vertex);

            var outPosition = slot++;
            var outNormal = slot++;
            function.AddSlot(new Vector3MaterialSlot(outPosition, "OutPositionOS", "OutPositionOS", SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            function.AddSlot(new Vector3MaterialSlot(outNormal, "OutNormalOS", "OutNormalOS", SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            ConnectBlock(graph, graph.vertexContext, BlockFields.VertexDescription.Position, function, outPosition);
            ConnectBlock(graph, graph.vertexContext, BlockFields.VertexDescription.Normal, function, outNormal);
        }

        private static void AddFragmentFunction(GraphData graph, ShaderGraphSpec spec, string hlslGuid, List<PropertyNode> properties)
        {
            var function = CreateFunction(spec.fragmentFunction, hlslGuid, -300f, 200f);
            var slot = 0;
            ConnectInput(graph, Add(graph, new UVNode(), -600f, 100f), UVNode.OutputSlotId, function, new Vector4MaterialSlot(slot++, "UV0", "UV0", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
            if (!spec.IsFullscreen)
            {
                ConnectInput(graph, Geometry<PositionNode>(graph, CoordinateSpace.World, -600f, 160f), GeometryOutputSlot, function, Vector3In(slot++, "PositionWS", ShaderStageCapability.Fragment));
                ConnectInput(graph, Geometry<NormalVectorNode>(graph, CoordinateSpace.World, -600f, 220f), NormalVectorNode.kOutputSlotId, function, Vector3In(slot++, "NormalWS", ShaderStageCapability.Fragment));
            }
            slot = AddPropertyInputs(graph, function, properties, spec, slot, ShaderStageCapability.Fragment);

            var outColor = slot++;
            function.AddSlot(new Vector3MaterialSlot(outColor, "OutBaseColor", "OutBaseColor", SlotType.Output, Vector3.zero, ShaderStageCapability.Fragment));
            ConnectBlock(graph, graph.fragmentContext, BlockFields.SurfaceDescription.BaseColor, function, outColor);

            if (spec.transparent || spec.IsFullscreen)
            {
                var outAlpha = slot++;
                function.AddSlot(new Vector1MaterialSlot(outAlpha, "OutAlpha", "OutAlpha", SlotType.Output, 1f, ShaderStageCapability.Fragment));
                ConnectBlock(graph, graph.fragmentContext, BlockFields.SurfaceDescription.Alpha, function, outAlpha);
            }

            if (!spec.IsLit)
                return;
            var outEmission = slot++;
            var outSmoothness = slot++;
            function.AddSlot(new Vector3MaterialSlot(outEmission, "OutEmission", "OutEmission", SlotType.Output, Vector3.zero, ShaderStageCapability.Fragment));
            function.AddSlot(new Vector1MaterialSlot(outSmoothness, "OutSmoothness", "OutSmoothness", SlotType.Output, 0f, ShaderStageCapability.Fragment));
            ConnectBlock(graph, graph.fragmentContext, BlockFields.SurfaceDescription.Emission, function, outEmission);
            ConnectBlock(graph, graph.fragmentContext, BlockFields.SurfaceDescription.Smoothness, function, outSmoothness);
        }

        private static int AddPropertyInputs(GraphData graph, CustomFunctionNode function, List<PropertyNode> properties, ShaderGraphSpec spec, int slot, ShaderStageCapability stage)
        {
            for (var i = 0; i < properties.Count; i++)
            {
                var name = spec.properties[i].name.TrimStart('_');
                MaterialSlot input = spec.properties[i].type switch
                {
                    "Texture2D" or "NormalMap" => new Texture2DMaterialSlot(slot, name, name, SlotType.Input, stage),
                    "Color" or "Vector" => new Vector4MaterialSlot(slot, name, name, SlotType.Input, Vector4.zero, stage),
                    _ => new Vector1MaterialSlot(slot, name, name, SlotType.Input, 0f, stage),
                };
                ConnectInput(graph, properties[i], PropertyNode.OutputSlotId, function, input);
                slot++;
            }
            return slot;
        }

        private static CustomFunctionNode CreateFunction(string functionName, string hlslGuid, float x, float y)
        {
            var function = new CustomFunctionNode
            {
                sourceType = HlslSourceType.File,
                functionName = functionName,
                functionSource = hlslGuid,
            };
            Place(function, x, y);
            return function;
        }

        private static void ConnectInput(GraphData graph, AbstractMaterialNode source, int sourceSlot, CustomFunctionNode function, MaterialSlot input)
        {
            function.AddSlot(input);
            if (graph.GetNodeFromId(function.objectId) == null)
                graph.AddNode(function);
            graph.Connect(new SlotReference(source, sourceSlot), new SlotReference(function, input.id));
        }

        private static void ConnectBlock(GraphData graph, ContextData context, BlockFieldDescriptor descriptor, CustomFunctionNode function, int outputSlot)
        {
            var block = context.blocks.Select(b => b.value).FirstOrDefault(b => b.descriptor == descriptor)
                ?? throw new InvalidOperationException($"The target has no {descriptor.name} block");
            graph.Connect(new SlotReference(function, outputSlot), new SlotReference(block, 0));
        }

        private static T Geometry<T>(GraphData graph, CoordinateSpace space, float x, float y) where T : GeometryNode, new()
        {
            var node = new T();
            typeof(GeometryNode).GetField("m_Space", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(node, space);
            return Add(graph, node, x, y);
        }

        private static T Add<T>(GraphData graph, T node, float x, float y) where T : AbstractMaterialNode
        {
            Place(node, x, y);
            graph.AddNode(node);
            return node;
        }

        private static void Place(AbstractMaterialNode node, float x, float y) =>
            node.drawState = new DrawState { position = new Rect(x, y, 0f, 0f), expanded = true };

        private static Vector3MaterialSlot Vector3In(int id, string name, ShaderStageCapability stage) =>
            new(id, name, name, SlotType.Input, Vector3.zero, stage);

        private static AbstractShaderProperty CreateProperty(PropertySpec spec)
        {
            AbstractShaderProperty property = spec.type switch
            {
                "Texture2D" => new Texture2DShaderProperty { defaultType = Texture2DShaderProperty.DefaultType.White },
                "NormalMap" => new Texture2DShaderProperty { defaultType = Texture2DShaderProperty.DefaultType.NormalMap },
                "Color" => new ColorShaderProperty { value = spec.color },
                "Vector" => new Vector4ShaderProperty { value = spec.vector },
                _ => new Vector1ShaderProperty { value = spec.value },
            };
            property.displayName = spec.name.TrimStart('_');
            property.overrideReferenceName = spec.name;
            return property;
        }

        private static IEnumerable<BlockFieldDescriptor> BlockDescriptors(ShaderGraphSpec spec)
        {
            if (spec.IsFullscreen)
            {
                yield return BlockFields.SurfaceDescription.BaseColor;
                yield return BlockFields.SurfaceDescription.Alpha;
                yield break;
            }
            yield return BlockFields.VertexDescription.Position;
            yield return BlockFields.VertexDescription.Normal;
            yield return BlockFields.VertexDescription.Tangent;
            yield return BlockFields.SurfaceDescription.BaseColor;
            if (spec.transparent)
                yield return BlockFields.SurfaceDescription.Alpha;
            if (!spec.IsLit)
                yield break;
            yield return BlockFields.SurfaceDescription.NormalTS;
            yield return BlockFields.SurfaceDescription.Metallic;
            yield return BlockFields.SurfaceDescription.Smoothness;
            yield return BlockFields.SurfaceDescription.Emission;
            yield return BlockFields.SurfaceDescription.Occlusion;
        }

        private static Target CreateTarget(ShaderGraphSpec spec)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == UrpEditorAssembly);
            var targetType = assembly.GetType(UrpNamespace + "UniversalTarget", true);
            var subTargetType = assembly.GetType(UrpNamespace + (spec.IsLit ? "UniversalLitSubTarget" : spec.IsFullscreen ? "UniversalFullscreenSubTarget" : "UniversalUnlitSubTarget"), true);
            var target = (Target)Activator.CreateInstance(targetType, true);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (!(bool)targetType.GetMethod("TrySetActiveSubTarget", flags).Invoke(target, new object[] { subTargetType }))
                throw new InvalidOperationException($"Couldn't set the {subTargetType.Name} sub-target");
            targetType.GetProperty("castShadows", flags).SetValue(target, spec.castShadows);
            var renderFace = targetType.GetProperty("renderFace", flags);
            renderFace.SetValue(target, Enum.Parse(renderFace.PropertyType, spec.renderFace, true));
            if (spec.transparent)
            {
                var surface = targetType.GetProperty("surfaceType", flags);
                surface.SetValue(target, Enum.Parse(surface.PropertyType, "Transparent"));
                var zWrite = targetType.GetProperty("zWriteControl", flags);
                zWrite.SetValue(target, Enum.Parse(zWrite.PropertyType, "ForceDisabled"));
            }
            if (!string.IsNullOrEmpty(spec.depthTest))
            {
                var zTest = targetType.GetProperty("zTestMode", flags);
                zTest.SetValue(target, Enum.Parse(zTest.PropertyType, spec.depthTest, true));
            }
            return target;
        }
    }
}
