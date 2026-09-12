using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class ReferencePhotoMatchV2Backup : ScriptableObject
{
    public bool hasBackup;
    public bool fog;
    public FogMode fogMode;
    public Color fogColor;
    public float fogDensity;
    public AmbientMode ambientMode;
    public Color ambientLight;
    public Color ambientSkyColor;
    public Color ambientEquatorColor;
    public Color ambientGroundColor;
    public float ambientIntensity;
    public float reflectionIntensity;
    public DefaultReflectionMode defaultReflectionMode;
    public Material skybox;
    public Light sun;
    public Volume volume;
    public bool volumeEnabled;
    public bool volumeGlobal;
    public float volumePriority;
    public float volumeWeight;
    public VolumeProfile volumeProfile;
    public Camera camera;
    public ReferencePhotoMatchV2CameraSnapshot cameraSnapshot = new ReferencePhotoMatchV2CameraSnapshot();
    public List<ReferencePhotoMatchV2LightSnapshot> lights = new List<ReferencePhotoMatchV2LightSnapshot>();
    public Renderer roadRenderer;
    public Material[] roadMaterials;
    public ScriptableRendererData rendererData;
    public Material rendererPassMaterial;
    public bool profileExisted;
    public bool fogMaterialExisted;
    public bool roadMaterialExisted;
    public bool skyMaterialExisted;
    public bool guardrailMaterialExisted;
    public bool edgeLineMaterialExisted;
}
