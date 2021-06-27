

namespace CubemapOnTheFly {

	/**
	 * レンダリング方法
	 */
	public enum RenderingMode {

		/**
		 * RTからCubemapへBlitして生成する。
		 * Pluginを使わずにUnityの機能のみで行う。遅い
		 */
		BlitNoUsePlugin,

		/**
		 * RTからCubemapへBlitして生成する。
		 * Pluginを使用してNativeでBlitするが、GetNativeTexturePtrを行うのでやっぱり遅い。
		 */
		BlitUsePlugin,

		/**
		 * Cubemapを直接RTとしてレンダリングする。早い
		 */
		DirectRT,
	}

}
