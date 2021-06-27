#include "RenderAPI.h"
#include "PlatformBase.h"

//
// OpenGL Core/ES �p�� RenderAPI ����
//   Supports several flavors: Core, ES2, ES3
//

#if SUPPORT_OPENGL_UNIFIED


#include <assert.h>
#if UNITY_IOS || UNITY_TVOS
#	include <OpenGLES/ES2/gl.h>
#elif UNITY_ANDROID || UNITY_WEBGL
#	include <GLES2/gl2.h>
#elif UNITY_OSX
#	include <OpenGL/gl3.h>
#elif UNITY_WIN
// On Windows, use gl3w to initialize and load OpenGL Core functions. In principle any other
// library (like GLEW, GLFW etc.) can be used; here we use gl3w since it's simple and
// straightforward.
#	include "gl3w/gl3w.h"
#elif UNITY_LINUX
#	define GL_GLEXT_PROTOTYPES
#	include <GL/gl.h>
#else
#	error Unknown platform
#endif


class RenderAPI_OpenGLCoreES : public RenderAPI
{
public:
	RenderAPI_OpenGLCoreES(UnityGfxRenderer apiType)
		: _apiType(apiType)
		, _frameBuffer(NULL)
	{}
	virtual ~RenderAPI_OpenGLCoreES() {}

	virtual void ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces) {
		switch (type) {
		case kUnityGfxDeviceEventInitialize:
			CreateResources();
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
		// �����Ώۂ̃e�N�X�`��
		GLuint srcTexs[] = {
			reinterpret_cast<GLuint>( srcTex0 ),
			reinterpret_cast<GLuint>( srcTex1 ),
			reinterpret_cast<GLuint>( srcTex2 ),
			reinterpret_cast<GLuint>( srcTex3 ),
			reinterpret_cast<GLuint>( srcTex4 ),
			reinterpret_cast<GLuint>( srcTex5 ),
		};
		auto dstTex = reinterpret_cast<GLuint>( cubemapTex );

		// �����ES3.1�ȍ~����Ȃ��Ǝ��Ȃ����ۂ��̂ŁA�p�����[�^����n���悤�ɂ���
//		int w, h;
//		glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_WIDTH, &w);
//		glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_HEIGHT, &h);

		// Texture���e�ʂ�Blit����
		for (int i=0; i<6; ++i) {
			blitTexByFrameBuffer(
				srcTexs[i],
				GL_TEXTURE_2D,
				dstTex,
				GL_TEXTURE_CUBE_MAP_POSITIVE_X + i,
				texWidth
			);
		}
	}

private:
	UnityGfxRenderer _apiType;
	GLuint _frameBuffer;

	/** ���̃N���X�Ŏg�p�������郊�\�[�X�ނ��ŏ��ɍ쐬���鏈�� */
	void CreateResources() {
		#	if SUPPORT_OPENGL_CORE && UNITY_WIN
			if (_apiType == kUnityGfxRendererOpenGLCore)
				gl3wInit();
		#	endif

		glGenFramebuffers(1, &_frameBuffer);
	}

	/** ���̃N���X�Ŏg�p�������郊�\�[�X�ނ��Ō�ɔj�����鏈�� */
	void ReleaseResources() {
		glDeleteFramebuffers(1, &_frameBuffer);
		_frameBuffer = NULL;
	}

	/** �t���[���o�b�t�@���g�p���āA�e�N�X�`�����R�s�[���� */
	void blitTexByFrameBuffer(
		GLuint srcTex,
		GLenum srcTexTgt,
		GLuint dstTex,
		GLenum dstTexTgt,
		int texWidth
	) {
		// �Q�l�Fhttps://gamedev.net/forums/topic/632847-how-do-i-do-opengl-texture-blitting/4990712/
		glBindFramebuffer(GL_FRAMEBUFFER, _frameBuffer);

#if UNITY_ANDROID || UNITY_WEBGL
		// TODO : ES2.0����GL_COLOR_ATTACHMENT1���g���Ȃ��̂ŁA��փR�[�h������
		// �Q�l�Fhttps://stackoverflow.com/questions/25439137/alternative-for-glblitframebuffer-in-opengl-es-2-0
#else
		// attach the textures to the frame buffer
		glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, srcTexTgt, srcTex, 0);
		glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT1, dstTexTgt, dstTex, 0);

		GLenum fboStatus = glCheckFramebufferStatus(GL_FRAMEBUFFER);
		if (fboStatus != GL_FRAMEBUFFER_COMPLETE) {
			assert(false);
			return;
		}

		GLenum bufferlist[] = { GL_COLOR_ATTACHMENT0, GL_COLOR_ATTACHMENT1 };
		glReadBuffer(bufferlist[0]);
		glDrawBuffers(1, &bufferlist[1]);
		glBlitFramebuffer(
			0, 0, texWidth, texWidth,
			0, 0, texWidth, texWidth,
			GL_COLOR_BUFFER_BIT, GL_NEAREST
		);
#endif

		glBindFramebuffer(GL_FRAMEBUFFER, 0); // Disable FBO when done
	}
};


RenderAPI* CreateRenderAPI_OpenGLCoreES(UnityGfxRenderer apiType) {
	return new RenderAPI_OpenGLCoreES(apiType);
}


#endif // #if SUPPORT_OPENGL_UNIFIED
