#pragma once

#include "PIFilter.h"				// Filter Photoshop header file.

// Globals structures
FilterRecord*	m_pFilterRecord;
intptr_t*		m_pLongData;
char*			m_pData; // Byte array with shaders as string (first byte = IsContainShaders)

// CLRProxy Functions
typedef int(*ExternInitCall)();
typedef int(*ExternRunProxyCall)(intptr_t PhotoshopWindowHandle, void* FilterRectordPtr, void* LastParamsPtr);

void Entry(FilterRecord* pFilterRecord, intptr_t* pData);
void InitParameters();
void Prepare();
short Start();
void Continue();
void Exit();