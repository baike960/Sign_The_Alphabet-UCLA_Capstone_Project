// // // //  ** ** ** ** ** ** ** // // // //  ** ** ** ** ** ** ** // // // // ** ** ** ** ** ** *
// * Copyright 2017  All Rights Reserved.
// *
// * Please direct any bugs or questions to vadakuma@gmail.com
// * version 1.4.1
// * author vadakuma 
// // // //  ** ** ** ** ** ** ** // // // //  ** ** ** ** ** ** ** // // // // ** ** ** ** ** ** **
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SwipeEffect
{
	/**data for new upload settings */
	[System.Serializable]
	public class DownLoadSettings {
		[Tooltip("The folder where images will be downloaded from URLs")]
		public	string	 		folderForWebImages;
		[Tooltip("set web url")]
		public	List<string> 	urls = new List<string>();
	}

	[System.Serializable]
	public class AspectRatio {
		[SerializeField, Tooltip("Preserve image aspect ratio when using screen height")]
		public bool		preserveAspectRatio = false;
		public float 	W = 1920;
		public float 	H = 1080;
	}

	[System.Serializable]
	public class TouchZoneControl {
		public enum TouchZoneType
		{
			TZT_FullScreen,
			TZT_AutoControl,
			TZT_Manual
		};
		public	bool			use = false;
		public TouchZoneType	touchZoneType 	= TouchZoneType.TZT_FullScreen;
		public Rect				touchZone 		= new Rect(0,0,512,512);
	}

	/**
	 * swipe images array with mouse or win touches  
	 * */
	public class SwipeEffect : MonoBehaviour {

		[HideInInspector][Tooltip("Not using now!")] 
		public		string[]			imagesFolderPath;
		[Tooltip("Required download settings")][HideInInspector]
		public		DownLoadSettings	imageDownloadSettings = new DownLoadSettings();	//

		[SerializeField, Tooltip("images array for slide")]
		protected  	ImageDataContainer	imageDataContainer = new ImageDataContainer();
		[Header("General Settings")]
		[SerializeField]
		protected	bool 				useSoundForEachImage  = false;
		[SerializeField, Tooltip("WASD, Arrows, control")]
		protected	bool 				useKeyBoardControl  = false;
		[SerializeField, Tooltip("Unique name")]
		protected  	string 				slideShowName = "Empty";

		public enum SlideDirection {
			SD_Horizontal,
			SD_Vertical
		};
		[SerializeField]
		protected 	SlideDirection 		slideDirection;		// up|down or left|right sliding direction 
		public enum SlideType {
			ST_Image_DEPRICATED, 		//  is Texture (Depricated)
			ST_Button_DEPRICATED,		//  is GUI.Button, 
			ST_UIRawImage	//  is RawImage
			//ST_GameObject	//	is GameObject
		};
		[SerializeField]
		protected 	SlideType 			slideType;	
		
		/**	*/
		public enum SlidingEffect {
			SE_Simple,
			SE_Circle,
			SE_CircleUp,
			SE_CircleDown,
			SE_ScreenThrough,
			
		};
		//[SerializeField, Tooltip("Images switching effect")] 
		protected 	SlidingEffect slidingEffect;
		[SerializeField]
		protected  	bool 		showSlide = false;
		[SerializeField]
		protected  	bool 		infiniteSwipe = true;
		[SerializeField]
		protected  	bool 		autoSlideOnIdle = false;
		[SerializeField]
		protected  	float 		waitOnAutoSlideTime = 5.0f;
		protected  	float 		waitOnAutoSlideTimer = 0.0f;
		[SerializeField]
		protected  	float 		autoSlideInterval = 3.0f;
		protected  	float 		autoSlideTimer = 0.0f;
		[SerializeField, Range (-1,1), Tooltip("Auto Slide Direction")]
		protected  	int 		autoSlideDirection = 1;
		[SerializeField, Tooltip("FullScreen foreGround")] //[FormerlySerializedAs("myValue")] working only in 5.6
		protected  	Texture 	foreGround;
		[SerializeField, Range (-3f,4f),Tooltip("Swipe Speed")]
		protected  	float 		swipeSpeed = 1f;
		[SerializeField, Tooltip("slides position offset. left right")]
		protected 	float		posX;
		[SerializeField, Tooltip("slides position offset. up down")]
		protected 	float		posY;
		[SerializeField, Tooltip("Work only for ST_UIRawImage. Event @TapImageEvent@")]
		protected 	bool 		useTapImageEvent = false;
		[SerializeField]
		protected	bool 		useScreenWidth  = true;
		[SerializeField]
		protected	bool 		useScreenHeight = true;
		[SerializeField, Range (32.0f,3840f),Tooltip("Max Images Width")]
		protected 	float 		imagesWidth = 512;
		[SerializeField, Range (32.0f,2160f),Tooltip("Max Images Height")]
		protected 	float 		imagesHeight = 512;
		[SerializeField, Tooltip("activate automoving slide system")] 
		protected 	bool 		useImageParking;
		protected 	bool 		currUseImageParking;
		[SerializeField, Range (4,128),Tooltip("Parking Sensitivity")]
		protected 	int 		parkingSensitivity = 64;
		[SerializeField, Range (0.0f,1.0f),Tooltip("Speed Dropping Smoothness")]
		protected 	float 		speedDroppingSmoothnes = 0.1f;
		[SerializeField, Tooltip("Category of parameters for setting up two buttons. This bittons can slide images/buttons to next or previos position.")] 
		protected 	ButtonNavigation 	ButtonNavigation;
		[SerializeField, Tooltip("Background sound for slideshow")]
		protected  	SlideShowSound 		backGroundSound;

		[Header("Only UI RawImage settings")]
		[SerializeField, Tooltip(" ")]
		protected  	int 		sortingOrder = 0;
		[SerializeField, Tooltip("You can sliding images from touch zone on the screen")]
		protected 	TouchZoneControl	touchZoneControl = new TouchZoneControl();
		//[SerializeField, Tooltip("Image aspect ratio width / height")]
		//protected AspectRatio	aspectWidthHeight = new AspectRatio();
		[SerializeField, Tooltip("Image aspect ratio screen width / height")]
		protected 	bool		aspectRatio = true;

		//[Header("EXPERIMENTAL. Only Object Swipe settings.")]
		//[SerializeField, Tooltip("Object Swipe Settings")]
		protected ObjectSwipeSettings objectSwipe = new ObjectSwipeSettings();

		//TODO: need refactoring to state pattern some parts of code
		private		IBaseState	InputState;
		private		IBaseState	State = new ObjectSwipe(null, null); // game object sliding

		/* NEW UI STUFF
		 * */
		private 	GameObject  canvas;						// main canvas gameobject for RawImage sliding
		private 	Canvas  	slideshowCanvas;			// main canvas object component for RawImage sliding
		private 	RawImage[] 	rawImages = new RawImage[1];
		private 	RawImage 	foregroundImage;			// for ST_UIRawImage slide mode
		private 	Button 		tapButton;					// for ST_UIRawImage slide mode
		private 	RectTransform	tapButtonRect;			// for ST_UIRawImage slide mode
		private		GameObject	tapButtonObject;
		private 	int 		lastDrawUIIndex    = -1;
		private 	bool 		isSmoothReturnMode = false;
		private 	bool 		isFreezeSliding	   = false;

		private		GUISkin 	btnSkin;
		private		int			currentImageCounter;		// image number at center of screen
		private		int			oldImageCounter;			// last image number at center of screen
		private		float 		currentimagesWidth;
		private		float 		currentimagesHeight;
		private		float 		deltaShift;
		private  	bool 		bTouchDown 		= false;
		private		float 		drawAlpha 		= 0.0f;
		private		float 		minDrawAlpha 	= 0.0125f;
		private 	Vector3 	lastPos;
		private 	bool 		mouseClick;
		private 	int 		imagesCount; 				// how many slides we have
		private		float 		deltaYaw 		= 0.0f;
		private		float 		parkingDeltaYaw = 0.0f;
		private		float 		clickDeltaYaw   = 0.0f;
	
		private 	float 		shiftCorrectionFactor 	= 1.0f;
		private 	float 		shiftCorrectionConstant = 360.448f;
		private 	int 		imagesCountOnScreen 	= 2;	// how many slides we can show screen on moment
		private		int 		guiDepth 		= 2;
		private 	float 		partCircleAngle = 360.0f;

		private 	float 		fingerPath;
		private 	float 		fingerPathThreshold = 15.0f;

		private 	float 		finalPosX;
		private 	float 		finalPosY;
		private		Rect 		totalPos 		= new Rect(0,0,0,0);
		private		float 		posCenterImage 	= 0;

		//check screen resolution
		private 	float 		lastScreenWidth  = 0f;
		private 	float 		lastScreenHeight = 0f;
		private		float 		scalePosX 		 = 1920.0f;
		private		float 		scalePosY 		 = 1080.0f;
		private	readonly Vector2 fullscreenRes	 = new Vector2(1920.0f,1080.0f);
		//
		protected 	delegate void 		AutoSlideEffect();
		protected 	AutoSlideEffect 	AutoSlide = () => {};

		protected 	delegate void 		DrawSlideSystem();
		protected 	DrawSlideSystem 	DrawSlideMode 		= () => {};
		protected 	DrawSlideSystem 	DrawForeGround  	= () => {};
		protected 	DrawSlideSystem 	EventImageCounter 	= () => {};

		protected 	delegate void 		DrawSlideType(Rect rect, int index);
		protected 	DrawSlideType 		DrawSlide;

		protected 	delegate void 		DrawNavigation();
		protected 	DrawNavigation 		DrawNavigationButton = () => {};

		protected 	delegate void 		PlaySlideShowSound();
		protected 	PlaySlideShowSound 	PlayBySlideSound = () => {};

		public 		delegate void 		TickUpdate();
		public 		TickUpdate 			UpdateInput = () => {};
		public 		TickUpdate 			UpdateState = () => {};


		void Awake() {  }

		/**
		 * Use this for initialization*/
		void Start () {
			if(imageDataContainer.GetImages == null || imageDataContainer.GetImages.Length < 1)
			{
				Debug.LogWarning("Warning! Images array is empty!");
				DrawSlideMode = new DrawSlideSystem(DrawSlideOff);
				return;
			}
			lastScreenWidth  	= Screen.width;
			lastScreenHeight 	= Screen.height;
			imagesCount 		= imageDataContainer.GetImagesAmount;
			partCircleAngle 	= 360.0f/imagesCount;
			currUseImageParking = useImageParking;
			
			StartCoroutine("ResetUIPosition");
			DrawSlideMode = new DrawSlideSystem(DrawSlideOn);

			btnSkin = ScriptableObject.CreateInstance("GUISkin") as GUISkin;

			
			//ResetUIPosition();

			if(backGroundSound.UseSound) {
				AudioSource aSource = gameObject.GetComponent<AudioSource>();
				if(aSource == null) {
					aSource = gameObject.AddComponent<AudioSource>();
				}
				aSource.Stop();
				aSource.clip = backGroundSound.GetAudioClip;
				aSource.volume = backGroundSound.GetVolume;
				aSource.loop = backGroundSound.GetLoop;
				if(ShowSlide)
					aSource.Play();
			}



			//ButtonNavigation.drawNextPrevBtns = true;
			OnValidate();
		}

		/** */
		public bool UseSoundForEachImage {
			get { return useSoundForEachImage; }
			set {
				useSoundForEachImage = value;
				imageDataContainer.UseImagesSound(useSoundForEachImage);
				if(useSoundForEachImage)
					PlayBySlideSound = new PlaySlideShowSound(ImageSoundSliding);
				else
					PlayBySlideSound = () => {};
			}
		}

		/** */
		public float PosOffsetX  {
			get { return posX; }
			set { posX = value; }
		}

		/** */
		public float PosOffsetY  {
			get { return posY; }
			set { posY = value; }
		}

		/** */
		public ObjectSwipeSettings ObjectSwipe {
			get { return objectSwipe; }
		}

		/** */
		public SlideDirection CurrentSlideDirection {
			get { return slideDirection; }
			set {
				slideDirection  = value;
				UseScreenWidth  = useScreenWidth;
				UseScreenHeight = useScreenHeight;
			}
		}

		/** */
		public bool UseKeyBoardControl {
			get { return useKeyBoardControl; }
			set {
				useKeyBoardControl = value;
				if(useKeyBoardControl) {
					InputState = new ButtonInputState(this, State);
				} else { 
					InputState = new IdleInputState(this, State);
				}
			}
		}

		/** */
		public ImageDataContainer ImageData {
			get { return imageDataContainer; }
		}

		/** */
		public SlideType CurrentSlideType {
			get { return slideType; }
			set {
				slideType = value;
				switch(slideType)
				{
					case SlideType.ST_Image_DEPRICATED:
						DrawSlide 	   = new DrawSlideType(DrawSlideImage);
						DrawForeGround = new DrawSlideSystem(DrawGUITexture);
					//  deactivate other states
						DeactivateUI();
						if(State != null)
							State.Exit();
						break;
					case SlideType.ST_Button_DEPRICATED:
						DrawSlide 	   = new DrawSlideType(DrawSlideButton);
						DrawForeGround = new DrawSlideSystem(DrawGUITexture);
					//  deactivate other states
						DeactivateUI();
						if(State != null)
							State.Exit();
						break;
					case SlideType.ST_UIRawImage: // new ui canvas
						if(Application.isPlaying) 
							Initialize_UIRawImage();
						DrawSlide 	   = new DrawSlideType(DrawSlideRawImage);
					//  deactivate other states	
						DrawForeGround = () => {};
						if(State != null)
							State.Exit();
						break;
//					case SlideType.ST_GameObject: 
//						State = new ObjectSwipe(this, null);
//					//  deactivate other states
//						DeactivateUI();
//						DrawSlide = new DrawSlideType(DrawSlideImageOff);
//						DrawForeGround = () => {};
//						break;
				}
			}
		}

		/** Reset general parameters to default*/
		public void ResetSlideShow() {
			bool lastShowSlide = ShowSlide;
			
			ShowSlide 		= false;
			Start ();
			deltaYaw 		= 0;
			parkingDeltaYaw = 0.0f;
			clickDeltaYaw   = 0.0f;
			deltaShift		= 0.0f;

			if(lastShowSlide)
				ShowSlide = true;
		}
		
		/** reset and update all major parametres */
		public void UpdateSlideShow()
		{
			int lastcurrentImageCounter = currentImageCounter;
			
			Start ();
			
			deltaYaw 		= partCircleAngle * lastcurrentImageCounter;
			parkingDeltaYaw = deltaYaw;
			clickDeltaYaw   = deltaYaw;
		}

		/** You can load image from full url*/
		public void LoadWithWebClient()
		{
			for(int idx=0; idx < imageDownloadSettings.urls.Count; ++idx)
			{
				WebClient client  	 = new WebClient();
				int 	  index	  	 = imageDownloadSettings.urls[0].LastIndexOf("/");
				string 	  prefabPath = @Application.dataPath + "/" + @imageDownloadSettings.folderForWebImages + "/" + imageDownloadSettings.urls[0].Substring(index+1);
				string 	  imgurl 	 = imageDownloadSettings.urls[0];
				//Debug.Log (imgurl);
				//Debug.Log (prefabPath);
				try {
					client.DownloadFile(imgurl, prefabPath);
				} catch {
					Debug.LogWarning("Fail to load images");
				}
			}
		}

		/** Add, insert images in current slideshow*/
		public void AddImage(Texture[] textures, int indexAt)
		{
			imageDataContainer.AddImages(textures, indexAt);
			UpdateSlideShow();
		}

		/**  Add, insert images and sounds in current slideshow*/
		public void AddImages(Texture[] textures, SlideShowSound[] slideShowSounds, int indexAt)
		{
			imageDataContainer.AddImages(textures,slideShowSounds, indexAt);
			UpdateSlideShow();
		}

		/** Add or replace images in current slideshow*/
		public void AddImage(Texture[] textures, bool bReplace)
		{
			imageDataContainer.AddImages(textures, bReplace);
			UpdateSlideShow();
		}

		/** Add or replace images and sounds in current slideshow*/
		public void AddImages(Texture[] textures, SlideShowSound[] slideShowSounds, bool bReplace)
		{
			imageDataContainer.AddImages(textures,slideShowSounds, bReplace);
			UpdateSlideShow();
		}


		/** Remove element from texture array*/
		public void RemoveImage(int index)
		{
			imageDataContainer.RemoveImage(index);

			int lastcurrentImageCounter = currentImageCounter;
			
			Start ();
			
			deltaYaw = partCircleAngle * (lastcurrentImageCounter - 1);
			parkingDeltaYaw = deltaYaw;
			clickDeltaYaw   = deltaYaw;
		}

		/** */
		protected void CreateCanvas()
		{
			if(canvas == null)
			{
				// clean old or lost canvas root object
				Canvas[] oldCanvas = gameObject.GetComponentsInChildren<Canvas>();
				for(int idx=0; idx < oldCanvas.Length; ++idx)
				{
					if(oldCanvas[idx].gameObject.name == "canvas")
						DestroyImmediate(oldCanvas[idx]);
				}
				canvas = new GameObject ("canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
				slideshowCanvas = canvas.GetComponent<Canvas>();

				slideshowCanvas.sortingOrder = sortingOrder;
				slideshowCanvas.renderMode = RenderMode.ScreenSpaceCamera;
				//slideshowCanvas.renderMode = RenderMode.WorldSpace;
				slideshowCanvas.worldCamera = Camera.main;
				//slideshowCanvas.planeDistance = 100;
				canvas.transform.SetParent(gameObject.transform);
			}
		}

		/** rawimage array creating*/
		protected void Initialize_UIRawImage ()
		{
			CreateCanvas();

			if(rawImages == null || 
			   rawImages.Length != imagesCountOnScreen || 
			   (rawImages.Length > 0 && rawImages[0] == null)
			   )
			{
				if(rawImages != null)
				{
					for(int idx=0; idx < rawImages.Length; ++idx)
						if(rawImages[idx])
							Destroy(rawImages[idx].gameObject);
				}
				//Debug.Log ("imagesCountOnScreen: " + imagesCountOnScreen + "||" + rawImages.Length);
				rawImages = new RawImage[imagesCountOnScreen];
				EventTrigger taptrigger;
				EventTrigger.Entry entry = new EventTrigger.Entry();
				//EventSystem es = GameObject.Find("EventSystem").GetComponent<EventSystem>();
				for(int idx=0; idx < imagesCountOnScreen; ++idx)
				{
					GameObject rawImageObject = new GameObject ("rawimage" + idx, typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
					rawImageObject.transform.SetParent(canvas.transform);
					rawImageObject.transform.localScale    = new Vector3(1, 1, 1);
					rawImageObject.transform.rotation      = Quaternion.Euler(0,0,0);
					rawImageObject.transform.localPosition = Vector3.zero;
					rawImages[idx] = rawImageObject.GetComponent<RawImage>();
					// Set up button events
					Button btn = rawImageObject.GetComponent<Button>();
					btn.transition = Selectable.Transition.None;
					if(!touchZoneControl.use) {
						// adding event to point click
						taptrigger = rawImageObject.gameObject.AddComponent<EventTrigger>();
						taptrigger.triggers = new List<EventTrigger.Entry>();
						// for sliding event
						entry = new EventTrigger.Entry();
						entry.eventID = EventTriggerType.PointerDown;
						entry.callback.AddListener((data) => { OnPointerDownDelegate((PointerEventData)data); });
						taptrigger.triggers.Add(entry);
						// for event by image button with needed args from EventSystem
						entry = new EventTrigger.Entry();
						entry.eventID = EventTriggerType.PointerClick;
						entry.callback.AddListener((data) => { OnPointerUpDelegate((PointerEventData)data); });
						taptrigger.triggers.Add(entry);
					} else {
						btn.enabled = false;
					}

					if(!ShowSlide)
						rawImages[idx].enabled = false;
				}
			}

			// create foreground
			if(foreGround && foregroundImage == null)
			{
				GameObject imageObject = new GameObject ("foreground", typeof(CanvasRenderer), typeof(RawImage));
				imageObject.transform.SetParent(canvas.transform);
				foregroundImage = imageObject.GetComponent<RawImage>();
				foregroundImage.texture = foreGround;
				// set  position and resolution
				#if UNITY_5_4_OR_NEWER
				foregroundImage.rectTransform.localPosition = new Vector3(0, 0, 0);
				#else
				foregroundImage.rectTransform.localPosition = new Vector3(0,
				                                                          0,
				                                                          foregroundImage.rectTransform.localPosition.z);
				#endif
				RectTransformExtensions.SetSize(foregroundImage.rectTransform, new Vector2(Screen.width, Screen.height));
				if(!ShowSlide)
					foregroundImage.enabled = false;
			}

			//
			if(touchZoneControl.use) {
				//
				if(tapButton != null)
				{
					Destroy(tapButton.gameObject); //.gameObject.SetActive(true);
				}

				if(tapButtonObject)
					Destroy(tapButtonObject);
				tapButtonObject = new GameObject ("tapEventButton", typeof(CanvasRenderer), typeof(Image), typeof(Button));
				tapButtonObject.transform.localScale = new Vector3(1, 1, 1);
				tapButtonObject.transform.SetParent(canvas.transform, false);
				tapButtonObject.GetComponent<Image>().color = new Color(0,0,0,0);
				/// single noname tap event
				tapButtonRect = tapButtonObject.GetComponent<Image>().rectTransform;
				RectTransformExtensions.SetSize(tapButtonRect, new Vector2(currentimagesWidth, currentimagesHeight));
				tapButtonRect.localPosition = new Vector3(posX/2, posY/2, 0);
				tapButton = tapButtonObject.GetComponent<Button>();
				tapButton.onClick.AddListener(() => TapImageEvent()); // public touch zone event

				// special zone touch event trigger for sliding
				EventTrigger taptrigger = tapButton.gameObject.AddComponent<EventTrigger>();
				EventTrigger.Entry entry = new EventTrigger.Entry();
				entry.eventID = EventTriggerType.PointerDown;
				entry.callback.AddListener((data) => { OnPointerDownDelegate((PointerEventData)data); });
				taptrigger.triggers = new List<EventTrigger.Entry>();
				taptrigger.triggers.Add(entry);
			}

			if(!ShowSlide)
				DeactivateUI();
			else
				ActivateUI();
		}

		/** new ui canvas */
		protected void DeactivateUI()
		{
			if(rawImages != null ) {
				for(int idx=0; idx < rawImages.Length; ++idx) {
					if(rawImages[idx] != null)
						rawImages[idx].enabled = false;
				}
			}

			if(tapButton != null) {
				tapButton.gameObject.SetActive(false);
			}

			if(foregroundImage)
				foregroundImage.enabled = false;
		}

		/**new ui canvas  */
		protected void ActivateUI()
		{
			if(rawImages != null ) {
				for(int idx=0; idx < rawImages.Length; ++idx) {
					if(rawImages[idx] != null)
						rawImages[idx].enabled = true;
				}
			}

			if(tapButton != null) {
				tapButton.gameObject.SetActive(true);
			}

			if(foregroundImage)
				foregroundImage.enabled = true;
		}
		
		/** */
		public bool ShowSlide
		{
			get { return showSlide; }
			set {
				showSlide = value;
				oldImageCounter = -1;

				// new ui canvas
				if(showSlide)
				{
					EventImageCounter = new DrawSlideSystem(ImageCounter);
					if(CurrentSlideType == SlideType.ST_UIRawImage)
					{
						ActivateUI();
					}

					PlayBackGroundSound();
				}
				else
				{
					EventImageCounter = () => {};
					StopBackGroundSound();
					StopSlideSound();
					//new ui canvas 
					DeactivateUI();
				}
			}
		}

		/** */
		public bool DrawButtonNavigation
		{
			get { return ButtonNavigation.drawNextPrevBtns; }
			set {
				ButtonNavigation.drawNextPrevBtns = value;
				if(ButtonNavigation.drawNextPrevBtns) {
					DrawNavigationButton = new DrawNavigation(DrawNavigationOn);
				} else {
					DrawNavigationButton = new DrawNavigation(DrawNavigationOff);
				}
			}
		}

		/** */
		public bool AutoSlideOnIdle
		{
			get { return autoSlideOnIdle; }
			set {
				autoSlideOnIdle = value;
				if(autoSlideOnIdle)
					AutoSlide = new AutoSlideEffect(AutomaticSlideEffect);
				else
					AutoSlide = () => {};
			}
		}
		
		/** */
		public bool UseScreenWidth
		{
			get { return useScreenWidth; }
			set {
				useScreenWidth = value;
				if(useScreenWidth)
					currentimagesWidth = Screen.width;
				else
					currentimagesWidth = imagesWidth * ((aspectRatio == true) ? (float)Screen.width/(float)Screen.height : 1) * ((float)Screen.width/fullscreenRes.x);

				// we are rescale touch zone with all images
				UpdateTouchZoneRect();

				// new ui canvas
				if(showSlide)
				{
					if(CurrentSlideType == SlideType.ST_UIRawImage)
					{
						if(currentimagesWidth != Screen.width)
						{
							if(rawImages != null )
							{
								for(int idx=0; idx < rawImages.Length; ++idx)
								{
									if(rawImages[idx] != null)
										rawImages[idx].rectTransform.pivot = new Vector2(1.0f,0.5f);
								}
							}
						}
						else
						{
							if(rawImages != null )
							{
								for(int idx=0; idx < rawImages.Length; ++idx)
								{
									if(rawImages[idx] != null)
										rawImages[idx].rectTransform.pivot = new Vector2(0.5f,0.5f);
								}
							}
						}

						if(foregroundImage)
							foregroundImage.rectTransform.pivot = new Vector2(0.5f,0.5f);
					}
				}
			}
		}

		/** */
		public bool UseScreenHeight
		{
			get { return useScreenHeight; }
			set {
				useScreenHeight = value;
				if(useScreenHeight)
					currentimagesHeight = Screen.height;
				else
					currentimagesHeight = imagesHeight * ((aspectRatio==true) ? (float)Screen.width/(float)Screen.height : 1) * ((float)Screen.height/fullscreenRes.y);

				if(aspectRatio == true)
					//we are rescale touch zone with all images
					UpdateTouchZoneRect();
			}
		}

		/** */
		public string SlideShowName
		{
			get { return slideShowName; }
		}
		
		/** 
		 * Loading images array from selected folder. See ImageLoader.cs script
		 */
		public void LoadImageContent(Texture[] newImages)
		{
			imageDataContainer.AddImages(newImages, true);
		}

		/** update settings on changing in inspector window*/
		void OnValidate()
		{
			CurrentSlideDirection 	= slideDirection;
			AutoSlideOnIdle  		= autoSlideOnIdle;
			CurrentSlideType 		= slideType;
			DrawButtonNavigation 	= ButtonNavigation.drawNextPrevBtns;
			//
			//autoSlideOnIdleLastValue= autoSlideOnIdle;
			//
			UseSoundForEachImage	= useSoundForEachImage;
			//we are rescale touch zone with all images
			if(touchZoneControl.use)
				UpdateTouchZoneRect();

			UseKeyBoardControl      = useKeyBoardControl;

			if(slideshowCanvas)
				slideshowCanvas.sortingOrder = sortingOrder;
		}

		/** we are rescale touch zone with all images */
		protected void UpdateTouchZoneRect()
		{
			if(tapButtonRect != null) {
				switch(touchZoneControl.touchZoneType)
				{
				case TouchZoneControl.TouchZoneType.TZT_FullScreen:
					tapButtonRect.localPosition = Vector3.zero;
					RectTransformExtensions.SetSize(tapButtonRect, new Vector2(Screen.width, Screen.height));
					break;
				case TouchZoneControl.TouchZoneType.TZT_AutoControl:
					if(slideDirection == SlideDirection.SD_Horizontal) {
							tapButtonRect.localPosition = new Vector3(0, posY / scalePosY, 0);
						RectTransformExtensions.SetSize(tapButtonRect, new Vector2(currentimagesWidth * (imagesCountOnScreen-1), currentimagesHeight));
					} else {
						tapButtonRect.localPosition = new Vector3(posX / scalePosX - currentimagesWidth/2, 0, 0);
						RectTransformExtensions.SetSize(tapButtonRect, new Vector2(currentimagesWidth, currentimagesHeight * (imagesCountOnScreen-1)));
					}
					break;
				case TouchZoneControl.TouchZoneType.TZT_Manual:
					tapButtonRect.localPosition = new Vector3(touchZoneControl.touchZone.x, touchZoneControl.touchZone.y, 0);
					RectTransformExtensions.SetSize(tapButtonRect, new Vector2(touchZoneControl.touchZone.width, touchZoneControl.touchZone.height));
					break;
				default:
					if(!useScreenWidth)
						tapButtonRect.localPosition = new Vector3(posX/2 - currentimagesWidth/2, posY/2, 0);
					else
						tapButtonRect.localPosition = new Vector3(posX/2, posY/2, 0);
					RectTransformExtensions.SetSize(tapButtonRect, new Vector2(currentimagesWidth, currentimagesHeight));
					break;
				}
			}
		}

		/** check screen resolution*/
		private void ResetUIPosition()
		{
			UseScreenWidth  = useScreenWidth;
			UseScreenHeight = useScreenHeight;

			int jdx 	 = 1;
			int newCount = 2;
			if(CurrentSlideDirection == SlideDirection.SD_Horizontal) {
				while(currentimagesWidth * jdx < Screen.width) {
					jdx++;
					newCount++;
				}
				shiftCorrectionFactor = shiftCorrectionConstant / (imagesCount * currentimagesWidth);
			} else {
				while(currentimagesHeight * jdx < Screen.height) {
					jdx++;
					newCount++;
				}
				shiftCorrectionFactor = shiftCorrectionConstant / (imagesCount * currentimagesHeight);
			}
			//Debug.Log ("ResetUIPosition");
			//
			if(imagesCountOnScreen != newCount) {
				imagesCountOnScreen = newCount;
				CurrentSlideType 	= slideType;
			}
	
			//yield return 0;
		}

		/** */
		protected void ImageSoundSliding()
		{
			if(ShowSlide)
			{
				if(currentImageCounter != oldImageCounter)
				{
					oldImageCounter = currentImageCounter;
					SpawnImageSound(currentImageCounter);
				}
			}
		}

		
		/** */
		protected void ImageCounter()
		{
			if(currentImageCounter != oldImageCounter)
			{
				SlideImageTransitionEvent(oldImageCounter);

				// current image sound control
				PlayBySlideSound();
			}
		}

		/** */
		public virtual void TapImageEvent()
		{
			if(!useTapImageEvent)
				return;

			//Debug.Log ("Image index: " + GetCurrentImageCounter().ToString());
		}
		
		/** Event on image trasition */
		public virtual void SlideImageTransitionEvent(int lastImageCounter)
		{
			//Debug.Log ("Last image index: " + lastImageCounter + "| New image index: " + GetCurrentImageCounter().ToString());
		}

		/** */
		public void SpawnImageSound(int index)
		{
			// check and stop last audio objects
			StopSlideSound();

			// create new object with audio component
			SlideShowSound sss = imageDataContainer.GetImageSound(index);
			if(sss != null) {
				if(sss.UseSound) {
					GameObject go = new GameObject("SoundSlide_" + index.ToString(), typeof(AudioSource));
					go.transform.SetParent(gameObject.transform);
					AudioSource _as = go.GetComponent<AudioSource>();
					_as.clip = sss.GetAudioClip;
					_as.loop = sss.GetLoop;
					_as.volume = sss.GetVolume;
					_as.Play();
				}
			}
		}

		/** */
		public void StopSlideSound()
		{
			// check last audio objects
			AudioSource[] gos = gameObject.GetComponentsInChildren<AudioSource>();
			for(int idx=0; idx < gos.Length; ++idx) {
				if(gos[idx].gameObject != gameObject)
					Destroy(gos[idx].gameObject);
			}
		}

		/** */
		public void PlayBackGroundSound()
		{
			AudioSource _as = gameObject.GetComponent<AudioSource>();
			if(_as) {
				_as.Play();
			}
		}

		/** */
		public void StopBackGroundSound()
		{
			AudioSource _as = gameObject.GetComponent<AudioSource>();
			if(_as) {
				_as.Stop();
			}
		}
		
		/** return number of image at screen center */
		public int ActiveSliding () {

			if(ShowSlide)
				return ((deltaShift == 0.0f) ? 0 : 1);
			else
				return -1;
		}

		/** Fast return effect to the first image*/
		public void ForceReturnToStart()
		{
			deltaYaw = 0;
		}

		/** Return effect throw all images to the first image*/
		public void ForceSmoothReturnToStart()
		{
			isSmoothReturnMode = true;
		}

		/** Stop sliding effect for touches. ButtonSlide will be work */
		public void StopSliding(bool isActivate)
		{
			isFreezeSliding = isActivate;
			if(isFreezeSliding) {
				bTouchDown = false;
				float oldDelataShift = deltaShift; // we need to save value, for smooth drooping speed, because in CalculateImageParking the deltaShift set to 0
				CalculateImageParking();
				deltaShift   = oldDelataShift;
		
				mouseClick = false;
				fingerPath = 0.0f;
				// moving images to edge of screen
				if(currUseImageParking) {
					CalculateImageParking();
				}
			}
		}

		// Update is called once per frame
		void Update () {
			// slider action
			if(isSmoothReturnMode)
			{
				deltaYaw = Mathf.Lerp(deltaYaw, 0.0f, 0.1f);

				if(deltaYaw < 0.1f)
				{
					isSmoothReturnMode = false;
					mouseClick = false;
					fingerPath = 0.0f;
					// moving images to edge of screen
					if(currUseImageParking) {
						CalculateImageParking();
					}
				}
			}
			else
			{
				if(!isFreezeSliding) { 
					// Touch and Mouse control
					ToucInputControl();
					// KeyBoard and gamepad control
					UpdateInput();
				}

				CheckScreenResolution();
				
				AutoSlide();
			}

			ImageCounter();
			// for object swipe
			UpdateState();
		}

		/** */
		private void CheckScreenResolution()
		{
			//  check screen resolution
			if( lastScreenWidth != Screen.width || lastScreenHeight != Screen.height) {
				lastScreenWidth  = Screen.width;
				lastScreenHeight = Screen.height;
				//StartCoroutine("ResetUIPosition");
				ResetUIPosition();
			}

			UseScreenWidth  = useScreenWidth;
			UseScreenHeight = useScreenHeight;
		}


		/** Mouse|Finger touch and move event (from taptrigger)
		 *  working on slideType == SlideType.ST_RawImage
		  */
		protected void OnPointerDownDelegate(PointerEventData ped)
		{
			if(!isSmoothReturnMode) {
				lastPos       = Input.mousePosition;
				mouseClick    = true;
				clickDeltaYaw = deltaYaw;
			}
			//Debug.Log("OnPointerDownDelegate: " + ped.pointerDrag + "||" + ped.pointerId + "||" + ped.pointerPressRaycast.gameObject.name + "||" + ped.position);
		}

		/**custom button event  handler for uirawimage mode*/
		protected void OnPointerUpDelegate(PointerEventData ped) {
			int id = ped.pointerPressRaycast.gameObject.GetComponent<RawImage>().texture.GetInstanceID();
			for(int idx=0; idx < imageDataContainer.GetImagesAmount; ++idx) {
				if(id == imageDataContainer.GetImage(idx).GetInstanceID()) {
					SlideButtonAction(idx);
					break;
				}
			}
		}

		/** Mouse and touch input controlling*/
		protected void ToucInputControl()
		{
			// see OnPointerDownDelegate rewrite after improove gameobject sliding
			if((touchZoneControl.touchZoneType == TouchZoneControl.TouchZoneType.TZT_FullScreen ||
			    slideType == SlideType.ST_Image_DEPRICATED ||
			    slideType == SlideType.ST_Button_DEPRICATED) &&
				Input.GetMouseButtonDown(0))
			{
				lastPos       = Input.mousePosition;
				mouseClick    = true;
				clickDeltaYaw = deltaYaw;
			}

			if(Input.GetMouseButtonUp(0))
			{
				mouseClick = false;
				fingerPath = 0.0f;
				// moving images to edge of screen
				if(currUseImageParking) {
					CalculateImageParking();
				}
			}
			
			if(mouseClick)
			{
				if(CurrentSlideDirection == SlideDirection.SD_Horizontal)
					deltaShift = (Input.mousePosition - lastPos).x * shiftCorrectionFactor;
				else
					deltaShift = -(Input.mousePosition - lastPos).y * shiftCorrectionFactor;

				fingerPath += Vector3.Distance(lastPos, Input.mousePosition);
			}
			else
			{
				if(currUseImageParking)  { // moving images to edge of screen
					deltaYaw = Mathf.Lerp(deltaYaw, parkingDeltaYaw, speedDroppingSmoothnes);
				} else {
					deltaShift = Mathf.Lerp(deltaShift, 0, speedDroppingSmoothnes);
				}
			}

			if(infiniteSwipe)
				deltaYaw -= deltaShift * swipeSpeed;
			else
				deltaYaw = Mathf.Clamp(deltaYaw - deltaShift * swipeSpeed, 0.0f, 360.0f - partCircleAngle);

			Quaternion 	tmpRot = Quaternion.Euler(0, deltaYaw, 0);
			deltaYaw 		   = tmpRot.eulerAngles.y;
			lastPos  		   = Input.mousePosition;
		}

		/** */
		protected void CalculateImageParking()
		{
			deltaShift = 0.0f;
			float partAngle      = (deltaYaw%partCircleAngle);
			float deltaPartAngle = ((clickDeltaYaw - deltaYaw)%(partCircleAngle));
			if(partAngle > partCircleAngle/parkingSensitivity && !(deltaPartAngle > partCircleAngle/parkingSensitivity) )
			{
				parkingDeltaYaw     = (deltaYaw + partCircleAngle - partAngle);
				Quaternion 	tmpRot1 = Quaternion.Euler(0, parkingDeltaYaw, 0);
				parkingDeltaYaw     = tmpRot1.eulerAngles.y;

				if(parkingDeltaYaw < 0.01f)
				{
					if((clickDeltaYaw - deltaYaw) > -(360.0f - partCircleAngle))
						parkingDeltaYaw = 359.990f;
					else
						parkingDeltaYaw = (deltaYaw - partAngle);
				}
			}
			else if(deltaYaw >= 0)
			{
				if((clickDeltaYaw - deltaYaw) > (360.0f - partCircleAngle))
					parkingDeltaYaw = partCircleAngle;
				else
					parkingDeltaYaw = (deltaYaw - partAngle);

				Quaternion 	tmpRot2 = Quaternion.Euler(0.0f, parkingDeltaYaw, 0.0f);
				parkingDeltaYaw     = tmpRot2.eulerAngles.y;
			}

			parkingDeltaYaw = Mathf.Clamp(parkingDeltaYaw,0.0f,359.990f);
		}


		/** Tick delegate for AutoSlide */
		protected void AutomaticSlideEffect()
		{
			if(showSlide) // sliding only in active state
			{
				if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || (Input.touchCount > 0 && !bTouchDown /*&& !useAutoSlideOff*/))
				{
					waitOnAutoSlideTimer = 0.0f;
					autoSlideTimer = 0.0f;
					bTouchDown = true;
				}

				if(Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || (Input.touchCount == 0 && bTouchDown))
				{
					bTouchDown = false;
				}

				if(bTouchDown) // no sliding if mouse|finger don't Up
					return;

				if(waitOnAutoSlideTimer > waitOnAutoSlideTime)
				{
					autoSlideTimer += Time.deltaTime;
					if(autoSlideTimer > autoSlideInterval)
					{
						// Make one swipe
						ButtonSlide (Mathf.Clamp(autoSlideDirection, -1, 1));
						autoSlideTimer = 0.0f;
					}
				}
				else
				{
					waitOnAutoSlideTimer += Time.deltaTime;
				}
			}
		}

		
		/** */
		void OnGUI(){
			// Old GUI System
			DrawSlideMode();
			// Old GUI System
			DrawNavigationButton();
		}

		/** */
		public void NextSlide() {
			ButtonSlide(1);
		}

		/** */
		public void PrevSlide() {
			ButtonSlide(-1);
		}

		/** Sliding effect
			direction =  1 - to the right
			direction = -1 - to the left
		 */
		public void ButtonSlide(int direction)
		{
			float divider =  1;
			if(currUseImageParking)
				divider =  4;

			clickDeltaYaw = deltaYaw;

			if(infiniteSwipe) {
				deltaYaw += partCircleAngle/divider * (direction);
			} else {
				float newDeltaYaw = deltaYaw + partCircleAngle/divider * (direction);
				if(!((deltaYaw <= partCircleAngle/2 || deltaYaw > 359) && direction < 0 && GetCurrentImageCounter() < 1) &&
					!(deltaYaw >= (359 - partCircleAngle) && direction > 0 && GetCurrentImageCounter() > 0))
				   deltaYaw = newDeltaYaw;
			}

			if(deltaYaw > 361.0f)
				deltaYaw = partCircleAngle/divider;
			if(deltaYaw < 0)
				deltaYaw = 360.0f - partCircleAngle/divider;

			// moving images to edge of screen
			if(currUseImageParking)
				CalculateImageParking();
		}

		/** */
		protected void DrawNavigationOn()
		{
			if(showSlide) // sliding only in active state
			{
				if(ButtonNavigation.btnSkin == null)
					ButtonNavigation.btnSkin = GUI.skin;
				//
				if(ButtonNavigation.drawNextPrevBtns)
				{
					GUI.BeginGroup(ButtonNavigation.GetRect);

					switch(ButtonNavigation.nextBtn.GetDrawDataType)
					{
					case BaseUIElement.DrawDataType.DDT_TEXT:

						if(GUI.Button(ButtonNavigation.nextBtn.GetRect, ButtonNavigation.nextBtn.DrawText, ButtonNavigation.btnSkin.button )) {
							NextSlide(); // ButtonSlide(1); 
						}
						break;
					case BaseUIElement.DrawDataType.DDT_TEXTURE:
						if(GUI.Button(ButtonNavigation.nextBtn.GetRect, ButtonNavigation.nextBtn.DrawTexture, ButtonNavigation.btnSkin.button )) {
							NextSlide(); //ButtonSlide(1);
						}
						break;
					case BaseUIElement.DrawDataType.DDT_NONE:
						if(GUI.Button(ButtonNavigation.nextBtn.GetRect, " ", ButtonNavigation.btnSkin.button )) {
							NextSlide(); //ButtonSlide(1);
						}
						break;
					}

					switch(ButtonNavigation.prevBtn.GetDrawDataType)
					{
					case BaseUIElement.DrawDataType.DDT_TEXT:
						if(GUI.Button(ButtonNavigation.prevBtn.GetRect, ButtonNavigation.prevBtn.DrawText, ButtonNavigation.btnSkin.button )) {
							PrevSlide(); //ButtonSlide(-1);
						}
						break;
					case BaseUIElement.DrawDataType.DDT_TEXTURE:
						if(GUI.Button(ButtonNavigation.prevBtn.GetRect, ButtonNavigation.prevBtn.DrawTexture, ButtonNavigation.btnSkin.button )) {
							PrevSlide(); //ButtonSlide(-1);
						}
						break;
					case BaseUIElement.DrawDataType.DDT_NONE:
						if(GUI.Button(ButtonNavigation.prevBtn.GetRect, " ", ButtonNavigation.btnSkin.button )) {
							PrevSlide(); //ButtonSlide(-1);
						}
						break;
					}
					GUI.EndGroup();
				}
			}
		}

		/** none functions if slides or button don't need to display*/
		protected void DrawNavigationOff()	{ }
		protected void DrawSlideOff() { }

			
		/** */
		protected void DrawSlideOn()
		{
			// showing or hiding slide show
			if(showSlide)
				drawAlpha = Mathf.Lerp(drawAlpha, 1.0f, 0.1f);
			else
				drawAlpha = Mathf.Lerp(drawAlpha, 0.0f, 0.1f);
			
			if(drawAlpha < minDrawAlpha)
				return;
			
			DrawSlideShow();

			DrawForeGround();
		}
		
		/** */
		public int GetCurrentImageCounter()
		{
			return currentImageCounter;
		}
		
		/** */
		protected void CheckCurrentImageCounter(Rect rect, int index)
		{
			// fast image counter
			if(rect.x > -50.0f && rect.x  < 50.0f) // 50 is pixels threshold
				currentImageCounter = index;
		}

		/** */
		protected void DrawGUITexture()
		{
			// draw texture under slides
			if(foreGround)
				GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), foreGround);
		}

		/** */
		protected void DrawSlideButton(Rect rect, int index)
		{
			GUI.skin 						  = btnSkin;
			GUI.skin.button.normal.background = imageDataContainer.GetImage(index) as Texture2D;
			GUI.skin.button.hover.background  = imageDataContainer.GetImage(index) as Texture2D;
			GUI.skin.button.active.background = imageDataContainer.GetImage(index) as Texture2D;
			if(GUI.Button(rect, "")) {
				if(fingerPath < fingerPathThreshold) {
					SlideButtonAction(index);
				}
			}

			// fast image counter
			CheckCurrentImageCounter( rect,  index);
		}

		protected void DrawSlideImageOff(Rect rect, int index) { }

		/** */
		protected void DrawSlideImage(Rect rect, int index)
		{
			GUI.DrawTexture(rect, imageDataContainer.GetImage(index), ScaleMode.StretchToFill, true, 1.0f);
			// fast image counter
			CheckCurrentImageCounter( rect,  index);
		}

		/** new ui canvas support */
		protected void DrawSlideRawImage(Rect rect, int index)
		{
			lastDrawUIIndex++;
			if(lastDrawUIIndex >= imagesCountOnScreen)
				lastDrawUIIndex = 0;

			rawImages[lastDrawUIIndex].texture = imageDataContainer.GetImage(index);

			#if UNITY_5_4_OR_NEWER
			rawImages[lastDrawUIIndex].rectTransform.localPosition = new Vector3(rect.x,
				rect.y,
				 0);
			#else
			rawImages[lastDrawUIIndex].rectTransform.localPosition = new Vector3(
				rect.x,
				rect.y,
				rawImages[lastDrawUIIndex].rectTransform.localPosition.z);
			#endif

			RectTransformExtensions.SetSize(rawImages[lastDrawUIIndex].rectTransform, new Vector2(rect.width, rect.height));
			// fast image counter
			CheckCurrentImageCounter( rect,  index);
		}

		/** 
		 * Main Button slide action
		 */
		public virtual void SlideButtonAction(int index)
		{
#if UNITY_EDITOR
			Debug.Log ("button action: " + index);
#endif
			// to do stuff

			TapImageEvent();
		}
		
		/** */
		private void DrawSlideShow()
		{
			float partAngle = -1;
			float checkRot  = 0;
			int   idx       = 0;

			scalePosX = (1920.0f) / Screen.width;
			scalePosY = (1080.0f) / Screen.height;

			for(idx = 0; idx < imagesCount; ++idx)
			{
				checkRot = partCircleAngle * idx;
				if(deltaYaw < checkRot)
				{
					partAngle = (checkRot - deltaYaw);
					break;
				}
			}
			
			if(partAngle < 0)
				partAngle = 360 - deltaYaw;

			GUI.depth = guiDepth;
			GUI.color = new Color(1.0f,1.0f,1.0f, drawAlpha);

			if(CurrentSlideDirection == SlideDirection.SD_Horizontal)
				posCenterImage = (partAngle/partCircleAngle) * currentimagesWidth - currentimagesWidth;
			else
				posCenterImage = (partAngle/partCircleAngle) * currentimagesHeight - currentimagesHeight;
			int k   = 0;
			int len = idx + (imagesCountOnScreen - 1);
			for(int jdx = idx-1; jdx < len; ++jdx) {
				int num = jdx;
				if(num >= imagesCount) {
					while(num >= imagesCount)
						num = num - imagesCount;
				}

				try {
					if(CurrentSlideDirection == SlideDirection.SD_Horizontal) {
						finalPosX = posX / scalePosX + posCenterImage + k * currentimagesWidth;
						finalPosY = posY / scalePosY;
					} else {
						finalPosX = posX / scalePosX;
						finalPosY = posY / scalePosY + posCenterImage + k * currentimagesHeight;
					}

					totalPos = new Rect(finalPosX, finalPosY, currentimagesWidth, currentimagesHeight);

					DrawSlide(totalPos, num);
				} catch {

				}
				++k;
			}
		}
	}


	/**************************************************************************************************************
	 * OLD GUI ELEMENTS
	 * ***********************************************************************************************************/

	[System.Serializable]
	public class ButtonNavigation
	{
		[Tooltip("draw navigation buttons or not.")]
		public		bool 		drawNextPrevBtns = false;
		//public	bool 	drawBackBtn = false;
		[Tooltip("special gui skin for buttons")]
		public 		GUISkin 	btnSkin;	
		[Tooltip("general buttons position on screen.")]
		public 		Vector2 	groupPos;
		[Tooltip("Button settings")]
		public 		BaseUIElement nextBtn;
		[Tooltip("Button settings")]
		public		BaseUIElement prevBtn;

		public ButtonNavigation() {	 }

		/** */
		public Rect GetRect	{
			get { 
				float resX = Screen.width/1920.0f;
				float resY = Screen.height/1080.0f;

				return	new Rect(groupPos.x * resX, groupPos.y * resY, Screen.width, Screen.height);
			}
			
			private set  {	}
		}
	}

	[System.Serializable]
	public class BaseUIElement
	{
		/***/
		public enum DrawDataType {
			DDT_NONE,
			DDT_TEXT,
			DDT_TEXTURE
		};
		
		/* main position for all UI elments in this group
		 * */
		[SerializeField]
		protected  	Rect 		UIElementRect;
		[SerializeField]
		protected  	bool 		useScreenSize;
		[SerializeField]
		protected  	Texture 	drawTexture;
		[SerializeField]
		protected  	string 		drawText;
		
		protected Rect 			setPosRect;
		
		[SerializeField]
		protected  	DrawDataType  drawDataType;
		[SerializeField]// for special event actions by this button.
		protected  	string 		eventString;

		// use fore reset to default values
		public BaseUIElement() {
			UIElementRect = new Rect(0, 0, Screen.width, Screen.height);
		}

		/** */
		public Rect GetRect {
			get { 
				float resX = Screen.width/1920.0f;
				float resY = Screen.height/1080.0f;
				
				if(useScreenSize)
					return	new Rect((UIElementRect.x + setPosRect.x)* resX, (UIElementRect.y + setPosRect.y) * resY, Screen.width, Screen.height);
				else
					return	new Rect((UIElementRect.x + setPosRect.x) * resX, (UIElementRect.y + setPosRect.y) * resY, UIElementRect.width * resX, UIElementRect.height * resY);
			}
			
			private set  {	}
		}
		
		/** */
		public Rect SetPosRect {
			set  { setPosRect = value; }
		}
		
		/** */
		public DrawDataType GetDrawDataType {
			get { return drawDataType; }
		}
		
		public string GetEventString {
			get { return eventString; }
		}
		
		/** */
		public Texture DrawTexture {
			get { return drawTexture; }
			set { drawTexture = value; }
		}
		
		public string DrawText {
			get { return drawText; }
		}
	}


	/**************************************************************************************************************
	 * Sound
	 * ***********************************************************************************************************/


	[System.Serializable]
	public class SlideShowSound
	{
		[SerializeField]
		protected  	bool 		useSound = false;
		[SerializeField]
		protected  	AudioClip	sound;
		[SerializeField][Range(0.0f,1.0f)]
		protected  	float 		soundVolume = 1.0f;
		[SerializeField]
		protected  	bool 		loop = true;

		// use fore reset to default values
		public SlideShowSound() {	}
		
		public AudioClip GetAudioClip {
			get { return sound; }
		}

		public bool GetLoop {
			get { return loop; }
		}
		
		public float GetVolume {
			get { return soundVolume; }
		}

		public bool UseSound {
			get { return useSound; }
			set { useSound = value; }
		}
	}

	/** for ui canvas  ver 1.3*/
	[System.Serializable]
	public static class RectTransformExtensions
	{
		public static void SetSize(this RectTransform trans, Vector2 newSize) {
			Vector2 oldSize = trans.rect.size;
			Vector2 deltaSize = newSize - oldSize;
			trans.offsetMin = trans.offsetMin - new Vector2(deltaSize.x * trans.pivot.x, deltaSize.y * trans.pivot.y);
			trans.offsetMax = trans.offsetMax + new Vector2(deltaSize.x * (1f - trans.pivot.x), deltaSize.y * (1f - trans.pivot.y));

			#if UNITY_5_0_OR_NEWER
			trans.localScale = new Vector3(1,1,1);
			trans.localRotation = Quaternion.Euler(0,0,0);
			#endif
		}
	}


	/**************************************************************************************************************
	 *  Image container with som functionality
	 * ***********************************************************************************************************/


	/** new data for ver 1.3*/
	[System.Serializable]
	public class ImageDataContainer
	{
		//[SerializeField, Tooltip("objects array for slide")]
		//protected  	GameObject[]		objects;
		[SerializeField, Tooltip("images array for slide")]
		protected  	Texture[] 			Images = new Texture[0];
		[SerializeField, Tooltip("add audio clip for each image")]
		protected  	SlideShowSound[] 	imagesSound = new SlideShowSound[0];

		public Texture[] GetImages {
			get { return Images; }
		}

		public int GetImagesAmount {
			get { return Images.Length; }
		}


		public Texture GetImage(int idx)
		{
			if(idx < Images.Length)
				return Images[idx];
			else
				return null;
		}
		/** Add or replace images in current slideshow*/
		public void AddImages(Texture[] newImages, bool bReplace = true)
		{
			if(bReplace) {
				Images = new Texture[newImages.Length];
				for(int idx=0; idx < newImages.Length; ++idx)
					Images[idx] = newImages[idx];
			} else {
				List<Texture> tempImgArray = new List<Texture>();
				for(int idx=0; idx < Images.Length; ++idx)
					tempImgArray.Add(Images[idx]);
				for(int idx = 0; idx < newImages.Length; ++idx)
					tempImgArray.Add(newImages[idx]);
				Images = new Texture[tempImgArray.Count];
				for(int idx = 0; idx < Images.Length; ++idx)
					Images[idx] = tempImgArray[idx];
			}

			// 
			imagesSound = new SlideShowSound[Images.Length];
			for(int idx = 0; idx < imagesSound.Length; ++idx)
				imagesSound[idx] = new SlideShowSound();
		}

		/** Add or replace images and sounds in current slideshow*/
		public void AddImages(Texture[] newImages, SlideShowSound[] newSlideShowSound, bool bReplace = true)
		{
			// add images
			AddImages(newImages,bReplace);
			// add sounds
			if(bReplace) {
				imagesSound = new SlideShowSound[newSlideShowSound.Length];
				for(int idx=0; idx < newImages.Length; ++idx)
					imagesSound[idx] = newSlideShowSound[idx];
			} else {
				// add sounds
				List<SlideShowSound> tempSndArray = new List<SlideShowSound>();
				// add old sound
				for(int idx=0; idx < imagesSound.Length; ++idx)
					tempSndArray.Add(imagesSound[idx]);
				// add new sound
				for(int idx = 0; idx < newSlideShowSound.Length; ++idx)
					tempSndArray.Add(newSlideShowSound[idx]);
				// set to main array
				imagesSound = new SlideShowSound[tempSndArray.Count];
				for(int idx = 0; idx < imagesSound.Length; ++idx)
					imagesSound[idx] = tempSndArray[idx];
			}
		}
			
		/** Add, insert images in current slideshow*/
		public void AddImages(Texture[] newImages, int indexAt)
		{
			indexAt = Mathf.Clamp(indexAt, 0, Images.Length);

			List<Texture> tempImgArray = new List<Texture>();

			for(int idx=0; idx < indexAt; ++idx)
				tempImgArray.Add(Images[idx]);
			for(int idx = 0; idx < newImages.Length; ++idx)
				tempImgArray.Add(newImages[idx]);
			if(Images.Length - indexAt > 0)
			{
				for(int idx = 0; (idx + indexAt) < Images.Length; ++idx)
					tempImgArray.Add(Images[indexAt + idx]);
			}

			Images = new Texture[tempImgArray.Count];
			for(int idx = 0; idx < Images.Length; ++idx)
				Images[idx] = tempImgArray[idx];

			// 
			imagesSound = new SlideShowSound[Images.Length];
			for(int idx = 0; idx < imagesSound.Length; ++idx)
				imagesSound[idx] = new SlideShowSound();
		}

		/** Add, insert images and sounds in current slideshow*/
		public void AddImages(Texture[] newImages, SlideShowSound[] newSlideShowSound, int indexAt)
		{
			// add images
			AddImages(newImages,indexAt);

			// now add sounds
			indexAt = Mathf.Clamp(indexAt, 0, newSlideShowSound.Length);

			List<SlideShowSound> tempSndArray = new List<SlideShowSound>();

			for(int idx=0; idx < indexAt; ++idx)
				tempSndArray.Add(imagesSound[idx]);
			for(int idx = 0; idx < newSlideShowSound.Length; ++idx)
				tempSndArray.Add(newSlideShowSound[idx]);
			if(imagesSound.Length - indexAt > 0)
			{
				for(int idx = 0; (idx + indexAt) < imagesSound.Length; ++idx)
					tempSndArray.Add(imagesSound[indexAt + idx]);
			}

			imagesSound = new SlideShowSound[tempSndArray.Count];
			for(int idx = 0; idx < imagesSound.Length; ++idx)
				imagesSound[idx] = tempSndArray[idx];
		}

		/** Remove element from texture array*/
		public void RemoveImage(int index)
		{
			if(Images.Length < 2)
				return;

			index = Mathf.Clamp(index, 0, Images.Length);
			List<Texture> tempImgArray = new List<Texture>();
			for(int idx=0; idx < Images.Length; ++idx)
				tempImgArray.Add(Images[idx]);

			tempImgArray.RemoveAt(index);

			Images = new Texture[tempImgArray.Count];
			for(int idx = 0; idx < Images.Length; ++idx)
				Images[idx] = tempImgArray[idx];

			// temp
			imagesSound = new SlideShowSound[Images.Length];
			for(int idx = 0; idx < imagesSound.Length; ++idx)
				imagesSound[idx] = new SlideShowSound();
		}

		public void  UseImagesSound(bool isUse)
		{
			for(int idx=0; idx < imagesSound.Length; ++idx)
				imagesSound[idx].UseSound = isUse;
		}

		public SlideShowSound GetImageSound(int idx)
		{
			if(idx < imagesSound.Length)
				return imagesSound[idx];
			else
				return null;
		}

		public SlideShowSound[] GetImagesSound {
			get { return imagesSound; }
		}
	}


	/**************************************************************************************************************
	 * KeyBoard Input
	 * ***********************************************************************************************************/
	// BASE STATE
	public interface IBaseState {
		void Update();
		void Exit();
	}

	// BASE COMMAND
	[System.Serializable]
	public class Command {
		public Command () { }
		public virtual void Execute (SwipeEffect sw) {	}
	}

	/** slide to next image*/
	[System.Serializable]
	public class NextCommand : Command{
		public NextCommand () { }
		public override void Execute (SwipeEffect sw) {
			if(sw) {
				sw.NextSlide();
			}
		}
	}
	/** slide to prev image*/
	[System.Serializable]
	public class PrevCommand : Command{
		public PrevCommand () { }
		public override void Execute (SwipeEffect sw) {
			if(sw) {
				sw.PrevSlide();
			}
		}
	}
	/** slide to up image*/
	[System.Serializable]
	public class UpCommand : Command{
		public UpCommand () { }
		public override void Execute (SwipeEffect sw) {
			if(sw) {
				sw.NextSlide();
			}
		}
	}
	/** slide to down image*/
	[System.Serializable]
	public class DownCommand : Command{
		public DownCommand () { }
		public override void Execute (SwipeEffect sw) {
			if(sw) {
				sw.PrevSlide();
			}
		}
	}


	/** KeyBoard and GamePad controling*/
	[System.Serializable]
	public class ButtonInputState : IBaseState	{

		public SwipeEffect	sw;

		private Command btnWcmd = new UpCommand();
		private Command btnScmd = new DownCommand();
		private Command btnAcmd = new PrevCommand();
		private Command btnDcmd = new NextCommand();

		private static float sensetivityThresholdPos = 0.5f;
		private static float sensetivityThresholdCon = -0.5f;

		private float moveVertical;
		private float moveHorizontal;

		private static float activeAxisTimeThreshold = 1.0f;
		private float activeAxisTimer;

		private bool	axisAction = false;

		public ButtonInputState(SwipeEffect _sw, IBaseState _ibs) {
			if(_ibs != null)
				_ibs.Exit();
			sw = _sw;
			if(sw)
				sw.UpdateInput += Update;
		}

		// WASD + arrows use next sliding or prev sliding
		// GAME PAD (XBOX)
		public void Update() {

			if(!sw.ShowSlide)
				return;
			
			// KEYBOARD INPUT
			if(Input.GetKeyDown(KeyCode.W)) {
				btnWcmd.Execute(sw);
			}
			if(Input.GetKeyDown(KeyCode.S)) {
				btnScmd.Execute(sw);
			}
			if(Input.GetKeyDown(KeyCode.A)) {
				Debug.Log ("d");
				btnAcmd.Execute(sw);
			}
			if(Input.GetKeyDown(KeyCode.D)) {
				btnDcmd.Execute(sw);
			}

			// GAMEPAD INPUT and keyboard arrows
			moveVertical   = Input.GetAxis ("Vertical");
			moveHorizontal = Input.GetAxis ("Horizontal");

			//TODO: write a handler for this
			if(moveHorizontal != 0) {
				if(!axisAction) {
					// slide image if axis value big
					if(moveHorizontal > sensetivityThresholdPos) {
						btnDcmd.Execute(sw);
						axisAction = true;
					} else if(moveHorizontal < sensetivityThresholdCon) {
						btnAcmd.Execute(sw);
						axisAction = true;
					}
				} else {
					activeAxisTimer -= Time.deltaTime;
					if(activeAxisTimer < 0.0f) {
						axisAction = false;
						activeAxisTimer = activeAxisTimeThreshold;
					}
				}
			} else if(moveVertical != 0) {
				if(!axisAction) {
					// slide image if axis value big
					if(moveVertical > sensetivityThresholdPos) {
						btnWcmd.Execute(sw);
						axisAction = true;
					} else if(moveVertical < sensetivityThresholdCon) {
						btnScmd.Execute(sw);
						axisAction = true;
					}
				} else {
					activeAxisTimer -= Time.deltaTime;
					if(activeAxisTimer < 0.0f) {
						axisAction = false;
						activeAxisTimer = activeAxisTimeThreshold;
					}
				}
			} else {
				axisAction = false;
			}
		}
		/** Exit from state*/
		public void Exit() {
			if(sw) {
				sw.UpdateInput -= Update;
			}
		}
	}

	/** Idle input state*/
	[System.Serializable]
	public class IdleInputState : IBaseState {
		public SwipeEffect	sw;

		public IdleInputState(SwipeEffect _sw, IBaseState _ibs) {
			if(_ibs != null)
				_ibs.Exit();
		}
		public void Update() {	}
		public void Exit() {	}
	}

	/**************************************************************************************************************
	 * Ojects Swipe (EXPERIMENTAL)
	 * ***********************************************************************************************************/

	[System.Serializable]
	public class ObjectSwipeSettings {
		[SerializeField]
		public 	bool			swipeBand = false;
		[SerializeField, Tooltip("дополнительное начальное смещение")] // 
		public 	Vector3			initPosOffset = new Vector3(0, 0, 0);
		[SerializeField]
		public 	float 			speed = 1.0f;
		[SerializeField]
		public 	float			scaleMultiplier = 1;
		[SerializeField]
		public 	float 			centerThreshold = 0.0f;
		[SerializeField]
		public 	bool			indicatorEnabled = false;
		[SerializeField]
		public 	bool			startWithOutDevices = false;
		[SerializeField]
		public 	Transform		firstPos;
		[SerializeField]
		public 	Transform		lastPos;
		[SerializeField, Tooltip(" точка главной-центральной парковки")]
		public   Transform       parkingPoint;
		[SerializeField]
		public	Vector3			buttonOffset;
		[SerializeField, Tooltip("использовать механизм парковки объектов в ленте в слоты")]	// 
		public   bool            useParkingSystem = false;
		[SerializeField, Tooltip(" временная задержка перед началом процесса парковки")]	//
		public   float           parkingDelay = 0.25f;
		[SerializeField]
		public   float           parkingMinDist = 1.0f;
		[SerializeField, Tooltip(" ")] /* походу уже не актуально*/
		public   int             deviceIdx = 0;
		[SerializeField, Tooltip("Набор объектов в ленте и их настройки")] //
		public 	List<ObjectSwipeContainer> 		objectSwiper = new List<ObjectSwipeContainer>();
	}


	[System.Serializable]
	public class ObjectSwipeContainer {
		public 		Transform 		objectSwiper;

		[HideInInspector]
		public		bool 			enabled;

		protected	Vector3			buttonOffset;
		protected	Transform 		firstPoint;
		protected	Transform 		lastPoint;
		protected	Vector3			offsetPosition;
		protected	Vector3			initialScale;
		protected	float 			scaleMultiplier;

		/** */
		public Vector3 ButtonOffset {
			set{ buttonOffset = value; }
		}
		/** */
		public Vector3 InitialScale {
			set{ initialScale = value; }
			get{ return initialScale; }
		}
		/** */
		public float ScaleMultiplier {
			set{ scaleMultiplier = value; }
			get{ return scaleMultiplier; }
		}
		/** начало ленты*/
		public Transform FirstPoint {
			set{ firstPoint = value; }
		}
		/** конец ленты*/
		public Transform LastPoint {
			set{ lastPoint = value; }
		}
		/** применяем смещение к позиции объекта */
		public void ObjectTranslate(Vector3 shift) {
			objectSwiper.Translate(shift, Space.World);
		}

		/** 
		 * Проверяем позицию объекта относительно начальной точки и конечной точки.
		 * Если объект вышел за пределы, переносим его в противоположную (закольцовываем)
		 */
		public void CheckDistance(float maxDist) {
			maxDist = Vector3.Distance(firstPoint.position, lastPoint.position);

			offsetPosition = firstPoint.position;
			float totalDist = Vector3.Distance(objectSwiper.position, offsetPosition);
			if(totalDist > maxDist) {
				Vector3 resetPos = firstPoint.position;
				resetPos.z += (totalDist - maxDist);
				objectSwiper.position = resetPos;
			}

			offsetPosition = lastPoint.position;
			totalDist = Vector3.Distance(objectSwiper.position, offsetPosition);
			if(totalDist > maxDist) {
				Vector3 resetPos = lastPoint.position;
				resetPos.z -= (totalDist - maxDist);
				objectSwiper.position = resetPos;
			}
		}

		/** проверка расстояния до центра ленты (для контроля над размером объекта) */
		public float CheckDistanceToCenter(float centerDistThreshold)
		{
			float dist = Vector3.Distance(objectSwiper.position, Vector3.zero);
			if(dist < centerDistThreshold) {
				return ScaleMultiplier;
			} else {
				return 1.0f;
			}
		}
	}

	/**OBJECT SWIPE STATE*/
	public class ObjectSwipe : IBaseState {
		private 	ObjectSwipeSettings oss = new ObjectSwipeSettings();
		private 	SwipeEffect			sw;

		private   	float           parkingDelayTimer 	= 0.25f;
		private 	bool			swipeEnabled 		= false;
		private 	Vector3			swipeSpeed 			= Vector3.zero;
		private 	Vector3 		deltaFingerPos 		= Vector3.zero; //lastFingerPos - newFingerPos
		private 	Vector3 		lastFingerPos  		= Vector3.zero;
		// avoid fake touches
		private 	float 			touchThreshold 		= 30.0f;
		// path dist finger
		private 	float 			fingerPathDist 		= 0.0f;
		// event on touch|click
		private 	bool 			touchDown       	= false;
		private  	 int            shiftDirection 		= 0;
		private  	 Vector3        parkingSpeed 		= Vector3.zero;
		private  	 bool           isParkingActive 	= false;
		private  	 int            deviceCounter 		= 0;

		protected   delegate void   ParkingSystem();
		protected   ParkingSystem   ParkingUpdate = () => {};


		public ObjectSwipe(SwipeEffect _sw, IBaseState _ibs) {
			if(_sw == null) {
				return;
			}
			sw = _sw;
			// get base settings
			oss = sw.ObjectSwipe;
			// exit to previous state
			//if(_ibs != null)
			//	_ibs.Exit();
			// Activate sliding
			Start();
			// Activate update
			sw.UpdateState = this.Update;
		}


		public void Start() {
			ReInitialize();

			deviceCounter = oss.objectSwiper.Count;

			for(int idx=0; idx < 5; ++idx)
				AddDevice();

			EnableObjectBand();
		}

		/** */
		public void EnableObjectBand() {
			Debug.Log("EnableObjectBand");
			swipeEnabled = true;
		}

		/** */
		public void DisableObjectBand() {
			Debug.Log("DisableObjectBand");
			swipeEnabled = false;
			touchDown 	 = false;
		}


		/** активируем объект в полоске */
		public void AddDevice() {
			// активируем девайс
			if(deviceCounter < oss.objectSwiper.Count) {
				swipeEnabled = false;
				oss.objectSwiper[deviceCounter].objectSwiper.gameObject.SetActive(true);
				oss.objectSwiper[deviceCounter].enabled = true;

				deviceCounter++;
				deviceCounter = Mathf.Clamp(deviceCounter, 0, oss.objectSwiper.Count);
			}
		}

		/** */
		public void RemoveDevice() {	}

		/** */
		protected void ReInitialize()
		{
			float bandWidth = Vector3.Distance(oss.firstPos.position, oss.lastPos.position);
			float interval	= bandWidth/oss.objectSwiper.Count;
			Vector3			defOffset = Vector3.zero;
			defOffset.z = interval;
			Debug.Log("interval: " + interval + "||" + oss.objectSwiper.Count);
			for(int idx=0; idx < oss.objectSwiper.Count; ++idx) {
				oss.objectSwiper[idx].FirstPoint 		= oss.firstPos;
				oss.objectSwiper[idx].LastPoint 		= oss.lastPos;
				oss.objectSwiper[idx].ScaleMultiplier 	= oss.scaleMultiplier;
				oss.objectSwiper[idx].ButtonOffset		= oss.buttonOffset;
				oss.objectSwiper[idx].InitialScale 		= oss.objectSwiper[idx].objectSwiper.localScale;
				oss.objectSwiper[idx].objectSwiper.position = oss.lastPos.position  - idx * defOffset + oss.initPosOffset;
			}
		}

		public static void DelegateNone() { }

		/** */
		protected void InputMethods()
		{
			if(!swipeEnabled)
				return;

			if(Input.GetMouseButtonDown(0)) {
				//Debug.Log(EventSystem.current.);
			//	if(EventSystem.current.IsPointerOverGameObject()) {
					touchDown = true;
			//	} else {
			//		touchDown = false;
			//	}
			}

			if (Input.GetMouseButtonUp(0)) {
				if (touchDown && oss.useParkingSystem)
				{
					// находим девайс ближайший к точке и запоминаем его индекс
					CheckDeviceDistance();
				}
				touchDown = false;
				fingerPathDist = 0.0f;
			}

			if(Input.GetMouseButton(0)) {
				fingerPathDist += Vector3.Distance(deltaFingerPos, Vector3.zero);
			}
		}

		// Update is called once per frame
		public void Update () {
			// плавное появление девайсов в цепочке
			for (int idx = 0; idx < deviceCounter; ++idx) {
				if(oss.objectSwiper[idx].enabled) {
					oss.objectSwiper[idx].objectSwiper.localScale = Vector3.Lerp(oss.objectSwiper[idx].objectSwiper.localScale, oss.objectSwiper[idx].InitialScale, 0.04f);
				}
			}

			if(!swipeEnabled) {
				return;
			} 

			Debug.Log("Update object");
			// отлавливаем касание пальцем
			InputMethods();

			//процесс смещения полоски объектов
			ShiftUpdater();
			//
			ParkingUpdate();
		}

		/** процесс смещения полоски объектов*/
		protected void ShiftUpdater() {
			//Debug.Log(touchDown);
			if(touchDown)
				deltaFingerPos  = CheckTouchDistance(deltaFingerPos, Input.mousePosition, lastFingerPos, touchThreshold);
			else
				deltaFingerPos = Vector3.Lerp(deltaFingerPos, Vector3.zero, 0.1f);

			swipeSpeed   = Vector3.Lerp(swipeSpeed, deltaFingerPos * oss.speed, 0.1f);
			swipeSpeed.z = -swipeSpeed.x;
			swipeSpeed.y = 0;
			swipeSpeed.x = 0;

			if (!isParkingActive)  { // не смещаем, если используется система парковки
				Vector3 shift = swipeSpeed * Time.deltaTime;
				for (int idx = 0; idx < deviceCounter; ++idx)
				{
					oss.objectSwiper[idx].ObjectTranslate(shift);
					oss.objectSwiper[idx].CheckDistance(0);
					if(oss.indicatorEnabled) {
						float scale = oss.objectSwiper[idx].CheckDistanceToCenter(oss.centerThreshold);
						oss.objectSwiper[idx].objectSwiper.localScale = Vector3.Lerp(oss.objectSwiper[idx].objectSwiper.localScale, oss.objectSwiper[idx].InitialScale * scale, 0.01f);
					}
					oss.objectSwiper[idx].ButtonOffset = oss.buttonOffset;
				}
			}
			// update finger|mouse position
			lastFingerPos = Input.mousePosition;
		}

		/** поиск и проверка ближайшего прибора/объекта */
		protected void CheckDeviceDistance()
		{
			parkingDelayTimer = oss.parkingDelay;
			ParkingUpdate = new ParkingSystem(ParkingUpdateDelay);
		}

		/** задержка перед началом процесса парковки объектов ленты*/
		protected void ParkingUpdateDelay()
		{
			parkingDelayTimer -= Time.deltaTime;

			if(parkingDelayTimer < 0.0f) {
				oss.deviceIdx = 0;
				oss.parkingMinDist = Vector3.Distance(oss.objectSwiper[oss.deviceIdx].objectSwiper.position, oss.parkingPoint.position);
				for (int idx = 1; idx < oss.objectSwiper.Count; ++idx) {
					float curDist = Vector3.Distance(oss.objectSwiper[idx].objectSwiper.position, oss.parkingPoint.position);
					if (curDist < oss.parkingMinDist) {
						oss.parkingMinDist = curDist;
						oss.deviceIdx = idx; // нашли ближайший девайс к точке притягивания и запомнили индекс
					}
				}
				isParkingActive = true;
				// активируем систему доводки ленты объектов к точке
				ParkingUpdate = new ParkingSystem(ParkingUpdateActive);
			}
		}

		/** паркуем полоску с объектами в невидимые клетки*/
		protected void ParkingUpdateActive()
		{
			// определить, в какую сторону нужно смещаться (по какой-нить дельте)
			if (oss.objectSwiper[oss.deviceIdx].objectSwiper.position.z > 0)
				shiftDirection = 1;
			else
				shiftDirection = -1;

			Vector3 shiftSpeed = parkingSpeed;
			shiftSpeed.z = -shiftSpeed.x * shiftDirection;
			shiftSpeed.y = 0;
			shiftSpeed.x = 0;

			// сдвигаем линейку приборов к нужном направлении
			Vector3 shift = shiftSpeed * Time.deltaTime;
			for (int idx = 0; idx < oss.objectSwiper.Count; ++idx)
			{
				oss.objectSwiper[idx].ObjectTranslate(shift);
				oss.objectSwiper[idx].CheckDistance(0);

				float scale = oss.objectSwiper[idx].CheckDistanceToCenter(oss.centerThreshold);
				oss.objectSwiper[idx].objectSwiper.localScale = Vector3.Lerp(oss.objectSwiper[idx].objectSwiper.localScale, oss.objectSwiper[idx].InitialScale * scale, 0.01f);
			}

			float curDist = Vector3.Distance(oss.objectSwiper[oss.deviceIdx].objectSwiper.position, oss.parkingPoint.position);
			// регулировка скорости смещения
			if (curDist < oss.parkingMinDist / 2)
				parkingSpeed = Vector3.Lerp(parkingSpeed, Vector3.zero, 0.01f);
			else
				parkingSpeed = Vector3.Lerp(parkingSpeed, new Vector3(1, 1, 1), 0.1f);

			// если приблизились - выключаем парковку
			if (curDist < 0.01f) {
				isParkingActive = false;
				oss.objectSwiper[oss.deviceIdx].objectSwiper.position = oss.parkingPoint.position;
				ParkingUpdate = new ParkingSystem(DelegateNone);
			}
		}

		/** */
		static Vector3 CheckTouchDistance(Vector3 _olddelta, Vector3 _newpos, Vector3 _lastpos, float touchthreshold) {
			if(Vector3.Distance(_newpos, _lastpos) > touchthreshold) {
				return _olddelta;
			} else {
				Vector3 _newvalue = _newpos - _lastpos;
				return _newvalue;
			}
		}

		public void Exit() {
			if(sw) {
				//sw.UpdateState = () => {};
			}
		}
	}
}