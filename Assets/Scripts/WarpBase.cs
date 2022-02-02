using UnityEngine;
using System.Collections;
using System.IO;

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

	// テクスチャ画像からシェーダー用のMapTexに変換する．
	// 画像のRG -> R, BA -> Gに変換．
	protected void ConvertRGBATexture2Map(Texture2D encodedMap, float mapDiv, out Texture2D decodedMapResult) {
		// 元のテクスチャ画像と同じ大きさで32 bit浮動小数点のRed, Greenを持つテクスチャを生成．
		decodedMapResult = new Texture2D(encodedMap.width, encodedMap.height, TextureFormat.RGFloat, false);

		// 保存用
		const bool isSave = false;
		if (isSave) {
			decodedMapResult = new Texture2D(encodedMap.width, encodedMap.height, TextureFormat.RGBAFloat, false);
		}

		// UV座標が0-1の範囲にクランプする．
		// 超えた場合は，境界の最後のピクセルが使用される．
		decodedMapResult.wrapMode = TextureWrapMode.Clamp;

		// 0-255 の 8bit x 4 = 32bitの色を持つ一次元配列を取得．
		Color32[] encodedColor32 = encodedMap.GetPixels32();
		// 0-1 範囲の浮動小数点の色を持つ一次元配列を取得．
		Color[] mapColor = new Color[encodedColor32.Length];

		print("length = " + encodedColor32.Length);
		Color32 ec;
		for (int i = 0; i < mapColor.Length; ++i)
		{
			ec = encodedColor32[i];
			// ビットシフト演算で各値を変換して格納する．
			// Redを8bit左にずらして，Greenを足す．その後に，4095 (0x3FFF) で割る
			// 0x3FFF 以上のピクセルは1になる．
			mapColor[i].r = ((ec.r << LOAD_TEX_COLOR_BIT_DEPTH) + ec.g) / mapDiv;
			//!! IMPORTANT: origin of opencv image is at top left while the origin of 
			// textures in unity is at bottom left. So map_y needs to be flipped
			// 重要: OpenCV画像の原点は左上で，Unityのテクスチャの原点は左下なので，map_yは反転する必要がある．
			// Blueを8bit左にずらして，Alphaを足す．その後に，4095 (0x3FFF) で割る
			// 0x3FFF 以上のピクセルは1になる．
			mapColor[i].g = ((ec.b << LOAD_TEX_COLOR_BIT_DEPTH) + ec.a) / mapDiv; 

			if (flipTexture) {
				// Greenの値を反転する．
				mapColor [i].g = 1 - mapColor [i].g;
			}

			// 保存用
			if (isSave) {
				mapColor[i].b = 0;
				mapColor[i].a = 1;
				const float EPS = 0.001f; //1e-3;
				if ((EPS <= mapColor[i].r && mapColor[i].r <= 1-EPS) && (EPS <= mapColor[i].g && mapColor[i].g <= 1-EPS)) {
					// 何もしない．
				} else {
					mapColor[i].r = 0;
					mapColor[i].g = 0;
				}
			}
		}
		print("done.");

		if (isSave) {
			// [ImageConversion-EncodeToPNG - Unity スクリプトリファレンス](https://docs.unity3d.com/ja/current/ScriptReference/ImageConversion.EncodeToPNG.html)
			// Save Decoded Texture
			// RGBAFloatはexrで保存したほうが良い?
			byte[] bytes = decodedMapResult.EncodeToPNG();
			File.WriteAllBytes(Application.dataPath + "/../decoded_map.png", bytes);

			// [Unity - Scripting API: ImageConversion.EncodeToEXR](https://docs.unity3d.com/ScriptReference/ImageConversion.EncodeToEXR.html)
			// Encode texture into the EXR
			// byte[] bytes = decodedMap.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
			// File.WriteAllBytes(Application.dataPath + "/../decoded_map.exr", bytes);

			Debug.Log(Application.dataPath);
		}

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
