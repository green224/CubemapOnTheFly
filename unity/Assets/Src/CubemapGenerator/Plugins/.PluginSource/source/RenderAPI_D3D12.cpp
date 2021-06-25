#include "RenderAPI.h"
#include "PlatformBase.h"

#include <cmath>

//
// Direct3D 12 用の RenderAPI 実装
//

#if SUPPORT_D3D12

#include <assert.h>
#include <d3d12.h>
#include "Unity/IUnityGraphicsD3D12.h"


class RenderAPI_D3D12 : public RenderAPI
{
public:
	RenderAPI_D3D12()
		: s_D3D12(nullptr)
		, s_D3D12CmdAlloc(nullptr)
		, s_D3D12CmdList(nullptr)
		, s_D3D12FenceValue(0)
		, s_D3D12Event(nullptr)
	{}
	virtual ~RenderAPI_D3D12() {}

	virtual void ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces) {
		switch (type) {
		case kUnityGfxDeviceEventInitialize:
			s_D3D12 = interfaces->Get<IUnityGraphicsD3D12v2>();
			CreateResources();
			break;
		case kUnityGfxDeviceEventShutdown:
			ReleaseResources();
			break;
		}
	}

	virtual void blitCubemap(
		void* srcTex0,
		void* srcTex1,
		void* srcTex2,
		void* srcTex3,
		void* srcTex4,
		void* srcTex5,
		void* cubemapTex,
		int texWidth
	) {

		// Wait on the previous job (example only - simplifies resource management)
		auto fence = s_D3D12->GetFrameFence();
		if (fence->GetCompletedValue() < s_D3D12FenceValue) {
			fence->SetEventOnCompletion(s_D3D12FenceValue, s_D3D12Event);
			WaitForSingleObject(s_D3D12Event, INFINITE);
		}

		// Begin a command list
		s_D3D12CmdAlloc->Reset();
		s_D3D12CmdList->Reset(s_D3D12CmdAlloc, nullptr);



		// 処理対象のテクスチャ
		ID3D12Resource* srcTexs[] = {
			static_cast<ID3D12Resource*>( srcTex0 ),
			static_cast<ID3D12Resource*>( srcTex1 ),
			static_cast<ID3D12Resource*>( srcTex2 ),
			static_cast<ID3D12Resource*>( srcTex3 ),
			static_cast<ID3D12Resource*>( srcTex4 ),
			static_cast<ID3D12Resource*>( srcTex5 ),
		};
		auto dstTex = static_cast<ID3D12Resource*>( cubemapTex );

		// コピー処理をコマンドリストに詰める
		for (int i=0; i<6; ++i) {
			D3D12_TEXTURE_COPY_LOCATION srcLoc = {};
			srcLoc.pResource = srcTexs[i];
			srcLoc.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
			srcLoc.SubresourceIndex = 0;

			D3D12_TEXTURE_COPY_LOCATION dstLoc = {};
			dstLoc.pResource = dstTex;
			dstLoc.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
			dstLoc.SubresourceIndex = i;

			s_D3D12CmdList->CopyTextureRegion(&dstLoc, 0, 0, 0, &srcLoc, nullptr);
		}
		s_D3D12CmdList->Close();

		// We inform Unity that we expect this resource to be in D3D12_RESOURCE_STATE_COPY_DEST state,
		// and because we do not barrier it ourselves, we tell Unity that no changes are done on our command list.
		UnityGraphicsD3D12ResourceState resourceState = {};
		resourceState.resource = dstTex;
		resourceState.expected = D3D12_RESOURCE_STATE_COPY_DEST;
		resourceState.current = D3D12_RESOURCE_STATE_COPY_DEST;
		s_D3D12FenceValue = s_D3D12->ExecuteCommandList(s_D3D12CmdList, 1, &resourceState);
	}

private:
	const UINT kNodeMask = 0;
	IUnityGraphicsD3D12v2* s_D3D12;
	ID3D12CommandAllocator* s_D3D12CmdAlloc;
	ID3D12GraphicsCommandList* s_D3D12CmdList;
	UINT64 s_D3D12FenceValue = 0;
	HANDLE s_D3D12Event = nullptr;

	/** このクラスで使用し続けるリソース類を最初に作成する処理 */
	void CreateResources() {
		auto device = s_D3D12->GetDevice();

		HRESULT hr = E_FAIL;

		// Command list
		hr = device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&s_D3D12CmdAlloc));
		if (FAILED(hr)) OutputDebugStringA("Failed to CreateCommandAllocator.\n");
		hr = device->CreateCommandList(kNodeMask, D3D12_COMMAND_LIST_TYPE_DIRECT, s_D3D12CmdAlloc, nullptr, IID_PPV_ARGS(&s_D3D12CmdList));
		if (FAILED(hr)) OutputDebugStringA("Failed to CreateCommandList.\n");
		s_D3D12CmdList->Close();

		// Fence
		s_D3D12FenceValue = 0;
		s_D3D12Event = CreateEvent(nullptr, FALSE, FALSE, nullptr);
	}

	/** このクラスで使用し続けるリソース類を最後に破棄する処理 */
	void ReleaseResources() {
		if (s_D3D12Event) CloseHandle(s_D3D12Event);
		SAFE_RELEASE(s_D3D12CmdList);
		SAFE_RELEASE(s_D3D12CmdAlloc);
	}
};


RenderAPI* CreateRenderAPI_D3D12() {
	return new RenderAPI_D3D12();
}



#endif // #if SUPPORT_D3D12
