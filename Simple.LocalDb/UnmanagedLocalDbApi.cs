using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Simple.LocalDb
{
  [SuppressMessage("ReSharper", "IdentifierTypo")]
  [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
  public class UnmanagedLocalDbApi
  {
    private readonly IntPtr _api;
    public UnmanagedLocalDbApi()
    {
      string dllName = GetLocalDbDllName();
      if (dllName == null) throw new InvalidOperationException("Could not find local db dll.");

      _api = Kernel32.LoadLibraryEx(dllName, IntPtr.Zero, Kernel32.LoadLibraryFlags.LoadLibrarySearchDefaultDirs);
      if (_api == IntPtr.Zero) throw new Win32Exception();

      CreateInstance = GetFunction<LocalDBCreateInstance>();
      DeleteInstance = GetFunction<LocalDBDeleteInstance>();
      FormatMessage = GetFunction<LocalDBFormatMessage>();
      GetInstanceInfo = GetFunction<LocalDBGetInstanceInfo>();
      GetInstances = GetFunction<LocalDBGetInstances>();
      GetVersionInfo = GetFunction<LocalDBGetVersionInfo>();
      GetVersions = GetFunction<LocalDBGetVersions>();
      ShareInstance = GetFunction<LocalDBShareInstance>();
      StartInstance = GetFunction<LocalDBStartInstance>();
      StartTracing = GetFunction<LocalDBStartTracing>();
      StopInstance = GetFunction<LocalDBStopInstance>();
      StopTracing = GetFunction<LocalDBStopTracing>();
      UnshareInstance = GetFunction<LocalDBUnshareInstance>();
    }
    public string ApiVersion { get; private set; }
    public const int MaxPath = 260;
    public const int MaxName = 129;
    public const int MaxSid = 187;

    public LocalDBCreateInstance CreateInstance;
    public LocalDBDeleteInstance DeleteInstance;
    public LocalDBFormatMessage FormatMessage;
    public LocalDBGetInstanceInfo GetInstanceInfo;
    public LocalDBGetInstances GetInstances;
    public LocalDBGetVersionInfo GetVersionInfo;
    public LocalDBGetVersions GetVersions;
    public LocalDBShareInstance ShareInstance;
    public LocalDBStartInstance StartInstance;
    public LocalDBStartTracing StartTracing;
    public LocalDBStopInstance StopInstance;
    public LocalDBStopTracing StopTracing;
    public LocalDBUnshareInstance UnshareInstance;

    private string GetLocalDbDllName()
    {
	    bool isWow64Process = RuntimeInformation.OSArchitecture == Architecture.X64 &&
	                          RuntimeInformation.OSArchitecture == Architecture.X86;
      var registryView = isWow64Process ? RegistryView.Registry32 : RegistryView.Default;
      using (var rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
      {
        var versions = rootKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions");
        if (versions == null) throw new InvalidOperationException("LocalDb not installed.");

        var latest = versions.GetSubKeyNames().Select(s => new Version(s)).OrderBy(s => s).FirstOrDefault();
        if (latest == null) throw new InvalidOperationException("LocalDb not installed.");
        using (var versionKey = versions.OpenSubKey(latest.ToString()))
        {
          ApiVersion = latest.ToString();
          var path = (string)versionKey?.GetValue("InstanceAPIPath");
          return path;
        }
      }
    }

    private T GetFunction<T>() where T : class
    {
      string name = typeof(T).Name;
      var ptr = Kernel32.GetProcAddress(_api, name);
      if (ptr == IntPtr.Zero) throw new EntryPointNotFoundException($@"{name}");

      object function = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
      return (T)function;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBCreateInstance([MarshalAs(UnmanagedType.LPWStr)] string wszVersion, [MarshalAs(UnmanagedType.LPWStr)] string pInstanceName, int dwFlags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBDeleteInstance([MarshalAs(UnmanagedType.LPWStr)] string pInstanceName, int dwFlags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBFormatMessage(int hrLocalDB, int dwFlags, int dwLanguageId, [MarshalAs(UnmanagedType.LPWStr)][Out] StringBuilder wszMessage, ref int lpcchMessage);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBGetInstanceInfo([MarshalAs(UnmanagedType.LPWStr)] string wszInstanceName, ref LocalDbInstanceInfo pInstanceInfo, int dwInstanceInfoSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBGetInstances(IntPtr pInstanceNames, ref int lpdwNumberOfInstances);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBGetVersionInfo([MarshalAs(UnmanagedType.LPWStr)] string wszVersionName, IntPtr pVersionInfo, int dwVersionInfoSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBGetVersions(IntPtr pVersion, ref int lpdwNumberOfVersions);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBShareInstance(IntPtr pOwnerSid, [MarshalAs(UnmanagedType.LPWStr)] string pInstancePrivateName, [MarshalAs(UnmanagedType.LPWStr)] string pInstanceSharedName, int dwFlags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBStartInstance([MarshalAs(UnmanagedType.LPWStr)] string pInstanceName, int dwFlags, [MarshalAs(UnmanagedType.LPWStr)][Out] StringBuilder wszSqlConnection, ref int lpcchSqlConnection);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBStartTracing();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBStopInstance([MarshalAs(UnmanagedType.LPWStr)] string pInstanceName, int dwFlags, int ulTimeout);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBStopTracing();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LocalDBUnshareInstance([MarshalAs(UnmanagedType.LPWStr)] string pInstanceName, int dwFlags);
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct LocalDbInstanceInfo
  {
    internal static readonly int MarshalSize = Marshal.SizeOf(typeof(LocalDbInstanceInfo));
    public uint Size;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UnmanagedLocalDbApi.MaxName)]
    public string InstanceName;
    public bool Exists;
    public bool ConfigurationCorrupted;
    public bool IsRunning;
    public uint Major;
    public uint Minor;
    public uint Build;
    public uint Revision;
    public FILETIME LastStartUtc;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UnmanagedLocalDbApi.MaxPath)]
    public string Connection;
    public bool IsShared;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UnmanagedLocalDbApi.MaxName)]
    public string SharedInstanceName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UnmanagedLocalDbApi.MaxSid)]
    public string OwnerSID;
    public bool IsAutomatic;
  }
}