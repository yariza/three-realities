using UnityEngine;
using System;
using System.Threading;
using UnityEngine.VR;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// The central script of the ZED Unity plugin, and the primary way a developer can interact with the camera.
/// It sets up and closes connection to the ZED, adjusts parameters based on user settings, enables/disables/handles
/// features like tracking, and holds numerous useful properties, methods, and callbacks.
/// </summary>
/// <remarks>
/// ZEDManager is attached to the root objects in the ZED_Rig_Mono and ZED_Rig_Stereo prefabs.
/// If using ZED_Rig_Stereo, it will set isStereoRig to true, which triggers several behaviors unique to stereo pass-through AR.
/// </remarks>
public class CustomZedManager : MonoBehaviour
{
    ////////////////////////////
    //////// Public ///////////
    ////////////////////////////
    /// <summary>
    /// Current instance of the ZED Camera, which handles calls to the Unity wrapper .dll.
    /// </summary>
    public sl.ZEDCamera zedCamera;

    /// <summary>
    /// Resolution setting for all images retrieved from the camera. Higher resolution means lower framerate.
    /// HD720 is strongly recommended for pass-through AR.
    /// </summary>
    [Header("Camera")]
    [Tooltip("Resolution setting for all images retrieved from the camera. Higher resolution means lower framerate. " +
        "HD720 is strongly recommended for pass-through AR.")]
    public sl.RESOLUTION resolution = sl.RESOLUTION.HD720;
    /// <summary>
    /// Targeted FPS, based on the resolution. VGA = 100, HD720 = 60, HD1080 = 30, HD2K = 15.
    /// </summary>
    private int FPS = 60;
    /// <summary>
    /// The accuracy of depth calculations. Higher settings mean more accurate occlusion and lighting but costs performance.
    /// Note there's a significant jump in performance cost between QUALITY and ULTRA modes.
    /// </summary>
    [Tooltip("The accuracy of depth calculations. Higher settings mean more accurate occlusion and lighting but costs performance.")]
    public sl.DEPTH_MODE depthMode = sl.DEPTH_MODE.PERFORMANCE;

    /// <summary>
    /// If enabled, the ZED will move/rotate itself using its own inside-out tracking.
    /// If false, the camera tracking will move with the VR HMD if connected and available.
    /// <para>Normally, ZEDManager's GameObject will move according to the tracking. But if in AR pass-through mode,
    /// then the Camera_eyes object in ZED_Rig_Stereo will move while this object stays still. </para>
    /// </summary>
    [Header("Motion Tracking")]

    [Tooltip("If enabled, the ZED will move/rotate itself using its own inside-out tracking. " +
        "If false, the camera tracking will move with the VR HMD if connected and available.")]
    public bool enableTracking = true;
    /// <summary>
    /// Enables the spatial memory. Will detect and correct tracking drift by remembering features and anchors in the environment,
	/// but may cause visible jumps when it happens.
    /// </summary>
	[Tooltip("Enables the spatial memory. Will detect and correct tracking drift by remembering features and anchors in the environment, "
        + "but may cause visible jumps when it happens")]
    public bool enableSpatialMemory = true;
    /// <summary>
    /// If using Spatial Memory, you can specify a path to an existing .area file to start with some memory already loaded.
    /// .area files are created by scanning a scene with ZEDSpatialMappingManager and saving the scan.
    /// </summary>
    [Tooltip("If using Spatial Memory, you can specify a path to an existing .area file to start with some memory already loaded. " +
        ".area files are created by scanning a scene with ZEDSpatialMappingManager and saving the scan.")]
    public string pathSpatialMemory = "ZED_spatial_memory";

    /// <summary>
    /// Brightness of the final real-world image. Default is 1. Lower to darken the environment in a realistic-looking way.
    /// This is a rendering setting that doesn't affect the raw input from the camera.
    /// </summary>
    [Range(0, 1)]
    [Tooltip("Brightness of the final real-world image. Default is 1. Lower to darken the environment in a realistic-looking way. "  +
        "This is a rendering setting that doesn't affect the raw input from the camera.")]
    public float m_cameraBrightness = 1.0f;
    /// <summary>
    /// Public accessor for m_cameraBrightness, which is the post-capture brightness setting of the real-world image.
    /// </summary>
	public float CameraBrightness
    {
		get {return m_cameraBrightness;}
        set {
			if (m_cameraBrightness == value) return;
			m_cameraBrightness = value;
			if (OnCamBrightnessChange != null)
				OnCamBrightnessChange(m_cameraBrightness);
        }
    }

    /// <summary>
    /// Delegate for OnCamBrightnessChange, which is used to update shader properties when the brightness setting changes.
    /// </summary>
    /// <param name="newVal"></param>
	public delegate void onCamBrightnessChangeDelegate(float newVal);
    /// <summary>
    /// Event fired when the camera brightness setting is changed. Used to update shader properties.
    /// </summary>
	public event onCamBrightnessChangeDelegate OnCamBrightnessChange;

    //Strings used for the Status display in the Inspector.
    /// <summary>
    /// Version of the installed ZED SDK, for display in the Inspector.
    /// </summary>
    [Header("Status")]
    [ReadOnly("Version")] [HideInInspector] public string versionZED = "-";
    /// <summary>
    /// How many frames per second the engine is rendering, for display in the Inspector.
    /// </summary>
    [ReadOnly("Engine FPS")] [HideInInspector] public string engineFPS = "-";
    /// <summary>
    /// How many images per second are received from the ZED, for display in the Inspector.
    /// </summary>
    [ReadOnly("Camera FPS")] [HideInInspector] public string cameraFPS = "-";
    /// <summary>
    /// The connected VR headset, if any, for display in the Inspector.
    /// </summary>
    [ReadOnly("HMD Device")] [HideInInspector] public string HMDDevice = "-";
    /// <summary>
    /// Whether the ZED's tracking is on, off, or searching (lost position, trying to recover) for display in the Inspector.
    /// </summary>
    [ReadOnly("Tracking State")] [HideInInspector] public string trackingState = "-";

    ////////////////////////////
    //////// Private ///////////
    ////////////////////////////
    /// <summary>
    /// Initialization parameters used to start the ZED. Holds settings that can't be changed at runtime
    /// (resolution, depth mode, .SVO path, etc.).
    /// </summary>
    private sl.InitParameters initParameters;
    /// <summary>
    /// Runtime parameters used to grab a new image. Settings can change each frame, but are lower level
    /// (sensing mode, point cloud, if depth is enabled, etc.).
    /// </summary>
    private sl.RuntimeParameters runtimeParameters;
    /// <summary>
    /// Enables the ZED SDK's depth stabilizer, which improves depth accuracy and stability. There's rarely a reason to disable this.
    /// </summary>
    private bool depthStabilizer = true;
    /// <summary>
    /// Whether the camera is currently being tracked using the ZED's inside-out tracking.
    /// </summary>
    private bool isZEDTracked = false;
    /// <summary>
    /// Whether the ZED's inside-out tracking has been activated.
    /// </summary>
    private bool isTrackingEnable = false;
	/// <summary>
	/// Whether the camera is tracked in any way (ZED's tracking or a VR headset's tracking).
	/// </summary>
	private bool isCameraTracked = false;
    /// <summary>
    /// Public accessor for whether the camera is tracked in any way (ZED's tracking or a VR headset's tracking).
    /// </summary>
	public bool IsCameraTracked
	{
		get { return isCameraTracked; }
	}


    /// <summary>
    /// Orientation last returned by the ZED's tracking.
    /// </summary>
	private Quaternion zedOrientation = Quaternion.identity;
    /// <summary>
    /// Position last returned by the ZED's tracking.
    /// </summary>
	private Vector3 zedPosition = new Vector3();

    /// <summary>
	/// Position of the camera (zedRigRoot) when the scene starts. Not used in Stereo AR.
    /// </summary>
    private Vector3 initialPosition = new Vector3();
    /// <summary>
	/// Orientation of the camera (zedRigRoot) when the scene starts. Not used in Stereo AR.
    /// </summary>
	private Quaternion initialRotation = Quaternion.identity;
    /// <summary>
    /// Sensing mode: STANDARD or FILL. FILL corrects for missing depth values.
    /// Almost always better to use FILL, since we need depth without holes for proper occlusion.
    /// </summary>
    private sl.SENSING_MODE sensingMode = sl.SENSING_MODE.FILL;
    /// <summary>
    /// Rotation offset used to retrieve the tracking with a rotational offset.
    /// </summary>
    private Quaternion rotationOffset;
    /// <summary>
    /// Position offset used to retrieve the tracking with a positional offset.
    /// </summary>
    private Vector3 positionOffset;
    /// <summary>
    /// Enables pose smoothing during drift correction. For AR, this is especially important when
    /// spatial memory is activated.
    /// </summary>
    private bool enablePoseSmoothing = true;
    /// <summary>
    /// The engine FPS, updated every frame.
    /// </summary>
    private float fps_engine = 90.0f;

    /// <summary>
    /// Checks if the ZED has finished initializing.
    /// </summary>
    private bool zedReady = false;
    /// <summary>
    /// Checks if the ZED has finished initializing.
    /// </summary>
    public bool IsZEDReady
    {
        get { return zedReady; }
    }

    /// <summary>
    /// Flag set to true if the camera was connected and the wasn't anymore.
    /// Causes ZEDDisconnected() to be called each frame, which attemps to restart it.
    /// </summary>
    private bool isDisconnected = false;

    /// <summary>
    /// Current state of tracking: On, Off, or Searching (lost tracking, trying to recover). Used by anti-drift.
    /// </summary>
    private sl.TRACKING_STATE zedtrackingState = sl.TRACKING_STATE.TRACKING_OFF;
    /// <summary>
    /// Current state of tracking: On, Off, or Searching (lost tracking, trying to recover). Used by anti-drift.
    /// </summary>
    public sl.TRACKING_STATE ZEDTrackingState
    {
        get { return zedtrackingState; }
    }

    /// <summary>
    /// First position registered after the tracking has started (whether via ZED or a VR HMD).
    /// </summary>
    public Vector3 OriginPosition { get; private set; }
    /// <summary>
    /// First rotation/orientation registered after the tracking has started (whether via ZED or a VR HMD).
    /// </summary>
    public Quaternion OriginRotation { get; private set; }

    ///////////////////////////////////////////////////
    [ReadOnly("Gravity Rotation")] public Quaternion gravityRotation = Quaternion.identity;
    [ReadOnly("Zed Sync Position")] public Vector3 ZEDSyncPosition;
    [ReadOnly("HMD Sync Position")] public Vector3 HMDSyncPosition;
    [ReadOnly("Zed Sync Rotation")] public Quaternion ZEDSyncRotation;
    [ReadOnly("HMD Sync Rotation")] public Quaternion HMDSyncRotation;

    CustomZedMixedRealityPlugin arRig;

    /// <summary>
    /// Delegate for OnZEDReady.
    /// </summary>
    public delegate void OnZEDManagerReady();
    /// <summary>
    /// Called when the ZED has finished initializing successfully.
    /// Used by many scripts to run startup logic that requires that the ZED is active.
    /// </summary>
    public static event OnZEDManagerReady OnZEDReady;

    /// <summary>
    /// Delegate for OnZEDDisconnected.
    /// </summary>
    public delegate void OnZEDManagerDisconnected();
    /// <summary>
    /// Event called when ZED was running but became disconnected.
    /// </summary>
    public static event OnZEDManagerDisconnected OnZEDDisconnected;


    /// <summary>
    /// ZEDManager instance for singleton implementation.
    /// </summary>
    // Static singleton instance
    private static CustomZedManager instance;

    /// <summary>
    /// Singleton implementation: Gets the scene's instance of ZEDManager, and creates one in if nonexistant.
    /// </summary>
    public static CustomZedManager Instance
    {
        get { return instance ?? (instance = new GameObject("ZEDManager").AddComponent<CustomZedManager>()); }
    }

    #region Threads

    /// <summary>
    /// Image acquisition thread.
    /// </summary>
    private Thread threadGrab = null;
    /// <summary>
    /// Mutex for the image acquisition thread.
    /// </summary>
    public object grabLock = new object();
    /// <summary>
    /// State of the image acquisition thread.
    /// </summary>
    private bool running = false;

    /// <summary>
    /// Initialization thread.
    /// </summary>
    private Thread threadOpening = null;
    /// <summary>
    /// Result of the latest attempt to initialize the ZED.
    /// </summary>
    public static sl.ERROR_CODE LastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;
    /// <summary>
    /// Result of last frame's attempt to initialize the ZED.
    /// </summary>
    public static sl.ERROR_CODE PreviousInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;
    /// <summary>
    /// State of the ZED initialization thread.
    /// </summary>
    private bool openingLaunched;

    /// <summary>
    /// Tracking initialization thread. Used as the tracking takes some time to start.
    /// </summary>
    private Thread trackerThread;

    #endregion

    #region Timestamps

	/////////////////////////////////////
	//////  Timestamps             //////
	/////////////////////////////////////

    /// <summary>
    /// Timestamp of the last ZED image grabbed. Textures from this grab may not have updated yet.
    /// </summary>
	private ulong cameraTimeStamp = 0;
    /// <summary>
    /// Timestamp of the last ZED image grabbed. Textures from this grab may not have updated yet.
    /// </summary>
	public ulong CameraTimeStamp
	{
		get { return cameraTimeStamp; }
	}

    /// <summary>
    /// Timestamp of the images used to create the current textures.
    /// </summary>
	private ulong imageTimeStamp = 0;
    /// <summary>
    /// Timestamp of the images used to create the current textures.
    /// </summary>
	public ulong ImageTimeStamp
	{
		get { return imageTimeStamp; }
	}

    /// <summary>
    /// Whether the grabbing thread should grab a new frame from the ZED SDK.
    /// True unless the last grabbed frame hasn't been applied yet, or the ZED isn't initialized.
    /// </summary>
	private bool requestNewFrame = false;
    /// <summary>
    /// Whether a new frame has been grabbed from the ZED SDK that needs to be updated.
    /// </summary>
	private bool newFrameAvailable = false;

    #endregion

    void Awake()
    {
        instance = this;
        zedReady = false;

        arRig = GetComponent<CustomZedMixedRealityPlugin>();
        if (arRig == null)
        {
            arRig = gameObject.AddComponent<CustomZedMixedRealityPlugin>();
        }

        //Set first few parameters for initialization. This will get passed to the ZED SDK when initialized.
        initParameters = new sl.InitParameters();
        initParameters.resolution = resolution;
        initParameters.depthMode = depthMode;
        initParameters.enableRightSideMeasure = true; //Creates a depth map for both eyes, not just one.
        initParameters.depthMinimumDistance = 0.1f; //Allow depth calculation to very close objects.
        initParameters.depthStabilization = depthStabilizer;

        //Create a ZEDCamera instance and return an error message if the ZED SDK's dependencies are not detected.
        zedCamera = sl.ZEDCamera.GetInstance();
        LastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;

        zedCamera.CreateCamera(false);

        versionZED = "[SDK]: " + sl.ZEDCamera.GetSDKVersion().ToString() + " [Plugin]: " + sl.ZEDCamera.PluginVersion.ToString();

        LastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;
        openingLaunched = false;
        StartCoroutine(InitZED());
    }

    void OnApplicationQuit()
    {
        zedReady = false;
        Destroy(); //Close the grab and initialization threads.

        if (zedCamera != null)
        {
            zedCamera.Destroy();
            zedCamera = null;
        }
    }

    /// <summary>
    /// Stops the initialization and grabbing threads.
    /// </summary>
    public void Destroy()
    {
        running = false;

        //In case the opening thread is still running.
        if (threadOpening != null)
        {
            threadOpening.Join();
            threadOpening = null;
        }

        //Shut down the image grabbing thread.
        if (threadGrab != null)
        {
            threadGrab.Join();
            threadGrab = null;
        }

        Thread.Sleep(10);
    }

    #region INITIALIZATION

	/// <summary>
	/// ZED opening function. Should be called in the initialization thread (threadOpening).
	/// </summary>
    void OpenZEDInBackground()
    {
        openingLaunched = true;
        LastInitStatus = zedCamera.Init(ref initParameters);
        openingLaunched = false;
    }

	/// <summary>
	/// Initialization coroutine.
	/// </summary>
	private uint numberTriesOpening = 0;/// Counter of tries to open the ZED
	const int MAX_OPENING_TRIES = 50;
    System.Collections.IEnumerator InitZED()
    {
        zedReady = false;
        while (LastInitStatus != sl.ERROR_CODE.SUCCESS)
        {
            //Initialize the camera
            if (!openingLaunched) //Don't try initializing again if the last attempt is still going.
            {
                threadOpening = new Thread(new ThreadStart(OpenZEDInBackground)); //Assign thread.

                if (LastInitStatus != sl.ERROR_CODE.SUCCESS) //If it failed, report it and log one failure.
                {
#if UNITY_EDITOR
                    numberTriesOpening++;
                    if (numberTriesOpening % 2 == 0 && LastInitStatus == PreviousInitStatus)
                    {
                        Debug.LogWarning("[ZEDPlugin]: " + LastInitStatus);
                    }

                    if (numberTriesOpening > MAX_OPENING_TRIES) //Failed too many times. Give up.
                    {
                        Debug.Log("[ZEDPlugin]: Stopping initialization.");
                        yield break;
                    }
#endif


                    PreviousInitStatus = LastInitStatus;
                }


                threadOpening.Start();
            }

            yield return new WaitForSeconds(0.3f);
        }


        //ZED has initialized successfully.
        if (LastInitStatus == sl.ERROR_CODE.SUCCESS)
        {
            threadOpening.Join();

            //Initialize the tracking thread, AR initial transforms and SVO read/write as needed.
            ZEDReady();

            //If using tracking, wait until the tracking thread has been initialized.
            while (enableTracking && !isTrackingEnable)
            {
                yield return new WaitForSeconds(0.5f);
            }

            //Tells all the listeners that the ZED is ready! :)
            if (OnZEDReady != null)
            {
                OnZEDReady();
            }

            //Make sure the screen is at 16:9 aspect ratio or close. Warn the user otherwise.
            float ratio = (float)Screen.width / (float)Screen.height;
            float target = 16.0f / 9.0f;
            if (Mathf.Abs(ratio - target) > 0.01)
            {
                Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SCREEN_RESOLUTION));
            }


            //If not already launched, launch the image grabbing thread.
            if (!running)
            {

                running = true;
                requestNewFrame = true;

                threadGrab = new Thread(new ThreadStart(ThreadedZEDGrab));
                threadGrab.Start();

            }

            zedReady = true;
            isDisconnected = false; //In case we just regained connection.

            // setRenderingSettings(); //Find the ZEDRenderingPlanes in the rig and configure them.
            // AdjustZEDRigCameraPosition(); //If in AR mode, move cameras to proper offset relative to zedRigRoot.
        }
    }

    #endregion

    /// <summary>
    /// Initialize the SVO, and launch the thread to initialize tracking. Called once the ZED
    /// is initialized successfully.
    /// </summary>
    private void ZEDReady()
    {
        FPS = (int)zedCamera.GetRequestedCameraFPS();
        if (enableTracking)
        {
            trackerThread = new Thread(EnableTrackingThreaded);
            trackerThread.Start();
        }

        if (enableTracking)
            trackerThread.Join();

        CustomZedMixedRealityPlugin.Pose pose = arRig.InitTrackingAR();
        OriginPosition = pose.translation;
        OriginRotation = pose.rotation;
    }

    /// <summary>
    /// Initializes the ZED's inside-out tracking. Started as a separate thread in OnZEDReady.
    /// </summary>
    void EnableTrackingThreaded()
    {
        enablePoseSmoothing = enableSpatialMemory;
        lock (grabLock)
        {
            //Make sure we have grabbed a frame first.
            sl.ERROR_CODE e = zedCamera.Grab(ref runtimeParameters);
            int timeOut_grab = 0;
            while (e != sl.ERROR_CODE.SUCCESS && timeOut_grab < 100)
            {
                e = zedCamera.Grab(ref runtimeParameters);
                Thread.Sleep(10);
                timeOut_grab++;
            }

            //If using spatial memory and given a path to a .area file, make sure that path is valid.
            if (enableSpatialMemory && pathSpatialMemory != "" && !System.IO.File.Exists(pathSpatialMemory))
            {
                Debug.Log("Specified path to .area file '" + pathSpatialMemory + "' does not exist. Ignoring.");
                pathSpatialMemory = "";
            }

            //Now enable the tracking with the proper parameters.
            if (!(enableTracking = (zedCamera.EnableTracking(ref zedOrientation, ref zedPosition, enableSpatialMemory,
                enablePoseSmoothing, pathSpatialMemory) == sl.ERROR_CODE.SUCCESS)))
            {
                throw new Exception(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.TRACKING_NOT_INITIALIZED));
            }
            else
            {
                isTrackingEnable = true;
            }
        }
    }

    #region IMAGE_ACQUIZ
    /// <summary>
    /// Continuously grabs images from the ZED. Runs on its own thread.
    /// </summary>
    private void ThreadedZEDGrab()
    {
        runtimeParameters = new sl.RuntimeParameters();
        runtimeParameters.sensingMode = sensingMode;
        runtimeParameters.enableDepth = true;
        //Don't change this reference frame. If we need normals in the world frame, better to do the conversion ourselves.
        runtimeParameters.measure3DReferenceFrame = sl.REFERENCE_FRAME.CAMERA;

        while (running)
        {
            if (zedCamera == null)
                return;

            AcquireImages();
        }

    }

    /// <summary>
    /// Grabs images from the ZED SDK and updates tracking, FPS and timestamp values.
    /// Called from ThreadedZEDGrab() in a separate thread.
    /// </summary>
    private void AcquireImages()
    {

		if (requestNewFrame && zedReady)
        {

			sl.ERROR_CODE e = sl.ERROR_CODE.NOT_A_NEW_FRAME;

            e = zedCamera.Grab (ref runtimeParameters);

            lock (grabLock)
            {
                if (e == sl.ERROR_CODE.CAMERA_NOT_DETECTED)
                {
                    Debug.Log("Camera not detected or disconnected.");
                    isDisconnected = true;
                    Thread.Sleep(10);
                    requestNewFrame = false;
                }
                else if (e == sl.ERROR_CODE.SUCCESS)
                {

                    //Save the timestamp
                    cameraTimeStamp = zedCamera.GetCameraTimeStamp();

#if UNITY_EDITOR
                    float camera_fps = zedCamera.GetCameraFPS();
                    cameraFPS = camera_fps.ToString() + "Fps";

                    if (camera_fps <= FPS * 0.8)
                        cameraFPS += " WARNING: Low USB bandwidth detected";
#endif

                    //Get position of camera
                    if (isTrackingEnable)
                    {
						zedtrackingState = zedCamera.GetPosition(ref zedOrientation, ref zedPosition, sl.TRACKING_FRAME.LEFT_EYE);
                    }
                    else
                        zedtrackingState = sl.TRACKING_STATE.TRACKING_OFF;


                    // Indicate that a new frame is available and pause the thread until a new request is called
                    newFrameAvailable = true;
                    requestNewFrame = false;
                }
				else
				   Thread.Sleep(1);
            }
        }
        else
        {
            //To avoid "overheating."
            Thread.Sleep(1);
        }
    }
    #endregion

    #region ENGINE_UPDATE

    /// <summary>
    /// If a new frame is available, this function retrieves the images and updates the Unity textures. Called in Update().
    /// </summary>
    public void UpdateImages()
    {
        if (zedCamera == null)
            return;

        if (newFrameAvailable) //ThreadedZEDGrab()/AcquireImages() grabbed images we haven't updated yet.
        {
            lock (grabLock)
            {
                zedCamera.RetrieveTextures(); //Tell the wrapper to compute the textures.
                zedCamera.UpdateTextures(); //Tell the wrapper to update the textures.
                imageTimeStamp = zedCamera.GetImagesTimeStamp();
            }

            requestNewFrame = true; //Lets ThreadedZEDGrab/AcquireImages() start grabbing again.
            newFrameAvailable = false;
        }
    }


    /// <summary>
    /// Gets the tracking position from the ZED and updates zedRigRoot's position. Also updates the AR tracking if enabled.
	/// Only called in Live (not SVO playback) mode. Called in Update().
    /// </summary>
    private void UpdateTracking()
    {
        if (!zedReady)
            return;

		if (isZEDTracked) //ZED inside-out tracking is enabled and initialized.
        {
			Quaternion r;
			Vector3 v;

			isCameraTracked = true;

			if (UnityEngine.XR.XRDevice.isPresent) //AR pass-through mode.
            {
				// if (calibrationHasChanged) //If the HMD offset calibration file changed during runtime.
                // {
				// 	AdjustZEDRigCameraPosition(); //Re-apply the ZED's offset from the VR headset.
				// 	calibrationHasChanged = false;
				// }

				arRig.ExtractLatencyPose (imageTimeStamp); //Find what HMD's pose was at ZED image's timestamp for latency compensation.
                arRig.AdjustTrackingAR (zedPosition, zedOrientation, out r, out v);
				// zedRigRoot.localRotation = r;
				// zedRigRoot.localPosition = v;

				ZEDSyncPosition = v;
				ZEDSyncRotation = r;
				HMDSyncPosition = arRig.LatencyPose ().translation;
				HMDSyncRotation = arRig.LatencyPose ().rotation;
			}
            else //Not AR pass-through mode.
            {
				// zedRigRoot.localRotation = zedOrientation;
				// zedRigRoot.localPosition = zedPosition;
			}
		} else if (UnityEngine.XR.XRDevice.isPresent) //ZED tracking is off but HMD tracking is on. Fall back to that.
        {
			isCameraTracked = true;
			arRig.ExtractLatencyPose (imageTimeStamp); //Find what HMD's pose was at ZED image's timestamp for latency compensation.
            HMDSyncPosition = arRig.LatencyPose ().translation;
            HMDSyncRotation = arRig.LatencyPose ().rotation;
            // zedRigRoot.localRotation = arRig.LatencyPose ().rotation;
			// zedRigRoot.localPosition = arRig.LatencyPose ().translation;
		}
        else //The ZED is not tracked by itself or an HMD.
			isCameraTracked = false;
    }

    /// <summary>
    /// Stores the HMD's current pose. Used in AR mode for latency compensation.
    /// Pose will be applied to final canvases when a new image's timestamp matches
    /// the time when this is called.
    /// </summary>
    void UpdateHmdPose()
    {
        arRig.CollectPose(); //Save headset pose with current timestamp.
    }

    /// <summary>
    /// Updates images, collects HMD poses for latency correction, and applies tracking.
    /// Called by Unity each frame.
    /// </summary>
	void Update()
    {
        // Then update the tracking
        UpdateImages(); //Image is updated first so we have its timestamp for latency compensation.
        UpdateHmdPose(); //Store the HMD's pose at the current timestamp.
        UpdateTracking(); //Apply position/rotation changes to zedRigRoot.

        //Check if ZED is disconnected; invoke event and call function if so.
        if (isDisconnected)
        {
            if (OnZEDDisconnected != null)
                OnZEDDisconnected(); //Invoke event. Used for GUI message and pausing ZEDRenderingPlanes.

            ZEDDisconnected(); //Tries to reset the camera.
        }

		#if UNITY_EDITOR
        //Update strings used for displaying stats in the Inspector.
        if (zedCamera != null)
        {
            float frame_drop_count = zedCamera.GetFrameDroppedPercent();
            float CurrentTickFPS = 1.0f / Time.deltaTime;
            fps_engine = (fps_engine + CurrentTickFPS) / 2.0f;
            engineFPS = fps_engine.ToString("F1") + " FPS";
            if (frame_drop_count > 30 && fps_engine < 45)
                engineFPS += "WARNING: Low engine framerate detected";

			if (isZEDTracked)
				trackingState = ZEDTrackingState.ToString();
			else if (UnityEngine.XR.XRDevice.isPresent)
				trackingState = "HMD Tracking";
			else
				trackingState = "Camera Not Tracked";
        }
		#endif

    }

    public void LateUpdate()
    {
    }
    #endregion

    /// <summary>
    /// Event called when camera is disconnected
    /// </summary>
    void ZEDDisconnected()
    {
        cameraFPS = "Disconnected";

        isDisconnected = true;

        if (zedReady)
        {
            Reset(); //Cache tracking, turn it off and turn it back on again.
        }
    }

    /// <summary>
    /// Closes out the current stream, then starts it up again while maintaining tracking data.
    /// Used when the zed becomes unplugged, or you want to change a setting at runtime that
    /// requires re-initializing the camera.
    /// </summary>
    public void Reset()
    {
        //Save tracking
        if (enableTracking && isTrackingEnable)
        {
            zedCamera.GetPosition(ref zedOrientation, ref zedPosition);
        }

        OnApplicationQuit();

        openingLaunched = false;
        running = false;

        Awake();

    }
}

