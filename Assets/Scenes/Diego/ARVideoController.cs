using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Vuforia;
using UnityEngine.EventSystems;

public class ARVideoTapController : MonoBehaviour
{
    [Header("Components")]
    public VideoPlayer videoPlayer;     // Quad 上的 VideoPlayer
    public GameObject videoSurface;     // Quad（用于显隐）

    [Header("UI Buttons")]
    public Button minus5Btn;            // 后退5秒按钮
    public Button plus5Btn;             // 前进5秒按钮
    public Button exitBtn;              // 退出按钮
    public Canvas uiCanvas;             // 可选：需要在识别到时才显示UI

    private ObserverBehaviour observer;
    private bool isTracked = false;

    void Awake()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
            observer.OnTargetStatusChanged += OnTargetStatusChanged;

        if (videoSurface) videoSurface.SetActive(false);
        if (uiCanvas) uiCanvas.gameObject.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;  
            videoPlayer.errorReceived += (_, msg) => Debug.LogError("Video error: " + msg);
            videoPlayer.Prepare();
        }

        // 绑定按钮事件
        if (minus5Btn) minus5Btn.onClick.AddListener(() => SeekSeconds(-5f));
        if (plus5Btn)  plus5Btn.onClick.AddListener(() => SeekSeconds(+5f));
        if (exitBtn)   exitBtn.onClick.AddListener(ExitToMainScene);
    }

    void OnDestroy()
    {
        if (observer != null)
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        isTracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;

        if (isTracked)
        {
            if (videoSurface) videoSurface.SetActive(true);
            if (uiCanvas) uiCanvas.gameObject.SetActive(true);

            if (videoPlayer != null)
            {
                videoPlayer.time = 0;   // 识别到从头播放
                videoPlayer.Play();
            }
        }
        else
        {
            if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Pause();
            if (videoSurface) videoSurface.SetActive(false);
            if (uiCanvas) uiCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        bool tapped = Input.GetMouseButtonDown(0);
        if (!tapped && Input.touchCount > 0)
            tapped = Input.touches[0].phase == TouchPhase.Began;

        if (!tapped) return;
        if (!isTracked || (videoSurface && !videoSurface.activeInHierarchy) || videoPlayer == null) return;

        if (videoPlayer.isPlaying) videoPlayer.Pause();
        else
        {
            if (!videoPlayer.isPrepared) videoPlayer.Prepare();
            videoPlayer.Play();
        }
    }

    // 前进 / 后退功能
    public void SeekSeconds(float delta)
    {
        if (videoPlayer == null || !videoPlayer.canSetTime) return;
        double len = videoPlayer.length > 0 ? videoPlayer.length : double.MaxValue;
        double newTime = Mathf.Clamp((float)videoPlayer.time + delta, 0f, (float)len);
        videoPlayer.time = newTime;
    }

    //  退出按钮功能：切换场景
    public void ExitToMainScene()
    {
        Debug.Log("Salir → Cargando escena principal...");
        SceneManager.LoadScene("MainMenu"); // 改成你目标场景名
    }
}
