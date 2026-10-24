EXTERN p_D3DPERF_BeginEvent:QWORD
EXTERN p_D3DPERF_EndEvent:QWORD
EXTERN p_D3DPERF_GetStatus:QWORD
EXTERN p_D3DPERF_QueryRepeatFrame:QWORD
EXTERN p_D3DPERF_SetMarker:QWORD
EXTERN p_D3DPERF_SetOptions:QWORD
EXTERN p_D3DPERF_SetRegion:QWORD
EXTERN p_DebugSetLevel:QWORD
EXTERN p_DebugSetMute:QWORD
EXTERN p_Direct3D9EnableMaximizedWindowedModeShim:QWORD
EXTERN p_Direct3DCreate9:QWORD
EXTERN p_Direct3DCreate9Ex:QWORD
EXTERN p_Direct3DCreate9On12:QWORD
EXTERN p_Direct3DCreate9On12Ex:QWORD
EXTERN p_Direct3DShaderValidatorCreate9:QWORD
EXTERN p_PSGPError:QWORD
EXTERN p_PSGPSampleTexture:QWORD

_TEXT SEGMENT

PUBLIC D3DPERF_BeginEvent
D3DPERF_BeginEvent PROC
    lea rax, p_D3DPERF_BeginEvent
    mov rax, QWORD PTR [rax]
    jmp rax
D3DPERF_BeginEvent ENDP

PUBLIC D3DPERF_EndEvent
D3DPERF_EndEvent PROC
    lea rax, p_D3DPERF_EndEvent
    mov rax, QWORD PTR [rax]
    jmp rax
D3DPERF_EndEvent ENDP

PUBLIC D3DPERF_GetStatus
D3DPERF_GetStatus PROC
    lea rax, p_D3DPERF_GetStatus
    mov rax, QWORD PTR [rax]
    jmp rax
D3DPERF_GetStatus ENDP

PUBLIC D3DPERF_QueryRepeatFrame
D3DPERF_QueryRepeatFrame PROC
    lea rax, p_D3DPERF_QueryRepeatFrame
    mov rax, QWORD PTR [rax]
    jmp rax
D3DPERF_QueryRepeatFrame ENDP

PUBLIC D3DPERF_SetMarker
D3DPERF_SetMarker PROC
    lea rax, p_D3DPERF_SetMarker
    mov rax, QWORD PTR [rax]
    jmp rax
D3DPERF_SetMarker ENDP

PUBLIC D3DPERF_SetOptions
D3DPERF_SetOptions PROC
    lea rax, p_D3DPERF_SetOptions
    mov rax, QWORD PTR [rax]
    jmp rax
D3DPERF_SetOptions ENDP

PUBLIC D3DPERF_SetRegion
D3DPERF_SetRegion PROC
    lea rax, p_D3DPERF_SetRegion
    mov rax, QWORD PTR [rax]
    jmp rax
D3DPERF_SetRegion ENDP

PUBLIC DebugSetLevel
DebugSetLevel PROC
    lea rax, p_DebugSetLevel
    mov rax, QWORD PTR [rax]
    jmp rax
DebugSetLevel ENDP

PUBLIC DebugSetMute
DebugSetMute PROC
    lea rax, p_DebugSetMute
    mov rax, QWORD PTR [rax]
    jmp rax
DebugSetMute ENDP

PUBLIC Direct3D9EnableMaximizedWindowedModeShim
Direct3D9EnableMaximizedWindowedModeShim PROC
    lea rax, p_Direct3D9EnableMaximizedWindowedModeShim
    mov rax, QWORD PTR [rax]
    jmp rax
Direct3D9EnableMaximizedWindowedModeShim ENDP

PUBLIC Direct3DCreate9
Direct3DCreate9 PROC
    lea rax, p_Direct3DCreate9
    mov rax, QWORD PTR [rax]
    jmp rax
Direct3DCreate9 ENDP

PUBLIC Direct3DCreate9Ex
Direct3DCreate9Ex PROC
    lea rax, p_Direct3DCreate9Ex
    mov rax, QWORD PTR [rax]
    jmp rax
Direct3DCreate9Ex ENDP

PUBLIC Direct3DCreate9On12
Direct3DCreate9On12 PROC
    lea rax, p_Direct3DCreate9On12
    mov rax, QWORD PTR [rax]
    jmp rax
Direct3DCreate9On12 ENDP

PUBLIC Direct3DCreate9On12Ex
Direct3DCreate9On12Ex PROC
    lea rax, p_Direct3DCreate9On12Ex
    mov rax, QWORD PTR [rax]
    jmp rax
Direct3DCreate9On12Ex ENDP

PUBLIC Direct3DShaderValidatorCreate9
Direct3DShaderValidatorCreate9 PROC
    lea rax, p_Direct3DShaderValidatorCreate9
    mov rax, QWORD PTR [rax]
    jmp rax
Direct3DShaderValidatorCreate9 ENDP

PUBLIC PSGPError
PSGPError PROC
    lea rax, p_PSGPError
    mov rax, QWORD PTR [rax]
    jmp rax
PSGPError ENDP

PUBLIC PSGPSampleTexture
PSGPSampleTexture PROC
    lea rax, p_PSGPSampleTexture
    mov rax, QWORD PTR [rax]
    jmp rax
PSGPSampleTexture ENDP

_TEXT ENDS

END
