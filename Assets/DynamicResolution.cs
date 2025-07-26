using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class DynamicResolution : MonoBehaviour
{
    private UniversalRenderPipelineAsset urpAsset;

    FrameTiming[] frameTimings = new FrameTiming[3];

    public float maxResolutionWidthScale = 1.5f;
    public float maxResolutionHeightScale = 1.5f;
    public float minResolutionWidthScale = 0.5f;
    public float minResolutionHeightScale = 0.5f;
    public float scaleWidthIncrement = 0.25f;
    public float scaleHeightIncrement = 0.25f;

    float m_widthScale = 1.0f;
    float m_heightScale = 1.0f;

    // Variables for dynamic resolution algorithm that persist across frames
    uint m_frameCount = 0;

    const uint kNumFrameTimings = 2;

    double m_gpuFrameTime;
    double m_cpuFrameTime;

    // Use this for initialization
    void Start()
    {
        // Get the current URP asset
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null)
        {
            Debug.LogError("No Universal Render Pipeline Asset found!");
            return;
        }

        // Initialize with current render scale
        m_widthScale = urpAsset.renderScale;
        m_heightScale = urpAsset.renderScale;
            
        // Start the render scale cycling coroutine
        StartCoroutine(CycleRenderScale());
    }

    // Coroutine that cycles render scale between min and max every 3 seconds
    private IEnumerator CycleRenderScale()
    {
        Debug.Log("Starting render scale cycling coroutine");
        while (true)
        {
            // Step up to max
            while (m_widthScale < maxResolutionWidthScale || m_heightScale < maxResolutionHeightScale)
            {
                if (m_widthScale < maxResolutionWidthScale)
                    m_widthScale = Mathf.Min(maxResolutionWidthScale, m_widthScale + scaleWidthIncrement);
                if (m_heightScale < maxResolutionHeightScale)
                    m_heightScale = Mathf.Min(maxResolutionHeightScale, m_heightScale + scaleHeightIncrement);
                
                // Apply the changes immediately
                ApplyRenderScale();
                Debug.LogFormat("Increasing resolution to {0:F3}x{1:F3}", m_widthScale, m_heightScale);
                yield return new WaitForSeconds(3f);
            }
            
            // Step down to min
            while (m_widthScale > minResolutionWidthScale || m_heightScale > minResolutionHeightScale)
            {
                if (m_widthScale > minResolutionWidthScale)
                    m_widthScale = Mathf.Max(minResolutionWidthScale, m_widthScale - scaleWidthIncrement);
                if (m_heightScale > minResolutionHeightScale)
                    m_heightScale = Mathf.Max(minResolutionHeightScale, m_heightScale - scaleHeightIncrement);
                
                // Apply the changes immediately
                ApplyRenderScale();
                Debug.LogFormat("Decreasing resolution to {0:F3}x{1:F3}", m_widthScale, m_heightScale);
                yield return new WaitForSeconds(3f);
            }
        }
    }

    // Apply render scale changes to the URP asset
    private void ApplyRenderScale()
    {
        // if (urpAsset != null)
        // {
        //     // Use the width scale as the primary render scale (URP typically uses a single scale value)
        //     urpAsset.renderScale = m_widthScale;
            
        //     // Force Unity to update the render pipeline
        //     GraphicsSettings.defaultRenderPipeline = urpAsset;
        // }
        #if !UNITY_EDITOR && UNITY_ANDROID
        XRSettings.renderViewportScale = 0.5f;
        ScalableBufferManager.ResizeBuffers(m_widthScale, m_heightScale);
        #else
        ScalableBufferManager.ResizeBuffers(Mathf.Min(m_widthScale, 1.0f), Mathf.Min(m_heightScale, 1.0f));
        #endif
        Debug.LogFormat("Render scale: {0:F3}x{1:F3}", m_widthScale, m_heightScale);
    }

    // Update is called once per frame
    void Update()
    {
        DetermineResolution();
    }

    private void ReduceResolution()
    {
        m_heightScale = Mathf.Max(minResolutionHeightScale, m_heightScale - scaleHeightIncrement);
        m_widthScale = Mathf.Max(minResolutionWidthScale, m_widthScale - scaleWidthIncrement);
        ApplyRenderScale();
    }

    private void IncreaseResolution()
    {
        m_heightScale = Mathf.Min(maxResolutionHeightScale, m_heightScale + scaleHeightIncrement);
        m_widthScale = Mathf.Min(maxResolutionWidthScale, m_widthScale + scaleWidthIncrement);
        ApplyRenderScale();
    }

    // Estimate the next frame time and update the resolution scale if necessary.
    private void DetermineResolution()
    {
        ++m_frameCount;
        if (m_frameCount <= kNumFrameTimings)
        {
            return;
        }
        FrameTimingManager.CaptureFrameTimings();
        FrameTimingManager.GetLatestTimings(kNumFrameTimings, frameTimings);
        if (frameTimings.Length < kNumFrameTimings)
        {
            Debug.LogFormat("Skipping frame {0}, didn't get enough frame timings.",
                m_frameCount);

            return;
        }

        m_gpuFrameTime = (double)frameTimings[0].gpuFrameTime;
        m_cpuFrameTime = (double)frameTimings[0].cpuFrameTime;
    }
}