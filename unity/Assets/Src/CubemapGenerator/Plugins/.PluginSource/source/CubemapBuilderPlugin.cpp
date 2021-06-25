
#include <stddef.h>

#include "PlatformBase.h"
#include "RenderAPI.h"
#include "Unity/IUnityGraphics.h"

#include <assert.h>
#include <math.h>
#include <vector>


static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);





// --------------------------------------------------------------------------
// プラグインロード・アンロード時に呼ばれる処理


static IUnityInterfaces* s_UnityInterfaces = NULL;
static IUnityGraphics* s_Graphics = NULL;

/** WebGLでない場合に、プラグインロード時に自動的に呼ばれる関数 */
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

/** WebGLでない場合に、プラグインアンロード時に自動的に呼ばれる関数 */
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
	s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}

// WebGL版の場合はUnityPluginLoadが自動で呼ばれないので、
// 手動でUnityRegisterRenderingPluginを呼ぶ必要がある。
// 参考：https://forum.unity.com/threads/low-level-plug-in-interface-unityregisterrenderingplugin-and-xr-interfaces.983298/
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
// プラグイン本処理


/** 6つの元テクスチャから、キューブマップを更新する処理 */
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



