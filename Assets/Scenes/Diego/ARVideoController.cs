using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using Vuforia;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ARVideoTapController : MonoBehaviour
{
    [Header("Components")]
    public VideoPlayer videoPlayer;     // Quad 上的 VideoPlayer
    public GameObject videoSurface;     // Quad（用于显隐）

    [Header("UI (optional)")]
    public Button minus5Btn;            // 后退5秒按钮
    public Button plus5Btn;             // 前进5秒按钮
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
            videoPlayer.isLooping = false;  // 需要循环可改为 true
            videoPlayer.errorReceived += (_, msg) => Debug.LogError("Video error: " + msg);
            videoPlayer.Prepare();
        }

        // 绑定按钮事件
        if (minus5Btn) minus5Btn.onClick.AddListener(() => SeekSeconds(-5f));
        if (plus5Btn) plus5Btn.onClick.AddListener(() => SeekSeconds(+5f));
    }

    void OnDestroy()
    {
        if (observer != null)
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    // 识别状态变化
    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        isTracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;

        if (isTracked)
        {
            if (videoSurface) videoSurface.SetActive(true);
            if (uiCanvas) uiCanvas.gameObject.SetActive(true);

            if (videoPlayer != null)
            {
                videoPlayer.time = 0;   // 识别到从头播放；想继续播放就删除这行
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
        // 忽略点在 UI 上（防止和按钮点击冲突）
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

    // 核心：前进/后退 5 秒
    public void SeekSeconds(float delta)
    {
        if (videoPlayer == null || !videoPlayer.canSetTime) return;

        // videoPlayer.length 在某些平台需要等到 Prepare 完成才有效
        double len = videoPlayer.length > 0 ? videoPlayer.length : double.MaxValue;
        double newTime = Mathf.Clamp((float)videoPlayer.time + delta, 0f, (float)len);
        videoPlayer.time = newTime;
    }

    public void Salir()
    {
        // Cargar la escena especificada
        SceneManager.LoadScene("MainMenu");
    }
}
