Khronos OpenXR loader, Windows x64, from OpenXR-SDK release-1.1.65
(https://github.com/KhronosGroup/OpenXR-SDK/releases/download/release-1.1.65/OpenXR.Loader.1.1.65.nupkg,
native/x64/release/bin/openxr_loader.dll). Licensed Apache-2.0 OR MIT.

RunActivity copies it to Program/Native/X64 next to OpenAL32.dll; the loader finds the
active runtime through HKLM\SOFTWARE\Khronos\OpenXR\1\ActiveRuntime (under Proton that is
wineopenxr64.json, which hands the session to the Linux OpenXR runtime: WiVRn, Monado or SteamVR).
