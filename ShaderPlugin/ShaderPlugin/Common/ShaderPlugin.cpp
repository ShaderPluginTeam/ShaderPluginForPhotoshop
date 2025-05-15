#include "ShaderPlugin.h"
#include <array>
#include <filesystem>
#include <format>
#include "PIUtilities.h" // PS SDK Utility

using namespace std;
using namespace std::filesystem;

#define NAMEOF(name) #name
#define NAMEOF_TEXT(name) TEXT(#name)

static const TCHAR* CLR_PROXY_DLL_NAME = TEXT("CLRProxy.dll");

void Entry(FilterRecord* pFilterRecord, intptr_t* pData)
{
	if (!pFilterRecord)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(pFilterRecord));
	}

	if (!pData)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(pData));
	}

	m_pFilterRecord = pFilterRecord;
	m_pLongData = pData;
}

// this will be called the first time your plug in is run
// if it's zero create a handle and store our default value in there
void InitParameters()
{
	if (!m_pFilterRecord)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(m_pFilterRecord));
	}

	if (!m_pLongData)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(m_pLongData));
	}

	if (*m_pLongData == NULL)
	{
		Handle sHandle = m_pFilterRecord->handleProcs->newProc(1); // 1 byte buffer ("Last Filter")
		if (!sHandle)
		{
			throw format("{} :: {} is Null!", __func__, NAMEOF(sHandle));
		}

		m_pData = m_pFilterRecord->handleProcs->lockProc(sHandle, true);
		if (!m_pData)
		{
			throw format("{} :: {} is Null!", __func__, NAMEOF(m_pData));
		}

		*m_pLongData = reinterpret_cast<intptr_t>(sHandle);
		*m_pData = 0b0;
	}
	else // Set byte to 0 (reset "Last Filter")
	{
		m_pData = m_pFilterRecord->handleProcs->lockProc(reinterpret_cast<Handle>(*m_pLongData), true);
		if (!m_pData)
		{
			throw format("{} :: {} is Null!", __func__, NAMEOF(m_pData));
		}

		*m_pData = 0b0;
	}
}

// the plug in could start here with the Ctrl-F command, "Last Filter"
// you better have a valid handle to get your data out of
void Prepare()
{
	if (!m_pFilterRecord)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(m_pFilterRecord));
	}

	if (!m_pLongData)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(m_pLongData));
	}

	if (*m_pLongData == NULL) // Automation (Action Scripts)
	{
		Handle sHandle = m_pFilterRecord->handleProcs->newProc(1); // 1 byte buffer ("Last Filter")
		if (!sHandle)
		{
			throw format("{} :: {} is Null!", __func__, NAMEOF(sHandle));
		}

		m_pData = m_pFilterRecord->handleProcs->lockProc(sHandle, true);
		if (!m_pData)
		{
			throw format("{} :: {} is Null!", __func__, NAMEOF(m_pData));
		}

		*m_pLongData = reinterpret_cast<intptr_t>(sHandle);
		*m_pData = 0b1;
	}

	m_pData = m_pFilterRecord->handleProcs->lockProc(reinterpret_cast<Handle>(*m_pLongData), true);
	if (!m_pData)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(m_pData));
	}
}

short Start()
{
	HMODULE ModuleHandle = GetModuleHandle(TEXT("ShaderPlugin.8bf"));
	if (!ModuleHandle)
	{
		throw format("{} :: Get Plugin Module Handle Error: {}", __func__, GetLastError());
	}

	// Find Dll Path
	array<TCHAR, MAX_PATH> ModulePath = {};
	if (!GetModuleFileName(ModuleHandle, ModulePath.data(), MAX_PATH))
	{
		throw format("{} :: Get Plugin Module file Path Error: {}", __func__, GetLastError());
	}

	path ShaderPluginPath = path(ModulePath.data()).remove_filename();
	if (!SetDllDirectory(ShaderPluginPath.string().c_str()))
	{
		throw format("{} :: Can't SetDllDirectory for \"{}\": {}", __func__, CLR_PROXY_DLL_NAME, GetLastError());
	}
	
	path CLRProxyDLLPath = ShaderPluginPath / CLR_PROXY_DLL_NAME;
	if (!exists(CLRProxyDLLPath))
	{
		throw format("{} :: Can't find {}, path: \"{}\"", __func__, CLR_PROXY_DLL_NAME, CLRProxyDLLPath.string());
	}

	// Call Library
	HMODULE DLLHandle = LoadLibrary(CLRProxyDLLPath.string().c_str());
	if (!DLLHandle)
	{
		throw format("{} :: LoadLibrary \"{}\" failed with error: {}.", __func__, CLR_PROXY_DLL_NAME, GetLastError());
	}

	ExternInitCall InitProxy = reinterpret_cast<ExternInitCall>(GetProcAddress(DLLHandle, NAMEOF_TEXT(Init)));
	if (!InitProxy)
	{
		throw format("{} :: Can't get {} function handle from {}", __func__, NAMEOF(Init), CLR_PROXY_DLL_NAME);
	}
	InitProxy();

	ExternRunProxyCall RunProxy = reinterpret_cast<ExternRunProxyCall>(GetProcAddress(DLLHandle, NAMEOF_TEXT(RunProxy)));
	if (!RunProxy)
	{
		throw format("{} :: Can't get {} function handle from {}", __func__, NAMEOF(RunProxy), CLR_PROXY_DLL_NAME);
	}

	const PlatformData* platform = static_cast<const PlatformData*>(m_pFilterRecord->platformData);
	const int16 Result = RunProxy(platform->hwnd, m_pFilterRecord, m_pData);

	FreeLibrary(DLLHandle);
	return Result;
}

// I should never get here. Just set the rect(s) to zero and it won't happen again.
void Continue()
{
	if (!m_pFilterRecord)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(m_pFilterRecord));
	}

	if (!m_pFilterRecord->bigDocumentData)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(m_pFilterRecord->bigDocumentData));
	}

	m_pFilterRecord->inRect.top = 0;
	m_pFilterRecord->inRect.bottom = 0;
	m_pFilterRecord->inRect.left = 0;
	m_pFilterRecord->inRect.right = 0;
	m_pFilterRecord->outRect.top = 0;
	m_pFilterRecord->outRect.bottom = 0;
	m_pFilterRecord->outRect.left = 0;
	m_pFilterRecord->outRect.right = 0;
	m_pFilterRecord->maskRect.top = 0;
	m_pFilterRecord->maskRect.bottom = 0;
	m_pFilterRecord->maskRect.left = 0;
	m_pFilterRecord->maskRect.right = 0;

	m_pFilterRecord->bigDocumentData->inRect32.top = 0;
	m_pFilterRecord->bigDocumentData->inRect32.bottom = 0;
	m_pFilterRecord->bigDocumentData->inRect32.left = 0;
	m_pFilterRecord->bigDocumentData->inRect32.right = 0;
	m_pFilterRecord->bigDocumentData->outRect32.top = 0;
	m_pFilterRecord->bigDocumentData->outRect32.bottom = 0;
	m_pFilterRecord->bigDocumentData->outRect32.left = 0;
	m_pFilterRecord->bigDocumentData->outRect32.right = 0;
}

void Exit()
{
	if (!m_pLongData)
	{
		throw format("{} :: {} in Null!", __func__, NAMEOF(m_pLongData));
	}

	if (!m_pFilterRecord)
	{
		throw format("{} :: {} is Null!", __func__, NAMEOF(m_pFilterRecord));
	}

	Handle DataHandle = reinterpret_cast<Handle>(*m_pLongData);
	if (DataHandle != nullptr && m_pFilterRecord->handleProcs != nullptr)
	{
		m_pFilterRecord->handleProcs->unlockProc(DataHandle);
	}
}

DLLExport MACPASCAL void PluginMain(const short selector, FilterRecordPtr filterParamBlock, intptr_t* data, short* result)
{
	try
	{
		Entry(filterParamBlock, data);

		switch (selector)
		{
		case filterSelectorParameters:
			InitParameters();
			break;
		case filterSelectorPrepare:
			Prepare();
			break;
		case filterSelectorStart:
			*result = Start();
			break;
		case filterSelectorContinue:
			Continue();
			break;
		default:
			break;
		}

		Exit();
	}
	catch (string ErrorString)
	{
		OutputDebugString(ErrorString.c_str());

		char* pErrorString = (char*)filterParamBlock->errorString;
		if (pErrorString != nullptr)
		{
			*pErrorString = (char)min(ErrorString.length(), 255);
			memcpy(pErrorString + 1, ErrorString.c_str(), (unsigned char)(*pErrorString));
		}

		*result = errReportString;
	}
	catch (short inError)
	{
		*result = inError;
	}
	catch (...)
	{
		*result = -1;
	}
}