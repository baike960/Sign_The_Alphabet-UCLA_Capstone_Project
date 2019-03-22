// // // //  ** ** ** ** ** ** ** // // // //  ** ** ** ** ** ** ** // // // // ** ** ** ** ** ** *
// * Copyright 2015  All Rights Reserved.
// *
// * Please direct any bugs or questions to vadakuma@gmail.com
// * version 1.0
// * author vadakuma 
// // // //  ** ** ** ** ** ** ** // // // //  ** ** ** ** ** ** ** // // // // ** ** ** ** ** ** **
using UnityEngine;
using System.Collections;

namespace SwipeEffect
{
	public class BaseMenu : MonoBehaviour {

		[SerializeField]
		protected 		bool 			backToMainMenu;
		[SerializeField][Tooltip("No touches - back to main menu")]
		protected 		float 			backToMainMenuTime = 1.0f;
		protected 		float 			backToMainMenuTimer = 0.0f;
		[SerializeField]
		protected  		SwipeEffect[]	slideShows;
		public enum DrawImageText
		{
			DrawImages,
			DrawText,
			DrawNone
		};
		[SerializeField]
		protected 		DrawImageText 	onElementContent;
		[SerializeField]
		protected   	Texture[] 		btnImages;
		[SerializeField]
		protected   	Texture 		mainBackGround;
		[SerializeField]
		protected   	GUISkin 		menuBtnSkin;
		[SerializeField]
		protected   	GUISkin 		backBtnSkin;
		[SerializeField]
		protected   	int				btnColumn = 3;
		[SerializeField]
		protected   	int				btnRow    = 1;
		[SerializeField][Tooltip("button relative shift")]
		protected		Vector2 		groupBtnMargin = Vector2.zero;
		[SerializeField][Tooltip("Button Scale")]
		protected		Vector2 		groupBtnScale = new Vector2(1.0f,1.0f);
		[SerializeField] //
		protected 		Rect 			groupElementParams = new Rect(0,0,128,128); 
		[SerializeField] // Back to Menu button params
		protected 		Rect 			backBtnParams = new Rect(0,0,128,128); 




		/** Protected
		 * */
		protected		float 			drawAlpha    = 0.0f;
		protected		float 			minDrawAlpha = 0.0125f;
		protected		bool 			showMenu = true;
		// get names from slideShows
		protected		string[]		slideShowNames;
		protected 		int 			currentActiveSlideShow;
		protected 		float 			padX = 0.0f;
		protected 		float 			padY = 0.0f;

		/** Delegates
		 * */
		protected 		delegate void   DrawGUIControl();
		protected 		DrawGUIControl  ActiveDrawGUI;

		protected 		delegate void   		DrawOnElememntContent(Rect rect, int index, Vector2 pos);
		protected 		DrawOnElememntContent   OnDrawElememnt;

		protected 		delegate void   AutoBackControl();
		protected 		AutoBackControl ActiveBackToMainMenu;


		// Use this for initialization
		void Start () {
		
			if(slideShows == null || slideShows.Length < 1)
			{
				Debug.LogWarning("Warning! SlideShows array is empty!");
				ActiveDrawGUI = new DrawGUIControl(DrawGUIEmpty);
				return;
			}
			else
			{
				ActiveDrawGUI = new DrawGUIControl(DrawGUI);
			}


			slideShowNames = new string[slideShows.Length];
			for(int idx=0; idx < slideShows.Length; ++idx)
				slideShowNames[idx] = slideShows[idx].SlideShowName;

			BackToMainMenu   = backToMainMenu;
			OnElementContent = onElementContent;
		}


		/** */
		protected DrawImageText OnElementContent
		{
			set 
			{
				onElementContent = value;
				switch (onElementContent)
				{
					case DrawImageText.DrawImages:
						if(btnImages == null || btnImages.Length < 1)
							OnDrawElememnt = new DrawOnElememntContent(OnDrawElememntNoneContent);
						else
							OnDrawElememnt = new DrawOnElememntContent(OnDrawElememntImageContent);
						break;
					case DrawImageText.DrawText:
						if(slideShowNames == null || slideShowNames.Length < 1)
							OnDrawElememnt = new DrawOnElememntContent(OnDrawElememntNoneContent);
						else
							OnDrawElememnt = new DrawOnElememntContent(OnDrawElememntTextContent);
						break;
					case DrawImageText.DrawNone:
						OnDrawElememnt = new DrawOnElememntContent(OnDrawElememntNoneContent);
						break;
				}
			}
			
			private get 
			{
				return onElementContent;
			}
		}

		/** */
		protected bool BackToMainMenu
		{
			set 
			{
				backToMainMenu = value;
				backToMainMenuTimer  = backToMainMenuTime;
				if(backToMainMenu)
				{
					ActiveBackToMainMenu = new AutoBackControl(AutoBackToMainMenu);
				}
				else
				{
					ActiveBackToMainMenu = new AutoBackControl(AutoBackEmpty);
				}
			}
			get
			{
				return backToMainMenu;
			}
		}
		

		// Update is called once per frame
		void Update () {

			// for exit
			if(Input.GetKey(KeyCode.Escape))
			{
				Application.Quit();
			}

			ActiveBackToMainMenu();
		}

		void OnGUI(){
			ActiveDrawGUI();
		}

		/** */
		void OnValidate()
		{
			BackToMainMenu   = backToMainMenu;
			OnElementContent = onElementContent;
		}

		/** */
		public void  AutoBackToMainMenu()
		{
			backToMainMenuTimer -= Time.deltaTime;

			if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
			{
				backToMainMenuTimer = backToMainMenuTime;
			}

			if(backToMainMenuTimer < 0)
			{
				ActiveBackToMainMenu = new AutoBackControl(AutoBackEmpty);
				backToMainMenuTimer  = backToMainMenuTime;
				HideCurrentSlideShow();
			}
		}

		/** */
		public void  AutoBackEmpty() { }
		/** */
		public void  DrawGUIEmpty() { }


		/** Draw none*/
		public void OnDrawElememntNoneContent(Rect rect, int index, Vector2 pos)
		{
			if(GUI.Button(rect, " "))
			{
				if(showMenu)
					ElementAction((int)pos.x,(int)pos.y);
			}
		}
		
		/** Draw Text*/
		public void OnDrawElememntTextContent(Rect rect, int index, Vector2 pos)
		{
			if(GUI.Button(rect, slideShowNames[index]))
			{
				if(showMenu)
					ElementAction((int)pos.x,(int)pos.y);
			}
		}
		
		 /** Draw Image*/
		public void OnDrawElememntImageContent(Rect rect, int index, Vector2 pos)
		{
			if(GUI.Button(rect, btnImages[index]))
			{
				if(showMenu)
					ElementAction((int)pos.x,(int)pos.y);
			}
		}


		/** */
		public void  DrawGUI()
		{
			if(showMenu)
				drawAlpha = Mathf.Lerp(drawAlpha, 1.0f, 0.1f);
			else
				drawAlpha = Mathf.Lerp(drawAlpha, 0.0f, 0.1f);

			if(drawAlpha > minDrawAlpha)
			{
				GUI.color = new Color(0.5f,0.5f,0.5f, drawAlpha);
				if(mainBackGround)
					GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), mainBackGround);
				
				
				DrawGridOfElements();
			}
			else
			{
				float resX = Screen.width/1920.0f;
				float resY = Screen.height/1080.0f;
				GUI.skin = backBtnSkin;
				if(GUI.Button(new Rect(backBtnParams.x * resX, backBtnParams.y * resY, backBtnParams.width * resX, backBtnParams.height * resY), "back" ))
				{
					HideCurrentSlideShow();
				}
			}
		}
			
		/** */
		public void  DrawGridOfElements()
		{
			int 	kdx = 0;
			float 	resX = Screen.width/1920.0f;
			float 	resY = Screen.height/1080.0f;
			float 	scaledScaleX = groupBtnScale.x * resX;
			float 	scaledScaleY = groupBtnScale.y * resY;
			
			padX = Mathf.Lerp(padX, groupElementParams.x * resX, 0.1f);
			padY = Mathf.Lerp(padY, groupElementParams.y * resY, 0.1f);
			
			for(int jdx=0; jdx < btnRow; ++jdx) {
				for(int idx=0; idx < btnColumn; ++idx) {	
					float posX = idx * (groupElementParams.width + groupBtnMargin.x) * scaledScaleX + padX;
					float posY = jdx * (groupElementParams.height + groupBtnMargin.y) * scaledScaleY + padY;
					// we don't draw those buttons that are beyond visibility
					if((posX > - groupElementParams.width && posX < Screen.width) && (posY > - groupElementParams.height && posY < Screen.height)) 
					{
						GUI.skin = menuBtnSkin;
						Rect elementRect = new Rect(posX, posY, groupElementParams.width * scaledScaleX, groupElementParams.height * scaledScaleY);
						OnDrawElememnt(elementRect,(jdx) * btnColumn + idx, new Vector2(jdx, idx));
					}
					//
					kdx++;
				}
			}
		}

		/** */
		public void  ElementAction(int _r, int _c)
		{
			showMenu = false;
			currentActiveSlideShow = (_r) * btnColumn + _c;
			slideShows[currentActiveSlideShow].ShowSlide = true;

			if(backToMainMenu)
				ActiveBackToMainMenu = new AutoBackControl(AutoBackToMainMenu);
		}

		/** */
		public void  HideCurrentSlideShow()
		{
			showMenu = true;
			slideShows[currentActiveSlideShow].ShowSlide = false;
		}
	}
}
