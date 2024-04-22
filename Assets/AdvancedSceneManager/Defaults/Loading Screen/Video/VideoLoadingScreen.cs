using System.Collections;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace AdvancedSceneManager.Defaults
{

    /// <summary>A default loading screen script. Displays a video.</summary>
    public class VideoLoadingScreen : FadeLoadingScreen
    {

        //Video clip in this example is set from the scene before to make it more dynamic. 
        //VideoLoadingScreen.videoClip = videoClip;

        public VideoClip defaultVideoClip;
        public static VideoClip videoClip;
        public RawImage VideoRenderer;

        [Header("VideoClip is static, Apply it before loading")]
        public VideoPlayer videoPlayer;

        bool videoFinished;

        public override IEnumerator OnOpen()
        {
            yield return FadeIn();
            SetupVideo();
        }

        public override IEnumerator OnClose()
        {

            // Unity's Coroutine does not support this, so make use of our, coroutine().StartCoroutine()
            // Lets wait until video is done before we continue
            yield return WaitUntil().StartCoroutine();
            yield return FadeOut();

        }

        void SetupVideo()
        {
            videoPlayer.clip = videoClip ? videoClip : defaultVideoClip;
            videoPlayer.loopPointReached += EndReached;
            videoPlayer.Play();
        }

        void EndReached(VideoPlayer source)
        {
            videoFinished = true;
            videoPlayer.Stop();
            VideoRenderer.enabled = false;
        }

        IEnumerator WaitUntil() =>
            new WaitUntil(() => videoFinished);

    }

}
