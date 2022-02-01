using UnityEngine;
using System.Collections;

/*
 * Warp the image of the model (the render texture of RenderCamera that looks at the models)
 * using the precomputed warp map so that after reflection from curved surface, 
 * the image looks undistorted.
 * モデルの画像（モデルを見るRenderCameraのレンダリングテクスチャ）を、
 * 事前に計算されたワープマップを使って、曲面からの反射後に画像が歪んで見えないようにワープします。
 */
public class WarpBase : MonoBehaviour {
	public float mapDiv = 4095; //0x3FFF;
	// flip texture in y direction
	public bool flipTexture = true;
	// to increase brightness. final_intensity = alpha*original_intensity^power
	public float power = 1, alpha = 1;
	// aspect ratio of the ipad or other tablet. When rotating the coordinate in shader, I need to first
	// convert the 0-1 uv value into pixel position and then rotate
	// iPadや他のタブレットのアスペクト比
	// シェーダ内の座標を回転するとき，最初に0-1 uv値をピクセル位置に変換してから回転する．
	// 4:3 ?
	protected Vector3 tabletScreenScale = new Vector3 (4f, 3f,  1f);
	protected Material material;

	static int LOAD_TEX_COLOR_BIT_DEPTH = 8;

	protected void ConvertRGBATexture2Map(Texture2D encodedMap, float mapDiv, out Texture2D decodedMapResult) {
		decodedMapResult = new Texture2D(encodedMap.width, encodedMap.height, TextureFormat.RGFloat, false);
		decodedMapResult.wrapMode = TextureWrapMode.Clamp;


		// 0-255 の 8bit x 4 = 32bit
		Color32[] encodedColor32 = encodedMap.GetPixels32();
		// 0-1 範囲の浮動小数点
		Color[] mapColor = new Color[encodedColor32.Length];

		print("length = " + encodedColor32.Length);
		Color32 ec;
		for (int i = 0; i < mapColor.Length; ++i)
		{
			ec = encodedColor32[i];
			// ビットシフト演算で各値を変換して格納する．
			mapColor[i].r = ((ec.r << LOAD_TEX_COLOR_BIT_DEPTH) + ec.g) / mapDiv;
			//!! IMPORTANT: origin of opencv image is at top left while the origin of 
			// textures in unity is at bottom left. So map_y needs to be flipped
			// 重要: OpenCV画像の原点は左上で，Unityのテクスチャの原点は左下なので，map_yは反転する必要がある．
			mapColor[i].g = ((ec.b << LOAD_TEX_COLOR_BIT_DEPTH) + ec.a) / mapDiv; 
			if (flipTexture)
				mapColor [i].g = 1 - mapColor [i].g;
		}
		print("done.");

		// 出力テクスチャに格納
		decodedMapResult.SetPixels(mapColor);
		decodedMapResult.Apply();
	}

	// Update is called once per frame
	// LateUpdate is called after update but before rendering
	protected virtual void LateUpdate () {
		// although the texture's rotating eulerZ degree, the uv needs to rotate -eulerZ
		// テクスチャの回転角が eulerZ のとき， uvは -eulerZ 回転させる必要がある．
		Quaternion rot = Quaternion.Euler (0, 0, -RotationManager.RotationAngle);
		// タブレットの大きさのスケールと，RotationManagerから取得した回転角を使用して変換行列を生成して，
		// マテリアル(シェーダ)に値を転送する．
		Matrix4x4 m = Matrix4x4.Scale(new Vector3(1.0f/tabletScreenScale.x, 1.0f/tabletScreenScale.y, 1f)) 
			* Matrix4x4.TRS (Vector3.zero, rot, tabletScreenScale);
		material.SetVector("_TexRotationVec", new Vector4(m[0,0], m[0,1], m[1,0], m[1,1]));
		material.SetFloat ("_power", power);
		material.SetFloat ("_alpha", alpha);
	}
}
