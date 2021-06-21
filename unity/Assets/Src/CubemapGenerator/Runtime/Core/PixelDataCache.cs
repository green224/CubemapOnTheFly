
using System;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections;



namespace CubemapGenerator.Core {

/**
 * テクスチャのピクセル情報のキャッシュ。
 * 取り方によって色々あるので、モジュール化している
 * 
 * Texture2Dへの反映などの機能は、特にこのアセンブリでは必要ないので入れていない
 */
sealed class PixelDataCache : IDisposable {
	// ------------------------------------- public メンバ ----------------------------------------

	public PixelDataCache(bool useRawTexData) {
		_useRawTexData = useRawTexData;
	}

	/** 指定のTexture2Dからデータを読み込み、キャッシュする */
	public void readFromTex2D(Texture2D src, bool flipX, bool flipY) {

		var w = src.width;
		var h = src.height;

		if (_useRawTexData) {

			var pixelData = src.GetRawTextureData<Color32>();
			var copiedPD = new NativeArray<Color32>( w*h, Allocator.Persistent );

	// なぜかPixelDataの長さが違う。なぜ？
	// if (pixelData.Length != _texSize*_texSize) Debug.LogError("aaa");
			// なぜかCopyFromやコンストラクタでコピーしようとするとエラーを吐くので、仕方なくforでコピーしている
	//		copiedPD.CopyFrom(pixelData);

			// X,Y反転が設定されている場合は、それぞれ反転しながらコピーする
			if (flipX && flipY) {
				for (int y=h-1, i=-1; 0<=y; --y) {
					for (int x=w-1; 0<=x; --x)
						copiedPD[++i] = pixelData[x + y*w];
				}
			} else if (flipX && !flipY) {
				for (int y=0, i=-1; y<h; ++y) {
					for (int x=w-1; 0<=x; --x)
						copiedPD[++i] = pixelData[x + y*w];
				}
			} else if (!flipX && flipY) {
				for (int y=h-1, i=-1; 0<=y; --y) {
					for (int x=0; x<w; ++x)
						copiedPD[++i] = pixelData[x + y*w];
				}
			} else if (!flipX && !flipY) {
				for (int i=0; i<w*h; ++i)
					copiedPD[i] = pixelData[i];
			}
// for (int i=0; i<w*h; ++i) {
// 	var c = copiedPD[i];
// 	var aa = (Color)new Color32(c.g, c.b, c.a, c.r );
// 	aa = aa.linear;
// 	aa = new Color(aa.a, aa.r, aa.g, aa.b);
// 	copiedPD[i] = aa;
// }
			_pixelsRaw = copiedPD;

		} else {

			var srcPixels = src.GetPixels();

			// X,Y反転が設定されている場合は、それぞれ反転しながらコピーする
			var copiedPD = new Color[srcPixels.Length];
			if (flipX && flipY) {
				for (int y=h-1, i=-1; 0<=y; --y) {
					for (int x=w-1; 0<=x; --x)
						copiedPD[++i] = srcPixels[x + y*w];
				}
			} else if (flipX && !flipY) {
				for (int y=0, i=-1; y<h; ++y) {
					for (int x=w-1; 0<=x; --x)
						copiedPD[++i] = srcPixels[x + y*w];
				}
			} else if (!flipX && flipY) {
				for (int y=h-1, i=-1; 0<=y; --y) {
					for (int x=0; x<w; ++x)
						copiedPD[++i] = srcPixels[x + y*w];
				}
			} else if (!flipX && !flipY) {
				for (int i=0; i<w*h; ++i)
					copiedPD[i] = srcPixels[i];
			}
			_pixelsMng = copiedPD;
		}
	}

	/** キャッシュされているデータを、指定のCubemapに適応する */
	public void writeToCubemap(Cubemap cubemap, CubemapFace face) {

		if (_useRawTexData) {
			cubemap.SetPixelData( _pixelsRaw, 0, face );
		} else {
			cubemap.SetPixels( _pixelsMng, face, 0 );
		}
	}

	public void Dispose() {
		if (_isDisposed) return;
		_isDisposed = true;

		if (_pixelsRaw.IsCreated) _pixelsRaw.Dispose();
		_pixelsMng = null;
	}


	// --------------------------------- private / protected メンバ -------------------------------

	bool _useRawTexData;		//!< Texture2DなどのRawPixelDataを使用した処理にするか否か

	bool _isDisposed = false;


	// ピクセル情報のキャッシュ用バッファ。RawTexDataを使用するか否かで二つ用意している
	NativeArray<Color32> _pixelsRaw;
	Color[] _pixelsMng;



	~PixelDataCache() {
		if (!_isDisposed) throw new InvalidProgramException();
	}



	// --------------------------------------------------------------------------------------------
}

}
