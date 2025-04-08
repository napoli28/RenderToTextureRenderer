using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal sealed class RenderToTexturePass : ScriptableRenderPass, IDisposable
{
    private RTHandle rtHandle;
    /// <summary>
    /// The override material to use.
    /// </summary>
    public Material overrideMaterial { get; set; }

    /// <summary>
    /// The pass index to use with the override material.
    /// </summary>
    public int overrideMaterialPassIndex { get; set; }

    /// <summary>
    /// The override shader to use.
    /// </summary>
    public Shader overrideShader { get; set; }

    /// <summary>
    /// The pass index to use with the override shader.
    /// </summary>
    public int overrideShaderPassIndex { get; set; }
    private FilteringSettings filteringSettings;
    private List<ShaderTagId> shaderTagsList = new List<ShaderTagId>();
    private string textureName;
    private float rtScale;
    private FilterMode rtFilterMode;
    private TextureWrapMode rtWrapMode;
    private new Color clearColor;
    private RenderTextureDescriptor descriptor;
    private readonly ProfilingSampler _profilingSampler;
    private bool debug;

    public RenderToTexturePass(string name, int layerMask, string[] shaderTags, RenderToTexture.RenderTextureSettings rtSettings, Color clearColor, bool debug)
    {
        filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
        textureName = string.IsNullOrWhiteSpace(rtSettings.textureName) ? "RenderTexture" : rtSettings.textureName;
        rtScale = rtSettings.textureSizeScale;
        rtFilterMode = rtSettings.filterMode;
        rtWrapMode = rtSettings.wrapMode;
        this.clearColor = clearColor;
        this.debug = debug;
        //rtHandle.rt.filterMode = FilterMode.Bilinear;
        descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, rtSettings.textureFormat, 0, mipCount: 4);
        _profilingSampler = new ProfilingSampler(name);
        if (shaderTags != null && shaderTags.Length > 0) {
            foreach (var tag in shaderTags) {
                shaderTagsList.Add(new ShaderTagId(tag));
            }
        }
        else {
            shaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagsList.Add(new ShaderTagId("UniversalForward"));
            shaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));
        }
    }

    // This method is called before executing the render pass.
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        descriptor.width = Math.Max(1, (int)(cameraTextureDescriptor.width * rtScale));
        descriptor.height = Math.Max(1, (int)(cameraTextureDescriptor.height * rtScale));
        RenderingUtils.ReAllocateIfNeeded(ref rtHandle, descriptor, name: textureName, filterMode: rtFilterMode, wrapMode: rtWrapMode);
        ConfigureTarget(rtHandle);
        ConfigureClear(ClearFlag.All, clearColor);
        cmd.SetGlobalTexture(textureName, rtHandle.rt);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get(_profilingSampler.name);

        var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
        var drawSettings = CreateDrawingSettings(shaderTagsList, ref renderingData, sortFlags);
        drawSettings.overrideMaterial = overrideMaterial;
        drawSettings.overrideMaterialPassIndex = 0;
        drawSettings.overrideShader = overrideShader;
        drawSettings.overrideShaderPassIndex = 0;

        using (new ProfilingScope(cmd, _profilingSampler)) {
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
            if (debug) {
                var cameraHendle = renderingData.cameraData.renderer.cameraColorTargetHandle;
                Blit(cmd, rtHandle, cameraHendle);
            }
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    /// Cleanup any allocated resources that were created during the execution of this render pass.
    public override void FrameCleanup(CommandBuffer cmd)
    {

    }

    public void Dispose()
    {
        rtHandle?.Release();
    }
}
