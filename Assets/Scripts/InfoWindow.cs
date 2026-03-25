using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class InfoWindow : MonoBehaviour
{
    public GameObject xrayPanel;
    private Transform charactersContent;
    private Transform objectsContent;
    private Transform locationContent;
    private Transform detailsContent;

    // Variables to keep track of the visor
    private Transform visorContainer;
    private GameObject currentMainContent;

    public static InfoWindow Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void CreateXrayWindow()
    {
        // ===== CANVAS =====
        GameObject canvasGO = new GameObject("XrayCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        canvas.sortingOrder = 20; // Prioritat alta

        // ===== PANEL =====
        xrayPanel = new GameObject("XrayPanel");
        xrayPanel.transform.SetParent(canvasGO.transform, false);

        RectTransform panelRect = xrayPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBG = xrayPanel.AddComponent<Image>();
        panelBG.color = new Color(0, 0, 0, 0.85f);


        // ===== LEFT COLUMN (CONTAINER) =====
        GameObject leftPanel = new GameObject("LeftPanel_ScrollContainer");
        leftPanel.transform.SetParent(xrayPanel.transform, false);

        RectTransform leftRect = leftPanel.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.25f, 1);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;

        // Add ScrollRect
        ScrollRect scrollRect = leftPanel.AddComponent<ScrollRect>();
        scrollRect.horizontal = false; // Disable side-scrolling
        scrollRect.vertical = true;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // ===== VIEWPORT (Masking) =====
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(leftPanel.transform, false);
        RectTransform viewRect = viewport.AddComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.sizeDelta = Vector2.zero; // Stretch
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f); // Background
        viewport.AddComponent<Mask>().showMaskGraphic = true;

        // ===== CONTENT (The moving part) =====
        GameObject contentGO = new GameObject("LeftPanel_Content");
        contentGO.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();

        // Anchors: Top-Stretch
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = new Vector2(0, -1000); // Temporary height
        contentRect.offsetMax = Vector2.zero;

        // Layout for the content
        VerticalLayoutGroup leftLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        leftLayout.spacing = 12;
        leftLayout.padding = new RectOffset(15, 15, 15, 15);
        leftLayout.childControlHeight = true;
        leftLayout.childForceExpandHeight = false;

        // CRITICAL: This allows the Content to grow based on the dropdowns
        ContentSizeFitter contentFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Link back to ScrollRect
        scrollRect.content = contentRect;
        scrollRect.viewport = viewRect;

        // ===== SCROLLBAR =====
        GameObject scrollbarGO = new GameObject("VerticalScrollbar");
        scrollbarGO.transform.SetParent(leftPanel.transform, false);

        Scrollbar scrollbar = scrollbarGO.AddComponent<Scrollbar>();
        RectTransform scrollbarRT = scrollbarGO.GetComponent<RectTransform>();

        // Position it on the far right of the Left Panel
        scrollbarRT.anchorMin = new Vector2(1, 0);
        scrollbarRT.anchorMax = new Vector2(1, 1);
        scrollbarRT.pivot = new Vector2(1, 1);
        scrollbarRT.sizeDelta = new Vector2(20, 0); // 20px wide, full height

        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // ===== SCROLLBAR TRACK (Background) =====
        GameObject trackGO = new GameObject("Track");
        trackGO.transform.SetParent(scrollbarGO.transform, false);
        Image trackImg = trackGO.AddComponent<Image>();
        trackImg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Dark background

        RectTransform trackRT = trackGO.GetComponent<RectTransform>();
        trackRT.anchorMin = Vector2.zero;
        trackRT.anchorMax = Vector2.one;
        trackRT.sizeDelta = Vector2.zero;

        // ===== SCROLLBAR HANDLE (The slider) =====
        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(trackGO.transform, false);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Grey handle

        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(0, 0); // Handle size is managed by Scrollbar

        // Link components
        scrollbar.handleRect = handleRT;
        scrollRect.verticalScrollbar = scrollbar;

        // ===== DROPDOWNS (Now attached to contentGO) =====
        charactersContent = CreateDropdown("CHARACTERS", contentGO.transform);
        objectsContent = CreateDropdown("OBJECTS", contentGO.transform);
        locationContent = CreateDropdown("LOCATION", contentGO.transform);

        // ===== RIGHT PANEL (CONTAINER) =====
        GameObject rightPanel = new GameObject("RightPanel_ScrollContainer");
        rightPanel.transform.SetParent(xrayPanel.transform, false);

        RectTransform rightRect = rightPanel.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.25f, 0);
        rightRect.anchorMax = new Vector2(1f, 1);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;

        // Add ScrollRect
        ScrollRect rightScrollRect = rightPanel.AddComponent<ScrollRect>();
        rightScrollRect.horizontal = false;
        rightScrollRect.vertical = true;
        rightScrollRect.scrollSensitivity = 25f;
        rightScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // ===== VIEWPORT =====
        GameObject rightViewport = new GameObject("RightViewport");
        rightViewport.transform.SetParent(rightPanel.transform, false);
        RectTransform rightViewRect = rightViewport.AddComponent<RectTransform>();
        rightViewRect.anchorMin = Vector2.zero;
        rightViewRect.anchorMax = Vector2.one;
        rightViewRect.sizeDelta = Vector2.zero;
        rightViewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.4f);
        rightViewport.AddComponent<Mask>().showMaskGraphic = true;

        // ===== CONTENT =====
        GameObject rightContentGO = new GameObject("RightPanel_Content");
        rightContentGO.transform.SetParent(rightViewport.transform, false);
        RectTransform rightContentRect = rightContentGO.AddComponent<RectTransform>();

        // Anchors: Top-Stretch
        rightContentRect.anchorMin = new Vector2(0, 1); // Top-Stretch
        rightContentRect.anchorMax = new Vector2(1, 1);
        rightContentRect.pivot = new Vector2(0.5f, 1);
        rightContentRect.anchoredPosition = Vector2.zero;
        rightContentRect.sizeDelta = new Vector2(0, 500); // Altura inicial

        // Layout Group
        VerticalLayoutGroup rightLayout = rightContentGO.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(30, 30, 30, 60); // Even padding
        rightLayout.spacing = 25;
        rightLayout.childAlignment = TextAnchor.UpperCenter;

        // Crucial: These must be TRUE for the text to fill the width
        rightLayout.childControlWidth = true;
        rightLayout.childForceExpandWidth = true;

        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandHeight = false;

        // Fitter (Esto es lo que hace que el scroll funcione)
        ContentSizeFitter rightFitter = rightContentGO.AddComponent<ContentSizeFitter>();
        rightFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Link ScrollRect
        rightScrollRect.content = rightContentRect;
        rightScrollRect.viewport = rightViewRect;

        // Reference this as our detailsContent for ShowDetails()
        detailsContent = rightContentGO.transform;

        // ===== RIGHT SCROLLBAR (Matched to Left Style) =====
        GameObject rightScrollbarGO = new GameObject("RightVerticalScrollbar");
        rightScrollbarGO.transform.SetParent(rightPanel.transform, false);

        Scrollbar rightScrollbar = rightScrollbarGO.AddComponent<Scrollbar>();
        RectTransform rightScrollbarRT = rightScrollbarGO.GetComponent<RectTransform>();

        // Position it on the far right of the Right Panel (20px wide, full height)
        rightScrollbarRT.anchorMin = new Vector2(1, 0);
        rightScrollbarRT.anchorMax = new Vector2(1, 1);
        rightScrollbarRT.pivot = new Vector2(1, 1);
        rightScrollbarRT.sizeDelta = new Vector2(20, 0);

        rightScrollbar.direction = Scrollbar.Direction.BottomToTop;

        // --- TRACK (Dark Background) ---
        GameObject rightTrackGO = new GameObject("RightTrack");
        rightTrackGO.transform.SetParent(rightScrollbarGO.transform, false);
        Image rightTrackImg = rightTrackGO.AddComponent<Image>();
        rightTrackImg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Same as left

        RectTransform rightTrackRT = rightTrackGO.GetComponent<RectTransform>();
        rightTrackRT.anchorMin = Vector2.zero;
        rightTrackRT.anchorMax = Vector2.one;
        rightTrackRT.sizeDelta = Vector2.zero;

        // --- HANDLE (Grey Slider) ---
        GameObject rightHandleGO = new GameObject("RightHandle");
        rightHandleGO.transform.SetParent(rightTrackGO.transform, false);
        Image rightHandleImg = rightHandleGO.AddComponent<Image>();
        rightHandleImg.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Same as left

        RectTransform rightHandleRT = rightHandleGO.GetComponent<RectTransform>();
        rightHandleRT.sizeDelta = new Vector2(0, 0);

        // Link components
        rightScrollbar.handleRect = rightHandleRT;
        rightScrollRect.verticalScrollbar = rightScrollbar;

        // DESCRIPTION TEXT
        Text initText = CreateText("Select an element to see details", detailsContent, 18, TextAnchor.UpperLeft);
        LayoutElement textLE = initText.gameObject.AddComponent<LayoutElement>();
        textLE.preferredWidth = -1; // -1 means "ignore and use parent's width"

        // Add close button at the bottom
        //AddCloseButton();

        xrayPanel.SetActive(false);
    }

    public void PopulateXrayData()
    {
        foreach (Transform t in charactersContent) Destroy(t.gameObject);
        foreach (Transform t in objectsContent) Destroy(t.gameObject);
        foreach (Transform t in locationContent) Destroy(t.gameObject);

        foreach (var c in RitualController.Instance.GetRitualData().Characters)
            //CreateText("- " + c.Id, charactersContent, 18, TextAnchor.MiddleLeft);
            CreateSelectableItem(c.Description, charactersContent, () => ShowDetails(c));

        foreach (var o in RitualController.Instance.GetRitualData().Objects)
            //CreateText("- " + o.Id, objectsContent, 18, TextAnchor.MiddleLeft);
            CreateSelectableItem(o.Description, objectsContent, () => ShowDetails(o));

        foreach (var e in RitualController.Instance.GetRitualData().Environments)
            //CreateText("- " + e.Id, locationContent, 18, TextAnchor.MiddleLeft);
            CreateSelectableItem(e.Description, locationContent, () => ShowDetails(e));
    }

    Transform CreateDropdown(string title, Transform parent)
    {
        // ===== Section container =====
        GameObject section = new GameObject(title + "_Section");
        section.transform.SetParent(parent, false);

        VerticalLayoutGroup sectionLayout = section.AddComponent<VerticalLayoutGroup>();
        sectionLayout.spacing = 6;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandHeight = false;

        LayoutElement sectionLE = section.AddComponent<LayoutElement>();
        sectionLE.preferredWidth = 400;

        // ===== HEADER =====
        GameObject headerGO = new GameObject("Header");
        headerGO.transform.SetParent(section.transform, false);

        Image headerBG = headerGO.AddComponent<Image>();
        headerBG.color = new Color(1, 1, 1, 0.1f);

        Button headerBtn = headerGO.AddComponent<Button>();

        LayoutElement headerLE = headerGO.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 48;
        headerLE.minHeight = 48;

        Text headerText = CreateText("▶ " + title, headerGO.transform, 22, TextAnchor.MiddleLeft);
        headerText.color = Color.white;
        headerText.rectTransform.offsetMin = new Vector2(10, 0);

        // ===== CONTENT =====
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(section.transform, false);

        VerticalLayoutGroup contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 4;
        contentLayout.padding = new RectOffset(10, 10, 5, 5);
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlHeight = true;

        ContentSizeFitter contentFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        CanvasGroup contentCG = contentGO.AddComponent<CanvasGroup>();
        contentGO.SetActive(false); // start collapsed

        // ===== DROPDOWN TOGGLE WITH ANIMATION =====
        headerBtn.onClick.AddListener(() =>
        {
            bool open = !contentGO.activeSelf;
            StartCoroutine(AnimateDropdown(contentGO.transform, open));
            headerText.text = (open ? "▼ " : "▶ ") + title;
        });

        return contentGO.transform;
    }

    IEnumerator AnimateDropdown(Transform content, bool open)
    {
        CanvasGroup cg = content.GetComponent<CanvasGroup>();
        if (cg == null) cg = content.gameObject.AddComponent<CanvasGroup>();

        content.gameObject.SetActive(true);

        RectTransform rt = content.GetComponent<RectTransform>();
        float startHeight = open ? 0 : rt.rect.height;
        float targetHeight = open ? GetContentHeight(rt) : 0;

        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float h = Mathf.Lerp(startHeight, targetHeight, t);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            cg.alpha = open ? t : 1 - t;
            yield return null;
        }

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        cg.alpha = open ? 1 : 0;
        content.gameObject.SetActive(open);
    }

    float GetContentHeight(RectTransform contentRT)
    {
        float height = 0f;
        foreach (RectTransform child in contentRT)
        {
            LayoutElement le = child.GetComponent<LayoutElement>();
            if (le != null)
                height += le.preferredHeight;
            else
                height += child.rect.height;

            // Add spacing if VerticalLayoutGroup exists
            VerticalLayoutGroup vg = contentRT.GetComponent<VerticalLayoutGroup>();
            if (vg != null)
                height += vg.spacing;
        }

        // Remove extra spacing after last item
        VerticalLayoutGroup vgl = contentRT.GetComponent<VerticalLayoutGroup>();
        if (vgl != null && contentRT.childCount > 0)
            height -= vgl.spacing;

        // Add padding
        if (vgl != null)
            height += vgl.padding.top + vgl.padding.bottom;

        return height;
    }



    Text CreateText(string txt, Transform parent, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);

        Text t = go.AddComponent<Text>();
        t.text = txt;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;

        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return t;
    }

    void CreateSelectableItem(string name, Transform parent, Action onClick)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            GameObject go = new GameObject(name + "_Item");
            go.transform.SetParent(parent, false);

            Button btn = go.AddComponent<Button>();
            Image img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.05f);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 30;

            Text txt = CreateText(name, go.transform, 18, TextAnchor.MiddleLeft);
            txt.color = Color.white;

            btn.onClick.AddListener(() => onClick());
        }
    }

    void ShowDetails(System.Object element)
    {
        // 1. CLEAR PREVIOUS CONTENT
        foreach (Transform child in detailsContent)
        {
            Destroy(child.gameObject);
        }

        string id = "";
        string title = "";
        string extendedDescription = "";

        // Extraiem les dades segons el tipus d'objecte
        if (element is Character c)
        {
            id = c.Id;
            title = c.Description; // Suposant que 'Description' és el nom/títol
            extendedDescription = c.ExtendedDescription;
        }
        else if (element is Object o)
        {
            id = o.Id;
            title = o.Description;
            extendedDescription = o.ExtendedDescription;
        }
        else if (element is Environment e)
        {
            id = e.Id;
            title = e.Description;
            extendedDescription = e.ExtendedDescription;
        }

        // 2. CREATE TEXTS (TITLE & DESCRIPTION)
        // Creem el Títol (més gran i en negreta)
        Text titleText = CreateText(title.ToUpper(), detailsContent, 24, TextAnchor.UpperLeft);
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white; // O el color que prefereixis
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = -1;

        // Creem l'ExtendedDescription (el cos del text)
        Text descText = CreateText(extendedDescription, detailsContent, 18, TextAnchor.UpperLeft);
        descText.gameObject.AddComponent<LayoutElement>().preferredHeight = -1;

        // 3. CREATE THE VISOR
        GameObject visorGO = new GameObject("BigVisor_Container", typeof(RectTransform));
        visorGO.transform.SetParent(detailsContent, false);
        visorContainer = visorGO.transform;
        visorGO.layer = 5;

        Image visorBg = visorGO.AddComponent<Image>();
        //visorBg.color = Color.black;
        visorBg.color = new Color(0, 0, 0, 0.5f); // 10% de opacidad

        LayoutElement visorLE = visorGO.AddComponent<LayoutElement>();
        visorLE.preferredHeight = 400;
        visorLE.minHeight = 400;
        visorLE.flexibleWidth = 1;

        // 4. CREATE THUMBNAIL SCROLL AREA
        GameObject thumbScrollGO = new GameObject("Thumbnail_ScrollArea", typeof(RectTransform));
        thumbScrollGO.transform.SetParent(detailsContent, false);
        thumbScrollGO.layer = 5;

        LayoutElement thumbLE = thumbScrollGO.AddComponent<LayoutElement>();
        thumbLE.preferredHeight = 110;
        thumbLE.minHeight = 110;

        ScrollRect thumbScroll = thumbScrollGO.AddComponent<ScrollRect>();
        thumbScroll.vertical = false;
        thumbScroll.horizontal = true;
        thumbScroll.viewport = CreateViewport(thumbScrollGO.transform);

        GameObject thumbContent = new GameObject("ThumbContent", typeof(RectTransform));
        thumbContent.transform.SetParent(thumbScroll.viewport, false);
        thumbScroll.content = thumbContent.GetComponent<RectTransform>();

        HorizontalLayoutGroup hlg = thumbContent.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        thumbContent.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 5. LOAD MEDIA (ORDRE: VÍDEO PRIMER)
        List<UnityEngine.Object> allMedia = new List<UnityEngine.Object>();

        // A. Intentem carregar el Vídeo primer
        VideoClip clip = Resources.Load<VideoClip>($"Videos/{id}_3Dmodel");
        if (clip != null)
        {
            allMedia.Add(clip);
            CreateThumbnail(clip, thumbContent.transform);
        }

        // B. Després carreguem les Imatges
        int index = 1;
        while (index < 15)
        {
            Texture2D tex = Resources.Load<Texture2D>($"Images/{id}{index}");
            if (tex == null) break;

            allMedia.Add(tex);
            CreateThumbnail(tex, thumbContent.transform);
            index++;
        }

        // 6. INITIALIZE VISOR (Agafarà el vídeo si n'hi ha, si no, la primera imatge)
        if (allMedia.Count > 0)
        {
            SetVisorContent(allMedia[0]);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.MarkLayoutForRebuild(detailsContent as RectTransform);
    }

    //void SetVisorContent(UnityEngine.Object media)
    //{
    //    if (currentMainContent != null) Destroy(currentMainContent);

    //    currentMainContent = new GameObject("ActiveVisorContent");
    //    currentMainContent.transform.SetParent(visorContainer, false);

    //    RectTransform rt = currentMainContent.AddComponent<RectTransform>();
    //    // Anclas al centro para que el Fitter trabaje desde el medio hacia afuera
    //    rt.anchorMin = new Vector2(0.5f, 0.5f);
    //    rt.anchorMax = new Vector2(0.5f, 0.5f);
    //    rt.pivot = new Vector2(0.5f, 0.5f);
    //    rt.sizeDelta = new Vector2(500, 400); // Tamaño inicial coincidente con el visor

    //    RawImage rawImg = currentMainContent.AddComponent<RawImage>();
    //    rawImg.color = Color.white;

    //    AspectRatioFitter fitter = currentMainContent.AddComponent<AspectRatioFitter>();
    //    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

    //    if (media is Texture2D tex)
    //    {
    //        rawImg.texture = tex;
    //        fitter.aspectRatio = (float)tex.width / tex.height;
    //    }
    //    else if (media is VideoClip clip)
    //    {
    //        VideoPlayer vp = currentMainContent.AddComponent<VideoPlayer>();
    //        vp.clip = clip;
    //        vp.playOnAwake = true;
    //        vp.isLooping = true;
    //        vp.renderMode = VideoRenderMode.APIOnly;

    //        vp.prepareCompleted += (source) => {
    //            rawImg.texture = source.texture;
    //            fitter.aspectRatio = (float)source.width / source.height;
    //        };
    //        vp.Prepare();
    //    }

    //    // Forzamos el orden para que esté por encima del fondo negro
    //    currentMainContent.transform.SetAsLastSibling();
    //}

    void SetVisorContent(UnityEngine.Object media)
    {
        if (currentMainContent != null) Destroy(currentMainContent);

        currentMainContent = new GameObject("ActiveVisorContent");
        currentMainContent.transform.SetParent(visorContainer, false);

        RectTransform rt = currentMainContent.AddComponent<RectTransform>();
        // Center the element
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Set a default size for the container, the fitter will adjust the aspect
        rt.sizeDelta = new Vector2(500, 400);

        RawImage rawImg = currentMainContent.AddComponent<RawImage>();
        rawImg.color = Color.white;

        // Use FitInParent so the whole image/video is visible without distortion
        AspectRatioFitter fitter = currentMainContent.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        if (media is Texture2D tex)
        {
            rawImg.texture = tex;
            fitter.aspectRatio = (float)tex.width / (float)tex.height;
        }
        else if (media is VideoClip clip)
        {
            VideoPlayer vp = currentMainContent.AddComponent<VideoPlayer>();
            vp.clip = clip;
            vp.playOnAwake = true;
            vp.isLooping = true;
            vp.renderMode = VideoRenderMode.APIOnly;

            vp.prepareCompleted += (source) => {
                rawImg.texture = source.texture;
                fitter.aspectRatio = (float)source.width / (float)source.height;
            };
            vp.Prepare();
        }
    }

    //void CreateThumbnail(UnityEngine.Object media, Transform parent)
    //{
    //    GameObject thumbGO = new GameObject("Thumbnail", typeof(RectTransform));
    //    thumbGO.transform.SetParent(parent, false);
    //    thumbGO.layer = 5;

    //    RawImage img = thumbGO.AddComponent<RawImage>();
    //    img.color = Color.white;

    //    LayoutElement le = thumbGO.AddComponent<LayoutElement>();
    //    le.preferredWidth = 90; le.preferredHeight = 90;
    //    le.minWidth = 90; le.minHeight = 90;

    //    if (media is Texture2D tex)
    //    {
    //        img.texture = tex;
    //    }
    //    else if (media is VideoClip clip)
    //    {
    //        img.color = new Color(0.15f, 0.15f, 0.15f); // Fondo oscuro

    //        // ICONO PLAY
    //        GameObject playIconGO = new GameObject("PlayIcon", typeof(RectTransform));
    //        playIconGO.transform.SetParent(thumbGO.transform, false);

    //        Text playText = playIconGO.AddComponent<Text>();
    //        // CAMBIO AQUÍ: Usamos LegacyRuntime.ttf para evitar el ArgumentException
    //        playText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    //        playText.text = "▶";
    //        playText.fontSize = 35;
    //        playText.alignment = TextAnchor.MiddleCenter;
    //        playText.color = Color.white;

    //        // TEXTO "3D VIDEO" (Opcional, para mayor claridad)
    //        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
    //        labelGO.transform.SetParent(thumbGO.transform, false);
    //        Text labelText = labelGO.AddComponent<Text>();
    //        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    //        //labelText.text = "VIDEO 3D";
    //        labelText.fontSize = 10;
    //        labelText.alignment = TextAnchor.LowerCenter;

    //        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
    //        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
    //        labelRT.offsetMin = new Vector2(0, 5); // Margen inferior
    //    }

    //    Button btn = thumbGO.AddComponent<Button>();
    //    btn.onClick.AddListener(() => SetVisorContent(media));
    //}

    void CreateThumbnail(UnityEngine.Object media, Transform parent)
    {
        // 1. CELL: This is the object that sits in the HorizontalLayoutGroup
        GameObject cellGO = new GameObject("ThumbnailCell", typeof(RectTransform));
        cellGO.transform.SetParent(parent, false);

        // This forces the cell to be a specific size in the row
        LayoutElement le = cellGO.AddComponent<LayoutElement>();
        le.preferredWidth = 90;
        le.preferredHeight = 90;
        le.flexibleWidth = 0; // Ensures it doesn't stretch

        // 2. IMAGE CONTAINER: This child handles the actual display and aspect ratio
        GameObject imgContainer = new GameObject("ImgContainer", typeof(RectTransform));
        imgContainer.transform.SetParent(cellGO.transform, false);

        // Stretch the container to fill the Cell (using anchors)
        RectTransform rt = imgContainer.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // The Fitter now works inside the container
        AspectRatioFitter fitter = imgContainer.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        RawImage img = imgContainer.AddComponent<RawImage>();

        if (media is Texture2D tex)
        {
            img.texture = tex;
            fitter.aspectRatio = (float)tex.width / (float)tex.height;
        }
        else if (media is VideoClip clip)
        {
            img.color = new Color(0.15f, 0.15f, 0.15f);
            fitter.aspectRatio = (float)clip.width / (float)clip.height;

            // Play Icon remains inside the cell (or imgContainer)
            GameObject playIconGO = new GameObject("PlayIcon", typeof(RectTransform));
            playIconGO.transform.SetParent(cellGO.transform, false);
            Text playText = playIconGO.AddComponent<Text>();
            playText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playText.text = "▶";
            playText.fontSize = 35;
            playText.alignment = TextAnchor.MiddleCenter;
        }

        // Add button to the CELL so it captures the click for the whole square
        Button btn = cellGO.AddComponent<Button>();
        btn.onClick.AddListener(() => SetVisorContent(media));
    }

    RectTransform CreateViewport(Transform parent)
    {
        GameObject vp = new GameObject("Viewport", typeof(RectTransform));
        vp.transform.SetParent(parent, false);
        vp.layer = 5;

        RectTransform rt = vp.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = vp.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.01f);
        vp.AddComponent<Mask>().showMaskGraphic = false;

        return rt;
    }
}
