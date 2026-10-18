// COPYRIGHT 2026 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Orts.Viewer3D.VR
{
    /// <summary>
    /// One OpenXR session driving a head-mounted display from the render thread.
    ///
    /// The session shares MonoGame's D3D11 device, so the normal Open Rails materials render each
    /// eye into an ordinary <see cref="RenderTarget2D"/> and the finished image is copied into the
    /// runtime's swapchain. The headset's pose arrives in a seated "LOCAL" reference space whose
    /// origin is where the driver's head was when the session started (or was last recentred), and
    /// the cameras treat that origin as their own viewpoint: in the 3D cab, that is ORTS3DCabHeadPos.
    ///
    /// Per frame, on the render thread: <see cref="BeginFrame"/> (events, xrWaitFrame, xrBeginFrame,
    /// xrLocateViews), one <see cref="SubmitEye"/> per eye, then <see cref="EndFrame"/>. The updater
    /// thread only reads <see cref="HeadPose"/>, for culling and the sound listener.
    /// </summary>
    [CallOnThread("Render")]
    public sealed unsafe class VRSession : IDisposable
    {
        public const int EyeCount = 2;

        public int EyeWidth { get; private set; }
        public int EyeHeight { get; private set; }
        public string RuntimeName { get; private set; }
        public string SystemName { get; private set; }

        /// <summary>True between <see cref="BeginFrame"/> and <see cref="EndFrame"/> when the runtime wants the eyes drawn.</summary>
        public bool FrameActive { get; private set; }

        /// <summary>Set once the runtime has told us to stop for good; the owner should dispose the session and carry on flat.</summary>
        public bool Dead { get; private set; }

        /// <summary>Half of the widest horizontal field of view of either eye, with a margin, for culling.</summary>
        public float CullHalfAngleRadians { get; private set; }
        /// <summary>Full vertical field of view of the headset, for the camera's symmetric projection.</summary>
        public float VerticalFovRadians { get; private set; }
        public float EyeAspectRatio { get { return (float)EyeWidth / EyeHeight; } }

        /// <summary>The head's pose in the seat frame (XNA axes: +Y up, -Z forward), updated every frame. Identity before tracking.</summary>
        public Matrix HeadPose
        {
            get { lock (PoseLock) return headPose; }
        }

        readonly object PoseLock = new object();
        Matrix headPose = Matrix.Identity;

        IntPtr Instance;
        ulong SystemId;
        IntPtr Session;
        IntPtr Space;      // LOCAL, offset by the last recentre
        IntPtr ViewSpace;  // VIEW, for recentring
        XrPosef SpaceOffset = XrPosef.Identity;
        bool RecenterRequested;

        readonly XrView[] Views = new XrView[EyeCount];
        XrFrameState FrameState;
        bool FrameBegun;
        bool SessionRunning;
        XrSessionState SessionState = XrSessionState.Unknown;
        bool RenderedOnce;
        long SwapchainFormat;
        readonly bool[] EyeSubmitted = new bool[EyeCount];

        class Eye
        {
            public IntPtr Swapchain;
            public SharpDX.Direct3D11.Texture2D[] Images;
        }
        readonly Eye[] Eyes = new Eye[EyeCount];

        readonly SharpDX.Direct3D11.Device D3DDevice;
        readonly SharpDX.Direct3D11.DeviceContext D3DContext;
        static readonly FieldInfo TextureField = typeof(Texture).GetField("_texture", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo CreateTextureMethod = typeof(Texture).GetMethod("CreateTexture", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Starts VR on the given device in the background and returns a handle to poll for the result.
        ///
        /// This is not done on the render thread because some runtimes (WiVRn, for one) block inside
        /// xrCreateInstance until a headset connects, which may be never. Until the runtime answers
        /// the game runs flat; when it does, the owner adopts the session (or reads why there is none).
        /// </summary>
        public static VRSessionStarter StartInBackground(GraphicsDevice graphicsDevice)
        {
            return new VRSessionStarter(graphicsDevice);
        }

        internal VRSession(GraphicsDevice graphicsDevice)
        {
            D3DDevice = (SharpDX.Direct3D11.Device)GetMonoGameField(graphicsDevice, "_d3dDevice");
            D3DContext = (SharpDX.Direct3D11.DeviceContext)GetMonoGameField(graphicsDevice, "_d3dContext");

            try
            {
                CreateInstance();
                GetSystem();
                CheckGraphicsRequirements();
                CreateSession();
                CreateSpaces();
                CreateSwapchains();
            }
            catch
            {
                Dispose();
                throw;
            }

            Trace.TraceInformation("VR: {0} on {1}, {2}x{3} per eye, swapchain format {4}", RuntimeName, SystemName, EyeWidth, EyeHeight, SwapchainFormat);
        }

        static object GetMonoGameField(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new InvalidOperationException("MonoGame has no " + name + " field; the VR code needs the WindowsDX (SharpDX) build.");
            var value = field.GetValue(target);
            if (value == null)
                throw new InvalidOperationException("MonoGame's " + name + " is not set yet.");
            return value;
        }

        void CreateInstance()
        {
            uint count;
            OpenXR.Check(OpenXR.xrEnumerateInstanceExtensionProperties(null, 0, out count, null), "xrEnumerateInstanceExtensionProperties");
            var extensions = new XrExtensionProperties[Math.Max(count, 1)];
            for (var i = 0; i < extensions.Length; i++)
                extensions[i].Type = XrStructureType.ExtensionProperties;
            fixed (XrExtensionProperties* p = extensions)
                OpenXR.Check(OpenXR.xrEnumerateInstanceExtensionProperties(null, count, out count, p), "xrEnumerateInstanceExtensionProperties");
            var names = extensions.Take((int)count).Select(e => e.Name).ToList();
            if (!names.Contains(OpenXR.KhrD3D11Enable))
                throw new InvalidOperationException("the OpenXR runtime does not offer " + OpenXR.KhrD3D11Enable + " (it has: " + String.Join(", ", names) + ")");

            var extensionName = System.Text.Encoding.ASCII.GetBytes(OpenXR.KhrD3D11Enable + "\0");
            fixed (byte* extensionNamePtr = extensionName)
            {
                byte** extensionNames = stackalloc byte*[1];
                extensionNames[0] = extensionNamePtr;

                var createInfo = new XrInstanceCreateInfo();
                createInfo.Type = XrStructureType.InstanceCreateInfo;
                OpenXR.FillFixed(createInfo.ApplicationInfo.ApplicationName, 128, "Open Rails");
                createInfo.ApplicationInfo.ApplicationVersion = 1;
                OpenXR.FillFixed(createInfo.ApplicationInfo.EngineName, 128, "MonoGame");
                createInfo.ApplicationInfo.EngineVersion = 3;
                createInfo.ApplicationInfo.ApiVersion = OpenXR.MakeVersion(1, 0, 34);
                createInfo.EnabledExtensionCount = 1;
                createInfo.EnabledExtensionNames = extensionNames;
                OpenXR.Check(OpenXR.xrCreateInstance(&createInfo, out Instance), "xrCreateInstance");
            }

            var properties = new XrInstanceProperties { Type = XrStructureType.InstanceProperties };
            OpenXR.Check(OpenXR.xrGetInstanceProperties(Instance, &properties), "xrGetInstanceProperties");
            RuntimeName = properties.Name + " " + OpenXR.VersionString(properties.RuntimeVersion);
        }

        void GetSystem()
        {
            var getInfo = new XrSystemGetInfo { Type = XrStructureType.SystemGetInfo, FormFactor = XrFormFactor.HeadMountedDisplay };
            var result = OpenXR.xrGetSystem(Instance, &getInfo, out SystemId);
            if (result == XrResult.ErrorFormFactorUnavailable)
                throw new InvalidOperationException("no headset is connected to " + RuntimeName);
            OpenXR.Check(result, "xrGetSystem");

            var properties = new XrSystemProperties { Type = XrStructureType.SystemProperties };
            OpenXR.Check(OpenXR.xrGetSystemProperties(Instance, SystemId, &properties), "xrGetSystemProperties");
            SystemName = properties.Name;

            uint count;
            var views = new XrViewConfigurationView[EyeCount];
            for (var i = 0; i < views.Length; i++)
                views[i].Type = XrStructureType.ViewConfigurationView;
            fixed (XrViewConfigurationView* p = views)
                OpenXR.Check(OpenXR.xrEnumerateViewConfigurationViews(Instance, SystemId, XrViewConfigurationType.PrimaryStereo, EyeCount, out count, p), "xrEnumerateViewConfigurationViews");
            if (count != EyeCount)
                throw new InvalidOperationException("expected a stereo headset, got " + count + " views");
            EyeWidth = (int)Math.Max(views[0].RecommendedImageRectWidth, views[1].RecommendedImageRectWidth);
            EyeHeight = (int)Math.Max(views[0].RecommendedImageRectHeight, views[1].RecommendedImageRectHeight);
        }

        void CheckGraphicsRequirements()
        {
            // The runtime insists on this call before xrCreateSession, and it tells us which adapter it renders on.
            var getRequirements = OpenXR.GetProc<OpenXR.GetD3D11GraphicsRequirementsKHR>(Instance, "xrGetD3D11GraphicsRequirementsKHR");
            var requirements = new XrGraphicsRequirementsD3D11KHR { Type = XrStructureType.GraphicsRequirementsD3D11KHR };
            OpenXR.Check(getRequirements(Instance, SystemId, &requirements), "xrGetD3D11GraphicsRequirementsKHR");

            if ((int)D3DDevice.FeatureLevel < requirements.MinFeatureLevel)
                throw new InvalidOperationException(String.Format("the runtime needs Direct3D feature level 0x{0:X} but the device has 0x{1:X}", requirements.MinFeatureLevel, (int)D3DDevice.FeatureLevel));

            using (var dxgiDevice = D3DDevice.QueryInterface<SharpDX.DXGI.Device>())
            using (var adapter = dxgiDevice.Adapter)
            {
                var luid = adapter.Description.Luid;
                var wanted = ((long)requirements.AdapterLuid.HighPart << 32) | requirements.AdapterLuid.LowPart;
                if (luid != wanted && wanted != 0)
                    Trace.TraceWarning("VR: the runtime renders on adapter LUID 0x{0:X} but Open Rails is on 0x{1:X} ({2}); expect the session to fail or run slowly.", wanted, luid, adapter.Description.Description);
            }
        }

        void CreateSession()
        {
            var binding = new XrGraphicsBindingD3D11KHR { Type = XrStructureType.GraphicsBindingD3D11KHR, Device = D3DDevice.NativePointer };
            var createInfo = new XrSessionCreateInfo { Type = XrStructureType.SessionCreateInfo, Next = (IntPtr)(&binding), SystemId = SystemId };
            OpenXR.Check(OpenXR.xrCreateSession(Instance, &createInfo, out Session), "xrCreateSession");
        }

        void CreateSpaces()
        {
            Space = CreateReferenceSpace(XrReferenceSpaceType.Local, SpaceOffset);
            ViewSpace = CreateReferenceSpace(XrReferenceSpaceType.View, XrPosef.Identity);
        }

        IntPtr CreateReferenceSpace(XrReferenceSpaceType type, XrPosef pose)
        {
            var createInfo = new XrReferenceSpaceCreateInfo { Type = XrStructureType.ReferenceSpaceCreateInfo, ReferenceSpaceType = type, PoseInReferenceSpace = pose };
            IntPtr space;
            OpenXR.Check(OpenXR.xrCreateReferenceSpace(Session, &createInfo, out space), "xrCreateReferenceSpace(" + type + ")");
            return space;
        }

        void CreateSwapchains()
        {
            // Open Rails draws into SurfaceFormat.Color (R8G8B8A8_UNORM) with display-ready values, so an
            // sRGB-typed swapchain is the honest choice; CopyResource is fine within the R8G8B8A8 family.
            const long R8G8B8A8_UNORM = 28, R8G8B8A8_UNORM_SRGB = 29;
            uint count;
            OpenXR.Check(OpenXR.xrEnumerateSwapchainFormats(Session, 0, out count, null), "xrEnumerateSwapchainFormats");
            var formats = new long[Math.Max(count, 1)];
            fixed (long* p = formats)
                OpenXR.Check(OpenXR.xrEnumerateSwapchainFormats(Session, count, out count, p), "xrEnumerateSwapchainFormats");
            var offered = formats.Take((int)count).ToList();
            if (offered.Contains(R8G8B8A8_UNORM_SRGB)) SwapchainFormat = R8G8B8A8_UNORM_SRGB;
            else if (offered.Contains(R8G8B8A8_UNORM)) SwapchainFormat = R8G8B8A8_UNORM;
            else throw new InvalidOperationException("the runtime offers no R8G8B8A8 swapchain format (it has: " + String.Join(", ", offered) + ")");

            for (var eye = 0; eye < EyeCount; eye++)
            {
                var createInfo = new XrSwapchainCreateInfo
                {
                    Type = XrStructureType.SwapchainCreateInfo,
                    UsageFlags = XrSwapchainUsageFlags.ColorAttachment | XrSwapchainUsageFlags.TransferDst | XrSwapchainUsageFlags.Sampled,
                    Format = SwapchainFormat,
                    SampleCount = 1,
                    Width = (uint)EyeWidth,
                    Height = (uint)EyeHeight,
                    FaceCount = 1,
                    ArraySize = 1,
                    MipCount = 1,
                };
                var e = new Eye();
                OpenXR.Check(OpenXR.xrCreateSwapchain(Session, &createInfo, out e.Swapchain), "xrCreateSwapchain");
                Eyes[eye] = e;

                OpenXR.Check(OpenXR.xrEnumerateSwapchainImages(e.Swapchain, 0, out count, null), "xrEnumerateSwapchainImages");
                var images = new XrSwapchainImageD3D11KHR[count];
                for (var i = 0; i < images.Length; i++)
                    images[i].Type = XrStructureType.SwapchainImageD3D11KHR;
                fixed (XrSwapchainImageD3D11KHR* p = images)
                    OpenXR.Check(OpenXR.xrEnumerateSwapchainImages(e.Swapchain, count, out count, p), "xrEnumerateSwapchainImages");
                // The runtime owns these textures; wrapping the pointer takes no reference, so never Dispose() the wrappers.
                e.Images = images.Take((int)count).Select(i => new SharpDX.Direct3D11.Texture2D(i.Texture)).ToArray();
            }
        }

        /// <summary>Ask for the seat origin to be moved to where the head is now, keeping only its yaw. Safe from any thread.</summary>
        public void RequestRecenter()
        {
            RecenterRequested = true;
        }

        /// <summary>
        /// Starts a headset frame. Afterwards <see cref="FrameActive"/> says whether the eyes should be
        /// drawn; either way <see cref="EndFrame"/> must follow before the next <see cref="BeginFrame"/>.
        /// </summary>
        public void BeginFrame()
        {
            FrameActive = false;
            PollEvents();
            if (Dead || !SessionRunning)
                return;

            var waitInfo = new XrFrameWaitInfo { Type = XrStructureType.FrameWaitInfo };
            var frameState = new XrFrameState { Type = XrStructureType.FrameState };
            if (!Succeeded(OpenXR.xrWaitFrame(Session, &waitInfo, &frameState), "xrWaitFrame"))
                return;
            FrameState = frameState;

            var beginInfo = new XrFrameBeginInfo { Type = XrStructureType.FrameBeginInfo };
            if (!Succeeded(OpenXR.xrBeginFrame(Session, &beginInfo), "xrBeginFrame"))
                return;
            FrameBegun = true;
            for (var eye = 0; eye < EyeCount; eye++)
                EyeSubmitted[eye] = false;

            if (RecenterRequested)
            {
                RecenterRequested = false;
                Recenter(FrameState.PredictedDisplayTime);
            }

            var locateInfo = new XrViewLocateInfo
            {
                Type = XrStructureType.ViewLocateInfo,
                ViewConfigurationType = XrViewConfigurationType.PrimaryStereo,
                DisplayTime = FrameState.PredictedDisplayTime,
                Space = Space,
            };
            var viewState = new XrViewState { Type = XrStructureType.ViewState };
            uint count;
            for (var eye = 0; eye < EyeCount; eye++)
                Views[eye] = new XrView { Type = XrStructureType.View };
            fixed (XrView* p = Views)
            {
                if (!Succeeded(OpenXR.xrLocateViews(Session, &locateInfo, &viewState, EyeCount, out count, p), "xrLocateViews"))
                    return;
            }
            if ((viewState.ViewStateFlags & XrViewStateFlags.OrientationValid) == 0)
                return;

            var center = new XrPosef
            {
                Orientation = Views[0].Pose.Orientation,
                Position = new XrVector3f
                {
                    X = (Views[0].Pose.Position.X + Views[1].Pose.Position.X) / 2,
                    Y = (Views[0].Pose.Position.Y + Views[1].Pose.Position.Y) / 2,
                    Z = (Views[0].Pose.Position.Z + Views[1].Pose.Position.Z) / 2,
                },
            };
            lock (PoseLock)
                headPose = PoseToMatrix(center);

            var halfAngle = 0f;
            var vertical = 0f;
            for (var eye = 0; eye < EyeCount; eye++)
            {
                halfAngle = Math.Max(halfAngle, Math.Max(Math.Abs(Views[eye].Fov.AngleLeft), Math.Abs(Views[eye].Fov.AngleRight)));
                vertical = Math.Max(vertical, Math.Abs(Views[eye].Fov.AngleUp) + Math.Abs(Views[eye].Fov.AngleDown));
            }
            CullHalfAngleRadians = halfAngle + MathHelper.ToRadians(10);
            VerticalFovRadians = vertical;

            FrameActive = FrameState.ShouldRender != 0;
            if (FrameActive && !RenderedOnce)
            {
                RenderedOnce = true;
                Trace.TraceInformation("VR: the runtime is showing our frames (session state {0})", SessionState);
                // The runtime's origin is wherever the headset was at session start, often still on the desk.
                // Make the seat where the driver's head is when the picture first appears; Alt+V redoes it.
                RecenterRequested = true;
            }
        }

        /// <summary>The eye's pose in the seat frame; the eye view matrix is seatView * Invert(EyePose).</summary>
        public Matrix EyePose(int eye)
        {
            return PoseToMatrix(Views[eye].Pose);
        }

        /// <summary>An off-centre projection matching the eye's field of view, for the given depth range.</summary>
        public Matrix EyeProjection(int eye, float nearPlane, float farPlane)
        {
            var fov = Views[eye].Fov;
            return Matrix.CreatePerspectiveOffCenter(
                (float)Math.Tan(fov.AngleLeft) * nearPlane,
                (float)Math.Tan(fov.AngleRight) * nearPlane,
                (float)Math.Tan(fov.AngleDown) * nearPlane,
                (float)Math.Tan(fov.AngleUp) * nearPlane,
                nearPlane, farPlane);
        }

        /// <summary>Copies a finished eye image (an unbound, non-multisampled or already resolved render target of EyeWidth x EyeHeight) into the headset.</summary>
        public void SubmitEye(int eye, Texture2D source)
        {
            if (!FrameActive)
                return;
            var e = Eyes[eye];
            var acquireInfo = new XrSwapchainImageAcquireInfo { Type = XrStructureType.SwapchainImageAcquireInfo };
            uint index;
            if (!Succeeded(OpenXR.xrAcquireSwapchainImage(e.Swapchain, &acquireInfo, out index), "xrAcquireSwapchainImage"))
                return;
            var waitInfo = new XrSwapchainImageWaitInfo { Type = XrStructureType.SwapchainImageWaitInfo, Timeout = OpenXR.InfiniteDuration };
            if (Succeeded(OpenXR.xrWaitSwapchainImage(e.Swapchain, &waitInfo), "xrWaitSwapchainImage"))
            {
                var native = GetNativeTexture(source);
                if (native != null)
                {
                    D3DContext.CopyResource(native, e.Images[index]);
                    EyeSubmitted[eye] = true;
                }
            }
            var releaseInfo = new XrSwapchainImageReleaseInfo { Type = XrStructureType.SwapchainImageReleaseInfo };
            Succeeded(OpenXR.xrReleaseSwapchainImage(e.Swapchain, &releaseInfo), "xrReleaseSwapchainImage");
        }

        /// <summary>Finishes the headset frame, submitting both eyes when they were drawn and an empty frame otherwise.</summary>
        public void EndFrame()
        {
            if (!FrameBegun)
                return;
            FrameBegun = false;

            XrCompositionLayerProjectionView* projectionViews = stackalloc XrCompositionLayerProjectionView[EyeCount];
            for (var eye = 0; eye < EyeCount; eye++)
            {
                projectionViews[eye] = new XrCompositionLayerProjectionView
                {
                    Type = XrStructureType.CompositionLayerProjectionView,
                    Pose = Views[eye].Pose,
                    Fov = Views[eye].Fov,
                    SubImage = new XrSwapchainSubImage
                    {
                        Swapchain = Eyes[eye].Swapchain,
                        ImageRect = new XrRect2Di { Extent = new XrExtent2Di { Width = EyeWidth, Height = EyeHeight } },
                    },
                };
            }
            var layer = new XrCompositionLayerProjection
            {
                Type = XrStructureType.CompositionLayerProjection,
                Space = Space,
                ViewCount = EyeCount,
                Views = projectionViews,
            };
            IntPtr* layers = stackalloc IntPtr[1];
            layers[0] = (IntPtr)(&layer);

            var submitted = FrameActive && EyeSubmitted.All(s => s);
            var endInfo = new XrFrameEndInfo
            {
                Type = XrStructureType.FrameEndInfo,
                DisplayTime = FrameState.PredictedDisplayTime,
                EnvironmentBlendMode = XrEnvironmentBlendMode.Opaque,
                LayerCount = submitted ? 1u : 0u,
                Layers = submitted ? layers : null,
            };
            Succeeded(OpenXR.xrEndFrame(Session, &endInfo), "xrEndFrame");
            FrameActive = false;
        }

        void Recenter(long time)
        {
            var location = new XrSpaceLocation { Type = XrStructureType.SpaceLocation };
            if (!Succeeded(OpenXR.xrLocateSpace(ViewSpace, Space, time, &location), "xrLocateSpace"))
                return;
            if ((location.LocationFlags & XrSpaceLocationFlags.OrientationValid) == 0)
                return;

            // Compose the head's pose (in the current space) onto the current offset to get it in the
            // runtime's own LOCAL frame, then keep only position and yaw so the horizon stays level.
            var head = PoseToMatrix(location.Pose) * PoseToMatrix(SpaceOffset);
            var forward = Vector3.TransformNormal(Vector3.Forward, head);
            var yaw = (float)Math.Atan2(-forward.X, -forward.Z);
            var orientation = Quaternion.CreateFromAxisAngle(Vector3.Up, yaw);
            var offset = new XrPosef
            {
                Orientation = new XrQuaternionf { X = orientation.X, Y = orientation.Y, Z = orientation.Z, W = orientation.W },
                Position = new XrVector3f { X = head.Translation.X, Y = head.Translation.Y, Z = head.Translation.Z },
            };

            IntPtr space;
            try
            {
                space = CreateReferenceSpace(XrReferenceSpaceType.Local, offset);
            }
            catch (OpenXRException error)
            {
                Trace.TraceWarning("VR: recentre failed. {0}", error.Message);
                return;
            }
            OpenXR.xrDestroySpace(Space);
            Space = space;
            SpaceOffset = offset;
        }

        void PollEvents()
        {
            var buffer = new XrEventDataBuffer();
            while (true)
            {
                buffer.Type = XrStructureType.EventDataBuffer;
                buffer.Next = IntPtr.Zero;
                var result = OpenXR.xrPollEvent(Instance, &buffer);
                if (result == XrResult.EventUnavailable)
                    break;
                if (!Succeeded(result, "xrPollEvent"))
                    break;
                switch (buffer.Type)
                {
                    case XrStructureType.EventDataSessionStateChanged:
                        var changed = *(XrEventDataSessionStateChanged*)&buffer;
                        OnSessionStateChanged(changed.State);
                        break;
                    case XrStructureType.EventDataInstanceLossPending:
                        Trace.TraceWarning("VR: the OpenXR runtime is going away; leaving VR.");
                        Dead = true;
                        break;
                    case XrStructureType.EventDataReferenceSpaceChangePending:
                        // The runtime recentred itself (e.g. the headset's own recentre); our offset still applies on top.
                        break;
                }
            }
        }

        void OnSessionStateChanged(XrSessionState state)
        {
            Trace.TraceInformation("VR: session state {0} -> {1}", SessionState, state);
            SessionState = state;
            switch (state)
            {
                case XrSessionState.Ready:
                    var beginInfo = new XrSessionBeginInfo { Type = XrStructureType.SessionBeginInfo, PrimaryViewConfigurationType = XrViewConfigurationType.PrimaryStereo };
                    if (Succeeded(OpenXR.xrBeginSession(Session, &beginInfo), "xrBeginSession"))
                    {
                        SessionRunning = true;
                        Trace.TraceInformation("VR: session running on {0}", SystemName);
                    }
                    break;
                case XrSessionState.Stopping:
                    SessionRunning = false;
                    Succeeded(OpenXR.xrEndSession(Session), "xrEndSession");
                    break;
                case XrSessionState.Exiting:
                case XrSessionState.LossPending:
                    SessionRunning = false;
                    Dead = true;
                    Trace.TraceInformation("VR: session {0}; leaving VR.", state);
                    break;
            }
        }

        static Matrix PoseToMatrix(XrPosef pose)
        {
            // OpenXR and XNA agree on axes: right-handed, +Y up, -Z forward.
            var q = new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W);
            return Matrix.CreateFromQuaternion(q) * Matrix.CreateTranslation(pose.Position.X, pose.Position.Y, pose.Position.Z);
        }

        static SharpDX.Direct3D11.Resource GetNativeTexture(Texture2D texture)
        {
            if (TextureField == null)
                return null;
            var native = TextureField.GetValue(texture) as SharpDX.Direct3D11.Resource;
            if (native == null && CreateTextureMethod != null)
            {
                native = CreateTextureMethod.Invoke(texture, null) as SharpDX.Direct3D11.Resource;
                TextureField.SetValue(texture, native);
            }
            return native;
        }

        static bool Succeeded(XrResult result, string call)
        {
            if (result >= 0)
                return true;
            Trace.TraceWarning("VR: {0} failed: {1} ({2})", call, result, (int)result);
            return false;
        }

        public void Dispose()
        {
            if (FrameBegun)
            {
                FrameActive = false;
                EndFrame();
            }
            foreach (var eye in Eyes)
                if (eye != null && eye.Swapchain != IntPtr.Zero)
                    OpenXR.xrDestroySwapchain(eye.Swapchain);
            if (ViewSpace != IntPtr.Zero) OpenXR.xrDestroySpace(ViewSpace);
            if (Space != IntPtr.Zero) OpenXR.xrDestroySpace(Space);
            if (Session != IntPtr.Zero)
            {
                if (SessionRunning)
                    OpenXR.xrRequestExitSession(Session);
                OpenXR.xrDestroySession(Session);
            }
            if (Instance != IntPtr.Zero) OpenXR.xrDestroyInstance(Instance);
            ViewSpace = Space = Session = Instance = IntPtr.Zero;
            SessionRunning = false;
            Dead = true;
        }
    }

    /// <summary>
    /// A VR session being created on its own thread. Poll <see cref="Done"/> from the render thread and
    /// then take <see cref="Session"/> (null when it failed; <see cref="Failure"/> says why, already logged).
    /// <see cref="Abandon"/> makes a session that arrives later get thrown away.
    /// </summary>
    public sealed class VRSessionStarter
    {
        public bool Done { get { return done; } }
        public VRSession Session { get; private set; }
        public string Failure { get; private set; }
        public DateTime Started { get; private set; }

        volatile bool done;
        volatile bool abandoned;
        readonly object Lock = new object();

        internal VRSessionStarter(GraphicsDevice graphicsDevice)
        {
            Started = DateTime.Now;
            var thread = new System.Threading.Thread(() => Run(graphicsDevice)) { Name = "OpenXR Startup", IsBackground = true };
            thread.Start();
        }

        void Run(GraphicsDevice graphicsDevice)
        {
            VRSession session = null;
            try
            {
                session = new VRSession(graphicsDevice);
            }
            catch (DllNotFoundException error)
            {
                Failure = "the OpenXR loader (openxr_loader.dll) could not be loaded: " + error.Message;
            }
            catch (Exception error)
            {
                Failure = error.Message;
            }
            lock (Lock)
            {
                if (abandoned)
                {
                    if (session != null)
                        session.Dispose();
                }
                else
                    Session = session;
                done = true;
            }
            if (Failure != null)
                Trace.TraceWarning("VR: could not start an OpenXR session; running without VR. {0}", Failure);
        }

        /// <summary>Called when the owner no longer wants the session (it is shutting down).</summary>
        public void Abandon()
        {
            lock (Lock)
            {
                abandoned = true;
                if (Session != null)
                {
                    Session.Dispose();
                    Session = null;
                }
            }
        }
    }
}
