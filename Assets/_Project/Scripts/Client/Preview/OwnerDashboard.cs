using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LinhThu.Prototype.Preview
{
    /// <summary>In-game owner inventory. Reads project metadata and the loaded map; never edits art or save data.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class OwnerDashboard : MonoBehaviour
    {
        private static readonly Color Ink = new Color(.10f, .17f, .22f);
        private static readonly Color Muted = new Color(.38f, .45f, .48f);
        private static readonly Color Paper = new Color(.96f, .97f, .96f);
        private static readonly Color Soft = new Color(.90f, .94f, .92f);
        private static readonly Color Accent = new Color(.12f, .40f, .34f);
        private const int PageSize = 12;
        private readonly string[] pageNames = { "Tổng quan", "Bản đồ", "Asset" };
        private readonly string[] groupIds = { "all", "image", "prefab", "animation", "other" };
        private readonly string[] groupNames = { "Tất cả", "Ảnh", "Prefab", "Animation", "Khác" };
        private TMP_FontAsset font;
        private ArtPreviewHost host;
        private OwnerCatalog catalog;
        private RectTransform canvas, modal, body, assetList, assetDetail;
        private TextMeshProUGUI heading, subtitle, resultCount, pageLabel;
        private Button[] tabs, filters;
        private Button previous, next, buildFilter;
        private ScrollRect assetScroll;
        private string query = "", group = "all", selectedId;
        private bool buildOnly;
        private int assetPage;
        public bool IsOpen { get; private set; }
        public int CurrentPage { get; private set; }
        public int AssetCount => catalog.assets.Length;

        private void Start()
        {
            host = GetComponent<ArtPreviewHost>();
            font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-TMP");
            var source = Resources.Load<TextAsset>("OwnerDashboard/catalog");
            if (host == null || font == null || source == null)
                throw new InvalidOperationException("Owner dashboard requires the preview host, Vietnamese font and generated catalogue.");
            catalog = JsonUtility.FromJson<OwnerCatalog>(source.text);
            if (EventSystem.current == null)
            {
                var events = new GameObject("Owner UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                var input = events.GetComponent<InputSystemUIInputModule>();
                input.AssignDefaultActions(); input.move = null; input.submit = null; input.cancel = null;
            }
            canvas = Node(transform, "Owner Dashboard Canvas", new Rect());
            var layer = canvas.gameObject.AddComponent<Canvas>(); layer.renderMode = RenderMode.ScreenSpaceOverlay; layer.sortingOrder = 2000;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var launcher = MakeButton(canvas, "Open owner dashboard", "Quản trị  ·  F2", new Rect(0, 0, 170, 42), () => SetOpen(true), Ink, Color.white);
            var launchRect = (RectTransform)launcher.transform;
            launchRect.anchorMin = launchRect.anchorMax = new Vector2(1, 1); launchRect.pivot = new Vector2(1, 1); launchRect.anchoredPosition = new Vector2(-20, -18);
            modal = Stretch(canvas, "Owner dashboard overlay");
            var dim = modal.gameObject.AddComponent<Image>(); dim.color = new Color(.03f, .07f, .08f, .55f);
            var shell = Box(modal, "Owner dashboard", new Rect(0, 0, 1240, 672), Paper);
            shell.anchorMin = shell.anchorMax = shell.pivot = new Vector2(.5f, .5f); shell.anchoredPosition = Vector2.zero;
            var rail = Box(shell, "Navigation", new Rect(0, 0, 216, 672), Ink);
            Label(rail, "Project", "NGỰ THÚ", new Rect(22, 27, 175, 34), 25, Color.white, true);
            Label(rail, "Role", "XƯỞNG DỰ ÁN", new Rect(23, 68, 170, 24), 12, new Color(.59f, .76f, .70f));
            tabs = new Button[3];
            for (int i = 0; i < tabs.Length; i++)
            {
                int page = i;
                tabs[i] = MakeButton(rail, "Page " + i, pageNames[i], new Rect(18, 126 + i * 58, 180, 46), () => SelectPage(page), Ink, Color.white);
            }
            Label(rail, "Local context", "CHỦ DỰ ÁN\n\nBản local · xem mỹ thuật\nMap, nhân vật và asset", new Rect(23, 518, 174, 110), 13, new Color(.69f, .78f, .79f));
            heading = Label(shell, "Page heading", "", new Rect(244, 21, 795, 42), 28, Ink, true);
            subtitle = Label(shell, "Page description", "", new Rect(244, 68, 920, 25), 13, Muted);
            MakeButton(shell, "Close owner dashboard", "Đóng · Esc", new Rect(1104, 24, 108, 38), () => SetOpen(false), Soft, Ink);
            body = Node(shell, "Page content", new Rect(244, 110, 968, 514));
            var updated = DateTime.Parse(catalog.generatedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime();
            Label(shell, "Catalogue time", "Danh mục: " + updated.ToString("dd/MM/yyyy HH:mm") + "  ·  Tự cập nhật khi vào Play / build", new Rect(244, 642, 962, 20), 11, Muted);
            SelectPage(0);
            modal.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (modal == null || Keyboard.current == null) return;
            if (Keyboard.current.f2Key.wasPressedThisFrame) SetOpen(!IsOpen);
            else if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame) SetOpen(false);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open; host.SetOwnerDashboardOpen(open); modal.gameObject.SetActive(open);
            if (open) SelectPage(CurrentPage);
            else if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void SelectPage(int page)
        {
            CurrentPage = page;
            Clear(body);
            heading.text = pageNames[page];
            for (int i = 0; i < tabs.Length; i++) tabs[i].GetComponent<Image>().color = i == page ? Accent : Ink;
            if (page == 0) BuildOverview();
            else if (page == 1) BuildMaps();
            else BuildAssets();
        }

        private void BuildOverview()
        {
            subtitle.text = "Nội dung thật của dự án và bản đồ đang mở.";
            Metric(body, 0, "ASSET MỸ THUẬT", catalog.assets.Length.ToString(), "Mỗi file nội dung được đếm một lần");
            Metric(body, 326, "BỐ CỤC BẢN ĐỒ", catalog.maps.Length.ToString(), catalog.maps.Count(m => m.inBuild) + " bố cục được bản chơi tham chiếu");
            Metric(body, 652, "VẬT THỂ TRÊN MAP", host.map.GetComponentsInChildren<ModularMapProp>(true).Length.ToString(), "Số lượt đặt, không phải số asset gốc");
            var map = Box(body, "Current map card", new Rect(0, 126, 474, 246), Color.white);
            Label(map, "Map heading", "Bản đồ đang xem", new Rect(20, 18, 420, 30), 19, Ink, true);
            Label(map, "Map summary", MapName(host.map.name) + "\n" + host.map.size.x.ToString("0.##") + " × " + host.map.size.y.ToString("0.##") + " đơn vị thế giới\n" + host.map.GetComponentsInChildren<ObstacleAuthoring>(true).Length + " vùng va chạm · nền và đường chỉnh riêng", new Rect(20, 62, 434, 92), 16, Muted);
            MakeButton(map, "Inspect maps", "Xem bản đồ", new Rect(20, 185, 170, 39), () => SelectPage(1), Accent, Color.white);
            var library = Box(body, "Asset library card", new Rect(494, 126, 474, 246), Color.white);
            Label(library, "Library heading", "Thư viện mỹ thuật", new Rect(20, 18, 425, 30), 19, Ink, true);
            Label(library, "Library summary", string.Join("\n", Enumerable.Range(1, 4).Select(i => groupNames[i] + "   " + catalog.assets.Count(a => a.group == groupIds[i]))), new Rect(20, 60, 220, 110), 16, Muted);
            Label(library, "Build count", catalog.assets.Count(a => a.inBuild) + " asset\ntrong bản chơi", new Rect(259, 68, 192, 80), 20, Accent, true);
            MakeButton(library, "Browse assets", "Duyệt asset", new Rect(20, 185, 170, 39), () => SelectPage(2), Accent, Color.white);
            var note = Box(body, "Scope card", new Rect(0, 392, 968, 116), Soft);
            Label(note, "Scope title", "Phạm vi hiện tại", new Rect(20, 15, 920, 26), 16, Ink, true);
            Label(note, "Scope description", "Map, đi lại, nhân vật và hiệu ứng. Catalogue có cả các mẫu mỹ thuật đang giữ để so sánh.\nBảng quản trị hiện dùng để quan sát; dữ liệu gameplay và save cũ không được nạp.", new Rect(20, 49, 922, 54), 14, Muted);
        }

        private void BuildMaps()
        {
            subtitle.text = "Bố cục dùng chung, kích thước và vật thể đã đặt. Chọn bản đồ để xem.";
            var list = Box(body, "Map list", new Rect(0, 0, 268, 508), Color.white);
            var detail = Node(body, "Map details", new Rect(288, 0, 680, 508));
            for (int i = 0; i < catalog.maps.Length; i++)
            {
                var map = catalog.maps[i];
                MakeButton(list, "Map " + map.name, MapName(map.name) + "\n" + (map.inBuild ? "Trong bản chơi" : "Bố cục tham khảo"), new Rect(12, 12 + i * 78, 244, 66), () => ShowMap(detail, map), Soft, Ink, 14);
            }
            Label(list, "Map scope", "Chỉ đếm prefab có bố cục map.\nScene thử nhân vật nằm ở Asset.", new Rect(16, 430, 236, 59), 12, Muted);
            if (catalog.maps.Length > 0) ShowMap(detail, catalog.maps[0]);
        }

        private void ShowMap(RectTransform detail, OwnerMap map)
        {
            Clear(detail);
            var summary = Box(detail, "Map summary", new Rect(0, 0, 680, 166), Color.white);
            Label(summary, "Map title", MapName(map.name), new Rect(18, 15, 640, 31), 21, Ink, true);
            Label(summary, "Map figures", $"{map.width:0.##} × {map.height:0.##} đơn vị     ·     {map.props} vật thể     ·     {map.obstacles} vùng va chạm\n" + (map.editableGround ? "Nền texture lặp · đường mesh" : "Nền ảnh") + $"     ·     {map.pathKnots} điểm uốn đường", new Rect(18, 56, 644, 57), 14, Muted);
            Label(summary, "Map path", map.path, new Rect(18, 121, 644, 30), 11, Muted);
            bool loaded = host.map.name.Replace("(Clone)", "").Trim() == map.name;
            Label(detail, "Map objects heading", loaded ? "VẬT THỂ TRONG PHIÊN HIỆN TẠI" : "BỐ CỤC CHƯA NẠP VÀO PHIÊN", new Rect(0, 183, 670, 26), 13, Accent, true);
            if (!loaded)
            {
                Label(detail, "Not loaded", "Thống kê lấy từ prefab đã lưu.\nMap đang mở được giữ nguyên khi bạn xem bố cục này.", new Rect(18, 230, 630, 70), 15, Muted);
                return;
            }
            var rows = MakeScroll(detail, "Map objects", new Rect(0, 222, 680, 286), out _);
            foreach (var prop in host.map.GetComponentsInChildren<ModularMapProp>(true).OrderBy(p => p.name, StringComparer.Ordinal))
            {
                var row = ListRow(rows, prop.name, 47, Color.white);
                Label(row, "Object", prop.name, new Rect(14, 6, 346, 23), 14, Ink);
                Vector3 p = prop.transform.position;
                Label(row, "Position", $"X {p.x:0.00}   Y {p.y:0.00}   ·   " + (prop.floorLayer ? "Sát nền" : "Đồ vật"), new Rect(368, 10, 278, 24), 12, Muted);
            }
        }

        private void BuildAssets()
        {
            subtitle.text = "Đếm file nội dung gốc; không đếm .meta, mã nguồn, font hoặc từng sprite con.";
            filters = new Button[5];
            for (int i = 0; i < filters.Length; i++)
            {
                string id = groupIds[i];
                filters[i] = MakeButton(body, "Filter " + id, groupNames[i], new Rect(i * 118, 0, 108, 37), () => { group = id; assetPage = 0; RefreshAssets(); }, Soft, Ink, 13);
            }
            BuildSearch(body, new Rect(610, 0, 358, 37));
            buildFilter = MakeButton(body, "Build assets only", "Chỉ trong bản chơi", new Rect(0, 51, 184, 32), () => { buildOnly = !buildOnly; assetPage = 0; RefreshAssets(); }, Soft, Ink, 12);
            resultCount = Label(body, "Result count", "", new Rect(200, 53, 387, 29), 13, Muted);
            assetList = MakeScroll(body, "Asset results", new Rect(0, 96, 590, 360), out assetScroll);
            assetDetail = Box(body, "Selected asset", new Rect(610, 51, 358, 457), Color.white);
            previous = MakeButton(body, "Previous asset page", "Trước", new Rect(0, 473, 91, 35), () => { assetPage--; RefreshAssets(); }, Soft, Ink, 13);
            next = MakeButton(body, "Next asset page", "Sau", new Rect(499, 473, 91, 35), () => { assetPage++; RefreshAssets(); }, Soft, Ink, 13);
            pageLabel = Label(body, "Asset page", "", new Rect(114, 478, 360, 25), 13, Muted);
            pageLabel.alignment = TextAlignmentOptions.Center;
            RefreshAssets();
        }

        private void BuildSearch(Transform parent, Rect area)
        {
            var box = Box(parent, "Asset search", area, Color.white);
            var viewport = Stretch(box, "Search viewport"); viewport.offsetMin = new Vector2(12, 0); viewport.offsetMax = new Vector2(-12, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = Label(viewport, "Search text", "", new Rect(0, 0, area.width - 24, area.height), 13, Ink);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            var hint = Label(viewport, "Search hint", "Tìm tên hoặc đường dẫn…", new Rect(0, 0, area.width - 24, area.height), 13, Muted);
            hint.alignment = TextAlignmentOptions.MidlineLeft;
            var input = box.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport; input.textComponent = text; input.placeholder = hint; input.fontAsset = font;
            input.lineType = TMP_InputField.LineType.SingleLine; input.characterLimit = 160; input.text = query;
            input.restoreOriginalTextOnEscape = false;
            input.onValueChanged.AddListener(value => { query = value; assetPage = 0; RefreshAssets(); });
        }

        private void RefreshAssets(bool resetScroll = true)
        {
            float scrollPosition = assetScroll.verticalNormalizedPosition;
            var matches = catalog.Search(query, group, buildOnly);
            int pages = Mathf.Max(1, Mathf.CeilToInt(matches.Length / (float)PageSize));
            assetPage = Mathf.Clamp(assetPage, 0, pages - 1);
            previous.interactable = assetPage > 0; next.interactable = assetPage + 1 < pages;
            pageLabel.text = $"Trang {assetPage + 1} / {pages}";
            resultCount.text = $"{matches.Length} kết quả / {catalog.assets.Length} asset";
            buildFilter.GetComponent<Image>().color = buildOnly ? new Color(.71f, .85f, .78f) : Soft;
            for (int i = 0; i < filters.Length; i++) filters[i].GetComponent<Image>().color = group == groupIds[i] ? new Color(.71f, .85f, .78f) : Soft;
            Clear(assetList);
            var shown = matches.Skip(assetPage * PageSize).Take(PageSize).ToArray();
            if (!shown.Any(a => a.id == selectedId)) selectedId = shown.FirstOrDefault()?.id;
            foreach (var asset in shown)
            {
                var row = ListRow(assetList, "Asset " + asset.id, 57, asset.id == selectedId ? Soft : Color.white);
                var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row.GetComponent<Image>();
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.AddListener(() => { selectedId = asset.id; RefreshAssets(false); });
                Label(row, "Name", asset.name, new Rect(13, 6, 540, 24), 14, Ink, true).overflowMode = TextOverflowModes.Truncate;
                Label(row, "Type", asset.kind + "  ·  " + asset.collection + (asset.inBuild ? "  ·  Trong bản chơi" : ""), new Rect(13, 31, 540, 20), 11, Muted).overflowMode = TextOverflowModes.Truncate;
            }
            if (matches.Length == 0)
            {
                var empty = ListRow(assetList, "No matching assets", 110, Color.white);
                Label(empty, "Empty result", "Không tìm thấy asset phù hợp.\nThử tên khác hoặc bỏ bộ lọc.", new Rect(20, 22, 520, 76), 16, Muted);
            }
            ShowAsset(shown.FirstOrDefault(a => a.id == selectedId));
            LayoutRebuilder.ForceRebuildLayoutImmediate(assetList);
            assetScroll.verticalNormalizedPosition = resetScroll ? 1 : scrollPosition;
        }

        private void ShowAsset(OwnerAsset asset)
        {
            Clear(assetDetail);
            Label(assetDetail, "Asset detail heading", "CHI TIẾT ASSET", new Rect(20, 20, 318, 25), 12, Accent, true);
            if (asset == null) return;
            Label(assetDetail, "Asset name", asset.name, new Rect(20, 61, 318, 65), 23, Ink, true);
            Label(assetDetail, "Asset status", asset.inBuild ? "Trong bản chơi" : "Có trong dự án · chưa thuộc bản chơi", new Rect(20, 135, 318, 48), 13, Accent);
            string dimensions = asset.width > 0 ? $"\nẢnh nguồn: {asset.width} × {asset.height} px" : "";
            Label(assetDetail, "Asset metadata", asset.kind + "\nBộ: " + asset.collection + "\nFile: " + FormatBytes(asset.bytes) + dimensions, new Rect(20, 196, 318, 93), 14, Muted);
            Label(assetDetail, "Source heading", "ĐƯỜNG DẪN UNITY", new Rect(20, 311, 318, 20), 11, Muted, true);
            Label(assetDetail, "Asset path", asset.path, new Rect(20, 340, 318, 62), 12, Ink);
            Label(assetDetail, "Asset ID", "ID: " + asset.id, new Rect(20, 417, 318, 26), 10, Muted);
        }

        private void Metric(Transform parent, int x, string label, string number, string note)
        {
            var box = Box(parent, label, new Rect(x, 0, 316, 106), Color.white);
            Label(box, "Label", label, new Rect(17, 13, 285, 20), 11, Muted, true);
            Label(box, "Value", number, new Rect(17, 36, 285, 38), 30, Ink, true);
            Label(box, "Note", note, new Rect(17, 79, 285, 19), 11, Muted);
        }

        private static string MapName(string name) => name.Replace("(Clone)", "").Trim() == "MeadowMainMap" ? "Đồng cỏ chính" : name == "MeadowMap" ? "Đồng cỏ · nền ảnh" : name;
        private static string FormatBytes(long bytes) => bytes >= 1048576 ? (bytes / 1048576f).ToString("0.0") + " MB" : (bytes / 1024f).ToString("0.0") + " KB";

        private static RectTransform Node(Transform parent, string name, Rect area)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(area.x, -area.y); rect.sizeDelta = area.size; return rect;
        }

        private static RectTransform Stretch(Transform parent, string name)
        {
            var rect = Node(parent, name, new Rect()); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }

        private static RectTransform Box(Transform parent, string name, Rect area, Color color)
        {
            var node = Node(parent, name, area); node.gameObject.AddComponent<Image>().color = color; return node;
        }

        private TextMeshProUGUI Label(Transform parent, string name, string value, Rect area, int size, Color color, bool bold = false)
        {
            var label = Node(parent, name, area).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = size; label.color = color; label.text = value; label.richText = false;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal; label.raycastTarget = false;
            return label;
        }

        private Button MakeButton(Transform parent, string name, string text, Rect area, Action action, Color background, Color foreground, int size = 15)
        {
            var box = Box(parent, name, area, background); var button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = box.GetComponent<Image>(); button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => action());
            var label = Label(box, "Caption", text, new Rect(8, 0, area.width - 16, area.height), size, foreground);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static RectTransform MakeScroll(Transform parent, string name, Rect area, out ScrollRect scroll)
        {
            var box = Box(parent, name, area, Color.white);
            scroll = box.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            var viewport = Stretch(box, "Viewport"); viewport.offsetMax = new Vector2(-12, 0); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Node(viewport, "Rows", new Rect()); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 3;
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var track = Stretch(box, "Scroll track"); track.anchorMin = new Vector2(1, 0); track.offsetMin = new Vector2(-8, 0); track.offsetMax = Vector2.zero;
            track.gameObject.AddComponent<Image>().color = Paper;
            var handle = Stretch(track, "Handle"); var image = handle.gameObject.AddComponent<Image>(); image.color = new Color(.62f, .72f, .68f);
            var bar = track.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop; bar.handleRect = handle; bar.targetGraphic = image;
            bar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.viewport = viewport; scroll.content = content; scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }

        private static RectTransform ListRow(Transform parent, string name, float height, Color color)
        {
            var row = Box(parent, name, new Rect(0, 0, 0, height), color);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height; return row;
        }

        private static void Clear(Transform root)
        {
            foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }

        private void OnDisable() { if (host != null) host.SetOwnerDashboardOpen(false); }
        private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
    }
}
