# Driving from a headset (OpenXR)

Open Rails can put the driver's seat in a head-mounted display through OpenXR. The headset
gets a stereo view of the world from the active camera; the window keeps showing the left eye
as a mirror with the usual 2D overlays (HUD, popup windows, labels) on top of it.

## Turning it on

* Set the `VR` option (`-vr` on the RunActivity command line, or `VR=bool:True` in the
  settings), or set the environment variable `OPENRAILS_VR=1`.
* The runtime is asked for a headset in the background while the game starts, so the game
  runs flat until the runtime answers. Some runtimes (WiVRn) answer only once a headset is
  connected; with others the answer is immediate. If there is no runtime or no headset, a
  warning naming the reason goes to the log and the game stays flat.
* Vertical sync is turned off while VR is requested: the headset paces the frames.
* The seat is where the camera is: in the 3D cab, the cab view's head position
  (`ORTS3DCabHeadPos`). The head is put there automatically on the first frame the headset
  shows, and `Alt+V` (`GameVRRecenter`) does it again from the current head position and yaw.
* The 3D cab is preferred over the 2D cab whenever a headset is in use.

## What it needs

* The WindowsDX (SharpDX / Direct3D 11) build of MonoGame, which is what Open Rails ships.
  The session shares MonoGame's D3D11 device through `XR_KHR_D3D11_enable`; the eyes are drawn
  into ordinary render targets and copied into the runtime's swapchain.
* `openxr_loader.dll`, the Khronos loader, next to the other native libraries in
  `Native/X64`. The copy in `Source/3rdPartyLibs/openxr` is from the OpenXR-SDK 1.1.65
  release (Apache-2.0 OR MIT; see the README there) and the project file copies it.
* A runtime that supports D3D11 swapchains: on Windows any of the usual ones; under Proton,
  Wine's `wineopenxr` translates to the Linux runtime (see below).

## How it is put together

* `OpenXR.cs` is the P/Invoke binding: the handful of OpenXR 1.0 calls and structures the
  session uses, plus `XR_KHR_D3D11_enable`.
* `VRSession.cs` owns the instance, system, session, the seated `LOCAL` reference space and the
  two swapchains. Per frame on the render thread: `BeginFrame` (events, `xrWaitFrame`,
  `xrBeginFrame`, `xrLocateViews`), one `SubmitEye` per eye, `EndFrame`. `VRSessionStarter`
  creates the session on its own thread, because `xrCreateInstance` may block until a headset
  turns up.
* `RenderProcess` starts the session when VR is requested and adopts it when it arrives;
  `RenderFrame.DrawVR` draws the two eyes with each eye's own view and off-centre projection,
  then the mirror and overlays into the window.
* `Camera` computes the seat view (`XnaSeatView`); the tracked head pose is applied on top for
  culling, sound and the mirror (`XnaView`), and the culling frustum is opened to the headset's
  field of view. While an eye is drawn, the render thread swaps the sky, distant-mountain and
  main projections for that eye's (thread-static, so the updater thread is unaffected).

## Under Proton (Linux)

Tested with GE-Proton 11 through umu-launcher and WiVRn:

* Proton's `wineopenxr.dll` is the runtime the Windows loader finds (GE-Proton registers it in
  the prefix). It calls the Linux OpenXR loader inside the Steam runtime container, which
  reads the *active* runtime, `~/.config/openxr/1/active_runtime.json`; pressure-vessel imports
  that runtime and the WiVRn socket into the container (umu sets
  `PRESSURE_VESSEL_IMPORT_OPENXR_1_RUNTIMES=1`).
* `wivrn-server` writes `active_runtime.json` when a headset connects and removes it when it
  disconnects, so connect the headset before starting the game (or run the server with
  `--early-active-runtime`).
* Do not set `XR_RUNTIME_JSON` to a host path: it is passed into the container unchanged, where
  the path does not exist, and the loader then reports `XR_ERROR_RUNTIME_UNAVAILABLE`.
* Wine's own `+openxr` debug channel (`PROTON_LOG=+openxr`) shows the calls going through
  `wineopenxr`.
