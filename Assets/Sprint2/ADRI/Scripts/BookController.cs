using UnityEngine;
using TMPro;

[System.Serializable]
public struct PageData
{
    public string Titulo;
    [TextArea(3, 10)] public string Cuerpo;
    public Sprite Imagen;
}

public class BookController : MonoBehaviour
{
    [Header("Referencias en escena (usa tus nombres)")]
    public Transform PagesRoot;       // -> PagesRoot
    public Paginas PagePrefab;       // -> PageTamemplate (con BookPage)
    public TMP_Text Indicador;        // -> Indicador

    [Header("Contenido")]
    public PageData[] Pages;

    int _index = 0;
    Paginas _instance;

    void Start()
    {
        // Auto-try si no arrastraste algo
        AutoFindIfMissing();

        BuildIfNeeded();
        Show(0);
    }

    void AutoFindIfMissing()
    {
        if (!PagesRoot)
        {
            var libro = transform; // este script está en "Libro"
            var pr = libro.Find("PagesRoot");
            if (pr) PagesRoot = pr;
        }

        if (!Indicador)
        {
            var canvas = transform.parent; // padre de Libro = CanvasComoJugar
            var t = canvas.Find("Indicador");
            if (t) Indicador = t.GetComponent<TMP_Text>();
        }

        if (!PagePrefab)
        {
            // intenta hallar una instancia hija llamada PageTamemplate
            var p = PagesRoot ? PagesRoot.Find("PageTamemplate") : transform.Find("PagesRoot/PageTamemplate");
            if (p) PagePrefab = p.GetComponent<Paginas>();
        }
    }

    void BuildIfNeeded()
    {
        if (!PagesRoot || !PagePrefab) return;

        // Si ya hay una instancia dentro de PagesRoot llamada PageTamemplate, úsala
        if (PagePrefab.transform.parent == PagesRoot)
        {
            _instance = PagePrefab;
        }
        else
        {
            // Si PagePrefab viene de un prefab del Project, instáncialo
            for (int i = PagesRoot.childCount - 1; i >= 0; i--)
                Destroy(PagesRoot.GetChild(i).gameObject);

            _instance = Instantiate(PagePrefab, PagesRoot);
            _instance.name = "PageTamemplate";
        }

        // Centrar
        var rt = _instance.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
    }

    public void Next()  => Show(Mathf.Min(_index + 1, (Pages?.Length ?? 1) - 1));
    public void Prev()  => Show(Mathf.Max(_index - 1, 0));

    public void Show(int index)
    {
        if (_instance == null || Pages == null || Pages.Length == 0) return;

        _index = Mathf.Clamp(index, 0, Pages.Length - 1);
        var d = Pages[_index];
        _instance.Set(d.Titulo, d.Cuerpo, d.Imagen);

        if (Indicador) Indicador.text = $"{_index + 1}/{Pages.Length}";
    }
}
