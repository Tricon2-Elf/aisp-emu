#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <strsafe.h>
#include <tlhelp32.h>
#include <cstring>
#include <cwchar>

namespace
{
constexpr UINT kJapaneseCodePage = 932;
constexpr LCID kJapaneseLcid = 0x0411;
constexpr LANGID kJapaneseLangId = 0x0411;

using GetACP_t = UINT(WINAPI*)();
using GetOEMCP_t = UINT(WINAPI*)();
using GetThreadLocale_t = LCID(WINAPI*)();
using GetUserDefaultLCID_t = LCID(WINAPI*)();
using GetSystemDefaultLCID_t = LCID(WINAPI*)();
using GetUserDefaultLangID_t = LANGID(WINAPI*)();
using GetSystemDefaultLangID_t = LANGID(WINAPI*)();
using MultiByteToWideChar_t = int(WINAPI*)(UINT, DWORD, LPCCH, int, LPWSTR, int);
using WideCharToMultiByte_t = int(WINAPI*)(UINT, DWORD, LPCWCH, int, LPSTR, int, LPCCH, LPBOOL);
using LoadLibraryW_t = HMODULE(WINAPI*)(LPCWSTR);

GetACP_t g_originalGetACP = nullptr;
GetOEMCP_t g_originalGetOEMCP = nullptr;
GetThreadLocale_t g_originalGetThreadLocale = nullptr;
GetUserDefaultLCID_t g_originalGetUserDefaultLCID = nullptr;
GetSystemDefaultLCID_t g_originalGetSystemDefaultLCID = nullptr;
GetUserDefaultLangID_t g_originalGetUserDefaultLangID = nullptr;
GetSystemDefaultLangID_t g_originalGetSystemDefaultLangID = nullptr;
MultiByteToWideChar_t g_originalMultiByteToWideChar = nullptr;
WideCharToMultiByte_t g_originalWideCharToMultiByte = nullptr;
LoadLibraryW_t g_originalLoadLibraryW = nullptr;

UINT WINAPI HookGetACP()
{
    return kJapaneseCodePage;
}

UINT WINAPI HookGetOEMCP()
{
    return kJapaneseCodePage;
}

LCID WINAPI HookGetThreadLocale()
{
    return kJapaneseLcid;
}

LCID WINAPI HookGetUserDefaultLCID()
{
    return kJapaneseLcid;
}

LCID WINAPI HookGetSystemDefaultLCID()
{
    return kJapaneseLcid;
}

LANGID WINAPI HookGetUserDefaultLangID()
{
    return kJapaneseLangId;
}

LANGID WINAPI HookGetSystemDefaultLangID()
{
    return kJapaneseLangId;
}

int WINAPI HookMultiByteToWideChar(UINT codePage, DWORD dwFlags, LPCCH multiByteStr, int cbMultiByte, LPWSTR wideCharStr, int cchWideChar)
{
    if (codePage == CP_ACP || codePage == CP_THREAD_ACP)
        codePage = kJapaneseCodePage;
    return g_originalMultiByteToWideChar ? g_originalMultiByteToWideChar(codePage, dwFlags, multiByteStr, cbMultiByte, wideCharStr, cchWideChar) : 0;
}

int WINAPI HookWideCharToMultiByte(
    UINT codePage,
    DWORD dwFlags,
    LPCWCH wideCharStr,
    int cchWideChar,
    LPSTR multiByteStr,
    int cbMultiByte,
    LPCCH defaultChar,
    LPBOOL usedDefaultChar
)
{
    if (codePage == CP_ACP || codePage == CP_THREAD_ACP)
        codePage = kJapaneseCodePage;
    return g_originalWideCharToMultiByte
               ? g_originalWideCharToMultiByte(codePage, dwFlags, wideCharStr, cchWideChar, multiByteStr, cbMultiByte, defaultChar, usedDefaultChar)
               : 0;
}

bool IsD3d9LibraryPath(LPCWSTR path)
{
    if (!path || !*path)
        return false;

    const wchar_t* fileName = path;
    if (const wchar_t* slash = std::wcsrchr(path, L'\\'))
        fileName = slash + 1;
    if (const wchar_t* slash = std::wcsrchr(fileName, L'/'))
        fileName = slash + 1;

    return _wcsicmp(fileName, L"d3d9.dll") == 0 || _wcsicmp(fileName, L"d3d9") == 0;
}

bool BuildLocalD3d9Path(wchar_t* outPath, size_t outPathCount)
{
    if (!outPath || outPathCount == 0)
        return false;

    wchar_t processPath[MAX_PATH] = {};
    const DWORD processPathLen = GetModuleFileNameW(nullptr, processPath, MAX_PATH);
    if (processPathLen == 0 || processPathLen >= MAX_PATH)
        return false;

    wchar_t* lastSlash = std::wcsrchr(processPath, L'\\');
    if (!lastSlash)
        return false;
    *(lastSlash + 1) = L'\0';

    if (FAILED(StringCchCopyW(outPath, outPathCount, processPath)))
        return false;
    if (FAILED(StringCchCatW(outPath, outPathCount, L"d3d9.dll")))
        return false;

    return GetFileAttributesW(outPath) != INVALID_FILE_ATTRIBUTES;
}

HMODULE WINAPI HookLoadLibraryW(LPCWSTR lpLibFileName)
{
    if (!g_originalLoadLibraryW)
        return nullptr;

    if (IsD3d9LibraryPath(lpLibFileName))
    {
        wchar_t localD3d9Path[MAX_PATH] = {};
        if (BuildLocalD3d9Path(localD3d9Path, sizeof(localD3d9Path) / sizeof(localD3d9Path[0])))
            return g_originalLoadLibraryW(localD3d9Path);
    }

    return g_originalLoadLibraryW(lpLibFileName);
}

template <typename T>
bool PatchSingleImport(HMODULE module, const char* importedModuleName, const char* importName, void* replacement, T* original)
{
    if (!module)
        return false;

    auto base = reinterpret_cast<BYTE*>(module);
    auto dos = reinterpret_cast<IMAGE_DOS_HEADER*>(base);
    if (!dos || dos->e_magic != IMAGE_DOS_SIGNATURE)
        return false;

    auto nt = reinterpret_cast<IMAGE_NT_HEADERS*>(base + dos->e_lfanew);
    if (!nt || nt->Signature != IMAGE_NT_SIGNATURE)
        return false;

    auto importDir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (!importDir.VirtualAddress || !importDir.Size)
        return false;

    auto importDesc = reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(base + importDir.VirtualAddress);
    while (importDesc->Name)
    {
        auto moduleName = reinterpret_cast<const char*>(base + importDesc->Name);
        if (_stricmp(moduleName, importedModuleName) == 0)
        {
            auto originalThunk = importDesc->OriginalFirstThunk
                                     ? reinterpret_cast<IMAGE_THUNK_DATA*>(base + importDesc->OriginalFirstThunk)
                                     : reinterpret_cast<IMAGE_THUNK_DATA*>(base + importDesc->FirstThunk);
            auto thunk = reinterpret_cast<IMAGE_THUNK_DATA*>(base + importDesc->FirstThunk);

            while (originalThunk->u1.AddressOfData)
            {
                if ((originalThunk->u1.Ordinal & IMAGE_ORDINAL_FLAG) == 0)
                {
                    auto byName = reinterpret_cast<IMAGE_IMPORT_BY_NAME*>(base + originalThunk->u1.AddressOfData);
                    if (std::strcmp(reinterpret_cast<const char*>(byName->Name), importName) == 0)
                    {
                        DWORD oldProtect = 0;
                        if (!VirtualProtect(&thunk->u1.Function, sizeof(void*), PAGE_EXECUTE_READWRITE, &oldProtect))
                            return false;

                        if (original && !*original)
                            *original = reinterpret_cast<T>(thunk->u1.Function);
                        thunk->u1.Function = reinterpret_cast<ULONG_PTR>(replacement);

                        FlushInstructionCache(GetCurrentProcess(), &thunk->u1.Function, sizeof(void*));

                        DWORD ignored = 0;
                        VirtualProtect(&thunk->u1.Function, sizeof(void*), oldProtect, &ignored);
                        return true;
                    }
                }

                ++originalThunk;
                ++thunk;
            }
        }
        ++importDesc;
    }

    return false;
}

template <typename T>
void PatchImport(HMODULE module, const char* importName, void* replacement, T* original)
{
    PatchSingleImport(module, "KERNEL32.dll", importName, replacement, original);
    PatchSingleImport(module, "KERNELBASE.dll", importName, replacement, original);
}

void PatchModule(HMODULE module)
{
    PatchImport(module, "GetACP", reinterpret_cast<void*>(HookGetACP), &g_originalGetACP);
    PatchImport(module, "GetOEMCP", reinterpret_cast<void*>(HookGetOEMCP), &g_originalGetOEMCP);
    PatchImport(module, "GetThreadLocale", reinterpret_cast<void*>(HookGetThreadLocale), &g_originalGetThreadLocale);
    PatchImport(module, "GetUserDefaultLCID", reinterpret_cast<void*>(HookGetUserDefaultLCID), &g_originalGetUserDefaultLCID);
    PatchImport(module, "GetSystemDefaultLCID", reinterpret_cast<void*>(HookGetSystemDefaultLCID), &g_originalGetSystemDefaultLCID);
    PatchImport(module, "GetUserDefaultLangID", reinterpret_cast<void*>(HookGetUserDefaultLangID), &g_originalGetUserDefaultLangID);
    PatchImport(module, "GetSystemDefaultLangID", reinterpret_cast<void*>(HookGetSystemDefaultLangID), &g_originalGetSystemDefaultLangID);
    PatchImport(module, "MultiByteToWideChar", reinterpret_cast<void*>(HookMultiByteToWideChar), &g_originalMultiByteToWideChar);
    PatchImport(module, "WideCharToMultiByte", reinterpret_cast<void*>(HookWideCharToMultiByte), &g_originalWideCharToMultiByte);
    PatchImport(module, "LoadLibraryW", reinterpret_cast<void*>(HookLoadLibraryW), &g_originalLoadLibraryW);
}

void PatchLoadedModules()
{
    const DWORD processId = GetCurrentProcessId();
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processId);
    if (snapshot == INVALID_HANDLE_VALUE)
        return;

    MODULEENTRY32 moduleEntry = {};
    moduleEntry.dwSize = sizeof(moduleEntry);

    if (Module32First(snapshot, &moduleEntry))
    {
        do
        {
            PatchModule(moduleEntry.hModule);
        } while (Module32Next(snapshot, &moduleEntry));
    }

    CloseHandle(snapshot);
}
} // namespace

BOOL WINAPI DllMain(HINSTANCE instance, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(instance);
        PatchLoadedModules();
    }
    return TRUE;
}
