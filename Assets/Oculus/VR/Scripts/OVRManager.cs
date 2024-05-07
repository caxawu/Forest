/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS
#define USING_XR_SDK
#endif

#if UNITY_2020_1_OR_NEWER
#define REQUIRES_XR_SDK
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
#define OVR_ANDROID_MRC
#endif

#if !UNITY_2018_3_OR_NEWER
#error Oculus Utilities require Unity 2018.3 or higher.
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.Rendering;

#if USING_XR_SDK
using UnityEngine.XR;
using UnityEngine.Experimental.XR;
#endif

using Settings = UnityEngine.XR.XRSettings;
using Node = UnityEngine.XR.XRNode;

/// <summary>
/// Configuration data for Oculus virtual reality.
/// </summary>
public class OVRManager : MonoBehaviour, OVRMixedRealityCaptureConfiguration
{
	public enum XrApi
	{
		Unknown = OVRPlugin.XrApi.Unknown,
		CAPI = OVRPlugin.XrApi.CAPI,
		VRAPI = OVRPlugin.XrApi.VRAPI,
		OpenXR = OVRPlugin.XrApi.OpenXR,
	}

	public enum TrackingOrigin
	{
		EyeLevel = OVRPlugin.TrackingOrigin.EyeLevel,
		FloorLevel = OVRPlugin.TrackingOrigin.FloorLevel,
		Stage = OVRPlugin.TrackingOrigin.Stage,
	}

	public enum EyeTextureFormat
	{
		Default = OVRPlugin.EyeTextureFormat.Default,
		R16G16B16A16_FP = OVRPlugin.EyeTextureFormat.R16G16B16A16_FP,
		R11G11B10_FP = OVRPlugin.EyeTextureFormat.R11G11B10_FP,
	}

	public enum FixedFoveatedRenderingLevel
	{
		Off = OVRPlugin.FixedFoveatedRenderingLevel.Off,
		Low = OVRPlugin.FixedFoveatedRenderingLevel.Low,
		Medium = OVRPlugin.FixedFoveatedRenderingLevel.Medium,
		High = OVRPlugin.FixedFoveatedRenderingLevel.High,
		HighTop = OVRPlugin.FixedFoveatedRenderingLevel.HighTop,
	}

	[Obsolete("Please use FixedFoveatedRenderingLevel instead")]
	public enum TiledMultiResLevel
	{
		Off = OVRPlugin.TiledMultiResLevel.Off,
		LMSLow = OVRPlugin.TiledMultiResLevel.LMSLow,
		LMSMedium = OVRPlugin.TiledMultiResLevel.LMSMedium,
		LMSHigh = OVRPlugin.TiledMultiResLevel.LMSHigh,
		LMSHighTop = OVRPlugin.TiledMultiResLevel.LMSHighTop,
	}

	public enum SystemHeadsetType
	{
		None = OVRPlugin.SystemHeadset.None,

		// Standalone headsets
		Oculus_Quest = OVRPlugin.SystemHeadset.Oculus_Quest,
		Oculus_Quest_2 = OVRPlugin.SystemHeadset.Oculus_Quest_2,
		Placeholder_10 = OVRPlugin.SystemHeadset.Placeholder_10,
		Placeholder_11 = OVRPlugin.SystemHeadset.Placeholder_11,
		Placeholder_12 = OVRPlugin.SystemHeadset.Placeholder_12,
		Placeholder_13 = OVRPlugin.SystemHeadset.Placeholder_13,
		Placeholder_14 = OVRPlugin.SystemHeadset.Placeholder_14,

		// PC headsets
		Rift_DK1 = OVRPlugin.SystemHeadset.Rift_DK1,
		Rift_DK2 = OVRPlugin.SystemHeadset.Rift_DK2,
		Rift_CV1 = OVRPlugin.SystemHeadset.Rift_CV1,
		Rift_CB = OVRPlugin.SystemHeadset.Rift_CB,
		Rift_S = OVRPlugin.SystemHeadset.Rift_S,
		Oculus_Link_Quest = OVRPlugin.SystemHeadset.Oculus_Link_Quest,
		Oculus_Link_Quest_2 = OVRPlugin.SystemHeadset.Oculus_Link_Quest_2,
		PC_Placeholder_4103 = OVRPlugin.SystemHeadset.PC_Placeholder_4103,
		PC_Placeholder_4104 = OVRPlugin.SystemHeadset.PC_Placeholder_4104,
		PC_Placeholder_4105 = OVRPlugin.SystemHeadset.PC_Placeholder_4105,
		PC_Placeholder_4106 = OVRPlugin.SystemHeadset.PC_Placeholder_4106,
		PC_Placeholder_4107 = OVRPlugin.SystemHeadset.PC_Placeholder_4107
	}

	public enum XRDevice
	{
		Unknown = 0,
		Oculus = 1,
		OpenVR = 2,
	}

	public enum ColorSpace
	{
		Unknown = OVRPlugin.ColorSpace.Unknown,
		Unmanaged = OVRPlugin.ColorSpace.Unmanaged,
		Rec_2020 = OVRPlugin.ColorSpace.Rec_2020,
		Rec_709 = OVRPlugin.ColorSpace.Rec_709,
		Rift_CV1 = OVRPlugin.ColorSpace.Rift_CV1,
		Rift_S = OVRPlugin.ColorSpace.Rift_S,
		Quest = OVRPlugin.ColorSpace.Quest,
		P3 = OVRPlugin.ColorSpace.P3,
		Adobe_RGB = OVRPlugin.ColorSpace.Adobe_RGB,
	}

	/// <summary>
	/// Gets the singleton instance.
	/// </summary>
	public static OVRManager instance { get; private set; }

	/// <summary>
	/// Gets a reference to the active display.
	/// </summary>
	public static OVRDisplay display { get; private set; }

	/// <summary>
	/// Gets a reference to the active sensor.
	/// </summary>
	public static OVRTracker tracker { get; private set; }

	/// <summary>
	/// Gets a reference to the active boundary system.
	/// </summary>
	public static OVRBoundary boundary { get; private set; }

	private static OVRProfile _profile;
	/// <summary>
	/// Gets the current profile, which contains information about the user's settings and body dimensions.
	/// </summary>
	public static OVRProfile profile
	{
		get {
			if (_profile == null)
				_profile = new OVRProfile();

			return _profile;
		}
	}

	private IEnumerable<Camera> disabledCameras;
	float prevTimeScale;

	/// <summary>
	/// Occurs when an HMD attached.
	/// </summary>
	public static event Action HMDAcquired;

	/// <summary>
	/// Occurs when an HMD detached.
	/// </summary>
	public static event Action HMDLost;

	/// <summary>
	/// Occurs when an HMD is put on the user's head.
	/// </summary>
	public static event Action HMDMounted;

	/// <summary>
	/// Occurs when an HMD is taken off the user's head.
	/// </summary>
	public static event Action HMDUnmounted;

	/// <summary>
	/// Occurs when VR Focus is acquired.
	/// </summary>
	public static event Action VrFocusAcquired;

	/// <summary>
	/// Occurs when VR Focus is lost.
	/// </summary>
	public static event Action VrFocusLost;

	/// <summary>
	/// Occurs when Input Focus is acquired.
	/// </summary>
	public static event Action InputFocusAcquired;

	/// <summary>
	/// Occurs when Input Focus is lost.
	/// </summary>
	public static event Action InputFocusLost;

	/// <summary>
	/// Occurs when the active Audio Out device has changed and a restart is needed.
	/// </summary>
	public static event Action AudioOutChanged;

	/// <summary>
	/// Occurs when the active Audio In device has changed and a restart is needed.
	/// </summary>
	public static event Action AudioInChanged;

	/// <summary>
	/// Occurs when the sensor gained tracking.
	/// </summary>
	public static event Action TrackingAcquired;

	/// <summary>
	/// Occurs when the sensor lost tracking.
	/// </summary>
	public static event Action TrackingLost;

	/// <summary>
	/// Occurs when the display refresh rate changes
	/// @params (float fromRefreshRate, float toRefreshRate)
	/// </summary>
	public static event Action<float, float> DisplayRefreshRateChanged;

	/// <summary>
	/// Occurs when Health & Safety Warning is dismissed.
	/// </summary>
	//Disable the warning about it being unused. It's deprecated.
#pragma warning disable 0067
	[Obsolete]
	public static event Action HSWDismissed;
#pragma warning restore

	private static bool _isHmdPresentCached = false;
	private static bool _isHmdPresent = false;
	private static bool _wasHmdPresent = false;
	/// <summary>
	/// If true, a head-mounted display is connected and present.
	/// </summary>
	public static bool isHmdPresent
	{
		get {
			if (!_isHmdPresentCached)
			{
				_isHmdPresentCached = true;
				_isHmdPresent = OVRNodeStateProperties.IsHmdPresent();
			}

			return _isHmdPresent;
		}

		private set {
			_isHmdPresentCached = true;
			_isHmdPresent = value;
		}
	}

	/// <summary>
	/// Gets the audio output device identifier.
	/// </summary>
	/// <description>
	/// On Windows, this is a string containing the GUID of the IMMDevice for the Windows audio endpoint to use.
	/// </description>
	public static string audioOutId
	{
		get { return OVRPlugin.audioOutId; }
	}

	/// <summary>
	/// Gets the audio input device identifier.
	/// </summary>
	/// <description>
	/// On Windows, this is a string containing the GUID of the IMMDevice for the Windows audio endpoint to use.
	/// </description>
	public static string audioInId
	{
		get { return OVRPlugin.audioInId; }
	}

	private static bool _hasVrFocusCached = false;
	private static bool _hasVrFocus = false;
	private static bool _hadVrFocus = false;
	/// <summary>
	/// If true, the app has VR Focus.
	/// </summary>
	public static bool hasVrFocus
	{
		get {
			if (!_hasVrFocusCached)
			{
				_hasVrFocusCached = true;
				_hasVrFocus = OVRPlugin.hasVrFocus;
			}

			return _hasVrFocus;
		}

		private set {
			_hasVrFocusCached = true;
			_hasVrFocus = value;
		}
	}

	private static bool _hadInputFocus = true;
	/// <summary>
	/// If true, the app has Input Focus.
	/// </summary>
	public static bool hasInputFocus
	{
		get
		{
			return OVRPlugin.hasInputFocus;
		}
	}

	/// <summary>
	/// If true, chromatic de-aberration will be applied, improving the image at the cost of texture bandwidth.
	/// </summary>
	public bool chromatic
	{
		get {
			if (!isHmdPresent)
				return false;

			return OVRPlugin.chromatic;
		}

		set {
			if (!isHmdPresent)
				return;

			OVRPlugin.chromatic = value;
		}
	}

	[Header("Performance/Quality")]
	/// <summary>
	/// If true, Unity will use the optimal antialiasing level for quality/performance on the current hardware.
	/// </summary>
	[Tooltip("If true, Unity will use the optimal antialiasing level for quality/performance on the current hardware.")]
	public bool useRecommendedMSAALevel = true;

	/// <summary>
	/// If true, both eyes will see the same image, rendered from the center eye pose, saving performance.
	/// </summary>
	[SerializeField]
	[Tooltip("If true, both eyes will see the same image, rendered from the center eye pose, saving performance.")]
	private bool _monoscopic = false;

	public bool monoscopic
	{
		get
		{
			if (!isHmdPresent)
				return _monoscopic;

			return OVRPlugin.monoscopic;
		}

		set
		{
			if (!isHmdPresent)
				return;

			OVRPlugin.monoscopic = value;
			_monoscopic = value;
		}
	}

	/// <summary>
	/// If true, dynamic resolution will be enabled
	/// </summary>
	[Tooltip("If true, dynamic resolution will be enabled On PC")]
	public bool enableAdaptiveResolution = false;

	[SerializeField, HideInInspector]
	private OVRManager.ColorSpace _colorGamut = OVRManager.ColorSpace.Rift_CV1;

	/// <summary>
	/// The target color gamut the HMD will perform a color space transformation to
	/// </summary>
	public OVRManager.ColorSpace colorGamut
	{
		get
		{
			return _colorGamut;
		}
		set
		{
			_colorGamut = value;
			OVRPlugin.SetClientColorDesc((OVRPlugin.ColorSpace)_colorGamut);
		}
	}

	/// <summary>
	/// The native color gamut of the target HMD
	/// </summary>
	public OVRManager.ColorSpace nativeColorGamut
	{
		get
		{
			return (OVRManager.ColorSpace)OVRPlugin.GetHmdColorDesc();
		}
	}

	/// <summary>
	/// Adaptive Resolution is based on Unity engine's renderViewportScale/eyeTextureResolutionScale feature
	/// But renderViewportScale was broken in an array of Unity engines, this function help to filter out those broken engines
	/// </summary>
	public static bool IsAdaptiveResSupportedByEngine()
	{
		return true;
	}

	/// <summary>
	/// Min RenderScale the app can reach under adaptive resolution mode ( enableAdaptiveResolution = true );
	/// </summary>
	[RangeAttribute(0.5f, 2.0f)]
	[Tooltip("Min RenderScale the app can reach under adaptive resolution mode")]
	public float minRenderScale = 0.7f;

	/// <summary>
	/// Max RenderScale the app can reach under adaptive resolution mode ( enableAdaptiveResolution = true );
	/// </summary>
	[RangeAttribute(0.5f, 2.0f)]
	[Tooltip("Max RenderScale the app can reach under adaptive resolution mode")]
	public float maxRenderScale = 1.0f;

	/// <summary>
	/// Set the relative offset rotation of head poses
	/// </summary>
	[SerializeField]
	[Tooltip("Set the relative offset rotation of head poses")]
	private Vector3 _headPoseRelativeOffsetRotation;
	public Vector3 headPoseRelativeOffsetRotation
	{
		get
		{
			return _headPoseRelativeOffsetRotation;
		}
		set
		{
			OVRPlugin.Quatf rotation;
			OVRPlugin.Vector3f translation;
			if (OVRPlugin.GetHeadPoseModifier(out rotation, out translation))
			{
				Quaternion finalRotation = Quaternion.Euler(value);
				rotation = finalRotation.ToQuatf();
				OVRPlugin.SetHeadPoseModifier(ref rotation, ref translation);
			}
			_headPoseRelativeOffsetRotation = value;
		}
	}

	/// <summary>
	/// Set the relative offset translation of head poses
	/// </summary>
	[SerializeField]
	[Tooltip("Set the relative offset translation of head poses")]
	private Vector3 _headPoseRelativeOffsetTranslation;
	public Vector3 headPoseRelativeOffsetTranslation
	{
		get
		{
			return _headPoseRelativeOffsetTranslation;
		}
		set
		{
			OVRPlugin.Quatf rotation;
			OVRPlugin.Vector3f translation;
			if (OVRPlugin.GetHeadPoseModifier(out rotation, out translation))
			{
				if (translation.FromFlippedZVector3f() != value)
				{
					translation = value.ToFlippedZVector3f();
					OVRPlugin.SetHeadPoseModifier(ref rotation, ref translation);
				}
			}
			_headPoseRelativeOffsetTranslation = value;
		}
	}

	/// <summary>
	/// The TCP listening port of Oculus Profiler Service, which will be activated in Debug/Developerment builds
	/// When the app is running on editor or device, open "Tools/Oculus/Oculus Profiler Panel" to view the realtime system metrics
	/// </summary>
	public int profilerTcpPort = OVRSystemPerfMetrics.TcpListeningPort;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
	/// <summary>
	/// If true, the MixedRealityCapture properties will be displayed
	/// </summary>
	[HideInInspector]
	public bool expandMixedRealityCapturePropertySheet = false;


	/// <summary>
	/// If true, Mixed Reality mode will be enabled
	/// </summary>
	[HideInInspector, Tooltip("If true, Mixed Reality mode will be enabled. It would be always set to false when the game is launching without editor")]
	public bool enableMixedReality = false;

	public enum CompositionMethod
	{
		External,
		Direct
	}

	/// <summary>
	/// Composition method
	/// </summary>
	[HideInInspector]
	public CompositionMethod compositionMethod = CompositionMethod.External;

	/// <summary>
	/// Extra hidden layers
	/// </summary>
	[HideInInspector, Tooltip("Extra hidden layers")]
	public LayerMask extraHiddenLayers;

	/// <summary>
	/// Extra visible layers
	/// </summary>
	[HideInInspector, Tooltip("Extra visible layers")]
	public LayerMask extraVisibleLayers;

	/// <summary>
	/// If premultipled alpha blending is used for the eye fov layer.
	/// Useful for changing how the eye fov layer blends with underlays.
	/// </summary>
	[HideInInspector]
	public static bool eyeFovPremultipliedAlphaModeEnabled
	{
		get
		{
			return OVRPlugin.eyeFovPremultipliedAlphaModeEnabled;
		}
		set
		{
			OVRPlugin.eyeFovPremultipliedAlphaModeEnabled = value;
		}
	}

	/// <summary>
	/// Whether MRC should dynamically update the culling mask using the Main Camera's culling mask, extraHiddenLayers, and extraVisibleLayers
	/// </summary>
	[HideInInspector, Tooltip("Dynamic Culling Mask")]
	public bool dynamicCullingMask = true;

	/// <summary>
	/// The backdrop color will be used when rendering the foreground frames (on Rift). It only applies to External Composition.
	/// </summary>
	[HideInInspector, Tooltip("Backdrop color for Rift (External Compositon)")]
	public Color externalCompositionBackdropColorRift = Color.green;

	/// <summary>
	/// The backdrop color will be used when rendering the foreground frames (on Quest). It only applies to External Composition.
	/// </summary>
	[HideInInspector, Tooltip("Backdrop color for Quest (External Compositon)")]
	public Color externalCompositionBackdropColorQuest = Color.clear;

	/// <summary>
	/// If true, Mixed Reality mode will use direct composition from the first web camera
	/// </summary>

	public enum CameraDevice
	{
		WebCamera0,
		WebCamera1,
		ZEDCamera
	}

	/// <summary>
	/// The camera device for direct composition
	/// </summary>
	[HideInInspector, Tooltip("The camera device for direct composition")]
	public CameraDevice capturingCameraDevice = CameraDevice.WebCamera0;

	/// <summary>
	/// Flip the camera frame horizontally
	/// </summary>
	[HideInInspector, Tooltip("Flip the camera frame horizontally")]
	public bool flipCameraFrameHorizontally = false;

	/// <summary>
	/// Flip the camera frame vertically
	/// </summary>
	[HideInInspector, Tooltip("Flip the camera frame vertically")]
	public bool flipCameraFrameVertically = false;

	/// <summary>
	/// Delay the touch controller pose by a short duration (0 to 0.5 second) to match the physical camera latency
	/// </summary>
	[HideInInspector, Tooltip("Delay the touch controller pose by a short duration (0 to 0.5 second) to match the physical camera latency")]
	public float handPoseStateLatency = 0.0f;

	/// <summary>
	/// Delay the foreground / background image in the sandwich composition to match the physical camera latency. The maximum duration is sandwichCompositionBufferedFrames / {Game FPS}
	/// </summary>
	[HideInInspector, Tooltip("Delay the foreground / background image in the sandwich composition to match the physical camera latency. The maximum duration is sandwichCompositionBufferedFrames / {Game FPS}")]
	public float sandwichCompositionRenderLatency = 0.0f;

	/// <summary>
	/// The number of frames are buffered in the SandWich composition. The more buffered frames, the more memory it would consume.
	/// </summary>
	[HideInInspector, Tooltip("The number of frames are buffered in the SandWich composition. The more buffered frames, the more memory it would consume.")]
	public int sandwichCompositionBufferedFrames = 8;


	/// <summary>
	/// Chroma Key Color
	/// </summary>
	[HideInInspector, Tooltip("Chroma Key Color")]
	public Color chromaKeyColor = Color.green;

	/// <summary>
	/// Chroma Key Similarity
	/// </summary>
	[HideInInspector, Tooltip("Chroma Key Similarity")]
	public float chromaKeySimilarity = 0.60f;

	/// <summary>
	/// Chroma Key Smooth Range
	/// </summary>
	[HideInInspector, Tooltip("Chroma Key Smooth Range")]
	public float chromaKeySmoothRange = 0.03f;

	/// <summary>
	///  Chroma Key Spill Range
	/// </summary>
	[HideInInspector, Tooltip("Chroma Key Spill Range")]
	public float chromaKeySpillRange = 0.06f;

	/// <summary>
	/// Use dynamic lighting (Depth sensor required)
	/// </summary>
	[HideInInspector, Tooltip("Use dynamic lighting (Depth sensor required)")]
	public bool useDynamicLighting = false;

	public enum DepthQuality
	{
		Low,
		Medium,
		High
	}
	/// <summary>
	/// The quality level of depth image. The lighting could be more smooth and accurate with high quality depth, but it would also be more costly in performance.
	/// </summary>
	[HideInInspector, Tooltip("The quality level of depth image. The lighting could be more smooth and accurate with high quality depth, but it would also be more costly in performance.")]
	public DepthQuality depthQuality = DepthQuality.Medium;

	/// <summary>
	/// Smooth factor in dynamic lighting. Larger is smoother
	/// </summary>
	[HideInInspector, Tooltip("Smooth factor in dynamic lighting. Larger is smoother")]
	public float dynamicLightingSmoothFactor = 8.0f;

	/// <summary>
	/// The maximum depth variation across the edges. Make it smaller to smooth the lighting on the edges.
	/// </summary>
	[HideInInspector, Tooltip("The maximum depth variation across the edges. Make it smaller to smooth the lighting on the edges.")]
	public float dynamicLightingDepthVariationClampingValue = 0.001f;

	public enum VirtualGreenScreenType
	{
		Off,
		OuterBoundary,
		PlayArea
	}

	/// <summary>
	/// Set the current type of the virtual green screen
	/// </summary>
	[HideInInspector, Tooltip("Type of virutal green screen ")]
	public VirtualGreenScreenType virtualGreenScreenType = VirtualGreenScreenType.Off;

	/// <summary>
	/// Top Y of virtual screen
	/// </summary>
	[HideInInspector, Tooltip("Top Y of virtual green screen")]
	public float virtualGreenScreenTopY = 10.0f;

	/// <summary>
	/// Bottom Y of virtual screen
	/// </summary>
	[HideInInspector, Tooltip("Bottom Y of virtual green screen")]
	public float virtualGreenScreenBottomY = -10.0f;

	/// <summary>
	/// When using a depth camera (e.g. ZED), whether to use the depth in virtual green screen culling.
	/// </summary>
	[HideInInspector, Tooltip("When using a depth camera (e.g. ZED), whether to use the depth in virtual green screen culling.")]
	public bool virtualGreenScreenApplyDepthCulling = false;

	/// <summary>
	/// The tolerance value (in meter) when using the virtual green screen with a depth camera. Make it bigger if the foreground objects got culled incorrectly.
	/// </summary>
	[HideInInspector, Tooltip("The tolerance value (in meter) when using the virtual green screen with a depth camera. Make it bigger if the foreground objects got culled incorrectly.")]
	public float virtualGreenScreenDepthTolerance = 0.2f;


	public enum MrcActivationMode
	{
		Automatic,
		Disabled
	}

	/// <summary>
	/// (Quest-only) control if the mixed reality capture mode can be activated automatically through remote network connection.
	/// </summary>
	[HideInInspector, Tooltip("(Quest-only) control if the mixed reality capture mode can be activated automatically through remote network connection.")]
	public MrcActivationMode mrcActivationMode;

	public enum MrcCameraType
	{
		Normal,
		Foreground,
		Background
	}

	public delegate GameObject InstantiateMrcCameraDelegate(GameObject mainCameraGameObject, MrcCameraType cameraType);

	/// <summary>
	/// Allows overriding the internal mrc camera creation
	/// </summary>
	public InstantiateMrcCameraDelegate instantiateMixedRealityCameraGameObject = null;

	// OVRMixedRealityCaptureConfiguration Interface implementation
	bool OVRMixedRealityCaptureConfiguration.enableMixedReality { get { return enableMixedReality; } set { enableMixedReality = value; } }
	LayerMask OVRMixedRealityCaptureConfiguration.extraHiddenLayers { get { return extraHiddenLayers; } set { extraHiddenLayers = value; } }
	LayerMask OVRMixedRealityCaptureConfiguration.extraVisibleLayers { get { return extraVisibleLayers; } set { extraVisibleLayers = value; } }
	bool OVRMixedRealityCaptureConfiguration.dynamicCullingMask { get { return dynamicCullingMask; } set { dynamicCullingMask = value; } }
	CompositionMethod OVRMixedRealityCaptureConfiguration.compositionMethod { get { return compositionMethod; } set { compositionMethod = value; } }
	Color OVRMixedRealityCaptureConfiguration.externalCompositionBackdropColorRift { get { return externalCompositionBackdropColorRift; } set { externalCompositionBackdropColorRift = value; } }
	Color OVRMixedRealityCaptureConfiguration.externalCompositionBackdropColorQuest { get { return externalCompositionBackdropColorQuest; } set { externalCompositionBackdropColorQuest = value; } }
	CameraDevice OVRMixedRealityCaptureConfiguration.capturingCameraDevice { get { return capturingCameraDevice; } set { capturingCameraDevice = value; } }
	bool OVRMixedRealityCaptureConfiguration.flipCameraFrameHorizontally { get { return flipCameraFrameHorizontally; } set { flipCameraFrameHorizontally = value; } }
	bool OVRMixedRealityCaptureConfiguration.flipCameraFrameVertically { get { return flipCameraFrameVertically; } set { flipCameraFrameVertically = value; } }
	float OVRMixedRealityCaptureConfiguration.handPoseStateLatency { get { return handPoseStateLatency; } set { handPoseStateLatency = value; } }
	float OVRMixedRealityCaptureConfiguration.sandwichCompositionRenderLatency { get { return sandwichCompositionRenderLatency; } set { sandwichCompositionRenderLatency = value; } }
	int OVRMixedRealityCaptureConfiguration.sandwichCompositionBufferedFrames { get { return sandwichCompositionBufferedFrames; } set { sandwichCompositionBufferedFrames = value; } }
	Color OVRMixedRealityCaptureConfiguration.chromaKeyColor { get { return chromaKeyColor; } set { chromaKeyColor = value; } }
	float OVRMixedRealityCaptureConfiguration.chromaKeySimilarity { get { return chromaKeySimilarity; } set { chromaKeySimilarity = value; } }
	float OVRMixedRealityCaptureConfiguration.chromaKeySmoothRange { get { return chromaKeySmoothRange; } set { chromaKeySmoothRange = value; } }
	float OVRMixedRealityCaptureConfiguration.chromaKeySpillRange { get { return chromaKeySpillRange; } set { chromaKeySpillRange = value; } }
	bool OVRMixedRealityCaptureConfiguration.useDynamicLighting { get { return useDynamicLighting; } set { useDynamicLighting = value; } }
	DepthQuality OVRMixedRealityCaptureConfiguration.depthQuality { get { return depthQuality; } set { depthQuality = value; } }
	float OVRMixedRealityCaptureConfiguration.dynamicLightingSmoothFactor { get { return dynamicLightingSmoothFactor; } set { dynamicLightingSmoothFactor = value; } }
	float OVRMixedRealityCaptureConfiguration.dynamicLightingDepthVariationClampingValue { get { return dynamicLightingDepthVariationClampingValue; } set { dynamicLightingDepthVariationClampingValue = value; } }
	VirtualGreenScreenType OVRMixedRealityCaptureConfiguration.virtualGreenScreenType { get { return virtualGreenScreenType; } set { virtualGreenScreenType = value; } }
	float OVRMixedRealityCaptureConfiguration.virtualGreenScreenTopY { get { return virtualGreenScreenTopY; } set { virtualGreenScreenTopY = value; } }
	float OVRMixedRealityCaptureConfiguration.virtualGreenScreenBottomY { get { return virtualGreenScreenBottomY; } set { virtualGreenScreenBottomY = value; } }
	bool OVRMixedRealityCaptureConfiguration.virtualGreenScreenApplyDepthCulling { get { return virtualGreenScreenApplyDepthCulling; } set { virtualGreenScreenApplyDepthCulling = value; } }
	float OVRMixedRealityCaptureConfiguration.virtualGreenScreenDepthTolerance { get { return virtualGreenScreenDepthTolerance; } set { virtualGreenScreenDepthTolerance = value; } }
	MrcActivationMode OVRMixedRealityCaptureConfiguration.mrcActivationMode { get { return mrcActivationMode; } set { mrcActivationMode = value; } }
	InstantiateMrcCameraDelegate OVRMixedRealityCaptureConfiguration.instantiateMixedRealityCameraGameObject { get { return instantiateMixedRealityCameraGameObject; } set { instantiateMixedRealityCameraGameObject = value; } }

#endif


	/// <summary>
	/// The native XR API being used
	/// </summary>
	public XrApi xrApi
	{
		get
		{
			return (XrApi)OVRPlugin.nativeXrApi;
		}
	}

	/// <summary>
	/// The value of current XrInstance when using OpenXR
	/// </summary>
	public UInt64 xrInstance
	{
		get
		{
			return OVRPlugin.GetNativeOpenXRInstance();
		}
	}

	/// <summary>
	/// The value of current XrSession when using OpenXR
	/// </summary>
	public UInt64 xrSession
	{
		get
		{
			return OVRPlugin.GetNativeOpenXRSession();
		}
	}

	/// <summary>
	/// The number of expected display frames per rendered frame.
	/// </summary>
	public int vsyncCount
	{
		get {
			if (!isHmdPresent)
				return 1;

			return OVRPlugin.vsyncCount;
		}

		set {
			if (!isHmdPresent)
				return;

			OVRPlugin.vsyncCount = value;
		}
	}

	public static string OCULUS_UNITY_NAME_STR = "Oculus";
	public static string OPENVR_UNITY_NAME_STR = "OpenVR";

	public static XRDevice loadedXRDevice;

	/// <summary>
	/// Gets the current battery level.
	/// </summary>
	/// <returns><c>battery level in the range [0.0,1.0]</c>
	/// <param name="batteryLevel">Battery level.</param>
	public static float batteryLevel
	{
		get {
			if (!isHmdPresent)
				return 1f;

			return OVRPlugin.batteryLevel;
		}
	}

	/// <summary>
	/// Gets the current battery temperature.
	/// </summary>
	/// <returns><c>battery temperature in Celsius</c>
	/// <param name="batteryTemperature">Battery temperature.</param>
	public static float batteryTemperature
	{
		get {
			if (!isHmdPresent)
				return 0f;

			return OVRPlugin.batteryTemperature;
		}
	}

	/// <summary>
	/// Gets the current battery status.
	/// </summary>
	/// <returns><c>battery status</c>
	/// <param name="batteryStatus">Battery status.</param>
	public static int batteryStatus
	{
		get {
			if (!isHmdPresent)
				return -1;

			return (int)OVRPlugin.batteryStatus;
		}
	}

	/// <summary>
	/// Gets the current volume level.
	/// </summary>
	/// <returns><c>volume level in the range [0,1].</c>
	public static float volumeLevel
	{
		get {
			if (!isHmdPresent)
				return 0f;

			return OVRPlugin.systemVolume;
		}
	}

	/// <summary>
	/// Gets or sets the current CPU performance level (0-2). Lower performance levels save more power.
	/// </summary>
	public static int cpuLevel
	{
		get {
			if (!isHmdPresent)
				return 2;

			return OVRPlugin.cpuLevel;
		}

		set {
			if (!isHmdPresent)
				return;

			OVRPlugin.cpuLevel = value;
		}
	}

	/// <summary>
	/// Gets or sets the current GPU performance level (0-2). Lower performance levels save more power.
	/// </summary>
	public static int gpuLevel
	{
		get {
			if (!isHmdPresent)
				return 2;

			return OVRPlugin.gpuLevel;
		}

		set {
			if (!isHmdPresent)
				return;

			OVRPlugin.gpuLevel = value;
		}
	}

	/// <summary>
	/// If true, the CPU and GPU are currently throttled to save power and/or reduce the temperature.
	/// </summary>
	public static bool isPowerSavingActive
	{
		get {
			if (!isHmdPresent)
				return false;

			return OVRPlugin.powerSaving;
		}
	}

	/// <summary>
	/// Gets or sets the eye texture format.
	/// </summary>
	public static EyeTextureFormat eyeTextureFormat
	{
		get
		{
			return (OVRManager.EyeTextureFormat)OVRPlugin.GetDesiredEyeTextureFormat();
		}

		set
		{
			OVRPlugin.SetDesiredEyeTextureFormat((OVRPlugin.EyeTextureFormat)value);
		}
	}

	/// <summary>
	/// Gets if tiled-based multi-resolution technique is supported
	/// This feature is only supported on QCOMM-based Android devices
	/// </summary>
	public static bool fixedFoveatedRenderingSupported
	{
		get
		{
			return OVRPlugin.fixedFoveatedRenderingSupported;
		}
	}

	/// <summary>
	/// Gets or sets the tiled-based multi-resolution level
	/// This feature is only supported on QCOMM-based Android devices
	/// </summary>
	public static FixedFoveatedRenderingLevel fixedFoveatedRenderingLevel
	{
		get
		{
			if (!OVRPlugin.fixedFoveatedRenderingSupported)
			{
				Debug.LogWarning("Fixed Foveated Rendering feature is not supported");
			}
			return (FixedFoveatedRenderingLevel)OVRPlugin.fixedFoveatedRenderingLevel;
		}
		set
		{
			if (!OVRPlugin.fixedFoveatedRenderingSupported)
			{
				Debug.LogWarning("Fixed Foveated Rendering feature is not supported");
			}
			OVRPlugin.fixedFoveatedRenderingLevel = (OVRPlugin.FixedFoveatedRenderingLevel)value;
		}
	}

	/// <summary>
	/// Let the system decide the best foveation level adaptively (Off .. fixedFoveatedRenderingLevel)
	/// This feature is only supported on QCOMM-based Android devices
	/// </summary>
	public static bool useDynamicFixedFoveatedRendering
	{
		get
		{
			if (!OVRPlugin.fixedFoveatedRenderingSupported)
			{
				Debug.LogWarning("Fixed Foveated Rendering feature is not supported");
			}
			return OVRPlugin.useDynamicFixedFoveatedRendering;
		}
		set
		{
			if (!OVRPlugin.fixedFoveatedRenderingSupported)
			{
				Debug.LogWarning("Fixed Foveated Rendering feature is not supported");
			}
			OVRPlugin.useDynamicFixedFoveatedRendering = value;
		}
	}

	[Obsolete("Please use fixedFoveatedRenderingSupported instead", false)]
	public static bool tiledMultiResSupported
	{
		get
		{
			return OVRPlugin.tiledMultiResSupported;
		}
	}

	[Obsolete("Please use fixedFoveatedRenderingLevel instead", false)]
	public static TiledMultiResLevel tiledMultiResLevel
	{
		get
		{
			if (!OVRPlugin.tiledMultiResSupported)
			{
				Debug.LogWarning("Tiled-based Multi-resolution feature is not supported");
			}
			return (TiledMultiResLevel)OVRPlugin.tiledMultiResLevel;
		}
		set
		{
			if (!OVRPlugin.tiledMultiResSupported)
			{
				Debug.LogWarning("Tiled-based Multi-resolution feature is not supported");
			}
			OVRPlugin.tiledMultiResLevel = (OVRPlugin.TiledMultiResLevel)value;
		}
	}

	/// <summary>
	/// Gets if the GPU Utility is supported
	/// This feature is only supported on QCOMM-based Android devices
	/// </summary>
	public static bool gpuUtilSupported
	{
		get
		{
			return OVRPlugin.gpuUtilSupported;
		}
	}

	/// <summary>
	/// Gets the GPU Utilised Level (0.0 - 1.0)
	/// This feature is only supported on QCOMM-based Android devices
	/// </summary>
	public static float gpuUtilLevel
	{
		get
		{
			if (!OVRPlugin.gpuUtilSupported)
			{
				Debug.LogWarning("GPU Util is not supported");
			}
			return OVRPlugin.gpuUtilLevel;
		}
	}

	/// <summary>
	/// Get the system headset type
	/// </summary>
	public static SystemHeadsetType systemHeadsetType
	{
		get
		{
			return (SystemHeadsetType)OVRPlugin.GetSystemHeadsetType();
		}
	}

	/// <summary>
	/// Sets the Color Scale and Offset which is commonly used for effects like fade-to-black.
	/// In our compositor, once a given frame is rendered, warped, and ready to be displayed, we then multiply
	/// each pixel by colorScale and add it to colorOffset, whereby newPixel = oldPixel * colorScale + colorOffset.
	/// Note that for mobile devices (Quest, etc.), colorOffset is only supported with OpenXR, so colorScale is all that can
	/// be used. A colorScale of (1, 1, 1, 1) and colorOffset of (0, 0, 0, 0) will lead to an identity multiplication
	/// and have no effect.
	/// </summary>
	public static void SetColorScaleAndOffset(Vector4 colorScale, Vector4 colorOffset, bool applyToAllLayers)
	{
		OVRPlugin.SetColorScaleAndOffset(colorScale, colorOffset, applyToAllLayers);
	}

	/// <summary>
	/// Specifies OpenVR pose local to tracking space
	/// </summary>
	public static void SetOpenVRLocalPose(Vector3 leftPos, Vector3 rightPos, Quaternion leftRot, Quaternion rightRot)
	{
		if (loadedXRDevice == XRDevice.OpenVR)
			OVRInput.SetOpenVRLocalPose(leftPos, rightPos, leftRot, rightRot);
	}

	//Series of offsets that line up the virtual controllers to the phsyical world.
	private static Vector3 OpenVRTouchRotationOffsetEulerLeft = new Vector3(40.0f, 0.0f, 0.0f);
	private static Vector3 OpenVRTouchRotationOffsetEulerRight = new Vector3(40.0f, 0.0f, 0.0f);
	private static Vector3 OpenVRTouchPositionOffsetLeft = new Vector3(0.0075f, -0.005f, -0.0525f);
	private static Vector3 OpenVRTouchPositionOffsetRight = new Vector3(-0.0075f, -0.005f, -0.0525f);

	/// <summary>
	/// Specifies the pose offset required to make an OpenVR controller's reported pose match the virtual pose.
	/// Currently we only specify this offset for Oculus Touch on OpenVR.
	/// </summary>
	public static OVRPose GetOpenVRControllerOffset(Node hand)
	{
		OVRPose poseOffset = OVRPose.identity;
		if ((hand == Node.LeftHand || hand == Node.RightHand) && loadedXRDevice == XRDevice.OpenVR)
		{
			int index = (hand == Node.LeftHand) ? 0 : 1;
			if (OVRInput.openVRControllerDetails[index].controllerType == OVRInput.OpenVRController.OculusTouch)
			{
				Vector3 offsetOrientation = (hand == Node.LeftHand) ? OpenVRTouchRotationOffsetEulerLeft : OpenVRTouchRotationOffsetEulerRight;
				poseOffset.orientation = Quaternion.Euler(offsetOrientation.x, offsetOrientation.y, offsetOrientation.z);
				poseOffset.position = (hand == Node.LeftHand) ? OpenVRTouchPositionOffsetLeft : OpenVRTouchPositionOffsetRight;
			}
		}
		return poseOffset;
	}


	[Header("Tracking")]
	[SerializeField]
	[Tooltip("Defines the current tracking origin type.")]
	private OVRManager.TrackingOrigin _trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
	/// <summary>
	/// Defines the current tracking origin type.
	/// </summary>
	public OVRManager.TrackingOrigin trackingOriginType
	{
		get {
			if (!isHmdPresent)
				return _trackingOriginType;

			return (OVRManager.TrackingOrigin)OVRPlugin.GetTrackingOriginType();
		}

		set {
			if (!isHmdPresent)
				return;

			if (OVRPlugin.SetTrackingOriginType((OVRPlugin.TrackingOrigin)value))
			{
				// Keep the field exposed in the Unity Editor synchronized with any changes.
				_trackingOriginType = value;
			}
		}
	}

	/// <summary>
	/// If true, head tracking will affect the position of each OVRCameraRig's cameras.
	/// </summary>
	[Tooltip("If true, head tracking will affect the position of each OVRCameraRig's cameras.")]
	public bool usePositionTracking = true;

	/// <summary>
	/// If true, head tracking will affect the rotation of each OVRCameraRig's cameras.
	/// </summary>
	[HideInInspector]
	public bool useRotationTracking = true;

	/// <summary>
	/// If true, the distance between the user's eyes will affect the position of each OVRCameraRig's cameras.
	/// </summary>
	[Tooltip("If true, the distance between the user's eyes will affect the position of each OVRCameraRig's cameras.")]
	public bool useIPDInPositionTracking = true;

	/// <summary>
	/// If true, each scene load will cause the head pose to reset. This function only works on Rift.
	/// </summary>
	[Tooltip("If true, each scene load will cause the head pose to reset. This function only works on Rift.")]
	public bool resetTrackerOnLoad = false;

	/// <summary>
	/// If true, the Reset View in the universal menu will cause the pose to be reset. This should generally be
	/// enabled for applications with a stationary position in the virtual world and will allow the View Reset
	/// command to place the person back to a predefined location (such as a cockpit seat).
	/// Set this to false if you have a locomotion system because resetting the view would effectively teleport
	/// the player to potentially invalid locations.
	/// </summary>
	[Tooltip("If true, the Reset View in the universal menu will cause the pose to be reset. This should generally be enabled for applications with a stationary position in the virtual world and will allow the View Reset command to place the person back to a predefined location (such as a cockpit seat). Set this to false if you have a locomotion system because resetting the view would effectively teleport the player to potentially invalid locations.")]
    public bool AllowRecenter = true;

	/// <summary>
	/// If true, a lower-latency update will occur right before rendering. If false, the only controller pose update will occur at the start of simulation for a given frame.
	/// Selecting this option lowers rendered latency for controllers and is often a net positive; however, it also creates a slight disconnect between rendered and simulated controller poses.
	/// Visit online Oculus documentation to learn more.
	/// </summary>
	[Tooltip("If true, rendered controller latency is reduced by several ms, as the left/right controllers will have their positions updated right before rendering.")]
	public bool LateControllerUpdate = true;

	/// <summary>
	/// True if the current platform supports virtual reality.
	/// </summary>
	public bool isSupportedPlatform { get; private set; }

	private static bool _isUserPresentCached = false;
	private static bool _isUserPresent = false;
	private static bool _wasUserPresent = false;
	/// <summary>
	/// True if the user is currently wearing the display.
	/// </summary>
	public bool isUserPresent
	{
		get {
			if (!_isUserPresentCached)
			{
				_isUserPresentCached = true;
				_isUserPresent = OVRPlugin.userPresent;
			}

			return _isUserPresent;
		}

		private set {
			_isUserPresentCached = true;
			_isUserPresent = value;
		}
	}

	private static bool prevAudioOutIdIsCached = false;
	private static bool prevAudioInIdIsCached = false;
	private static string prevAudioOutId = string.Empty;
	private static string prevAudioInId = string.Empty;
	private static bool wasPositionTracked = false;

	private static OVRPlugin.EventDataBuffer eventDataBuffer = new OVRPlugin.EventDataBuffer();

	public static System.Version utilitiesVersion
	{
		get { return OVRPlugin.wrapperVersion; }
	}

	public static System.Version pluginVersion
	{
		get { return OVRPlugin.version; }
	}

	public static System.Version sdkVersion
	{
		get { return OVRPlugin.nativeSDKVersion; }
	}

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
	private static bool MixedRealityEnabledFromCmd()
	{
		var args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].ToLower() == "-mixedreality")
				return true;
		}
		return false;
	}

	private static bool UseDirectCompositionFromCmd()
	{
		var args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].ToLower() == "-directcomposition")
				return true;
		}
		return false;
	}

	private static bool UseExternalCompositionFromCmd()
	{
		var args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].ToLower() == "-externalcomposition")
				return true;
		}
		return false;
	}

	private static bool CreateMixedRealityCaptureConfigurationFileFromCmd()
	{
		var args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].ToLower() == "-create_mrc_config")
				return true;
		}
		return false;
	}

	private static bool LoadMixedRealityCaptureConfigurationFileFromCmd()
	{
		var args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].ToLower() == "-load_mrc_config")
				return true;
		}
		return false;
	}
#endif

	public static bool IsUnityAlphaOrBetaVersion()
	{
		string ver = Application.unityVersion;
		int pos = ver.Length - 1;

		while (pos >= 0 && ver[pos] >= '0' && ver[pos] <= '9')
		{
			--pos;
		}

		if (pos >= 0 && (ver[pos] == 'a' || ver[pos] == 'b'))
			return true;

		return false;
	}

	public static string UnityAlphaOrBetaVersionWarningMessage = "WARNING: It's not recommended to use Unity alpha/beta release in Oculus development. Use a stable release if you encounter any issue.";

#region Unity Messages

	public static bool OVRManagerinitialized = false;
	private void InitOVRManager()
	{
		// Only allow one instance at runtime.
		if (instance != null)
		{
			enabled = false;
			DestroyImmediate(this);
			return;
		}

		instance = this;

		// uncomment the following line to disable the callstack printed to log
		//Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);  // TEMPORARY

		Debug.Log("Unity v" + Application.unityVersion + ", " +
				"Oculus Utilities v" + OVRPlugin.wrapperVersion + ", " +
				"OVRPlugin v" + OVRPlugin.version + ", " +
				"SDK v" + OVRPlugin.nativeSDKVersion + ".");

		Debug.LogFormat("SystemHeadset {0}, API {1}", systemHeadsetType.ToString(), xrApi.ToString());

		if (xrApi == XrApi.OpenXR)
		{
			Debug.LogFormat("OpenXR instance 0x{0:X} session 0x{1:X}", xrInstance, xrSession);
		}

#if !UNITY_EDITOR
		if (IsUnityAlphaOrBetaVersion())
		{
			Debug.LogWarning(UnityAlphaOrBetaVersionWarningMessage);
		}
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		var supportedTypes =
			UnityEngine.Rendering.GraphicsDeviceType.Direct3D11.ToString() + ", " +
			UnityEngine.Rendering.GraphicsDeviceType.Direct3D12.ToString();

		if (!supportedTypes.Contains(SystemInfo.graphicsDeviceType.ToString()))
			Debug.LogWarning("VR rendering requires one of the following device types: (" + supportedTypes + "). Your graphics device: " + SystemInfo.graphicsDeviceType.ToString());
#endif

		// Detect whether this platform is a supported platform
		RuntimePlatform currPlatform = Application.platform;
		if (currPlatform == RuntimePlatform.Android ||
			// currPlatform == RuntimePlatform.LinuxPlayer ||
			currPlatform == RuntimePlatform.OSXEditor ||
			currPlatform == RuntimePlatform.OSXPlayer ||
			currPlatform == RuntimePlatform.WindowsEditor ||
			currPlatform == RuntimePlatform.WindowsPlayer)
		{
			isSupportedPlatform = true;
		}
		else
		{
			isSupportedPlatform = false;
		}
		if (!isSupportedPlatform)
		{
			Debug.LogWarning("This platform is unsupported");
			return;
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		// Turn off chromatic aberration by default to save texture bandwidth.
		chromatic = false;
#endif

#if (UNITY_STANDALONE_WIN || UNITY_ANDROID) && !UNITY_EDITOR
		enableMixedReality = false;		// we should never start the standalone game in MxR mode, unless the command-line parameter is provided
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		if (!staticMixedRealityCaptureInitialized)
		{
			bool loadMrcConfig = LoadMixedRealityCaptureConfigurationFileFromCmd();
			bool createMrcConfig = CreateMixedRealityCaptureConfigurationFileFromCmd();

			if (loadMrcConfig || createMrcConfig)
			{
				OVRMixedRealityCaptureSettings mrcSettings = ScriptableObject.CreateInstance<OVRMixedRealityCaptureSettings>();
				mrcSettings.ReadFrom(this);
				if (loadMrcConfig)
				{
					mrcSettings.CombineWithConfigurationFile();
					mrcSettings.ApplyTo(this);
				}
				if (createMrcConfig)
				{
					mrcSettings.WriteToConfigurationFile();
				}
				ScriptableObject.Destroy(mrcSettings);
			}

			if (MixedRealityEnabledFromCmd())
			{
				enableMixedReality = true;
			}

			if (enableMixedReality)
			{
				Debug.Log("OVR: Mixed Reality mode enabled");
				if (UseDirectCompositionFromCmd())
				{
					compositionMethod = CompositionMethod.Direct;
				}
				if (UseExternalCompositionFromCmd())
				{
					compositionMethod = CompositionMethod.External;
				}
				Debug.Log("OVR: CompositionMethod : " + compositionMethod);
			}
		}
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || OVR_ANDROID_MRC
		StaticInitializeMixedRealityCapture(this);
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		if (enableAdaptiveResolution && !OVRManager.IsAdaptiveResSupportedByEngine())
		{
			enableAdaptiveResolution = false;
			UnityEngine.Debug.LogError("Your current Unity Engine " + Application.unityVersion + " might have issues to support adaptive resolution, please disable it under OVRManager");
		}
#endif

		Initialize();

		Debug.LogFormat("Current display frequency {0}, available frequencies [{1}]",
			display.displayFrequency, string.Join(", ", display.displayFrequenciesAvailable.Select(f => f.ToString()).ToArray()));

		if (resetTrackerOnLoad)
			display.RecenterPose();

		if (Debug.isDebugBuild)
		{
			// Activate system metrics collection in Debug/Developerment build
			if (GetComponent<OVRSystemPerfMetrics.OVRSystemPerfMetricsTcpServer>() == null)
			{
				gameObject.AddComponent<OVRSystemPerfMetrics.OVRSystemPerfMetricsTcpServer>();
			}
			OVRSystemPerfMetrics.OVRSystemPerfMetricsTcpServer perfTcpServer = GetComponent<OVRSystemPerfMetrics.OVRSystemPerfMetricsTcpServer>();
			perfTcpServer.listeningPort = profilerTcpPort;
			if (!perfTcpServer.enabled)
			{
				perfTcpServer.enabled = true;
			}
#if !UNITY_EDITOR
			OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
#endif
		}

		// Refresh the client color space
		OVRManager.ColorSpace clientColorSpace = colorGamut;
		colorGamut = clientColorSpace;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		// Force OcculusionMesh on all the time, you can change the value to false if you really need it be off for some reasons,
		// be aware there are performance drops if you don't use occlusionMesh.
		OVRPlugin.occlusionMesh = true;
#endif

		OVRManagerinitialized = true;

	}

	private void Awake()
	{
#if !USING_XR_SDK
		//For legacy, we should initialize OVRManager in all cases.
		//For now, in XR SDK, only initialize if OVRPlugin is initialized.
		InitOVRManager();
#else
		if (OVRPlugin.initialized)
			InitOVRManager();
#endif
	}

#if UNITY_EDITOR
	private static bool _scriptsReloaded;

	[UnityEditor.Callbacks.DidReloadScripts]
	static void ScriptsReloaded()
	{
		_scriptsReloaded = true;
	}
#endif

	void SetCurrentXRDevice()
	{
#if USING_XR_SDK
		XRDisplaySubsystem currentDisplaySubsystem = GetCurrentDisplaySubsystem();
		XRDisplaySubsystemDescriptor currentDisplaySubsystemDescriptor = GetCurrentDisplaySubsystemDescriptor();
#endif
		if (OVRPlugin.initialized)
		{
			loadedXRDevice = XRDevice.Oculus;
		}
#if USING_XR_SDK
		else if (currentDisplaySubsystem != null && currentDisplaySubsystemDescriptor != null && currentDisplaySubsystem.running)
#else
		else if (Settings.enabled)
#endif
		{
#if USING_XR_SDK
			string loadedXRDeviceName = currentDisplaySubsystemDescriptor.id;
#else
			string loadedXRDeviceName = Settings.loadedDeviceName;
#endif
			if (loadedXRDeviceName == OPENVR_UNITY_NAME_STR)
				loadedXRDevice = XRDevice.OpenVR;
			else
				loadedXRDevice = XRDevice.Unknown;
		}
		else
		{
			loadedXRDevice = XRDevice.Unknown;
		}
	}

#if USING_XR_SDK
	static List<XRDisplaySubsystem> s_displaySubsystems;
	public static XRDisplaySubsystem GetCurrentDisplaySubsystem()
	{
		if (s_displaySubsystems == null)
			s_displaySubsystems = new List<XRDisplaySubsystem>();
		SubsystemManager.GetInstances(s_displaySubsystems);
		if (s_displaySubsystems.Count > 0)
			return s_displaySubsystems[0];
		return null;
	}

	static List<XRDisplaySubsystemDescriptor> s_displaySubsystemDescriptors;
	public static XRDisplaySubsystemDescriptor GetCurrentDisplaySubsystemDescriptor()
	{
		if (s_displaySubsystemDescriptors == null)
			s_displaySubsystemDescriptors = new List<XRDisplaySubsystemDescriptor>();
		SubsystemManager.GetSubsystemDescriptors(s_displaySubsystemDescriptors);
		if (s_displaySubsystemDescriptors.Count > 0)
			return s_displaySubsystemDescriptors[0];
		return null;
	}

	static List<XRInputSubsystem> s_inputSubsystems;
	public static XRInputSubsystem GetCurrentInputSubsystem()
	{
		if (s_inputSubsystems == null)
			s_inputSubsystems = new List<XRInputSubsystem>();
		SubsystemManager.GetInstances(s_inputSubsystems);
		if (s_inputSubsystems.Count > 0)
			return s_inputSubsystems[0];
		return null;
	}
#endif

	void Initialize()
	{
		if (display == null)
			display = new OVRDisplay();
		if (tracker == null)
			tracker = new OVRTracker();
		if (boundary == null)
			boundary = new OVRBoundary();

		SetCurrentXRDevice();
	}

	private void Update()
	{
		//Only if we're using the XR SDK do we have to check if OVRManager isn't yet initialized, and init it.
		//If we're on legacy, we know initialization occurred properly in Awake()
#if USING_XR_SDK
		if (!OVRManagerinitialized)
		{
			XRDisplaySubsystem currentDisplaySubsystem = GetCurrentDisplaySubsystem();
			XRDisplaySubsystemDescriptor currentDisplaySubsystemDescriptor = GetCurrentDisplaySubsystemDescriptor();
			if (currentDisplaySubsystem == null || currentDisplaySubsystemDescriptor == null || !OVRPlugin.initialized)
				return;
			//If we're using the XR SDK and the display subsystem is present, and OVRPlugin is initialized, we can init OVRManager
			InitOVRManager();
		}
#endif

#if UNITY_EDITOR
		if (_scriptsReloaded)
		{
			_scriptsReloaded = false;
			instance = this;
			Initialize();
		}
#endif

		SetCurrentXRDevice();

		if (OVRPlugin.shouldQuit)
		{
			Debug.Log("[OVRManager] OVRPlugin.shouldQuit detected");
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || OVR_ANDROID_MRC
			StaticShutdownMixedRealityCapture(instance);
#endif
			Application.Quit();
		}

		if (AllowRecenter && OVRPlugin.shouldRecenter)
		{
			OVRManager.display.RecenterPose();
		}

		if (trackingOriginType != _trackingOriginType)
			trackingOriginType = _trackingOriginType;

		tracker.isEnabled = usePositionTracking;

		OVRPlugin.rotation = useRotationTracking;

		OVRPlugin.useIPDInPositionTracking = useIPDInPositionTracking;

		// Dispatch HMD events.

		isHmdPresent = OVRNodeStateProperties.IsHmdPresent();

		if (useRecommendedMSAALevel && QualitySettings.antiAliasing != display.recommendedMSAALevel)
		{
			Debug.Log("The current MSAA level is " + QualitySettings.antiAliasing +
			", but the recommended MSAA level is " + display.recommendedMSAALevel +
			". Switching to the recommended level.");

			QualitySettings.antiAliasing = display.recommendedMSAALevel;
		}

		if (monoscopic != _monoscopic)
		{
			monoscopic = _monoscopic;
		}

		if (headPoseRelativeOffsetRotation != _headPoseRelativeOffsetRotation)
		{
			headPoseRelativeOffsetRotation = _headPoseRelativeOffsetRotation;
		}

		if (headPoseRelativeOffsetTranslation != _headPoseRelativeOffsetTranslation)
		{
			headPoseRelativeOffsetTranslation = _headPoseRelativeOffsetTranslation;
		}

		if (_wasHmdPresent && !isHmdPresent)
		{
			try
			{
				Debug.Log("[OVRManager] HMDLost event");
				if (HMDLost != null)
					HMDLost();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		if (!_wasHmdPresent && isHmdPresent)
		{
			try
			{
				Debug.Log("[OVRManager] HMDAcquired event");
				if (HMDAcquired != null)
					HMDAcquired();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		_wasHmdPresent = isHmdPresent;

		// Dispatch HMD mounted events.

		isUserPresent = OVRPlugin.userPresent;

		if (_wasUserPresent && !isUserPresent)
		{
			try
			{
				Debug.Log("[OVRManager] HMDUnmounted event");
				if (HMDUnmounted != null)
					HMDUnmounted();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		if (!_wasUserPresent && isUserPresent)
		{
			try
			{
				Debug.Log("[OVRManager] HMDMounted event");
				if (HMDMounted != null)
					HMDMounted();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		_wasUserPresent = isUserPresent;

		// Dispatch VR Focus events.

		hasVrFocus = OVRPlugin.hasVrFocus;

		if (_hadVrFocus && !hasVrFocus)
		{
			try
			{
				Debug.Log("[OVRManager] VrFocusLost event");
				if (VrFocusLost != null)
					VrFocusLost();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		if (!_hadVrFocus && hasVrFocus)
		{
			try
			{
				Debug.Log("[OVRManager] VrFocusAcquired event");
				if (VrFocusAcquired != null)
					VrFocusAcquired();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		_hadVrFocus = hasVrFocus;

		// Dispatch VR Input events.

		bool hasInputFocus = OVRPlugin.hasInputFocus;

		if (_hadInputFocus && !hasInputFocus)
		{
			try
			{
				Debug.Log("[OVRManager] InputFocusLost event");
				if (InputFocusLost != null)
					InputFocusLost();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		if (!_hadInputFocus && hasInputFocus)
		{
			try
			{
				Debug.Log("[OVRManager] InputFocusAcquired event");
				if (InputFocusAcquired != null)
					InputFocusAcquired();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		_hadInputFocus = hasInputFocus;

		// Changing effective rendering resolution dynamically according performance
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)

		if (enableAdaptiveResolution)
		{
			if (Settings.eyeTextureResolutionScale < maxRenderScale)
			{
				// Allocate renderScale to max to avoid re-allocation
				Settings.eyeTextureResolutionScale = maxRenderScale;
			}
			else
			{
				// Adjusting maxRenderScale in case app started with a larger renderScale value
				maxRenderScale = Mathf.Max(maxRenderScale, Settings.eyeTextureResolutionScale);
			}
			minRenderScale = Mathf.Min(minRenderScale, maxRenderScale);
			float minViewportScale = minRenderScale / Settings.eyeTextureResolutionScale;
			float recommendedViewportScale = Mathf.Clamp(Mathf.Sqrt(OVRPlugin.GetAdaptiveGPUPerformanceScale()) * Settings.eyeTextureResolutionScale * Settings.renderViewportScale, 0.5f, 2.0f);
			recommendedViewportScale /= Settings.eyeTextureResolutionScale;
			recommendedViewportScale = Mathf.Clamp(recommendedViewportScale, minViewportScale, 1.0f);
			Settings.renderViewportScale = recommendedViewportScale;
		}
#endif

		// Dispatch Audio Device events.

		string audioOutId = OVRPlugin.audioOutId;
		if (!prevAudioOutIdIsCached)
		{
			prevAudioOutId = audioOutId;
			prevAudioOutIdIsCached = true;
		}
		else if (audioOutId != prevAudioOutId)
		{
			try
			{
				Debug.Log("[OVRManager] AudioOutChanged event");
				if (AudioOutChanged != null)
					AudioOutChanged();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}

			prevAudioOutId = audioOutId;
		}

		string audioInId = OVRPlugin.audioInId;
		if (!prevAudioInIdIsCached)
		{
			prevAudioInId = audioInId;
			prevAudioInIdIsCached = true;
		}
		else if (audioInId != prevAudioInId)
		{
			try
			{
				Debug.Log("[OVRManager] AudioInChanged event");
				if (AudioInChanged != null)
					AudioInChanged();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}

			prevAudioInId = audioInId;
		}

		// Dispatch tracking events.

		if (wasPositionTracked && !tracker.isPositionTracked)
		{
			try
			{
				Debug.Log("[OVRManager] TrackingLost event");
				if (TrackingLost != null)
					TrackingLost();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		if (!wasPositionTracked && tracker.isPositionTracked)
		{
			try
			{
				Debug.Log("[OVRManager] TrackingAcquired event");
				if (TrackingAcquired != null)
					TrackingAcquired();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		wasPositionTracked = tracker.isPositionTracked;

		display.Update();
		OVRInput.Update();

		UpdateHMDEvents();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || OVR_ANDROID_MRC
		StaticUpdateMixedRealityCapture(this, gameObject, trackingOriginType);
#endif
	}

	private void UpdateHMDEvents()
	{
		while(OVRPlugin.PollEvent(ref eventDataBuffer))
		{
			switch(eventDataBuffer.EventType)
			{
				case OVRPlugin.EventType.DisplayRefreshRateChanged:
					if(DisplayRefreshRateChanged != null)
					{
						float FromRefreshRate = BitConverter.ToSingle(eventDataBuffer.EventData, 0);
						float ToRefreshRate = BitConverter.ToSingle(eventDataBuffer.EventData, 4);
						DisplayRefreshRateChanged(FromRefreshRate, ToRefreshRate);
					}
					break;
				default:
					break;
			}
		}
	}

	private static bool multipleMainCameraWarningPresented = false;
	private static WeakReference<Camera> lastFoundMainCamera = null;
	private static Camera FindMainCamera() {

		Camera lastCamera;
		if (lastFoundMainCamera != null &&
		    lastFoundMainCamera.TryGetTarget(out lastCamera) &&
		    lastCamera != null &&
		    lastCamera.CompareTag("MainCamera"))
		{
			return lastCamera;
		}

		Camera result = null;

		GameObject[] objects = GameObject.FindGameObjectsWithTag("MainCamera");
		List<Camera> cameras = new List<Camera>(4);
		foreach (GameObject obj in objects)
		{
			Camera camera = obj.GetComponent<Camera>();
			if (camera != null && camera.enabled)
			{
				OVRCameraRig cameraRig = camera.GetComponentInParent<OVRCameraRig>();
				if (cameraRig != null && cameraRig.trackingSpace != null)
				{
					cameras.Add(camera);
				}
			}
		}
		if (cameras.Count == 0)
		{
			result = Camera.main; // pick one of the cameras which tagged as "MainCamera"
		}
		else if (cameras.Count == 1)
		{
			result = cameras[0];
		}
		else
		{
			if (!multipleMainCameraWarningPresented)
			{
				Debug.LogWarning("Multiple MainCamera found. Assume the real MainCamera is the camera with the least depth");
				multipleMainCameraWarningPresented = true;
			}
			// return the camera with least depth
			cameras.Sort((Camera c0, Camera c1) => { return c0.depth < c1.depth ? -1 : (c0.depth > c1.depth ? 1 : 0); });
			result = cameras[0];
		}

		if (result != null)
		{
			Debug.LogFormat("[OVRManager] mainCamera found for MRC: ", result.gameObject.name);
		}
		else
		{
			Debug.Log("[OVRManager] unable to find a vaild camera");
		}
		lastFoundMainCamera = new WeakReference<Camera>(result);
		return result;
	}

	private void OnDisable()
	{
		OVRSystemPerfMetrics.OVRSystemPerfMetricsTcpServer perfTcpServer = GetComponent<OVRSystemPerfMetrics.OVRSystemPerfMetricsTcpServer>();
		if (perfTcpServer != null)
		{
			perfTcpServer.enabled = false;
		}
	}

	private void LateUpdate()
	{
		OVRHaptics.Process();
	}

	private void FixedUpdate()
	{
		OVRInput.FixedUpdate();
	}

	private void OnDestroy()
	{
		Debug.Log("[OVRManager] OnDestroy");
		OVRManagerinitialized = false;
	}

	private void OnApplicationPause(bool pause)
	{
		if (pause)
		{
			Debug.Log("[OVRManager] OnApplicationPause(true)");
		}
		else
		{
			Debug.Log("[OVRManager] OnApplicationPause(false)");
		}
	}

	private void OnApplicationFocus(bool focus)
	{
		if (focus)
		{
			Debug.Log("[OVRManager] OnApplicationFocus(true)");
		}
		else
		{
			Debug.Log("[OVRManager] OnApplicationFocus(false)");
		}
	}

	private void OnApplicationQuit()
	{
		Debug.Log("[OVRManager] OnApplicationQuit");
	}

#endregion // Unity Messages

	/// <summary>
	/// Leaves the application/game and returns to the launcher/dashboard
	/// </summary>
	public void ReturnToLauncher()
	{
		// show the platform UI quit prompt
		OVRManager.PlatformUIConfirmQuit();
	}

	public static void PlatformUIConfirmQuit()
	{
		if (!isHmdPresent)
			return;

		OVRPlugin.ShowUI(OVRPlugin.PlatformUI.ConfirmQuit);
	}

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || OVR_ANDROID_MRC

	public static bool staticMixedRealityCaptureInitialized = false;
	public static bool staticPrevEnableMixedRealityCapture = false;
	public static OVRMixedRealityCaptureSettings staticMrcSettings = null;
	private static bool suppressDisableMixedRealityBecauseOfNoMainCameraWarning = false;

	public static void StaticInitializeMixedRealityCapture(OVRMixedRealityCaptureConfiguration configuration)
	{
		if (!staticMixedRealityCaptureInitialized)
		{
			staticMrcSettings = ScriptableObject.CreateInstance<OVRMixedRealityCaptureSettings>();
			staticMrcSettings.ReadFrom(configuration);

#if OVR_ANDROID_MRC
			bool mediaInitialized = OVRPlugin.Media.Initialize();
			Debug.Log(mediaInitialized ? "OVRPlugin.Media initialized" : "OVRPlugin.Media not initialized");
			if (mediaInitialized)
			{
				var audioConfig = AudioSettings.GetConfiguration();
				if(audioConfig.sampleRate > 0)
				{
					OVRPlugin.Media.SetMrcAudioSampleRate(audioConfig.sampleRate);
					Debug.LogFormat("[MRC] SetMrcAudioSampleRate({0})", audioConfig.sampleRate);
				}

				OVRPlugin.Media.SetMrcInputVideoBufferType(OVRPlugin.Media.InputVideoBufferType.TextureHandle);
				Debug.LogFormat("[MRC] Active InputVideoBufferType:{0}", OVRPlugin.Media.GetMrcInputVideoBufferType());
				if (configuration.mrcActivationMode == MrcActivationMode.Automatic)
				{
					OVRPlugin.Media.SetMrcActivationMode(OVRPlugin.Media.MrcActivationMode.Automatic);
					Debug.LogFormat("[MRC] ActivateMode: Automatic");
				}
				else if (configuration.mrcActivationMode == MrcActivationMode.Disabled)
				{
					OVRPlugin.Media.SetMrcActivationMode(OVRPlugin.Media.MrcActivationMode.Disabled);
					Debug.LogFormat("[MRC] ActivateMode: Disabled");
				}
				if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
				{
					OVRPlugin.Media.SetAvailableQueueIndexVulkan(1);
					OVRPlugin.Media.SetMrcFrameImageFlipped(true);
				}
			}
#endif
			staticPrevEnableMixedRealityCapture = false;

			staticMixedRealityCaptureInitialized = true;
		}
		else
		{
			staticMrcSettings.ApplyTo(configuration);
		}
	}

	public static void StaticUpdateMixedRealityCapture(OVRMixedRealityCaptureConfiguration configuration, GameObject gameObject, TrackingOrigin trackingOrigin)
	{
		if (!staticMixedRealityCaptureInitialized)
		{
			return;
		}

#if OVR_ANDROID_MRC
		configuration.enableMixedReality = OVRPlugin.Media.GetInitialized() && OVRPlugin.Media.IsMrcActivated();
		configuration.compositionMethod = CompositionMethod.External;		// force external composition on Android MRC

		if (OVRPlugin.Media.GetInitialized())
		{
			OVRPlugin.Media.Update();
		}
#endif

		if (configuration.enableMixedReality && !staticPrevEnableMixedRealityCapture)
		{
			OVRPlugin.SendEvent("mixed_reality_capture", "activated");
			Debug.Log("MixedRealityCapture: activate");
		}

		if (!configuration.enableMixedReality && staticPrevEnableMixedRealityCapture)
		{
			Debug.Log("MixedRealityCapture: deactivate");
		}

		if (configuration.enableMixedReality || staticPrevEnableMixedRealityCapture)
		{
			Camera mainCamera = FindMainCamera();
			if (mainCamera != null)
			{
				suppressDisableMixedRealityBecauseOfNoMainCameraWarning = false;

				if (configuration.enableMixedReality)
				{
					OVRMixedReality.Update(gameObject, mainCamera, configuration, trackingOrigin);
				}

				if (staticPrevEnableMixedRealityCapture && !configuration.enableMixedReality)
				{
					OVRMixedReality.Cleanup();
				}

				staticPrevEnableMixedRealityCapture = configuration.enableMixedReality;
			}
			else
			{
				if (!suppressDisableMixedRealityBecauseOfNoMainCameraWarning)
				{
					Debug.LogWarning("Main Camera is not set, Mixed Reality disabled");
					suppressDisableMixedRealityBecauseOfNoMainCameraWarning = true;
				}
			}
		}

		staticMrcSettings.ReadFrom(configuration);
	}

	public static void StaticShutdownMixedRealityCapture(OVRMixedRealityCaptureConfiguration configuration)
	{
		if (staticMixedRealityCaptureInitialized)
		{
			ScriptableObject.Destroy(staticMrcSettings);
			staticMrcSettings = null;

			OVRMixedReality.Cleanup();

#if OVR_ANDROID_MRC
			if (OVRPlugin.Media.GetInitialized())
			{
				OVRPlugin.Media.Shutdown();
			}
#endif
			staticMixedRealityCaptureInitialized = false;
		}
	}

#endif


}
