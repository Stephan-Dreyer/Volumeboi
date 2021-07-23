﻿using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable InconsistentNaming













namespace AudioController
{
    /// <summary>
    /// Controls audio using the Windows CoreAudio API
    /// from: http://stackoverflow.com/questions/14306048/controling-volume-mixer
    /// and: http://netcoreaudio.codeplex.com/
    /// </summary>
    public static class AudioManager
    {
        #region Master Volume Manipulation

        /// <summary>
        /// Gets the current master volume in scalar values (percentage)
        /// </summary>
        /// <returns>-1 in case of an error, if successful the value will be between 0 and 100</returns>
        public static float GetMasterVolume()
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return -1;

                float volumeLevel;
                masterVol.GetMasterVolumeLevelScalar(out volumeLevel);
                return volumeLevel * 100;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Gets the mute state of the master volume. 
        /// While the volume can be muted the <see cref="GetMasterVolume"/> will still return the pre-muted volume value.
        /// </summary>
        /// <returns>false if not muted, true if volume is muted</returns>
        public static bool GetMasterVolumeMute()
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return false;

                bool isMuted;
                masterVol.GetMute(out isMuted);
                return isMuted;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Sets the master volume to a specific level
        /// </summary>
        /// <param name="newLevel">Value between 0 and 100 indicating the desired scalar value of the volume</param>
        public static void SetMasterVolume(float newLevel)
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return;

                masterVol.SetMasterVolumeLevelScalar(newLevel / 100, Guid.Empty);
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Increments or decrements the current volume level by the <see cref="stepAmount"/>.
        /// </summary>
        /// <param name="stepAmount">Value between -100 and 100 indicating the desired step amount. Use negative numbers to decrease
        /// the volume and positive numbers to increase it.</param>
        /// <returns>the new volume level assigned</returns>
        public static float StepMasterVolume(float stepAmount)
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return -1;

                float stepAmountScaled = stepAmount / 100;

                // Get the level
                float volumeLevel;
                masterVol.GetMasterVolumeLevelScalar(out volumeLevel);

                // Calculate the new level
                float newLevel = volumeLevel + stepAmountScaled;
                newLevel = Math.Min(1, newLevel);
                newLevel = Math.Max(0, newLevel);

                masterVol.SetMasterVolumeLevelScalar(newLevel, Guid.Empty);

                // Return the new volume level that was set
                return newLevel * 100;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Mute or unmute the master volume
        /// </summary>
        /// <param name="isMuted">true to mute the master volume, false to unmute</param>
        public static void SetMasterVolumeMute(bool isMuted)
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return;

                masterVol.SetMute(isMuted, Guid.Empty);
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Switches between the master volume mute states depending on the current state
        /// </summary>
        /// <returns>the current mute state, true if the volume was muted, false if unmuted</returns>
        public static bool ToggleMasterVolumeMute()
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return false;

                bool isMuted;
                masterVol.GetMute(out isMuted);
                masterVol.SetMute(!isMuted, Guid.Empty);

                return !isMuted;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        private static IAudioEndpointVolume GetMasterVolumeObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IMMDevice speakers = null;
            try
            {
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
                object o;
                speakers.Activate(ref IID_IAudioEndpointVolume, 0, IntPtr.Zero, out o);
                IAudioEndpointVolume masterVol = (IAudioEndpointVolume)o;

                return masterVol;
            }
            finally
            {
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }

        #endregion

        #region Individual Application Volume Manipulation

        public static float? GetApplicationVolume(int pid)
        {
            {
                IMMDeviceEnumerator deviceEnumerator = null;
                IAudioSessionEnumerator sessionEnumerator = null;
                IAudioSessionManager2 mgr = null;
                IMMDevice speakers = null;
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                    // activate the session manager. we need the enumerator
                    Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                    object o;
                    speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                    mgr = (IAudioSessionManager2)o;

                    // enumerate sessions for on this device
                    mgr.GetSessionEnumerator(out sessionEnumerator);
                    int count;
                    sessionEnumerator.GetCount(out count);

                    // search for an audio session with the required process-id
                    ISimpleAudioVolume volumeControl = null;
                    float level;
                    for (int i = 0; i < count; ++i)
                    {
                        IAudioSessionControl2 ctl = null;
                        try
                        {
                            sessionEnumerator.GetSession(i, out ctl);

                            // NOTE: we could also use the app name from ctl.GetDisplayName()
                            int cpid;
                            ctl.GetProcessId(out cpid);

                            if (cpid == pid)
                            {
                                volumeControl = ctl as ISimpleAudioVolume;
                                volumeControl.GetMasterVolume(out level);
                                return level * 100;
                            }
                        }
                        finally
                        {
                            if (ctl != null) Marshal.ReleaseComObject(ctl);
                        }
                    }
                    return null;
                }
                finally
                {
                    if (speakers != null) Marshal.ReleaseComObject(speakers);
                    if (mgr != null) Marshal.ReleaseComObject(mgr);
                    if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
        }

        public static bool? GetApplicationMute(int pid)
        {
            {
                IMMDeviceEnumerator deviceEnumerator = null;
                IAudioSessionEnumerator sessionEnumerator = null;
                IAudioSessionManager2 mgr = null;
                IMMDevice speakers = null;
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                    // activate the session manager. we need the enumerator
                    Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                    object o;
                    speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                    mgr = (IAudioSessionManager2)o;

                    // enumerate sessions for on this device
                    mgr.GetSessionEnumerator(out sessionEnumerator);
                    int count;
                    sessionEnumerator.GetCount(out count);

                    // search for an audio session with the required process-id
                    ISimpleAudioVolume volumeControl = null;
                    bool mute;
                    for (int i = 0; i < count; ++i)
                    {
                        IAudioSessionControl2 ctl = null;
                        try
                        {
                            sessionEnumerator.GetSession(i, out ctl);

                            // NOTE: we could also use the app name from ctl.GetDisplayName()
                            int cpid;
                            ctl.GetProcessId(out cpid);

                            if (cpid == pid)
                            {
                                volumeControl = ctl as ISimpleAudioVolume;
                                volumeControl.GetMute(out mute);
                                return mute;
                            }
                        }
                        finally
                        {
                            if (ctl != null) Marshal.ReleaseComObject(ctl);
                        }
                    }
                    return null;
                }
                finally
                {
                    if (speakers != null) Marshal.ReleaseComObject(speakers);
                    if (mgr != null) Marshal.ReleaseComObject(mgr);
                    if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
        }

        public static void SetApplicationVolume(int pid, float? level)
        {
            if (level == null) return;
            {
                IMMDeviceEnumerator deviceEnumerator = null;
                IAudioSessionEnumerator sessionEnumerator = null;
                IAudioSessionManager2 mgr = null;
                IMMDevice speakers = null;
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                    // activate the session manager. we need the enumerator
                    Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                    object o;
                    speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                    mgr = (IAudioSessionManager2)o;

                    // enumerate sessions for on this device
                    mgr.GetSessionEnumerator(out sessionEnumerator);
                    int count;
                    sessionEnumerator.GetCount(out count);

                    // search for an audio session with the required process-id
                    ISimpleAudioVolume volumeControl = null;
                    for (int i = 0; i < count; ++i)
                    {
                        IAudioSessionControl2 ctl = null;
                        try
                        {
                            sessionEnumerator.GetSession(i, out ctl);

                            // NOTE: we could also use the app name from ctl.GetDisplayName()
                            int cpid;
                            ctl.GetProcessId(out cpid);

                            if (cpid == pid)
                            {
                                Guid guid = Guid.Empty;
                                volumeControl = ctl as ISimpleAudioVolume;
                                volumeControl.SetMasterVolume(Convert.ToSingle(level) / 100, ref guid);
                            }
                        }
                        finally
                        {
                            if (ctl != null) Marshal.ReleaseComObject(ctl);
                        }
                    }
                }
                finally
                {
                    if (speakers != null) Marshal.ReleaseComObject(speakers);
                    if (mgr != null) Marshal.ReleaseComObject(mgr);
                    if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
        }

        public static void SetApplicationMute(int pid, bool? mute)
        {
            if (mute == null) return;
            {
                IMMDeviceEnumerator deviceEnumerator = null;
                IAudioSessionEnumerator sessionEnumerator = null;
                IAudioSessionManager2 mgr = null;
                IMMDevice speakers = null;
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                    // activate the session manager. we need the enumerator
                    Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                    object o;
                    speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                    mgr = (IAudioSessionManager2)o;

                    // enumerate sessions for on this device
                    mgr.GetSessionEnumerator(out sessionEnumerator);
                    int count;
                    sessionEnumerator.GetCount(out count);

                    // search for an audio session with the required process-id
                    ISimpleAudioVolume volumeControl = null;
                    for (int i = 0; i < count; ++i)
                    {
                        IAudioSessionControl2 ctl = null;
                        try
                        {
                            sessionEnumerator.GetSession(i, out ctl);

                            // NOTE: we could also use the app name from ctl.GetDisplayName()
                            int cpid;
                            ctl.GetProcessId(out cpid);

                            if (cpid == pid)
                            {
                                Guid guid = Guid.Empty;
                                volumeControl = ctl as ISimpleAudioVolume;
                                volumeControl.SetMute(Convert.ToBoolean(mute), ref guid);
                            }
                        }
                        finally
                        {
                            if (ctl != null) Marshal.ReleaseComObject(ctl);
                        }
                    }
                }
                finally
                {
                    if (speakers != null) Marshal.ReleaseComObject(speakers);
                    if (mgr != null) Marshal.ReleaseComObject(mgr);
                    if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
        }

        #endregion

    }

    #region Abstracted COM interfaces from Windows CoreAudio API

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    internal enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    internal enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int NotImpl1();

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

        // the rest is not implemented
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        // the rest is not implemented
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        int NotImpl1();
        int NotImpl2();

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

        // the rest is not implemented
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int SessionCount);

        [PreserveSig]
        int GetSession(int SessionCount, out IAudioSessionControl2 Session);
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float fLevel, ref Guid EventContext);

        [PreserveSig]
        int GetMasterVolume(out float pfLevel);

        [PreserveSig]
        int SetMute(bool bMute, ref Guid EventContext);

        [PreserveSig]
        int GetMute(out bool pbMute);
    }

    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl2
    {
        // IAudioSessionControl
        [PreserveSig]
        int NotImpl0();

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid pRetVal);

        [PreserveSig]
        int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int NotImpl1();

        [PreserveSig]
        int NotImpl2();

        // IAudioSessionControl2
        [PreserveSig]
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetProcessId(out int pRetVal);

        [PreserveSig]
        int IsSystemSoundsSession();

        [PreserveSig]
        int SetDuckingPreference(bool optOut);
    }

    // http://netcoreaudio.codeplex.com/SourceControl/latest#trunk/Code/CoreAudio/Interfaces/IAudioEndpointVolume.cs
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioEndpointVolume
    {
        [PreserveSig]
        int NotImpl1();

        [PreserveSig]
        int NotImpl2();

        /// <summary>
        /// Gets a count of the channels in the audio stream.
        /// </summary>
        /// <param name="channelCount">The number of channels.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int GetChannelCount(
            [Out][MarshalAs(UnmanagedType.U4)] out UInt32 channelCount);

        /// <summary>
        /// Sets the master volume level of the audio stream, in decibels.
        /// </summary>
        /// <param name="level">The new master volume level in decibels.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int SetMasterVolumeLevel(
            [In][MarshalAs(UnmanagedType.R4)] float level,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Sets the master volume level, expressed as a normalized, audio-tapered value.
        /// </summary>
        /// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int SetMasterVolumeLevelScalar(
            [In][MarshalAs(UnmanagedType.R4)] float level,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Gets the master volume level of the audio stream, in decibels.
        /// </summary>
        /// <param name="level">The volume level in decibels.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int GetMasterVolumeLevel(
            [Out][MarshalAs(UnmanagedType.R4)] out float level);

        /// <summary>
        /// Gets the master volume level, expressed as a normalized, audio-tapered value.
        /// </summary>
        /// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int GetMasterVolumeLevelScalar(
            [Out][MarshalAs(UnmanagedType.R4)] out float level);

        /// <summary>
        /// Sets the volume level, in decibels, of the specified channel of the audio stream.
        /// </summary>
        /// <param name="channelNumber">The channel number.</param>
        /// <param name="level">The new volume level in decibels.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int SetChannelVolumeLevel(
            [In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
            [In][MarshalAs(UnmanagedType.R4)] float level,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Sets the normalized, audio-tapered volume level of the specified channel in the audio stream.
        /// </summary>
        /// <param name="channelNumber">The channel number.</param>
        /// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int SetChannelVolumeLevelScalar(
            [In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
            [In][MarshalAs(UnmanagedType.R4)] float level,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Gets the volume level, in decibels, of the specified channel in the audio stream.
        /// </summary>
        /// <param name="channelNumber">The zero-based channel number.</param>
		/// <param name="level">The volume level in decibels.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int GetChannelVolumeLevel(
            [In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
            [Out][MarshalAs(UnmanagedType.R4)] out float level);

        /// <summary>
        /// Gets the normalized, audio-tapered volume level of the specified channel of the audio stream.
        /// </summary>
        /// <param name="channelNumber">The zero-based channel number.</param>
		/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int GetChannelVolumeLevelScalar(
            [In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
            [Out][MarshalAs(UnmanagedType.R4)] out float level);

        /// <summary>
        /// Sets the muting state of the audio stream.
        /// </summary>
        /// <param name="isMuted">True to mute the stream, or false to unmute the stream.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int SetMute(
            [In][MarshalAs(UnmanagedType.Bool)] Boolean isMuted,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Gets the muting state of the audio stream.
        /// </summary>
        /// <param name="isMuted">The muting state. True if the stream is muted, false otherwise.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int GetMute(
            [Out][MarshalAs(UnmanagedType.Bool)] out Boolean isMuted);

        /// <summary>
        /// Gets information about the current step in the volume range.
        /// </summary>
        /// <param name="step">The current zero-based step index.</param>
        /// <param name="stepCount">The total number of steps in the volume range.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int GetVolumeStepInfo(
            [Out][MarshalAs(UnmanagedType.U4)] out UInt32 step,
            [Out][MarshalAs(UnmanagedType.U4)] out UInt32 stepCount);

        /// <summary>
        /// Increases the volume level by one step.
        /// </summary>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int VolumeStepUp(
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Decreases the volume level by one step.
        /// </summary>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int VolumeStepDown(
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Queries the audio endpoint device for its hardware-supported functions.
        /// </summary>
        /// <param name="hardwareSupportMask">A hardware support mask that indicates the capabilities of the endpoint.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int QueryHardwareSupport(
            [Out][MarshalAs(UnmanagedType.U4)] out UInt32 hardwareSupportMask);

        /// <summary>
        /// Gets the volume range of the audio stream, in decibels.
        /// </summary>
		/// <param name="volumeMin">The minimum volume level in decibels.</param>
		/// <param name="volumeMax">The maximum volume level in decibels.</param>
		/// <param name="volumeStep">The volume increment level in decibels.</param>
        /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
        [PreserveSig]
        int GetVolumeRange(
            [Out][MarshalAs(UnmanagedType.R4)] out float volumeMin,
            [Out][MarshalAs(UnmanagedType.R4)] out float volumeMax,
            [Out][MarshalAs(UnmanagedType.R4)] out float volumeStep);
        static SerialPort _serialPort;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);


        static Tuple<SerialPort, string[]> find_arduino(string[] prev_ports)
        {
            string[] ports = SerialPort.GetPortNames();
            bool found = false;

  

            if (!ports.SequenceEqual(prev_ports) )
            {
                for (int j = 0; j < 10; j++)
                {
                    Console.WriteLine("itteration " + j);
                    List<SerialPort> Portslist = new List<SerialPort>();
                    List<int> busyports = new List<int>();

                    for (int i = 0; i < ports.Length; i++)

                    {
                        Console.WriteLine(ports[i]);
                        Portslist.Add(new SerialPort());
                        Portslist[i].PortName = ports[i];
                        Portslist[i].BaudRate = 112500;
                        Portslist[i].ReadTimeout = 500;
                        try
                        {
                            Portslist[i].Open();
                        }
                        catch
                        {
                            Console.WriteLine("port " + ports[i] + " busy");
                            busyports.Add(i);
                        }



                    }

                    foreach (int busy in busyports)
                    {
                        Portslist.RemoveAt(busy);
                    }


                    for (int i = 0; i < Portslist.Count; i++)

                    {
                        string ping;
                        Portslist[i].Write("!");
                        try
                        {
                            ping = Portslist[i].ReadLine();
                            Console.WriteLine("response recieved from device: " + ping);
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("Arduino is not at " + Portslist[i].PortName);
                            continue;
                        }


                        if (ping == "!\r")
                        {
                            _serialPort = Portslist[i];
                            _serialPort.ReadTimeout = -1;
                            found = true;
                            

                            Console.WriteLine("Arduino is at " + Portslist[i].PortName);
                            Portslist.RemoveAt(i);
                        }
                    }
                    // close remaining ports
                    for (int i = 0; i < Portslist.Count; i++)
                    {
                        Console.WriteLine(Portslist[i].PortName + " Closed");
                        Portslist[i].Close();
                    }
                    if (found) break;
                    Thread.Sleep(500);
                }

            }
             var result=Tuple.Create(_serialPort, ports);
            return result;
        }

        //static void update_volume(int index, int[] prev, List<int> Volume) {

        //    if (prev_volumes[0] != volumes[0])
        //    {
        //        AudioManager.SetApplicationVolume(active_ID, volumes[0]);
        //    }

        //}
        static void Main(string[] args)

        {
           
            int[] prev_volumes = new int[4];
            string[] initial_ports = { "0" };
            var output = find_arduino(initial_ports);
            SerialPort _serialPort = output.Item1;
            string[] prev_ports =output.Item2;
           

            while (true)
            {
                try
                {
                    bool use_active = true;
                    string input = _serialPort.ReadLine();
                    Console.WriteLine(input);
                    List<int> volumes = new List<int>(Array.ConvertAll(input.Split(' '), int.Parse));


                    // get active window
                    IntPtr hWnd = GetForegroundWindow();
                    int active_ID;
                    GetWindowThreadProcessId(hWnd, out active_ID);


                    // set active aplication

                    if (prev_volumes[0] != volumes[0])
                    {
                        AudioManager.SetApplicationVolume(active_ID, volumes[0]);
                    }

                    if (prev_volumes[1] != volumes[1])
                    {
                        var discord_processes = Process.GetProcessesByName("Discord");
                        if (discord_processes.Length > 0)
                        {

                            //if (active_ID == discord_processes[i].Id) use_active = false;// check if discord is active program
                            AudioManager.SetApplicationVolume(discord_processes[4].Id, volumes[1]);
                        }
                        else {
                            Console.WriteLine("discord not open");
                       }

                    }
                    if (prev_volumes[2] != volumes[2])
                    {
                        var firefox_processes = Process.GetProcessesByName("Firefox");
                        if (firefox_processes.Length > 0) { 
                        for (int i = 0; i < firefox_processes.Length; i++)
                        {
                            //if (active_ID == firefox_processes[i].Id) use_active = false; // check if firefox is active program
                            AudioManager.SetApplicationVolume(firefox_processes[i].Id, volumes[2]);
                        }
                    }
                    }
                 
                   
                        // create previous  volumes array
                    for (int i = 0; i < prev_volumes.Length; i++)
                    {
                        prev_volumes[i] = volumes[i];
                    }
                }
                catch (System.NullReferenceException)
                {
                    Console.WriteLine("null exception");
                    output = find_arduino(prev_ports);
                     _serialPort = output.Item1;
                    prev_ports = output.Item2;
                    Thread.Sleep(5000);

                }
                catch
                {
                    Console.WriteLine("was yoiked");
                    output = find_arduino(prev_ports);
                    _serialPort = output.Item1;
                    prev_ports = output.Item2;
                    Thread.Sleep(5000);

                }
            }
        }
    }

    #endregion
}
