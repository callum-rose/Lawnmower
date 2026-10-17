using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace Game.Tiles
{
	// Copies the PlanarWaterReflection camera's depth into a texture the Water shader can sample. URP otherwise renders
	// that camera straight into its target, whose depth can't be read in the graph and is discarded afterwards, so the
	// pass asks for an intermediate texture (one extra half-resolution blit).
	public class PlanarReflectionDepthFeature : ScriptableRendererFeature
	{
		private CopyPass _pass;

		public override void Create()
		{
			_pass = new CopyPass();
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (PlanarWaterReflection.TryGetDepthTarget(renderingData.cameraData.camera, out RTHandle target))
			{
				_pass.Target = target;
				renderer.EnqueuePass(_pass);
			}
		}

		protected override void Dispose(bool disposing)
		{
			_pass?.Dispose();
		}

		private class CopyPass : ScriptableRenderPass
		{
			private readonly CopyDepthPass _copy;

			public RTHandle Target { get; set; }

			public CopyPass()
			{
				renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
				requiresIntermediateTexture = true;
				Shader shader = GraphicsSettings.GetRenderPipelineSettings<UniversalRendererResources>().copyDepthPS;
				_copy = new CopyDepthPass(renderPassEvent, shader, customPassName: "Planar Reflection Depth");
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
				TextureHandle destination = renderGraph.ImportTexture(Target);
				_copy.Render(renderGraph, frameData, destination, resourceData.activeDepthTexture, false, "Planar Reflection Depth");
			}

			public void Dispose()
			{
				_copy.Dispose();
			}
		}
	}
}
