
#include <stddef.h>

#include "PlatformBase.h"
#include "RenderAPI.h"
#include "Unity/IUnityGraphics.h"

#include <assert.h>
#include <math.h>
#include <vector>


static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);





// --------------------------------------------------------------------------
// �v���O�C�����[�h�E�A�����[�h���ɌĂ΂�鏈��


static IUnityInterfaces* s_UnityInterfaces = NULL;
static IUnityGraphics* s_Graphics = NULL;

/** WebGL�łȂ��ꍇ�ɁA�v���O�C�����[�h���Ɏ����I�ɌĂ΂��֐� */
extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces * unityInterfaces)
{
	s_UnityInterfaces = unityInterfaces;
	s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
	s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

#if SUPPORT_VULKAN
	if (s_Graphics->GetRenderer() == kUnityGfxRendererNull)
	{
		extern void RenderAPI_Vulkan_OnPluginLoad(IUnityInterfaces*);
		RenderAPI_Vulkan_OnPluginLoad(unityInterfaces);
	}
#endif // SUPPORT_VULKAN

	// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
	OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

/** WebGL�łȂ��ꍇ�ɁA�v���O�C���A�����[�h���Ɏ����I�ɌĂ΂��֐� */
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
	s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}

// WebGL�ł̏ꍇ��UnityPluginLoad�������ŌĂ΂�Ȃ��̂ŁA
// �蓮��UnityRegisterRenderingPlugin���ĂԕK�v������B
// �Q�l�Fhttps://forum.unity.com/threads/low-level-plug-in-interface-unityregisterrenderingplugin-and-xr-interfaces.983298/
#if UNITY_WEBGL
typedef void	(UNITY_INTERFACE_API* PluginLoadFunc)(IUnityInterfaces* unityInterfaces);
typedef void	(UNITY_INTERFACE_API* PluginUnloadFunc)();

extern "C" void	UnityRegisterRenderingPlugin(PluginLoadFunc loadPlugin, PluginUnloadFunc unloadPlugin);

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API RegisterPlugin()
{
	UnityRegisterRenderingPlugin(UnityPluginLoad, UnityPluginUnload);
}
#endif





// --------------------------------------------------------------------------
// GraphicsDeviceEvent


static RenderAPI* s_CurrentAPI = NULL;
static UnityGfxRenderer s_DeviceType = kUnityGfxRendererNull;


static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
	// Create graphics API implementation upon initialization
	if (eventType == kUnityGfxDeviceEventInitialize)
	{
		assert(s_CurrentAPI == NULL);
		s_DeviceType = s_Graphics->GetRenderer();
		s_CurrentAPI = CreateRenderAPI(s_DeviceType);
	}

	// Let the implementation process the device related events
	if (s_CurrentAPI)
	{
		s_CurrentAPI->ProcessDeviceEvent(eventType, s_UnityInterfaces);
	}

	// Cleanup graphics API implementation upon shutdown
	if (eventType == kUnityGfxDeviceEventShutdown)
	{
		delete s_CurrentAPI;
		s_CurrentAPI = NULL;
		s_DeviceType = kUnityGfxRendererNull;
	}
}






// --------------------------------------------------------------------------
// �v���O�C���{����


/** 6�̌��e�N�X�`������A�L���[�u�}�b�v���X�V���鏈�� */
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API BlitCubemap(
	void* srcTex0,
	void* srcTex1,
	void* srcTex2,
	void* srcTex3,
	void* srcTex4,
	void* srcTex5,
	void* cubemapTex,
	int texWidth
) {
	if (s_CurrentAPI)
		s_CurrentAPI->blitCubemap(
			srcTex0,
			srcTex1,
			srcTex2,
			srcTex3,
			srcTex4,
			srcTex5,
			cubemapTex,
			texWidth
		);
}



