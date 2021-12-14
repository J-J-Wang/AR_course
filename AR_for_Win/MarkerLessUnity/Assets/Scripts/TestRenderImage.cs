﻿using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;

public class TestRenderImage : MonoBehaviour {

    public Shader curShader;
    private Material curMaterial;
    byte[] image = new byte[640 * 480 * 3];
    float[] rotation1 = new float[9];
    float[] transform1 = new float[3];
    Texture2D tex;

	bool isshow;
	public GameObject Earth;

    [DllImport("markerlessAR")]
    private static extern int Initialize(int deviceID);
	[DllImport("markerlessAR")]
	private static extern int process_Image(byte[] temp,float[] RotationT,float[] TransformT, ref bool isShow);
	[DllImport("markerlessAR")]
    private static extern int Release();

	[ImageEffectOpaque]

    void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
    {
        if (curShader != null)
        {
            material.SetTexture("tex", tex);
            Graphics.Blit(sourceTexture, destTexture, material);
        }
        else
        {
            Graphics.Blit(sourceTexture, destTexture);
        }
    }
    public Material material
    {
        get
        {
            if (curMaterial == null)
            {
                curMaterial = new Material(curShader);
                curMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            return curMaterial;
        }
    }
    // Use this for initialization
    void Start ()
	{
		Earth.SetActive (false);

		tex = new Texture2D(640, 480, TextureFormat.RGB24, false);

		StreamReader sr = new StreamReader ("./CamIndex.txt");
		string s = sr.ReadLine ();
		int CamIndex = int.Parse(s);
		sr.Close ();
		Initialize(CamIndex);

        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        if (SystemInfo.supportsImageEffects == false)
        {
            enabled = false;
            return;
        }

        if (curShader != null && curShader.isSupported == false)
        {
            enabled = false;
        }

    }
  
    // Update is called once per frame
    void Update ()
	{
			process_Image (image, rotation1, transform1, ref isshow);


			if (isshow == true) {
				Earth.SetActive (true);
			} else {
				Earth.SetActive (false);
			}

			tex.LoadRawTextureData (image);
			tex.Apply ();

			Matrix4x4 x180 = Matrix4x4.identity;
			Vector4 x1 = new Vector4 (1, 0, 0, 0);
			x180.SetColumn (0, x1);
			Vector4 x2 = new Vector4 (0, -1, 0, 0);
			x180.SetColumn (1, x2);
			Vector4 x3 = new Vector4 (0, 0, -1, 0);
			x180.SetColumn (2, x3);
			Vector4 x4 = new Vector4 (0, 0, 0, 1);
			x180.SetColumn (3, x4);

			float qx, qy, qz, qw;
			qw = Mathf.Sqrt (1.0f + rotation1 [0] + rotation1 [4] + rotation1 [8]) / 2.0f;
			qx = -(rotation1 [7] - rotation1 [5]) / (4 * qw);
			qz = -(rotation1 [2] - rotation1 [6]) / (4 * qw);
			qy = (rotation1 [3] - rotation1 [1]) / (4 * qw);

			Matrix4x4 ygx = Matrix4x4.identity;
			Vector4 y1 = new Vector4 (1 - 2 * qy * qy - 2 * qz * qz, 2 * qx * qy + 2 * qz * qw, 2 * qx * qz - 2 * qy * qw, 0);
			ygx.SetColumn (0, y1);
			Vector4 y2 = new Vector4 (2 * qx * qy - 2 * qz * qw, 1 - 2 * qx * qx - 2 * qz * qz, 2 * qy * qz + 2 * qx * qw, 0);
			ygx.SetColumn (1, y2);
			Vector4 y3 = new Vector4 (2 * qx * qz + 2 * qy * qw, 2 * qy * qz - 2 * qx * qw, 1 - 2 * qx * qx - 2 * qy * qy, 0);
			ygx.SetColumn (2, y3);
			Vector4 y4 = new Vector4 (0, 0, 0, 1);
			ygx.SetColumn (3, y4);

			ygx = x180 * ygx;

			Vector4 vz = ygx.GetColumn (1);
			Vector4 vy = ygx.GetColumn (2);
			Quaternion newQ = Quaternion.LookRotation (new Vector3 (vz.x, vz.y, vz.z), new Vector3 (vy.x, vy.y, vy.z));

			transform.rotation = newQ;

			Vector4 t = new Vector4 (transform1 [0], -transform1 [2], transform1 [1], 1);
			transform.position = ygx * t;
    }

    void OnDisable()
    {
        Release();
        if (curMaterial != null)
        {
            DestroyImmediate(curMaterial);
        }
       
    }
}
