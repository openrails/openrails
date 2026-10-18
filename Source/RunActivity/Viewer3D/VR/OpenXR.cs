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

// The subset of the OpenXR 1.0 C API that the VR driver's cab needs: instance, system, a D3D11
// session, reference spaces, swapchains, the frame loop and events. Everything is a plain
// P/Invoke into the Khronos loader (openxr_loader.dll, shipped in Native\X64), so there is no
// managed wrapper library to carry. Layouts follow openxr.h on x64; every struct starts with
// the (type, next) header. Handles are pointer-sized, XrSystemId/XrTime/XrVersion are 64-bit.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Orts.Viewer3D.VR
{
    enum XrResult
    {
        Success = 0,
        TimeoutExpired = 1,
        SessionLossPending = 3,
        EventUnavailable = 4,
        SpaceBoundsUnavailable = 7,
        SessionNotFocused = 8,
        FrameDiscarded = 9,
        ErrorValidationFailure = -1,
        ErrorRuntimeFailure = -2,
        ErrorOutOfMemory = -3,
        ErrorApiVersionUnsupported = -4,
        ErrorInitializationFailed = -6,
        ErrorFunctionUnsupported = -7,
        ErrorFeatureUnsupported = -8,
        ErrorExtensionNotPresent = -9,
        ErrorLimitReached = -10,
        ErrorSizeInsufficient = -11,
        ErrorHandleInvalid = -12,
        ErrorInstanceLost = -13,
        ErrorSessionRunning = -14,
        ErrorSessionNotRunning = -16,
        ErrorSessionLost = -17,
        ErrorSystemInvalid = -18,
        ErrorSwapchainFormatUnsupported = -26,
        ErrorSessionNotReady = -28,
        ErrorSessionNotStopping = -29,
        ErrorTimeInvalid = -30,
        ErrorReferenceSpaceUnsupported = -31,
        ErrorFormFactorUnsupported = -34,
        ErrorFormFactorUnavailable = -35,
        ErrorCallOrderInvalid = -37,
        ErrorGraphicsDeviceInvalid = -38,
        ErrorPoseInvalid = -39,
        ErrorViewConfigurationTypeUnsupported = -41,
        ErrorEnvironmentBlendModeUnsupported = -42,
        ErrorGraphicsRequirementsCallMissing = -50,
        ErrorRuntimeUnavailable = -51,
    }

    enum XrStructureType
    {
        Unknown = 0,
        ApiLayerProperties = 1,
        ExtensionProperties = 2,
        InstanceCreateInfo = 3,
        SystemGetInfo = 4,
        SystemProperties = 5,
        ViewLocateInfo = 6,
        View = 7,
        SessionCreateInfo = 8,
        SwapchainCreateInfo = 9,
        SessionBeginInfo = 10,
        ViewState = 11,
        FrameEndInfo = 12,
        EventDataBuffer = 16,
        EventDataInstanceLossPending = 17,
        EventDataSessionStateChanged = 18,
        InstanceProperties = 32,
        FrameWaitInfo = 33,
        CompositionLayerProjection = 35,
        ReferenceSpaceCreateInfo = 37,
        EventDataReferenceSpaceChangePending = 40,
        ViewConfigurationView = 41,
        SpaceLocation = 42,
        FrameState = 44,
        FrameBeginInfo = 46,
        CompositionLayerProjectionView = 48,
        EventDataEventsLost = 49,
        SwapchainImageAcquireInfo = 55,
        SwapchainImageWaitInfo = 56,
        SwapchainImageReleaseInfo = 57,
        HapticVibration = 13,
        ActionStateBoolean = 23,
        ActionStateFloat = 24,
        ActionStateVector2f = 25,
        ActionStatePose = 27,
        ActionSetCreateInfo = 28,
        ActionCreateInfo = 29,
        ActionSpaceCreateInfo = 38,
        InteractionProfileSuggestedBinding = 51,
        EventDataInteractionProfileChanged = 52,
        InteractionProfileState = 53,
        ActionStateGetInfo = 58,
        HapticActionInfo = 59,
        SessionActionSetAttachInfo = 60,
        ActionsSyncInfo = 61,
        GraphicsRequirementsD3D11KHR = 1000027002,
        GraphicsBindingD3D11KHR = 1000027000,
        SwapchainImageD3D11KHR = 1000027001,
    }

    enum XrFormFactor { HeadMountedDisplay = 1, HandheldDisplay = 2 }
    enum XrActionType { BooleanInput = 1, FloatInput = 2, Vector2fInput = 3, PoseInput = 4, VibrationOutput = 100 }
    enum XrViewConfigurationType { PrimaryMono = 1, PrimaryStereo = 2 }
    enum XrReferenceSpaceType { View = 1, Local = 2, Stage = 3 }
    enum XrEnvironmentBlendMode { Opaque = 1, Additive = 2, AlphaBlend = 3 }

    enum XrSessionState
    {
        Unknown = 0,
        Idle = 1,
        Ready = 2,
        Synchronized = 3,
        Visible = 4,
        Focused = 5,
        Stopping = 6,
        LossPending = 7,
        Exiting = 8,
    }

    [Flags]
    enum XrSwapchainUsageFlags : ulong
    {
        ColorAttachment = 0x01,
        DepthStencilAttachment = 0x02,
        UnorderedAccess = 0x04,
        TransferSrc = 0x08,
        TransferDst = 0x10,
        Sampled = 0x20,
        MutableFormat = 0x40,
    }

    [Flags]
    enum XrViewStateFlags : ulong
    {
        OrientationValid = 0x1,
        PositionValid = 0x2,
        OrientationTracked = 0x4,
        PositionTracked = 0x8,
    }

    [Flags]
    enum XrSpaceLocationFlags : ulong
    {
        OrientationValid = 0x1,
        PositionValid = 0x2,
        OrientationTracked = 0x4,
        PositionTracked = 0x8,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrVector3f { public float X, Y, Z; }

    [StructLayout(LayoutKind.Sequential)]
    struct XrVector2f { public float X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    struct XrQuaternionf { public float X, Y, Z, W; }

    [StructLayout(LayoutKind.Sequential)]
    struct XrPosef
    {
        public XrQuaternionf Orientation;
        public XrVector3f Position;

        public static XrPosef Identity
        {
            get
            {
                var p = new XrPosef();
                p.Orientation.W = 1;
                return p;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrFovf { public float AngleLeft, AngleRight, AngleUp, AngleDown; }

    [StructLayout(LayoutKind.Sequential)]
    struct XrOffset2Di { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    struct XrExtent2Di { public int Width, Height; }

    [StructLayout(LayoutKind.Sequential)]
    struct XrRect2Di { public XrOffset2Di Offset; public XrExtent2Di Extent; }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrExtensionProperties
    {
        public XrStructureType Type;
        public IntPtr Next;
        public fixed byte ExtensionName[128];
        public uint ExtensionVersion;

        public string Name { get { fixed (byte* p = ExtensionName) return OpenXR.FixedString(p, 128); } }
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrApplicationInfo
    {
        public fixed byte ApplicationName[128];
        public uint ApplicationVersion;
        public fixed byte EngineName[128];
        public uint EngineVersion;
        public ulong ApiVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrInstanceCreateInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public ulong CreateFlags;
        public XrApplicationInfo ApplicationInfo;
        public uint EnabledApiLayerCount;
        public byte** EnabledApiLayerNames;
        public uint EnabledExtensionCount;
        public byte** EnabledExtensionNames;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrInstanceProperties
    {
        public XrStructureType Type;
        public IntPtr Next;
        public ulong RuntimeVersion;
        public fixed byte RuntimeName[128];

        public string Name { get { fixed (byte* p = RuntimeName) return OpenXR.FixedString(p, 128); } }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSystemGetInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrFormFactor FormFactor;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSystemGraphicsProperties
    {
        public uint MaxSwapchainImageHeight;
        public uint MaxSwapchainImageWidth;
        public uint MaxLayerCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSystemTrackingProperties
    {
        public uint OrientationTracking;
        public uint PositionTracking;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrSystemProperties
    {
        public XrStructureType Type;
        public IntPtr Next;
        public ulong SystemId;
        public uint VendorId;
        public fixed byte SystemName[256];
        public XrSystemGraphicsProperties GraphicsProperties;
        public XrSystemTrackingProperties TrackingProperties;

        public string Name { get { fixed (byte* p = SystemName) return OpenXR.FixedString(p, 256); } }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrLuid { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    struct XrGraphicsRequirementsD3D11KHR
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrLuid AdapterLuid;
        public int MinFeatureLevel; // D3D_FEATURE_LEVEL
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrGraphicsBindingD3D11KHR
    {
        public XrStructureType Type;
        public IntPtr Next;
        public IntPtr Device; // ID3D11Device*
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSessionCreateInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public ulong CreateFlags;
        public ulong SystemId;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSessionBeginInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrViewConfigurationType PrimaryViewConfigurationType;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrReferenceSpaceCreateInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrReferenceSpaceType ReferenceSpaceType;
        public XrPosef PoseInReferenceSpace;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSpaceLocation
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrSpaceLocationFlags LocationFlags;
        public XrPosef Pose;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrViewConfigurationView
    {
        public XrStructureType Type;
        public IntPtr Next;
        public uint RecommendedImageRectWidth;
        public uint MaxImageRectWidth;
        public uint RecommendedImageRectHeight;
        public uint MaxImageRectHeight;
        public uint RecommendedSwapchainSampleCount;
        public uint MaxSwapchainSampleCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSwapchainCreateInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public ulong CreateFlags;
        public XrSwapchainUsageFlags UsageFlags;
        public long Format;
        public uint SampleCount;
        public uint Width;
        public uint Height;
        public uint FaceCount;
        public uint ArraySize;
        public uint MipCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSwapchainImageD3D11KHR
    {
        public XrStructureType Type;
        public IntPtr Next;
        public IntPtr Texture; // ID3D11Texture2D*
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSwapchainImageAcquireInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSwapchainImageWaitInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public long Timeout;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSwapchainImageReleaseInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrEventDataBuffer
    {
        public XrStructureType Type;
        public IntPtr Next;
        public fixed byte Varying[4000];
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrEventDataSessionStateChanged
    {
        public XrStructureType Type;
        public IntPtr Next;
        public IntPtr Session;
        public XrSessionState State;
        public long Time;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrFrameWaitInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrFrameState
    {
        public XrStructureType Type;
        public IntPtr Next;
        public long PredictedDisplayTime;
        public long PredictedDisplayPeriod;
        public uint ShouldRender;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrFrameBeginInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrViewLocateInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrViewConfigurationType ViewConfigurationType;
        public long DisplayTime;
        public IntPtr Space;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrViewState
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrViewStateFlags ViewStateFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrView
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrPosef Pose;
        public XrFovf Fov;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrSwapchainSubImage
    {
        public IntPtr Swapchain;
        public XrRect2Di ImageRect;
        public uint ImageArrayIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrCompositionLayerProjectionView
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrPosef Pose;
        public XrFovf Fov;
        public XrSwapchainSubImage SubImage;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrCompositionLayerProjection
    {
        public XrStructureType Type;
        public IntPtr Next;
        public ulong LayerFlags;
        public IntPtr Space;
        public uint ViewCount;
        public XrCompositionLayerProjectionView* Views;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrFrameEndInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public long DisplayTime;
        public XrEnvironmentBlendMode EnvironmentBlendMode;
        public uint LayerCount;
        public IntPtr* Layers; // XrCompositionLayerBaseHeader* const*
    }

    // Actions (controller input), OpenXR 1.0 core.

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrActionSetCreateInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public fixed byte ActionSetName[64];
        public fixed byte LocalizedActionSetName[128];
        public uint Priority;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrActionCreateInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public fixed byte ActionName[64];
        public XrActionType ActionType;
        public uint CountSubactionPaths;
        public ulong* SubactionPaths;
        public fixed byte LocalizedActionName[128];
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrActionSuggestedBinding
    {
        public IntPtr Action;
        public ulong Binding;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrInteractionProfileSuggestedBinding
    {
        public XrStructureType Type;
        public IntPtr Next;
        public ulong InteractionProfile;
        public uint CountSuggestedBindings;
        public XrActionSuggestedBinding* SuggestedBindings;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrSessionActionSetAttachInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public uint CountActionSets;
        public IntPtr* ActionSets;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrActionSpaceCreateInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public IntPtr Action;
        public ulong SubactionPath;
        public XrPosef PoseInActionSpace;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrActiveActionSet
    {
        public IntPtr ActionSet;
        public ulong SubactionPath;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XrActionsSyncInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public uint CountActiveActionSets;
        public XrActiveActionSet* ActiveActionSets;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrActionStateGetInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public IntPtr Action;
        public ulong SubactionPath;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrActionStateBoolean
    {
        public XrStructureType Type;
        public IntPtr Next;
        public uint CurrentState;
        public uint ChangedSinceLastSync;
        public long LastChangeTime;
        public uint IsActive;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrActionStateFloat
    {
        public XrStructureType Type;
        public IntPtr Next;
        public float CurrentState;
        public uint ChangedSinceLastSync;
        public long LastChangeTime;
        public uint IsActive;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrActionStateVector2f
    {
        public XrStructureType Type;
        public IntPtr Next;
        public XrVector2f CurrentState;
        public uint ChangedSinceLastSync;
        public long LastChangeTime;
        public uint IsActive;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrActionStatePose
    {
        public XrStructureType Type;
        public IntPtr Next;
        public uint IsActive;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrInteractionProfileState
    {
        public XrStructureType Type;
        public IntPtr Next;
        public ulong InteractionProfile;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrEventDataInteractionProfileChanged
    {
        public XrStructureType Type;
        public IntPtr Next;
        public IntPtr Session;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrHapticActionInfo
    {
        public XrStructureType Type;
        public IntPtr Next;
        public IntPtr Action;
        public ulong SubactionPath;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XrHapticVibration
    {
        public XrStructureType Type;
        public IntPtr Next;
        public long Duration;
        public float Frequency;
        public float Amplitude;
    }

    static unsafe class OpenXR
    {
        const string Loader = "openxr_loader.dll";

        public const long InfiniteDuration = 0x7fffffffffffffff;
        public const string KhrD3D11Enable = "XR_KHR_D3D11_enable";
        public const string MetaTouchControllerPlus = "XR_META_touch_controller_plus";
        public const string FbTouchControllerPro = "XR_FB_touch_controller_pro";

        public static ulong MakeVersion(ushort major, ushort minor, uint patch)
        {
            return ((ulong)major << 48) | ((ulong)minor << 32) | patch;
        }

        public static string VersionString(ulong version)
        {
            return String.Format("{0}.{1}.{2}", version >> 48, (version >> 32) & 0xffff, version & 0xffffffff);
        }

        [DllImport(Loader)] public static extern XrResult xrGetInstanceProcAddr(IntPtr instance, byte* name, out IntPtr function);
        [DllImport(Loader)] public static extern XrResult xrEnumerateInstanceExtensionProperties(byte* layerName, uint capacity, out uint count, XrExtensionProperties* properties);
        [DllImport(Loader)] public static extern XrResult xrCreateInstance(XrInstanceCreateInfo* createInfo, out IntPtr instance);
        [DllImport(Loader)] public static extern XrResult xrDestroyInstance(IntPtr instance);
        [DllImport(Loader)] public static extern XrResult xrGetInstanceProperties(IntPtr instance, XrInstanceProperties* properties);
        [DllImport(Loader)] public static extern XrResult xrResultToString(IntPtr instance, XrResult value, byte* buffer);
        [DllImport(Loader)] public static extern XrResult xrGetSystem(IntPtr instance, XrSystemGetInfo* getInfo, out ulong systemId);
        [DllImport(Loader)] public static extern XrResult xrGetSystemProperties(IntPtr instance, ulong systemId, XrSystemProperties* properties);
        [DllImport(Loader)] public static extern XrResult xrEnumerateViewConfigurationViews(IntPtr instance, ulong systemId, XrViewConfigurationType viewConfigurationType, uint capacity, out uint count, XrViewConfigurationView* views);
        [DllImport(Loader)] public static extern XrResult xrCreateSession(IntPtr instance, XrSessionCreateInfo* createInfo, out IntPtr session);
        [DllImport(Loader)] public static extern XrResult xrDestroySession(IntPtr session);
        [DllImport(Loader)] public static extern XrResult xrBeginSession(IntPtr session, XrSessionBeginInfo* beginInfo);
        [DllImport(Loader)] public static extern XrResult xrEndSession(IntPtr session);
        [DllImport(Loader)] public static extern XrResult xrRequestExitSession(IntPtr session);
        [DllImport(Loader)] public static extern XrResult xrCreateReferenceSpace(IntPtr session, XrReferenceSpaceCreateInfo* createInfo, out IntPtr space);
        [DllImport(Loader)] public static extern XrResult xrDestroySpace(IntPtr space);
        [DllImport(Loader)] public static extern XrResult xrLocateSpace(IntPtr space, IntPtr baseSpace, long time, XrSpaceLocation* location);
        [DllImport(Loader)] public static extern XrResult xrEnumerateSwapchainFormats(IntPtr session, uint capacity, out uint count, long* formats);
        [DllImport(Loader)] public static extern XrResult xrCreateSwapchain(IntPtr session, XrSwapchainCreateInfo* createInfo, out IntPtr swapchain);
        [DllImport(Loader)] public static extern XrResult xrDestroySwapchain(IntPtr swapchain);
        [DllImport(Loader)] public static extern XrResult xrEnumerateSwapchainImages(IntPtr swapchain, uint capacity, out uint count, XrSwapchainImageD3D11KHR* images);
        [DllImport(Loader)] public static extern XrResult xrAcquireSwapchainImage(IntPtr swapchain, XrSwapchainImageAcquireInfo* acquireInfo, out uint index);
        [DllImport(Loader)] public static extern XrResult xrWaitSwapchainImage(IntPtr swapchain, XrSwapchainImageWaitInfo* waitInfo);
        [DllImport(Loader)] public static extern XrResult xrReleaseSwapchainImage(IntPtr swapchain, XrSwapchainImageReleaseInfo* releaseInfo);
        [DllImport(Loader)] public static extern XrResult xrPollEvent(IntPtr instance, XrEventDataBuffer* eventData);
        [DllImport(Loader)] public static extern XrResult xrWaitFrame(IntPtr session, XrFrameWaitInfo* frameWaitInfo, XrFrameState* frameState);
        [DllImport(Loader)] public static extern XrResult xrBeginFrame(IntPtr session, XrFrameBeginInfo* frameBeginInfo);
        [DllImport(Loader)] public static extern XrResult xrEndFrame(IntPtr session, XrFrameEndInfo* frameEndInfo);
        [DllImport(Loader)] public static extern XrResult xrLocateViews(IntPtr session, XrViewLocateInfo* viewLocateInfo, XrViewState* viewState, uint capacity, out uint count, XrView* views);
        [DllImport(Loader)] public static extern XrResult xrStringToPath(IntPtr instance, byte* pathString, out ulong path);
        [DllImport(Loader)] public static extern XrResult xrPathToString(IntPtr instance, ulong path, uint capacity, out uint count, byte* buffer);
        [DllImport(Loader)] public static extern XrResult xrGetCurrentInteractionProfile(IntPtr session, ulong topLevelUserPath, XrInteractionProfileState* state);
        [DllImport(Loader)] public static extern XrResult xrCreateActionSet(IntPtr instance, XrActionSetCreateInfo* createInfo, out IntPtr actionSet);
        [DllImport(Loader)] public static extern XrResult xrDestroyActionSet(IntPtr actionSet);
        [DllImport(Loader)] public static extern XrResult xrCreateAction(IntPtr actionSet, XrActionCreateInfo* createInfo, out IntPtr action);
        [DllImport(Loader)] public static extern XrResult xrSuggestInteractionProfileBindings(IntPtr instance, XrInteractionProfileSuggestedBinding* suggestedBindings);
        [DllImport(Loader)] public static extern XrResult xrAttachSessionActionSets(IntPtr session, XrSessionActionSetAttachInfo* attachInfo);
        [DllImport(Loader)] public static extern XrResult xrCreateActionSpace(IntPtr session, XrActionSpaceCreateInfo* createInfo, out IntPtr space);
        [DllImport(Loader)] public static extern XrResult xrSyncActions(IntPtr session, XrActionsSyncInfo* syncInfo);
        [DllImport(Loader)] public static extern XrResult xrGetActionStateBoolean(IntPtr session, XrActionStateGetInfo* getInfo, XrActionStateBoolean* state);
        [DllImport(Loader)] public static extern XrResult xrGetActionStateFloat(IntPtr session, XrActionStateGetInfo* getInfo, XrActionStateFloat* state);
        [DllImport(Loader)] public static extern XrResult xrGetActionStateVector2f(IntPtr session, XrActionStateGetInfo* getInfo, XrActionStateVector2f* state);
        [DllImport(Loader)] public static extern XrResult xrGetActionStatePose(IntPtr session, XrActionStateGetInfo* getInfo, XrActionStatePose* state);
        [DllImport(Loader)] public static extern XrResult xrApplyHapticFeedback(IntPtr session, XrHapticActionInfo* hapticActionInfo, XrHapticVibration* hapticFeedback);

        // XR_KHR_D3D11_enable adds this one; it is only reachable through xrGetInstanceProcAddr.
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate XrResult GetD3D11GraphicsRequirementsKHR(IntPtr instance, ulong systemId, XrGraphicsRequirementsD3D11KHR* requirements);

        public static T GetProc<T>(IntPtr instance, string name) where T : class
        {
            var bytes = Encoding.ASCII.GetBytes(name + "\0");
            IntPtr fn;
            fixed (byte* p = bytes)
                Check(xrGetInstanceProcAddr(instance, p, out fn), "xrGetInstanceProcAddr(" + name + ")");
            return Marshal.GetDelegateForFunctionPointer(fn, typeof(T)) as T;
        }

        public static string FixedString(byte* p, int max)
        {
            var n = 0;
            while (n < max && p[n] != 0) n++;
            return Encoding.UTF8.GetString(p, n);
        }

        public static void FillFixed(byte* dest, int max, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var n = Math.Min(bytes.Length, max - 1);
            for (var i = 0; i < n; i++) dest[i] = bytes[i];
            dest[n] = 0;
        }

        public static bool Succeeded(XrResult result) { return result >= 0; }

        public static void Check(XrResult result, string call)
        {
            if (result < 0)
                throw new OpenXRException(result, call);
        }
    }

    public class OpenXRException : Exception
    {
        internal OpenXRException(XrResult result, string call)
            : base(String.Format("{0} failed: {1} ({2})", call, result, (int)result))
        {
        }
    }
}
