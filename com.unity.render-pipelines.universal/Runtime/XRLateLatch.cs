using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal struct XRLateLatch
    {
    #if ENABLE_VR && ENABLE_XR_MODULE
        internal bool isEnabled { get; set; }
        internal bool canMark   { get; set; }
        internal bool hasMarked { get; set; }

        // Prevent GC by keeping an array pre-allocated
        static Matrix4x4[] s_projMatrix = new Matrix4x4[2];
     #endif

        internal void AllowMark(bool allowMark)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            canMark = allowMark;
#endif
        }

        internal void BeginLateLatching(Camera camera, XRPass xrPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XR.XRDisplaySubsystem xrDisplay = XRSystem.GetActiveDisplay();

            if (xrDisplay != null && xrPass.viewCount == 2) // multiview only
            {
                xrDisplay.BeginRecordingIfLateLatched(camera);
                isEnabled = true;
            }
#endif
        }

        internal void EndLateLatching(Camera camera, XRPass xrPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XR.XRDisplaySubsystem xrDisplay = XRSystem.GetActiveDisplay();

            if (xrDisplay != null && isEnabled)
            {
                xrDisplay.EndRecordingIfLateLatched(camera);
                isEnabled = false;
            }
#endif
        }

        internal void UnmarkShaderProperties(CommandBuffer cmd)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (isEnabled && hasMarked)
            {
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.View);
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.InverseView);
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.ViewProjection);
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.InverseViewProjection);
                hasMarked = false;
            }
#endif
        }

        internal void MarkShaderProperties(XRPass xrPass, CommandBuffer cmd, bool renderIntoTexture)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (isEnabled && canMark)
            {
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.View, XRBuiltinShaderConstants.unity_StereoMatrixV);
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.InverseView, XRBuiltinShaderConstants.unity_StereoMatrixInvV);
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.ViewProjection, XRBuiltinShaderConstants.unity_StereoMatrixVP);
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.InverseViewProjection, XRBuiltinShaderConstants.unity_StereoMatrixInvVP);

                for (int viewIndex = 0; viewIndex < 2; ++viewIndex)
                    s_projMatrix[viewIndex] = GL.GetGPUProjectionMatrix(xrPass.GetProjMatrix(viewIndex), renderIntoTexture);

                cmd.SetLateLatchProjectionMatrices(s_projMatrix);
                hasMarked = true;
            }
#endif
        }
    }
}
