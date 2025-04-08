using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Experimental.Rendering.Universal.RenderObjects;

public class RenderToTexture : ScriptableRendererFeature
{
    [System.Serializable]
    public class RenderToTextureSetting
    {
        /// <summary>
        /// The profiler tag used with the pass.
        /// </summary>
        public string passTag = "RenderObjectsFeature";

        /// <summary>
        /// Controls when the render pass executes.
        /// </summary>
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;
        public Color clearColor = Color.black;
        public bool debug = false;

        /// <summary>
        /// The filter settings for the pass.
        /// </summary>
        public FilterSettings filterSettings = new FilterSettings();

        /// <summary>
        /// The override material to use.
        /// </summary>
        public Material overrideMaterial = null;

        /// <summary>
        /// The pass index to use with the override material.
        /// </summary>
        public int overrideMaterialPassIndex = 0;

        /// <summary>
        /// The override shader to use.
        /// </summary>
        public Shader overrideShader = null;

        /// <summary>
        /// The pass index to use with the override shader.
        /// </summary>
        public int overrideShaderPassIndex = 0;

        /// <summary>
        /// Options to select which type of override mode should be used.
        /// </summary>
        public enum OverrideMaterialMode
        {
            /// <summary>
            /// Use this to not override.
            /// </summary>
            None,

            /// <summary>
            /// Use this to use an override material.
            /// </summary>
            Material,

            /// <summary>
            /// Use this to use an override shader.
            /// </summary>
            Shader
        };

        /// <summary>
        /// The selected override mode.
        /// </summary>
        public OverrideMaterialMode overrideMode = OverrideMaterialMode.Material; //default to Material as this was previously the only option

        /// <summary>
        /// Sets whether it should override depth or not.
        /// </summary>
        public bool overrideDepthState = false;

        /// <summary>
        /// The depth comparison function to use.
        /// </summary>
        public CompareFunction depthCompareFunction = CompareFunction.LessEqual;

        /// <summary>
        /// Sets whether it should write to depth or not.
        /// </summary>
        public bool enableWrite = true;

        /// <summary>
        /// The stencil settings to use.
        /// </summary>
        public StencilStateData stencilSettings = new StencilStateData();

        /// <summary>
        /// The camera settings to use.
        /// </summary>
        public CustomCameraSettings cameraSettings = new CustomCameraSettings();
        public RenderTextureSettings renderTextureSettings = new RenderTextureSettings();
    }

    [System.Serializable]
    public class RenderTextureSettings
    {
        public string textureName;
        public float textureSizeScale;
        public RenderTextureFormat textureFormat;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
    }

    public RenderToTextureSetting settings = new();
    private RenderToTexturePass pass;

    public override void Create()
    {

        pass = new RenderToTexturePass(name, settings.filterSettings.LayerMask, settings.filterSettings.PassNames,
            settings.renderTextureSettings, settings.clearColor, settings.debug);
        switch (settings.overrideMode) {
            case RenderToTextureSetting.OverrideMaterialMode.None:
                pass.overrideMaterial = null;
                pass.overrideShader = null;
                break;
            case RenderToTextureSetting.OverrideMaterialMode.Material:
                pass.overrideMaterial = settings.overrideMaterial;
                pass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;
                pass.overrideShader = null;
                break;
            case RenderToTextureSetting.OverrideMaterialMode.Shader:
                pass.overrideMaterial = null;
                pass.overrideShader = settings.overrideShader;
                pass.overrideShaderPassIndex = settings.overrideShaderPassIndex;
                break;
        }
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(settings.debug) {
            pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }
        else {
            pass.renderPassEvent = settings.Event;
        }
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) {
            pass?.Dispose();
        }
    }
}
