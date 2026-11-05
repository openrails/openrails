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
using ORTS.Settings;
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
    /// xrLocateViews, xrSyncActions), one <see cref="SubmitEye"/> per eye, then <see cref="EndFrame"/>.
    /// The updater thread only reads <see cref="HeadPose"/> (for culling and the sound listener) and
    /// <see cref="Hand"/> (the controllers, for pointing at things).
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

        public const int HandCount = 2;
        public const int LeftHand = 0;
        public const int RightHand = 1;

        /// <summary>A copy of the controller's state from the last frame; its pose is in the seat frame like <see cref="HeadPose"/>. Safe from any thread.</summary>
        public VRHand Hand(int hand)
        {
            lock (PoseLock) return hands[hand].Clone();
        }

        /// <summary>True when the runtime accepted our controller actions (it may still have no controllers to drive them).</summary>
        public bool HasControllers { get { return ActionsAvailable; } }

        /// <summary>
        /// Whether the controllers are listened to at all (the VRControllers setting; Alt+F toggles it in the cab).
        /// Independently of this, a controller that has not moved for a few seconds (put down) goes idle:
        /// <see cref="VRHand.Active"/> is false until it is picked up again.
        /// </summary>
        public volatile bool ControllersEnabled;
        /// <summary>
        /// Runtimes that track bare hands (WiVRn on a Quest) present them as the Khronos simple controller, and a
        /// Quest switches to them whenever it decides the controllers are at rest, even in the driver's hands.
        /// They are accepted as a pointer (pinch to click) only when the VRHandTracking setting is on; otherwise a hand
        /// drives nothing. Turning hand tracking off on the headset avoids the switching.
        /// </summary>
        public readonly bool HandTrackingAllowed;
        const string SimpleControllerProfile = "/interaction_profiles/khr/simple_controller";
        readonly string[] HandProfiles = new string[HandCount];
        bool ProfilesLogged;
        public const float IdleAfterSeconds = 3;
        const float IdleMoveMetres = 0.015f;
        const float IdleTurnRadians = 0.035f;
        readonly Matrix[] RestPose = new Matrix[HandCount];
        readonly DateTime[] LastMoved = new DateTime[HandCount];
        readonly bool[] WasPressed = new bool[HandCount];

        readonly VRHand[] hands = { new VRHand(), new VRHand() };
        IntPtr ActionSet;
        IntPtr AimAction, TriggerAction, GripAction, StickAction, PrimaryAction, SecondaryAction, MenuAction, HapticAction;
        readonly ulong[] HandPaths = new ulong[HandCount];
        readonly IntPtr[] AimSpaces = new IntPtr[HandCount];
        bool ActionsAvailable;
        List<string> EnabledExtensions = new List<string>();
        bool SyncWarned;
        readonly bool[] HandSeen = new bool[HandCount];
        readonly int[] HapticMilliseconds = new int[HandCount];

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
        public static VRSessionStarter StartInBackground(GraphicsDevice graphicsDevice, UserSettings settings)
        {
            return new VRSessionStarter(graphicsDevice, settings);
        }

        internal VRSession(GraphicsDevice graphicsDevice, UserSettings settings)
        {
            ControllersEnabled = settings.VRControllers;
            HandTrackingAllowed = settings.VRHandTracking;
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
                CreateActions();
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

            // The newer Meta controllers have their own interaction profiles behind extensions; take them when offered.
            EnabledExtensions = new[] { OpenXR.KhrD3D11Enable, OpenXR.MetaTouchControllerPlus, OpenXR.FbTouchControllerPro }.Where(names.Contains).ToList();
            var extensionBytes = EnabledExtensions.Select(e => System.Text.Encoding.ASCII.GetBytes(e + "\0")).ToArray();
            var handles = extensionBytes.Select(b => System.Runtime.InteropServices.GCHandle.Alloc(b, System.Runtime.InteropServices.GCHandleType.Pinned)).ToArray();
            try
            {
                byte** extensionNames = stackalloc byte*[extensionBytes.Length];
                for (var i = 0; i < handles.Length; i++)
                    extensionNames[i] = (byte*)handles[i].AddrOfPinnedObject();

                var createInfo = new XrInstanceCreateInfo();
                createInfo.Type = XrStructureType.InstanceCreateInfo;
                OpenXR.FillFixed(createInfo.ApplicationInfo.ApplicationName, 128, "Open Rails");
                createInfo.ApplicationInfo.ApplicationVersion = 1;
                OpenXR.FillFixed(createInfo.ApplicationInfo.EngineName, 128, "MonoGame");
                createInfo.ApplicationInfo.EngineVersion = 3;
                createInfo.ApplicationInfo.ApiVersion = OpenXR.MakeVersion(1, 0, 34);
                createInfo.EnabledExtensionCount = (uint)extensionBytes.Length;
                createInfo.EnabledExtensionNames = extensionNames;
                OpenXR.Check(OpenXR.xrCreateInstance(&createInfo, out Instance), "xrCreateInstance");
            }
            finally
            {
                foreach (var handle in handles)
                    handle.Free();
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
            SyncActions(FrameState.PredictedDisplayTime);

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

        /// <summary>Buzz a controller briefly (a click's worth of feedback). Safe from any thread; applied on the next frame.</summary>
        public void Vibrate(int hand, float seconds)
        {
            System.Threading.Interlocked.Exchange(ref HapticMilliseconds[hand], (int)(seconds * 1000));
        }

        #region Controllers
        // One action set holding one action per input, each with a left and a right subaction path, bound
        // to the common controller profiles. The runtime picks the profile that matches the controllers in
        // use and maps our actions to it; a profile it does not know is just declined.
        void CreateActions()
        {
            try
            {
                HandPaths[LeftHand] = StringToPath("/user/hand/left");
                HandPaths[RightHand] = StringToPath("/user/hand/right");

                var setInfo = new XrActionSetCreateInfo { Type = XrStructureType.ActionSetCreateInfo };
                OpenXR.FillFixed(setInfo.ActionSetName, 64, "openrails");
                OpenXR.FillFixed(setInfo.LocalizedActionSetName, 128, "Open Rails");
                OpenXR.Check(OpenXR.xrCreateActionSet(Instance, &setInfo, out ActionSet), "xrCreateActionSet");

                AimAction = CreateAction("aim", "Aim", XrActionType.PoseInput);
                TriggerAction = CreateAction("trigger", "Trigger", XrActionType.FloatInput);
                GripAction = CreateAction("grip", "Grip", XrActionType.FloatInput);
                StickAction = CreateAction("stick", "Thumbstick", XrActionType.Vector2fInput);
                PrimaryAction = CreateAction("primary", "Primary button", XrActionType.BooleanInput);
                SecondaryAction = CreateAction("secondary", "Secondary button", XrActionType.BooleanInput);
                MenuAction = CreateAction("menu", "Menu button", XrActionType.BooleanInput);
                HapticAction = CreateAction("haptic", "Vibration", XrActionType.VibrationOutput);

                SuggestBindings("/interaction_profiles/oculus/touch_controller", new[] {
                    Bind(AimAction, "/user/hand/left/input/aim/pose"), Bind(AimAction, "/user/hand/right/input/aim/pose"),
                    Bind(TriggerAction, "/user/hand/left/input/trigger/value"), Bind(TriggerAction, "/user/hand/right/input/trigger/value"),
                    Bind(GripAction, "/user/hand/left/input/squeeze/value"), Bind(GripAction, "/user/hand/right/input/squeeze/value"),
                    Bind(StickAction, "/user/hand/left/input/thumbstick"), Bind(StickAction, "/user/hand/right/input/thumbstick"),
                    Bind(PrimaryAction, "/user/hand/left/input/x/click"), Bind(PrimaryAction, "/user/hand/right/input/a/click"),
                    Bind(SecondaryAction, "/user/hand/left/input/y/click"), Bind(SecondaryAction, "/user/hand/right/input/b/click"),
                    Bind(MenuAction, "/user/hand/left/input/menu/click"),
                    Bind(HapticAction, "/user/hand/left/output/haptic"), Bind(HapticAction, "/user/hand/right/output/haptic"),
                });
                var touchPlus = new[] {
                    Bind(AimAction, "/user/hand/left/input/aim/pose"), Bind(AimAction, "/user/hand/right/input/aim/pose"),
                    Bind(TriggerAction, "/user/hand/left/input/trigger/value"), Bind(TriggerAction, "/user/hand/right/input/trigger/value"),
                    Bind(GripAction, "/user/hand/left/input/squeeze/value"), Bind(GripAction, "/user/hand/right/input/squeeze/value"),
                    Bind(StickAction, "/user/hand/left/input/thumbstick"), Bind(StickAction, "/user/hand/right/input/thumbstick"),
                    Bind(PrimaryAction, "/user/hand/left/input/x/click"), Bind(PrimaryAction, "/user/hand/right/input/a/click"),
                    Bind(SecondaryAction, "/user/hand/left/input/y/click"), Bind(SecondaryAction, "/user/hand/right/input/b/click"),
                    Bind(MenuAction, "/user/hand/left/input/menu/click"),
                    Bind(HapticAction, "/user/hand/left/output/haptic"), Bind(HapticAction, "/user/hand/right/output/haptic"),
                };
                if (EnabledExtensions.Contains(OpenXR.MetaTouchControllerPlus))
                    SuggestBindings("/interaction_profiles/meta/touch_controller_plus", touchPlus);
                if (EnabledExtensions.Contains(OpenXR.FbTouchControllerPro))
                    SuggestBindings("/interaction_profiles/facebook/touch_controller_pro", touchPlus);
                SuggestBindings("/interaction_profiles/valve/index_controller", new[] {
                    Bind(AimAction, "/user/hand/left/input/aim/pose"), Bind(AimAction, "/user/hand/right/input/aim/pose"),
                    Bind(TriggerAction, "/user/hand/left/input/trigger/value"), Bind(TriggerAction, "/user/hand/right/input/trigger/value"),
                    Bind(GripAction, "/user/hand/left/input/squeeze/value"), Bind(GripAction, "/user/hand/right/input/squeeze/value"),
                    Bind(StickAction, "/user/hand/left/input/thumbstick"), Bind(StickAction, "/user/hand/right/input/thumbstick"),
                    Bind(PrimaryAction, "/user/hand/left/input/a/click"), Bind(PrimaryAction, "/user/hand/right/input/a/click"),
                    Bind(SecondaryAction, "/user/hand/left/input/b/click"), Bind(SecondaryAction, "/user/hand/right/input/b/click"),
                    Bind(HapticAction, "/user/hand/left/output/haptic"), Bind(HapticAction, "/user/hand/right/output/haptic"),
                });
                SuggestBindings("/interaction_profiles/khr/simple_controller", new[] {
                    Bind(AimAction, "/user/hand/left/input/aim/pose"), Bind(AimAction, "/user/hand/right/input/aim/pose"),
                    Bind(TriggerAction, "/user/hand/left/input/select/click"), Bind(TriggerAction, "/user/hand/right/input/select/click"),
                    Bind(MenuAction, "/user/hand/left/input/menu/click"), Bind(MenuAction, "/user/hand/right/input/menu/click"),
                    Bind(HapticAction, "/user/hand/left/output/haptic"), Bind(HapticAction, "/user/hand/right/output/haptic"),
                });

                for (var hand = 0; hand < HandCount; hand++)
                {
                    var spaceInfo = new XrActionSpaceCreateInfo { Type = XrStructureType.ActionSpaceCreateInfo, Action = AimAction, SubactionPath = HandPaths[hand], PoseInActionSpace = XrPosef.Identity };
                    OpenXR.Check(OpenXR.xrCreateActionSpace(Session, &spaceInfo, out AimSpaces[hand]), "xrCreateActionSpace");
                }

                var set = ActionSet;
                var attachInfo = new XrSessionActionSetAttachInfo { Type = XrStructureType.SessionActionSetAttachInfo, CountActionSets = 1, ActionSets = &set };
                OpenXR.Check(OpenXR.xrAttachSessionActionSets(Session, &attachInfo), "xrAttachSessionActionSets");
                ActionsAvailable = true;
                Trace.TraceInformation("VR: controller actions attached (extensions: {0})", String.Join(", ", EnabledExtensions));
            }
            catch (OpenXRException error)
            {
                Trace.TraceWarning("VR: the controllers will not work: {0}", error.Message);
            }
        }

        ulong StringToPath(string path)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(path + "\0");
            fixed (byte* p = bytes)
            {
                ulong result;
                OpenXR.Check(OpenXR.xrStringToPath(Instance, p, out result), "xrStringToPath(" + path + ")");
                return result;
            }
        }

        IntPtr CreateAction(string name, string localizedName, XrActionType type)
        {
            fixed (ulong* paths = HandPaths)
            {
                var info = new XrActionCreateInfo { Type = XrStructureType.ActionCreateInfo, ActionType = type, CountSubactionPaths = HandCount, SubactionPaths = paths };
                OpenXR.FillFixed(info.ActionName, 64, name);
                OpenXR.FillFixed(info.LocalizedActionName, 128, localizedName);
                IntPtr action;
                OpenXR.Check(OpenXR.xrCreateAction(ActionSet, &info, out action), "xrCreateAction(" + name + ")");
                return action;
            }
        }

        XrActionSuggestedBinding Bind(IntPtr action, string path)
        {
            return new XrActionSuggestedBinding { Action = action, Binding = StringToPath(path) };
        }

        void SuggestBindings(string profile, XrActionSuggestedBinding[] bindings)
        {
            fixed (XrActionSuggestedBinding* p = bindings)
            {
                var info = new XrInteractionProfileSuggestedBinding
                {
                    Type = XrStructureType.InteractionProfileSuggestedBinding,
                    InteractionProfile = StringToPath(profile),
                    CountSuggestedBindings = (uint)bindings.Length,
                    SuggestedBindings = p,
                };
                var result = OpenXR.xrSuggestInteractionProfileBindings(Instance, &info);
                if (result < 0)
                    Trace.TraceInformation("VR: the runtime declined the bindings for {0}: {1}", profile, result);
                else
                    Trace.TraceInformation("VR: bindings suggested for {0}", profile);
            }
        }

        string PathToString(ulong path)
        {
            var buffer = new byte[256];
            uint count;
            fixed (byte* p = buffer)
                if (OpenXR.xrPathToString(Instance, path, (uint)buffer.Length, out count, p) < 0 || count == 0)
                    return "?";
            return System.Text.Encoding.ASCII.GetString(buffer, 0, (int)count - 1);
        }

        void LogInteractionProfiles()
        {
            if (!ActionsAvailable)
                return;
            for (var hand = 0; hand < HandCount; hand++)
            {
                var state = new XrInteractionProfileState { Type = XrStructureType.InteractionProfileState };
                var result = OpenXR.xrGetCurrentInteractionProfile(Session, HandPaths[hand], &state);
                var profile = result < 0 ? null : state.InteractionProfile == 0 ? null : PathToString(state.InteractionProfile);
                HandProfiles[hand] = profile;
                Trace.TraceInformation("VR: {0} controller profile: {1}{2}", hand == LeftHand ? "left" : "right", profile ?? (result < 0 ? result.ToString() : "none"),
                    profile == SimpleControllerProfile ? (HandTrackingAllowed ? " (hand tracking)" : " (hand tracking; ignored)") : "");
            }
        }

        void SyncActions(long time)
        {
            if (!ActionsAvailable)
                return;
            var active = new XrActiveActionSet { ActionSet = ActionSet };
            var syncInfo = new XrActionsSyncInfo { Type = XrStructureType.ActionsSyncInfo, CountActiveActionSets = 1, ActiveActionSets = &active };
            var result = OpenXR.xrSyncActions(Session, &syncInfo);
            if (result < 0)
            {
                Trace.TraceWarning("VR: xrSyncActions failed: {0}; the controllers will not work.", result);
                ActionsAvailable = false;
                return;
            }
            if (result == XrResult.Success && !ProfilesLogged)
            {
                ProfilesLogged = true;
                LogInteractionProfiles();
            }
            if (result != XrResult.Success && !SyncWarned)
            {
                // SessionNotFocused: another application (an overlay, the system menu) has the controllers.
                SyncWarned = true;
                Trace.TraceInformation("VR: xrSyncActions says {0}; the controllers are idle until the session is focused.", result);
            }
            else if (result == XrResult.Success && SyncWarned)
            {
                SyncWarned = false;
                Trace.TraceInformation("VR: the session is focused; the controllers are live.");
            }

            for (var hand = 0; hand < HandCount; hand++)
            {
                var state = new VRHand();
                var getInfo = new XrActionStateGetInfo { Type = XrStructureType.ActionStateGetInfo, Action = AimAction, SubactionPath = HandPaths[hand] };
                var pose = new XrActionStatePose { Type = XrStructureType.ActionStatePose };
                if (OpenXR.xrGetActionStatePose(Session, &getInfo, &pose) >= 0 && pose.IsActive != 0)
                {
                    state.Connected = true;
                    var location = new XrSpaceLocation { Type = XrStructureType.SpaceLocation };
                    const XrSpaceLocationFlags valid = XrSpaceLocationFlags.OrientationValid | XrSpaceLocationFlags.PositionValid;
                    if (OpenXR.xrLocateSpace(AimSpaces[hand], Space, time, &location) >= 0 && (location.LocationFlags & valid) == valid)
                    {
                        state.Tracked = true;
                        state.Pose = PoseToMatrix(location.Pose);
                    }
                }
                state.Trigger = GetFloat(TriggerAction, hand);
                state.Grip = GetFloat(GripAction, hand);
                state.Stick = GetVector2(StickAction, hand);
                state.PrimaryButton = GetBoolean(PrimaryAction, hand);
                state.SecondaryButton = GetBoolean(SecondaryAction, hand);
                state.MenuButton = GetBoolean(MenuAction, hand);
                state.Idle = IsIdle(hand, state);
                state.Profile = HandProfiles[hand];
                state.Enabled = ControllersEnabled && (HandTrackingAllowed || state.Profile != SimpleControllerProfile);
                lock (PoseLock)
                    hands[hand] = state;
                if (state.Tracked && !HandSeen[hand])
                {
                    HandSeen[hand] = true;
                    Trace.TraceInformation("VR: the {0} controller is tracked.", hand == LeftHand ? "left" : "right");
                }

                var milliseconds = System.Threading.Interlocked.Exchange(ref HapticMilliseconds[hand], 0);
                if (milliseconds > 0)
                {
                    var hapticInfo = new XrHapticActionInfo { Type = XrStructureType.HapticActionInfo, Action = HapticAction, SubactionPath = HandPaths[hand] };
                    var vibration = new XrHapticVibration { Type = XrStructureType.HapticVibration, Duration = milliseconds * 1000000L, Frequency = 0, Amplitude = 0.5f };
                    OpenXR.xrApplyHapticFeedback(Session, &hapticInfo, &vibration);
                }
            }
        }

        /// <summary>A controller counts as put down when it has stayed within a couple of centimetres and degrees of its resting pose with nothing pressed for a few seconds.</summary>
        bool IsIdle(int hand, VRHand state)
        {
            var now = DateTime.UtcNow;
            var pressed = state.TriggerDown || state.Grip > 0.5f || state.PrimaryButton || state.SecondaryButton || state.MenuButton || state.Stick.LengthSquared() > 0.25f;
            if (!state.Tracked)
                return true;
            var moved = (state.Pose.Translation - RestPose[hand].Translation).Length() > IdleMoveMetres
                || Vector3.Dot(Vector3.TransformNormal(Vector3.Forward, state.Pose), Vector3.TransformNormal(Vector3.Forward, RestPose[hand])) < Math.Cos(IdleTurnRadians);
            if (moved || pressed || (!pressed && WasPressed[hand]))
            {
                RestPose[hand] = state.Pose;
                LastMoved[hand] = now;
            }
            WasPressed[hand] = pressed;
            return (now - LastMoved[hand]).TotalSeconds > IdleAfterSeconds;
        }

        float GetFloat(IntPtr action, int hand)
        {
            var getInfo = new XrActionStateGetInfo { Type = XrStructureType.ActionStateGetInfo, Action = action, SubactionPath = HandPaths[hand] };
            var state = new XrActionStateFloat { Type = XrStructureType.ActionStateFloat };
            return OpenXR.xrGetActionStateFloat(Session, &getInfo, &state) >= 0 && state.IsActive != 0 ? state.CurrentState : 0;
        }

        bool GetBoolean(IntPtr action, int hand)
        {
            var getInfo = new XrActionStateGetInfo { Type = XrStructureType.ActionStateGetInfo, Action = action, SubactionPath = HandPaths[hand] };
            var state = new XrActionStateBoolean { Type = XrStructureType.ActionStateBoolean };
            return OpenXR.xrGetActionStateBoolean(Session, &getInfo, &state) >= 0 && state.IsActive != 0 && state.CurrentState != 0;
        }

        Vector2 GetVector2(IntPtr action, int hand)
        {
            var getInfo = new XrActionStateGetInfo { Type = XrStructureType.ActionStateGetInfo, Action = action, SubactionPath = HandPaths[hand] };
            var state = new XrActionStateVector2f { Type = XrStructureType.ActionStateVector2f };
            return OpenXR.xrGetActionStateVector2f(Session, &getInfo, &state) >= 0 && state.IsActive != 0 ? new Vector2(state.CurrentState.X, state.CurrentState.Y) : Vector2.Zero;
        }
        #endregion

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
                    case XrStructureType.EventDataInteractionProfileChanged:
                        LogInteractionProfiles();
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
            for (var hand = 0; hand < HandCount; hand++)
                if (AimSpaces[hand] != IntPtr.Zero)
                {
                    OpenXR.xrDestroySpace(AimSpaces[hand]);
                    AimSpaces[hand] = IntPtr.Zero;
                }
            if (ActionSet != IntPtr.Zero)
            {
                OpenXR.xrDestroyActionSet(ActionSet);
                ActionSet = IntPtr.Zero;
            }
            ActionsAvailable = false;
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
    /// One controller's state as of the last headset frame. <see cref="Pose"/> is the aim pose in the seat
    /// frame (XNA axes; the controller points along its -Z), so world-from-hand is Pose * Invert(seatView).
    /// </summary>
    public sealed class VRHand
    {
        /// <summary>The runtime has a controller for this hand (it may be out of view or asleep).</summary>
        public bool Connected;
        /// <summary><see cref="Pose"/> is valid this frame.</summary>
        public bool Tracked;
        public Matrix Pose = Matrix.Identity;
        /// <summary>0..1; the index trigger, or the select button on simple controllers.</summary>
        public float Trigger;
        public float Grip;
        public Vector2 Stick;
        /// <summary>A/X.</summary>
        public bool PrimaryButton;
        /// <summary>B/Y.</summary>
        public bool SecondaryButton;
        /// <summary>The menu button (left controller on Touch controllers).</summary>
        public bool MenuButton;
        /// <summary>Put down: not moved and nothing pressed for <see cref="VRSession.IdleAfterSeconds"/>.</summary>
        public bool Idle;
        /// <summary>The controllers are switched on (see <see cref="VRSession.ControllersEnabled"/>) and this is a real controller, not tracked hands.</summary>
        public bool Enabled;
        /// <summary>The interaction profile the runtime is using for this hand, or null.</summary>
        public string Profile;

        /// <summary>Tracked, in use and switched on: the only state in which a controller does anything.</summary>
        public bool Active { get { return Tracked && Enabled && !Idle; } }

        public bool TriggerDown { get { return Trigger > 0.6f; } }

        public VRHand Clone()
        {
            return (VRHand)MemberwiseClone();
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

        internal VRSessionStarter(GraphicsDevice graphicsDevice, UserSettings settings)
        {
            Started = DateTime.Now;
            var thread = new System.Threading.Thread(() => Run(graphicsDevice, settings)) { Name = "OpenXR Startup", IsBackground = true };
            thread.Start();
        }

        void Run(GraphicsDevice graphicsDevice, UserSettings settings)
        {
            VRSession session = null;
            try
            {
                session = new VRSession(graphicsDevice, settings);
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
