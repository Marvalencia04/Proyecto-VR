using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class ARTapVideoController : MonoBehaviour
{
    UnityEngine.Video.VideoPlayer videoPlayer;
    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }
    private void OnMouseDown()
    {
        ToggleVideo();
        Debug.Log("Click");
    }
    private void ToggleVideo()
    {
        if (videoPlayer == null) return;
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause(); Debug.Log("Pausando video " + videoPlayer.clip.name);
        }
        else
        {
            videoPlayer.Play();
            Debug.Log("Reproduciendo video " + videoPlayer.clip.name);
        }
    }
}
