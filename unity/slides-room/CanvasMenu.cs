using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace SwipeEffect
{
	public class CanvasMenu : MonoBehaviour {

		[SerializeField]
		protected  		List<SlideShowUIContainer>	slideShows = new List<SlideShowUIContainer>();
		[SerializeField]
		protected  		GameObject		menuCanvas;

		protected 		int 			currentActiveSlideShow;


		// Use this for initialization
		void Start () {
			menuCanvas = gameObject;

			// close all inSlideShow ui buttons
			for(int idx=0; idx < slideShows.Count; ++idx)
				for(int jdx=0; jdx < slideShows[idx].ButtonCount; ++jdx)
					slideShows[idx].ButtonActive(jdx, false);
		}
		
		// Update is called once per frame
		void Update () {
		
		}

		/** */
		public void  BtnMenuAction(int index)
		{
			menuCanvas.SetActive(false);

			slideShows[index].GetSlideShow.ShowSlide = true;
			currentActiveSlideShow = index;
			//if(backToMainMenu)
			//	ActiveBackToMainMenu = new AutoBackControl(AutoBackToMainMenu);

			for(int jdx=0; jdx < slideShows[currentActiveSlideShow].ButtonCount; ++jdx)
				slideShows[currentActiveSlideShow].ButtonActive(jdx, true);
		}

		/** */
		public void  HideCurrentSlideShow()
		{
			slideShows[currentActiveSlideShow].GetSlideShow.ShowSlide = false;
			menuCanvas.SetActive(true);

			// close all inSlideShow ui buttons
			for(int idx=0; idx < slideShows.Count; ++idx)
				for(int jdx=0; jdx < slideShows[idx].ButtonCount; ++jdx)
					slideShows[idx].ButtonActive(jdx, false);
		}
	}

	[System.Serializable]
	public class SlideShowUIContainer
	{
		[SerializeField]
		protected  		SwipeEffect		slideShow;
		[SerializeField]
		protected  		List<Button>	inSlideShowButtons = new List<Button>();


		public SwipeEffect 	GetSlideShow { get { return slideShow; } }

		public int 			ButtonCount { get { return inSlideShowButtons.Count; } }

		public void ButtonActive (int index, bool enabled)
		{ 
			if(index < inSlideShowButtons.Count)
				inSlideShowButtons[index].gameObject.SetActive(enabled);
		}
	}
}