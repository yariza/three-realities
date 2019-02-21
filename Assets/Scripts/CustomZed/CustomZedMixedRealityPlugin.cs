//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.VR;
using System.IO;

/// <summary>
/// In pass-through AR mode, handles the final output to the VR headset, positioning the final images
/// to make the pass-through effect natural and comfortable. Also moves/rotates the images to
/// compensate for the ZED image's latency using our Video Asynchronous Timewarp.
/// ZEDManager attaches this component to a second stereo rig called "ZEDRigDisplayer" that it
/// creates and hides in the editor at runtime; see ZEDManager.CreateZEDRigDisplayer() to see this process.
///
/// The Timewarp effect is achieved by logging the pose of the headset each time it's available within the
/// wrapper. Then, when a ZED image is available, the wrapper looks up the headset's position using the timestamp
/// of the image, and moves the final viewing planes according to that position. In this way, the ZED's images
/// line up with real-life, even after a ~60ms latency.
/// </summary>
public class CustomZedMixedRealityPlugin : MonoBehaviour
{
    #region DLL Calls
    const string nameDll = "sl_unitywrapper";
	[DllImport(nameDll, EntryPoint = "dllz_compute_size_plane_with_gamma")]
	private static extern System.IntPtr dllz_compute_size_plane_with_gamma(sl.Resolution resolution, float perceptionDistance, float eyeToZedDistance, float planeDistance, float HMDFocal, float zedFocal);

	[DllImport(nameDll, EntryPoint = "dllz_compute_hmd_focal")]
	private static extern float dllz_compute_hmd_focal(sl.Resolution r, float w, float h);

	/*****LATENCY CORRECTOR***/
	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_add_key_pose")]
	private static extern void dllz_latency_corrector_add_key_pose(ref Vector3 translation, ref Quaternion rotation, ulong timeStamp);

	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_get_transform")]
	private static extern int dllz_latency_corrector_get_transform(ulong timeStamp, bool useLatency,out Vector3 translation, out Quaternion rotation);

	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_initialize")]
	private static extern void dllz_latency_corrector_initialize(int device);

	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_shutdown")]
	private static extern void dllz_latency_corrector_shutdown();

	/****ANTI DRIFT ***/
	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_initialize")]
	public static extern void dllz_drift_corrector_initialize();

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_shutdown")]
	public static extern void dllz_drift_corrector_shutdown();

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_get_tracking_data")]
	public static extern void dllz_drift_corrector_get_tracking_data(ref TrackingData trackingData, ref Pose HMDTransform, ref Pose latencyCorrectorTransform, int hasValidTrackingPosition,bool checkDrift);

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_set_calibration_transform")]
	public static extern void dllz_drift_corrector_set_calibration_transform(ref Pose pose);

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_set_calibration_const_offset_transform")]
	public static extern void dllz_drift_corrector_set_calibration_const_offset_transform(ref Pose pose);
    #endregion

    /// <summary>
    /// Container for storing historic pose information, used by the latency corrector.
    /// </summary>
    public struct KeyPose
	{
		public Quaternion Orientation;
		public Vector3 Translation;
		public ulong Timestamp;
	};

    /// <summary>
    /// Container for position and rotation. Used when timestamps are not needed or have already
    /// been processed, such as setting the initial camera offset or updating the stereo rig's
    /// transform from data pulled from the wrapper.
    /// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Pose
	{
		public Vector3 translation;
		public Quaternion rotation;

		public Pose(Vector3 t, Quaternion q)
		{
			translation = t;
			rotation = q;
		}
	}

    /// <summary>
    ///
    /// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct TrackingData
	{
		public Pose zedPathTransform;
		public Pose zedWorldTransform;
		public Pose offsetZedWorldTransform;

		public int trackingState;
	}

    /// <summary>
    /// Camera object in 'finalCameraLeft', which captures the final image output to the headset's left screen.
    /// </summary>
    // [Tooltip("")]
    // public Camera finalLeftEye;
    /// <summary>
    /// Camera object in 'finalCameraRight', which captures the final image output to the headset's right screen.
    /// </summary>
    // [Tooltip("")]
    // public Camera finalRightEye;

    /// <summary>
    /// Base, pre-Timewarp offset between each final plane and its corresponding camera.
    /// </summary>
    [Tooltip("Offset between each final plane and its corresponding camera.")]
    public Vector3 offset = new Vector3(0, 0, (float)sl.Constant.PLANE_DISTANCE);

    /// <summary>
    /// Distance to set each intermediate camera from the point between them. This is half of the post-calibration
    /// distance between the ZED cameras, so X is usually very close to 0.0315m (63mm / 2).
    /// </summary>
    [Tooltip("")]
    public Vector3 halfBaselineOffset;

    /// <summary>
    /// Reference to the ZEDCamera instance, which communicates with the SDK.
    /// </summary>
    [Tooltip("Reference to the ZEDCamera instance, which communicates with the SDK.")]
    public sl.ZEDCamera zedCamera;

    /// <summary>
    /// Reference to the scene's ZEDManager instance, usually contained in ZED_Rig_Stereo.
    /// </summary>
    [Tooltip("Reference to the scene's ZEDManager instance, usually contained in ZED_Rig_Stereo.")]
    public CustomZedManager manager;

    /// <summary>
    /// Flag set to true when the target textures from the ZEDRenderingPlane overlays are ready.
    /// </summary>
    [Tooltip("Flag set to true when the target textures from the ZEDRenderingPlane overlays are ready.")]
    public bool ready = false;

    /// <summary>
    /// Flag set to true when a grab is ready, used to collect a pose from the latest time possible.
    /// </summary>
    [Tooltip("Flag set to true when a grab is ready, used to collect a pose from the latest time possible.")]
    public bool grabSucceeded = false;

    /// <summary>
    /// Flag set to true when the ZED is ready (after ZEDManager.OnZEDReady is invoked).
    /// </summary>
    [Tooltip("Flag set to true when the ZED is ready (after ZEDManager.OnZEDReady is invoked).")]
    public bool zedReady = false;

	/// <summary>
	/// If a VR device is still detected. Updated each frame. Used to know if certain updates should still happen.
	/// </summary>
	private bool hasVRDevice = false;
	public bool HasVRDevice {
		get { return hasVRDevice; }
	}

	/// <summary>
	/// The current latency pose - the pose the headset was at when the last ZED frame was captured (based on its timestamp).
	/// </summary>
	private Pose latencyPose;

    /// <summary>
    /// The physical offset of the HMD to the ZED. Represents the offset from the approximate center of the user's
    /// head to the ZED's left sensor.
    /// </summary>
    private Pose hmdtozedCalibration;

    /// <summary>
    /// Public accessor for the physical offset of the HMD to the ZED. Represents the offset from the
    /// approximate center of the user's head to the ZED's left sensor.
    /// </summary>
	public Pose HmdToZEDCalibration {
		get { return hmdtozedCalibration; }
	}

	/// <summary>
	/// Whether the latency correction is ready.
	/// </summary>
	private bool latencyCorrectionReady = false;

	/// <summary>
	/// Contains the last position computed by the anti-drift.
	/// </summary>
	public TrackingData trackingData = new TrackingData();

    /// <summary>
    /// Filename of the saved HMD to ZED calibration file loaded into hmdtozedCalibration.
    /// //If it doesn't exist, it's created with hard-coded values.
    /// </summary>
    [Tooltip("")]
    [SerializeField]
	private string calibrationFile = "CalibrationZEDHMD.ini";
    /// <summary>
    /// Path of the saved HMD to ZED calibration file loaded into hmdtozedCalibration.
    /// By default, corresponds to C:/ProgramData/Stereolabs/mr.
    /// </summary>
	private string calibrationFilePath = @"Stereolabs\mr";

    /// <summary>
    /// Delegate for the OnHMDCalibChanged event.
    /// </summary>
    public delegate void OnHmdCalibrationChanged();
    /// <summary>
    /// Event invoked if the calibration file that sets the physical ZED offset is changed at runtime.
    /// Causes ZEDManger.CalibrationHasChanged() to get called, which re-initialized the ZED's position
    /// with ZEDManager.AdjustZEDRigCameraPosition() at the next tracking update.
    /// </summary>
	public static event OnHmdCalibrationChanged OnHmdCalibChanged;



	#if UNITY_2017_OR_NEWER
	List<UnityEngine.VR.VRNodeState> nodes = new List<UnityEngine.VR.VRNodeState>();

	UnityEngine.VR.VRNodeState nodeState = new UnityEngine.VR.VRNodeState();
	#endif
	private void Awake()
	{
        //Initialize the latency tracking only if a supported headset is detected.
        //You can force it to work for unsupported headsets by implementing your own logic for calling
        //dllz_latency_corrector_initialize.
        hasVRDevice = UnityEngine.XR.XRDevice.isPresent;
		if (hasVRDevice) {
			if (UnityEngine.XR.XRDevice.model.ToLower().Contains ("vive")) //Vive or Vive Pro
				dllz_latency_corrector_initialize (0);
			else if (UnityEngine.XR.XRDevice.model.ToLower().Contains ("oculus")) //Oculus Rift
				dllz_latency_corrector_initialize (1);
			else if (UnityEngine.XR.XRDevice.model.ToLower().Contains ("windows")) //Windows MR through SteamVR Only (Beta)
				dllz_latency_corrector_initialize (1);


			dllz_drift_corrector_initialize ();
		}
		#if UNITY_2017_OR_NEWER

		nodeState.nodeType = VRNode.Head;
		nodes.Add(nodeState);
		#endif
	}

    /// <summary>
    /// Sets references not set in ZEDManager.CreateZEDRigDisplayer(), sets materials,
    /// adjusts final plane scale, loads the ZED calibration offset and other misc. values.
    /// </summary>
    void Start()
	{
		hasVRDevice = UnityEngine.XR.XRDevice.isPresent;
		manager = GetComponent<CustomZedManager>();
		zedCamera = sl.ZEDCamera.GetInstance();
		// finalLeftEye = finalCameraLeft.GetComponent<Camera>();
		// finalRightEye = finalCameraRight.GetComponent<Camera>();

		zedReady = false;
	}

	/// <summary>
	/// Computes the size of the final planes.
	/// </summary>
	/// <param name="resolution">ZED's current resolution. Usually 1280x720.</param>
	/// <param name="perceptionDistance">Typically 1.</param>
	/// <param name="eyeToZedDistance">Distance from your eye to the camera. Estimated at 0.1m.</param>
	/// <param name="planeDistance">Distance to final quad (quadLeft or quadRight). Arbitrary but set by offset.z.</param>
	/// <param name="HMDFocal">Focal length of the HMD, retrieved from the wrapper.</param>
	/// <param name="zedFocal">Focal length of the ZED, retrieved from the camera's rectified calibration parameters.</param>
	/// <returns></returns>
	public Vector2 ComputeSizePlaneWithGamma(sl.Resolution resolution, float perceptionDistance, float eyeToZedDistance, float planeDistance, float HMDFocal, float zedFocal)
	{
		System.IntPtr p = dllz_compute_size_plane_with_gamma(resolution, perceptionDistance, eyeToZedDistance, planeDistance, HMDFocal, zedFocal);

		if (p == System.IntPtr.Zero)
		{
			return new Vector2();
		}
		Vector2 parameters = (Vector2)Marshal.PtrToStructure(p, typeof(Vector2));
		return parameters;

	}

	/// <summary>
	/// Compute the focal length of the HMD.
	/// </summary>
	/// <param name="targetSize">Resolution of the headset's eye textures.</param>
	/// <returns></returns>
	// public float ComputeFocal(sl.Resolution targetSize)
	// {
	// 	float focal_hmd = dllz_compute_hmd_focal(targetSize, finalLeftEye.projectionMatrix.m00,finalLeftEye.projectionMatrix.m11);
	// 	return focal_hmd;
	// }

    /// <summary>
    /// Called once the ZED is finished initializing. Subscribed to ZEDManager.OnZEDReady in OnEnable.
    /// Uses the newly-available ZED parameters to scale the final planes (quadLeft and quadRight) to appear
    /// properly in the currently-connected headset.
    /// </summary>
	void ZEDReady()
	{
		Vector2 scaleFromZED;
		halfBaselineOffset.x = zedCamera.Baseline / 2.0f;

		float perception_distance = 1.0f;
		float zed2eye_distance = 0.1f; //Estimating 10cm between your eye and physical location of the ZED Mini.
		hasVRDevice = UnityEngine.XR.XRDevice.isPresent;

		// if (hasVRDevice) {
		// 	sl.CalibrationParameters parameters = zedCamera.CalibrationParametersRectified;

		// 	scaleFromZED = ComputeSizePlaneWithGamma (new sl.Resolution ((uint)zedCamera.ImageWidth, (uint)zedCamera.ImageHeight),
		// 		perception_distance, zed2eye_distance, offset.z,
		// 		ComputeFocal (new sl.Resolution ((uint)UnityEngine.XR.XRSettings.eyeTextureWidth, (uint)UnityEngine.XR.XRSettings.eyeTextureHeight)),
		// 		parameters.leftCam.fx);

		// 	ready = false;
		// }


		// If using Vive, change ZED's settings to compensate for different screen.
		if (UnityEngine.XR.XRDevice.model.ToLower().Contains ("vive")) {
			zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.CONTRAST, 3);
			zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.SATURATION, 3);
		}


        //Set eye layers to respective eyes. They were each set to Both during the loading screen to avoid one eye going blank at some rotations.
		// finalLeftEye.stereoTargetEye = StereoTargetEyeMask.Left;
		// finalRightEye.stereoTargetEye = StereoTargetEyeMask.Right;

		/// AR Passtrough is recommended in 1280x720 at 60, due to FoV, FPS, etc.
		/// If not set to this resolution, warn the user.
		if (zedCamera.ImageWidth != 1280 && zedCamera.ImageHeight != 720)
			Debug.LogWarning ("[ZED AR Passthrough] This resolution is not ideal for a proper AR passthrough experience. Recommended resolution is 1280x720.");

		zedReady = true;

	}

	public void OnEnable()
	{
		latencyCorrectionReady = false;
		ZEDManager.OnZEDReady += ZEDReady;
	}

	public void OnDisable()
	{
		latencyCorrectionReady = false;
		ZEDManager.OnZEDReady -= ZEDReady;
	}

	void OnGrab()
	{
		grabSucceeded = true;
	}

	/// <summary>
	/// Collects the position of the HMD with a timestamp, to be looked up later to correct for latency.
	/// </summary>
	public void CollectPose()
	{
		KeyPose k = new KeyPose();
		k.Orientation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.Head);
		k.Translation = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head);
		if (sl.ZEDCamera.GetInstance().IsCameraReady)
		{
			k.Timestamp = sl.ZEDCamera.GetInstance().GetCurrentTimeStamp();
			if (k.Timestamp >= 0)
			{
				dllz_latency_corrector_add_key_pose(ref k.Translation, ref k.Orientation, k.Timestamp); //Poses are handled by the wrapper.
			}
		}
	}

    /// <summary>
    /// Returns a pose at a specific time.
    /// </summary>
    /// <param name="r">Rotation of the latency pose.</param>
    /// <param name="t">Translation/position of the latency pose.</param>
    /// <param name="cameraTimeStamp">Timestamp for looking up the pose.</param>
    /// <param name="useLatency">Whether to use latency.</param>
    public int LatencyCorrector(out Quaternion r, out Vector3 t, ulong cameraTimeStamp, bool useLatency)
	{
		return dllz_latency_corrector_get_transform(cameraTimeStamp, useLatency, out t, out r);
	}

    /// <summary>
    /// Sets the GameObject's 3D local scale based on a 2D resolution (Z scale is unchanged).
    /// Used for scaling quadLeft/quadRight.
    /// </summary>
    /// <param name="screen">Target GameObject to scale.</param>
    /// <param name="s">2D scale factor.</param>
	public void scale(GameObject screen,  Vector2 s)
	{
		screen.transform.localScale = new Vector3(s.x, s.y, 1);
	}

	/// <summary>
	/// Set the planes/canvases to the proper position after accounting for latency.
	/// </summary>
	public void UpdateRenderPlane()
	{
		if (!ZEDManager.IsStereoRig) return; //Make sure we're in pass-through AR mode.

		Quaternion r;
		r = latencyPose.rotation;

        //Plane's distance from the final camera never changes, but it's rotated around it based on the latency pose.
	}

	/// <summary>
	/// Initialize the ZED's tracking with the current HMD position and HMD-ZED calibration.
    /// This causes the ZED's internal tracking to start where the HMD is, despite being initialized later than the HMD.
	/// </summary>
	/// <returns>Initial offset for the ZED's tracking. </returns>
	public Pose InitTrackingAR()
	{
		Transform tmpHMD = transform;
		tmpHMD.position = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head);
		tmpHMD.rotation = UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head);

		Quaternion r = Quaternion.identity;
		Vector3 t = Vector3.zero;
		Pose const_offset = new Pose(t, r);
		dllz_drift_corrector_set_calibration_const_offset_transform(ref const_offset);

		zedCamera.ResetTrackingWithOffset(tmpHMD.rotation,tmpHMD.position,HmdToZEDCalibration.rotation,HmdToZEDCalibration.translation);

		return new Pose(tmpHMD.position, tmpHMD.rotation);
	}

    /// <summary>
    /// Sets latencyPose to the pose of the headset at a given timestamp and flags whether or not it's valid for use.
    /// </summary>
    /// <param name="cameraTimeStamp">Timestamp for looking up the pose.</param>
	public void ExtractLatencyPose(ulong cameraTimeStamp)
	{
		Quaternion latency_rot;
		Vector3 latency_pos;
		if (LatencyCorrector (out latency_rot, out latency_pos, cameraTimeStamp, true) == 1) {
			latencyPose = new Pose (latency_pos, latency_rot);
			latencyCorrectionReady = true;
		} else
			latencyCorrectionReady = false;
	}

    /// <summary>
    /// Returns the most recently retrieved latency pose.
    ///
    /// </summary>
    /// <returns>Last retrieved latency pose. </returns>
	public Pose LatencyPose()
	{
		return latencyPose;
	}

    /// <summary>
    /// Gets the proper position of the ZED virtual camera, factoring in HMD offset, latency, and anti-drift.
    /// Used by ZEDManager to set the pose of Camera_eyes in the 'intermediate' rig (ZED_Rig_Stereo).
    /// </summary>
    /// <param name="position">Current position as returned by the ZED's tracking.</param>
    /// <param name="orientation">Current rotation as returned by the ZED's tracking.</param>
    /// <param name="r">Final rotation.</param>
    /// <param name="t">Final translation/position.</param>
	public void AdjustTrackingAR(Vector3 position, Quaternion orientation, out Quaternion r, out Vector3 t)
	{
		hasVRDevice = UnityEngine.XR.XRDevice.isPresent;

		Pose hmdTransform = new Pose(UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head), UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.Head)); //Current HMD position
		trackingData.trackingState = (int)manager.ZEDTrackingState; //Whether the ZED's tracking is currently valid (not off or unable to localize).
		trackingData.zedPathTransform = new Pose (position, orientation);

		if (zedReady && latencyCorrectionReady) {
			zedCamera.SetIMUOrientationPrior (ref latencyPose.rotation);
		}

		dllz_drift_corrector_get_tracking_data (ref trackingData, ref hmdTransform, ref latencyPose, 0, true);
		r = trackingData.offsetZedWorldTransform.rotation;
		t = trackingData.offsetZedWorldTransform.translation;
	}

    /// <summary>
    /// Close related ZED processes when the application ends.
    /// </summary>
	private void OnApplicationQuit()
	{
		dllz_latency_corrector_shutdown();
		dllz_drift_corrector_shutdown();
	}

}
