using UnityEngine;

public class PanelFader : MonoBehaviour
{
    public CanvasGroup Group;
    public float Duration = 0.2f;
    Coroutine _anim;

    void Awake()
    {
        if (!Group) Group = GetComponent<CanvasGroup>();
        SetVisible(false, true);
    }

    public void Show() => SetVisible(true, false);
    public void Hide() => SetVisible(false, false);

    void SetVisible(bool visible, bool instant)
    {
        if (!Group) return;

        if (_anim != null) StopCoroutine(_anim);
        if (instant)
        {
            Group.alpha = visible ? 1f : 0f;
            Group.interactable = visible;
            Group.blocksRaycasts = visible;
            return;
        }
        _anim = StartCoroutine(Fade(visible));
    }

    System.Collections.IEnumerator Fade(bool toVisible)
    {
        float start = Group.alpha;
        float end = toVisible ? 1f : 0f;
        float t = 0f;
        while (t < Duration)
        {
            t += Time.deltaTime;
            Group.alpha = Mathf.Lerp(start, end, t / Duration);
            yield return null;
        }
        Group.alpha = end;
        Group.interactable = toVisible;
        Group.blocksRaycasts = toVisible;
        _anim = null;
    }
}
