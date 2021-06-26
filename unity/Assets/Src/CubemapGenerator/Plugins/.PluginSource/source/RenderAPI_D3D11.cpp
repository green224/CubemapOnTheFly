#include "RenderAPI.h"
#include "PlatformBase.h"

//
// Direct3D 11 用の RenderAPI 実装
//

#if SUPPORT_D3D11

#include <assert.h>
#include <d3d11.h>
#include "Unity/IUnityGraphicsD3D11.h"


class RenderAPI_D3D11 : public RenderAPI
{
public:
	RenderAPI_D3D11()
		: _d3d11(nullptr)
	{}
	virtual ~RenderAPI_D3D11() { }

	virtual void ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces) {
		switch (type) {
		case kUnityGfxDeviceEventInitialize:
			_d3d11 = interfaces->Get<IUnityGraphicsD3D11>();
			break;
		case kUnityGfxDeviceEventShutdown:
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
		auto device = _d3d11->GetDevice();
		ID3D11DeviceContext* ctx = nullptr;
		device->GetImmediateContext(&ctx);

		// 処理対象のテクスチャ
		ID3D11Texture2D* srcTexs[] = {
			static_cast<ID3D11Texture2D*>( srcTex0 ),
			static_cast<ID3D11Texture2D*>( srcTex1 ),
			static_cast<ID3D11Texture2D*>( srcTex2 ),
			static_cast<ID3D11Texture2D*>( srcTex3 ),
			static_cast<ID3D11Texture2D*>( srcTex4 ),
			static_cast<ID3D11Texture2D*>( srcTex5 ),
		};
		auto dstTex = static_cast<ID3D11Texture2D*>( cubemapTex );

		// コピー処理を行う
		for (int i=0; i<6; ++i) {
			ctx->CopySubresourceRegion(dstTex, i, 0, 0, 0, srcTexs[i], 0, nullptr);
		}

		ctx->Release();
	}

private:
	IUnityGraphicsD3D11* _d3d11;
};


RenderAPI* CreateRenderAPI_D3D11() {
	return new RenderAPI_D3D11();
}




#endif // #if SUPPORT_D3D11
