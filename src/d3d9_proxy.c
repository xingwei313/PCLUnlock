// d3d9.dll 代理：转发系统 d3d9 导出，并从自身资源加载托管实现。
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#define PAYLOAD_RESOURCE 101

void *p_D3DPERF_BeginEvent;
void *p_D3DPERF_EndEvent;
void *p_D3DPERF_GetStatus;
void *p_D3DPERF_QueryRepeatFrame;
void *p_D3DPERF_SetMarker;
void *p_D3DPERF_SetOptions;
void *p_D3DPERF_SetRegion;
void *p_DebugSetLevel;
void *p_DebugSetMute;
void *p_Direct3D9EnableMaximizedWindowedModeShim;
void *p_Direct3DCreate9;
void *p_Direct3DCreate9Ex;
void *p_Direct3DCreate9On12;
void *p_Direct3DCreate9On12Ex;
void *p_Direct3DShaderValidatorCreate9;
void *p_PSGPError;
void *p_PSGPSampleTexture;

static HINSTANCE g_self;
static HMODULE g_real;

static void Fill(HMODULE dll, const char *name, void **slot)
{
    *slot = (void *)GetProcAddress(dll, name);
}

static BOOL LoadReal(void)
{
    WCHAR path[MAX_PATH];
    UINT length;
    if (g_real != NULL)
        return TRUE;
    length = GetSystemDirectoryW(path, MAX_PATH);
    if (length == 0 || length + 10 >= MAX_PATH)
        return FALSE;
    lstrcatW(path, L"\\d3d9.dll");
    g_real = LoadLibraryW(path);
    if (g_real == NULL)
        return FALSE;
    Fill(g_real, "D3DPERF_BeginEvent", &p_D3DPERF_BeginEvent);
    Fill(g_real, "D3DPERF_EndEvent", &p_D3DPERF_EndEvent);
    Fill(g_real, "D3DPERF_GetStatus", &p_D3DPERF_GetStatus);
    Fill(g_real, "D3DPERF_QueryRepeatFrame", &p_D3DPERF_QueryRepeatFrame);
    Fill(g_real, "D3DPERF_SetMarker", &p_D3DPERF_SetMarker);
    Fill(g_real, "D3DPERF_SetOptions", &p_D3DPERF_SetOptions);
    Fill(g_real, "D3DPERF_SetRegion", &p_D3DPERF_SetRegion);
    Fill(g_real, "DebugSetLevel", &p_DebugSetLevel);
    Fill(g_real, "DebugSetMute", &p_DebugSetMute);
    Fill(g_real, "Direct3D9EnableMaximizedWindowedModeShim", &p_Direct3D9EnableMaximizedWindowedModeShim);
    Fill(g_real, "Direct3DCreate9", &p_Direct3DCreate9);
    Fill(g_real, "Direct3DCreate9Ex", &p_Direct3DCreate9Ex);
    Fill(g_real, "Direct3DCreate9On12", &p_Direct3DCreate9On12);
    Fill(g_real, "Direct3DCreate9On12Ex", &p_Direct3DCreate9On12Ex);
    Fill(g_real, "Direct3DShaderValidatorCreate9", &p_Direct3DShaderValidatorCreate9);
    Fill(g_real, "PSGPError", &p_PSGPError);
    Fill(g_real, "PSGPSampleTexture", &p_PSGPSampleTexture);
    return TRUE;
}

typedef struct ICLRRuntimeHost ICLRRuntimeHost;
typedef struct ICLRRuntimeHostVtbl
{
    HRESULT(STDMETHODCALLTYPE *QueryInterface)(ICLRRuntimeHost *, REFIID, void **);
    ULONG(STDMETHODCALLTYPE *AddRef)(ICLRRuntimeHost *);
    ULONG(STDMETHODCALLTYPE *Release)(ICLRRuntimeHost *);
    HRESULT(STDMETHODCALLTYPE *Start)(ICLRRuntimeHost *);
    HRESULT(STDMETHODCALLTYPE *Stop)(ICLRRuntimeHost *);
    HRESULT(STDMETHODCALLTYPE *SetHostControl)(ICLRRuntimeHost *, void *);
    HRESULT(STDMETHODCALLTYPE *GetCLRControl)(ICLRRuntimeHost *, void **);
    HRESULT(STDMETHODCALLTYPE *UnloadAppDomain)(ICLRRuntimeHost *, DWORD, BOOL);
    HRESULT(STDMETHODCALLTYPE *ExecuteInAppDomain)(ICLRRuntimeHost *, DWORD, void *, void *);
    HRESULT(STDMETHODCALLTYPE *GetCurrentAppDomainId)(ICLRRuntimeHost *, DWORD *);
    HRESULT(STDMETHODCALLTYPE *ExecuteApplication)(ICLRRuntimeHost *, LPCWSTR, DWORD, LPCWSTR *, DWORD *, DWORD *);
    HRESULT(STDMETHODCALLTYPE *ExecuteInDefaultAppDomain)(ICLRRuntimeHost *, LPCWSTR, LPCWSTR, LPCWSTR, LPCWSTR, DWORD *);
} ICLRRuntimeHostVtbl;
struct ICLRRuntimeHost
{
    const ICLRRuntimeHostVtbl *lpVtbl;
};

static const CLSID CLSID_CLRRuntimeHost = {0x90F1A06E, 0x7712, 0x4762, {0x86, 0xB5, 0x7A, 0x5E, 0xBA, 0x6B, 0xDB, 0x02}};
static const IID IID_ICLRRuntimeHost = {0x90F1A06C, 0x7712, 0x4762, {0x86, 0xB5, 0x7A, 0x5E, 0xBA, 0x6B, 0xDB, 0x02}};

typedef HRESULT(WINAPI *PFN_CorBindToRuntimeEx)(LPCWSTR, LPCWSTR, DWORD, REFCLSID, REFIID, LPVOID *);

static BOOL ExtractPayload(WCHAR *path)
{
    HRSRC resource;
    HGLOBAL global;
    DWORD size;
    void *data;
    WCHAR temp[MAX_PATH];
    HANDLE file;
    DWORD written;
    DWORD tempLength;

    resource = FindResourceW(g_self, MAKEINTRESOURCEW(PAYLOAD_RESOURCE), MAKEINTRESOURCEW(10));
    if (resource == NULL)
        return FALSE;
    global = LoadResource(g_self, resource);
    if (global == NULL)
        return FALSE;
    size = SizeofResource(g_self, resource);
    data = LockResource(global);
    if (data == NULL || size == 0)
        return FALSE;

    tempLength = GetTempPathW(MAX_PATH, temp);
    if (tempLength == 0 || tempLength >= MAX_PATH)
        return FALSE;
    wsprintfW(path, L"%sPCLUnlock_%lu.dll", temp, GetCurrentProcessId());

    file = CreateFileW(path, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, CREATE_ALWAYS,
                       FILE_ATTRIBUTE_TEMPORARY, NULL);
    if (file == INVALID_HANDLE_VALUE)
        return FALSE;
    if (!WriteFile(file, data, size, &written, NULL) || written != size)
    {
        CloseHandle(file);
        return FALSE;
    }
    FlushFileBuffers(file);
    CloseHandle(file);
    return TRUE;
}

static DWORD WINAPI Bootstrap(LPVOID parameter)
{
    WCHAR payload[MAX_PATH];
    HMODULE mscoree;
    PFN_CorBindToRuntimeEx bind;
    ICLRRuntimeHost *host = NULL;
    HRESULT hr;
    DWORD result = 0;
    (void)parameter;

    for (int i = 0; i < 600; i++)
    {
        if (GetModuleHandleW(L"clr.dll") != NULL)
            break;
        Sleep(100);
    }
    if (GetModuleHandleW(L"clr.dll") == NULL)
        return 0;
    if (!ExtractPayload(payload))
        return 0;

    mscoree = LoadLibraryW(L"mscoree.dll");
    if (mscoree != NULL)
    {
        bind = (PFN_CorBindToRuntimeEx)GetProcAddress(mscoree, "CorBindToRuntimeEx");
        if (bind != NULL)
        {
            hr = bind(L"v4.0.30319", NULL, 0, &CLSID_CLRRuntimeHost, &IID_ICLRRuntimeHost, (void **)&host);
            if (SUCCEEDED(hr) && host != NULL)
            {
                host->lpVtbl->Start(host);
                host->lpVtbl->ExecuteInDefaultAppDomain(host, payload, L"PCLUnlock.Injector", L"Run", L"", &result);
            }
        }
    }
    DeleteFileW(payload);
    return 0;
}

BOOL WINAPI DllMain(HINSTANCE self, DWORD reason, LPVOID reserved)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        (void)reserved;
        g_self = self;
        DisableThreadLibraryCalls(self);
        LoadReal();
        CreateThread(NULL, 0, Bootstrap, NULL, 0, NULL);
    }
    return TRUE;
}
