using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Gallery.Services.Ppt;

#pragma warning disable CA1416

internal static class IcccePptRotConnectionHelper
{
    private static readonly string[] PresentationExtensions =
    [
        ".pptx", ".pptm", ".ppt",
        ".ppsx", ".ppsm", ".pps",
        ".potx", ".potm", ".pot",
        ".dps", ".dpt"
    ];

    public static object? TryConnectViaRot(bool supportWps)
    {
        object? bestApp = GetAnyActivePowerPoint(out var bestPriority);
        if (bestApp != null && bestPriority > 0)
        {
            return bestApp;
        }

        SafeReleaseComObject(bestApp);

        foreach (var progId in EnumerateProgIds(supportWps))
        {
            var app = TryGetActiveObject(progId);
            if (app != null && IsComApplicationAlive(app))
            {
                return app;
            }

            SafeReleaseComObject(app);
        }

        return null;
    }

    private static object? GetAnyActivePowerPoint(out int bestPriority)
    {
        IRunningObjectTable? rot = null;
        IEnumMoniker? enumMoniker = null;
        object? bestApp = null;
        bestPriority = 0;
        var foundApps = new List<object>();

        try
        {
            if (GetRunningObjectTable(0, out rot) != 0 || rot == null)
            {
                return null;
            }

            rot.EnumRunning(out enumMoniker);
            if (enumMoniker == null)
            {
                return null;
            }

            var applicationMonikers = GetApplicationMonikersFromProgIds();
            var monikers = new IMoniker[1];
            while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
            {
                IBindCtx? bindCtx = null;
                object? comObject = null;
                object? candidateApp = null;
                object? activePresentation = null;
                object? slideShowWindow = null;
                var keepCandidate = false;

                try
                {
                    CreateBindCtx(0, out bindCtx);
                    monikers[0].GetDisplayName(bindCtx, null, out var displayName);

                    var isApplicationMoniker = ContainsMoniker(applicationMonikers, displayName) ||
                                               IsFallbackApplicationMoniker(displayName);
                    if (!isApplicationMoniker && !LooksLikePresentationFile(displayName))
                    {
                        continue;
                    }

                    rot.GetObject(monikers[0], out comObject);
                    if (comObject == null)
                    {
                        continue;
                    }

                    if (isApplicationMoniker)
                    {
                        candidateApp = comObject;
                        comObject = null;
                    }
                    else
                    {
                        candidateApp = TryGetProperty(comObject, "Application");
                    }

                    if (candidateApp == null || IsDuplicate(candidateApp, foundApps))
                    {
                        continue;
                    }

                    foundApps.Add(candidateApp);
                    keepCandidate = true;

                    var priority = 0;
                    activePresentation = TryGetProperty(candidateApp, "ActivePresentation");
                    if (activePresentation != null)
                    {
                        priority = 1;
                        slideShowWindow = TryGetProperty(activePresentation, "SlideShowWindow");
                        if (slideShowWindow != null)
                        {
                            priority = IsSlideShowWindowActive(slideShowWindow) ? 3 : 2;
                        }
                    }

                    if (priority > bestPriority)
                    {
                        bestPriority = priority;
                        SafeReleaseComObject(bestApp);
                        bestApp = candidateApp;
                        candidateApp = null;
                        keepCandidate = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                finally
                {
                    SafeReleaseComObject(slideShowWindow);
                    SafeReleaseComObject(activePresentation);

                    if (!keepCandidate)
                    {
                        SafeReleaseComObject(candidateApp);
                    }

                    SafeReleaseComObject(comObject);
                    SafeReleaseComObject(bindCtx);
                    SafeReleaseComObject(monikers[0]);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            foreach (var app in foundApps)
            {
                if (bestApp != null && AreComObjectsEqual(app, bestApp))
                {
                    continue;
                }

                SafeReleaseComObject(app);
            }

            SafeReleaseComObject(enumMoniker);
            SafeReleaseComObject(rot);
        }

        return bestApp;
    }

    public static bool IsSlideShowWindowActive(object slideShowWindow)
    {
        try
        {
            var foregroundHwnd = GetForegroundWindow();
            if (foregroundHwnd == IntPtr.Zero)
            {
                return false;
            }

            var slideShowHwnd = TryGetSlideShowHwnd(slideShowWindow);
            if (slideShowHwnd == IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(foregroundHwnd, out var foregroundPid);
            GetWindowThreadProcessId(slideShowHwnd, out var slideShowPid);
            if (foregroundPid == slideShowPid)
            {
                return true;
            }

            using var foregroundProcess = Process.GetProcessById((int)foregroundPid);
            using var slideShowProcess = Process.GetProcessById((int)slideShowPid);
            var foregroundName = foregroundProcess.ProcessName.ToLowerInvariant();
            var slideShowName = slideShowProcess.ProcessName.ToLowerInvariant();

            return foregroundName.StartsWith("wps", StringComparison.OrdinalIgnoreCase) &&
                   slideShowName.StartsWith("wpp", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static bool AreComObjectsEqual(object? first, object? second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        if (ReferenceEquals(first, second))
        {
            return true;
        }

        var firstUnknown = IntPtr.Zero;
        var secondUnknown = IntPtr.Zero;
        try
        {
            firstUnknown = Marshal.GetIUnknownForObject(first);
            secondUnknown = Marshal.GetIUnknownForObject(second);
            return firstUnknown == secondUnknown;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (firstUnknown != IntPtr.Zero)
            {
                Marshal.Release(firstUnknown);
            }

            if (secondUnknown != IntPtr.Zero)
            {
                Marshal.Release(secondUnknown);
            }
        }
    }

    public static void SafeReleaseComObject(object? comObject)
    {
        if (comObject == null)
        {
            return;
        }

        try
        {
            if (Marshal.IsComObject(comObject))
            {
                Marshal.ReleaseComObject(comObject);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private static object? TryGetProperty(object target, string propertyName)
    {
        try
        {
            return target.GetType().InvokeMember(propertyName, BindingFlags.GetProperty, null, target, null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    private static bool IsDuplicate(object candidate, IEnumerable<object> processed)
    {
        foreach (var app in processed)
        {
            if (AreComObjectsEqual(candidate, app))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsComApplicationAlive(object app)
    {
        try
        {
            dynamic dynamicApp = app;
            _ = dynamicApp.Name;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static object? TryGetActiveObject(string progId)
    {
        try
        {
            var hr = CLSIDFromProgID(progId, out var clsid);
            if (hr != 0 || clsid == Guid.Empty)
            {
                return null;
            }

            hr = GetActiveObject(ref clsid, IntPtr.Zero, out var obj);
            return hr == 0 ? obj : null;
        }
        catch
        {
            return null;
        }
    }

    private static string[] GetApplicationMonikersFromProgIds()
    {
        var monikers = new List<string>();
        foreach (var progId in EnumerateProgIds(true))
        {
            if (CLSIDFromProgID(progId, out var clsid) == 0 && clsid != Guid.Empty)
            {
                var moniker = "!" + clsid.ToString("B").ToUpperInvariant();
                if (!ContainsMoniker(monikers, moniker))
                {
                    monikers.Add(moniker);
                }
            }
        }

        return monikers.ToArray();
    }

    private static IEnumerable<string> EnumerateProgIds(bool supportWps)
    {
        yield return "PowerPoint.Application";

        if (!supportWps)
        {
            yield break;
        }

        yield return "KWPP.Application";
        yield return "kwpp.Application";
        yield return "Wpp.Application";
        yield return "WPP.Application";
    }

    private static bool LooksLikePresentationFile(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        var lower = displayName.ToLowerInvariant();
        foreach (var extension in PresentationExtensions)
        {
            if (lower.Contains(extension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsMoniker(IEnumerable<string> monikers, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        foreach (var moniker in monikers)
        {
            if (string.Equals(moniker, displayName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFallbackApplicationMoniker(string displayName)
    {
        string[] fallbackMonikers =
        [
            "!{91493441-5A91-11CF-8700-00AA0060263B}",
            "!{44720441-94BF-4940-926D-4F38FECF2A48}"
        ];

        return ContainsMoniker(fallbackMonikers, displayName);
    }

    private static IntPtr TryGetSlideShowHwnd(object slideShowWindow)
    {
        try
        {
            dynamic dynamicWindow = slideShowWindow;
            var hwnd = Convert.ToInt32(dynamicWindow.HWND);
            return hwnd == 0 ? IntPtr.Zero : new IntPtr(hwnd);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string lpszProgID, out Guid pclsid);

    [DllImport("oleaut32.dll", PreserveSig = true)]
    private static extern int GetActiveObject(
        ref Guid rclsid,
        IntPtr pvReserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
