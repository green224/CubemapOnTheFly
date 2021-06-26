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
		: _d3d12(nullptr)
		, _d3d12CmdAlloc(nullptr)
		, _d3d12CmdList(nullptr)
		, _d3d12FenceValue(0)
		, _d3d12Event(nullptr)
	{}
	virtual ~RenderAPI_D3D12() {}

	virtual void ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces) {
		switch (type) {
		case kUnityGfxDeviceEventInitialize:
			_d3d12 = interfaces->Get<IUnityGraphicsD3D12v2>();
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
		auto fence = _d3d12->GetFrameFence();
		if (fence->GetCompletedValue() < _d3d12FenceValue) {
			fence->SetEventOnCompletion(_d3d12FenceValue, _d3d12Event);
			WaitForSingleObject(_d3d12Event, INFINITE);
		}

		// Begin a command list
		_d3d12CmdAlloc->Reset();
		_d3d12CmdList->Reset(_d3d12CmdAlloc, nullptr);



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

			_d3d12CmdList->CopyTextureRegion(&dstLoc, 0, 0, 0, &srcLoc, nullptr);
		}
		_d3d12CmdList->Close();

		// We inform Unity that we expect this resource to be in D3D12_RESOURCE_STATE_COPY_DEST state,
		// and because we do not barrier it ourselves, we tell Unity that no changes are done on our command list.
		UnityGraphicsD3D12ResourceState resourceState = {};
		resourceState.resource = dstTex;
		resourceState.expected = D3D12_RESOURCE_STATE_COPY_DEST;
		resourceState.current = D3D12_RESOURCE_STATE_COPY_DEST;
		_d3d12FenceValue = _d3d12->ExecuteCommandList(_d3d12CmdList, 1, &resourceState);
	}

private:
	const UINT kNodeMask = 0;
	IUnityGraphicsD3D12v2* _d3d12;
	ID3D12CommandAllocator* _d3d12CmdAlloc;
	ID3D12GraphicsCommandList* _d3d12CmdList;
	UINT64 _d3d12FenceValue = 0;
	HANDLE _d3d12Event = nullptr;

	/** このクラスで使用し続けるリソース類を最初に作成する処理 */
	void CreateResources() {
		auto device = _d3d12->GetDevice();

		HRESULT hr = E_FAIL;

		// Command list
		hr = device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&_d3d12CmdAlloc));
		if (FAILED(hr)) OutputDebugStringA("Failed to CreateCommandAllocator.\n");
		hr = device->CreateCommandList(kNodeMask, D3D12_COMMAND_LIST_TYPE_DIRECT, _d3d12CmdAlloc, nullptr, IID_PPV_ARGS(&_d3d12CmdList));
		if (FAILED(hr)) OutputDebugStringA("Failed to CreateCommandList.\n");
		_d3d12CmdList->Close();

		// Fence
		_d3d12FenceValue = 0;
		_d3d12Event = CreateEvent(nullptr, FALSE, FALSE, nullptr);
	}

	/** このクラスで使用し続けるリソース類を最後に破棄する処理 */
	void ReleaseResources() {
		if (_d3d12Event) CloseHandle(_d3d12Event);
		SAFE_RELEASE(_d3d12CmdList);
		SAFE_RELEASE(_d3d12CmdAlloc);
	}
};


RenderAPI* CreateRenderAPI_D3D12() {
	return new RenderAPI_D3D12();
}



#endif // #if SUPPORT_D3D12
