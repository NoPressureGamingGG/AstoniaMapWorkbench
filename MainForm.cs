using System.Text;
using System.Text.Json;

namespace AstoniaMapWorkbench;

internal sealed class MainForm : Form
{
    private readonly TextBox spriteSearch = new() { PlaceholderText = "Enter sprite ID or range, e.g. 21006", Dock = DockStyle.Fill };
    private const int MaxSpriteBrowserResults = 2000;
    private const int SpriteGalleryPageSize = 200;
    private readonly TextBox spriteBrowserStart = new() { Text = "12000", Width = 88, PlaceholderText = "Start ID" };
    private readonly TextBox spriteBrowserEnd = new() { Text = "12500", Width = 88, PlaceholderText = "End ID" };
    private readonly ToolTip spriteBrowserToolTip = new() { AutoPopDelay = 12000, InitialDelay = 400, ReshowDelay = 100, ShowAlways = true };
    private bool updatingSpriteBrowserRange;
    private bool spriteBrowserEndWasEdited;
    private readonly ComboBox spriteBrowserSource = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly ComboBox spriteBrowserTarget = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private uint? selectedBrowserSprite;
    private readonly FlowLayoutPanel spriteGallery = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8), BackColor = Color.FromArgb(24, 26, 30) };
    private readonly ListBox spriteWorkspace = new() { Height = 54, Width = 230, AllowDrop = true };
    private readonly ListBox recentSprites = new() { Height = 38, Width = 108 };
    private readonly PictureBox recentlySelectedPreview = new() { Width = 36, Height = 36, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(24, 26, 30) };
    private readonly Label recentlySelectedId = new() { AutoSize = true, Text = "None", AutoEllipsis = true, Width = 66, Margin = new Padding(4, 10, 2, 2) };
    private readonly Button showMoreSprites = new() { Text = "Show next 200", AutoSize = true, Enabled = false };
    private readonly Label spriteBrowserResultCount = new() { AutoSize = true, Text = "" };
    private readonly List<uint> spriteBrowserResultIds = [];
    private int renderedSpriteCount;
    private readonly CheckBox placeSpriteMode = new() { Text = "Click-to-place preview", AutoSize = true };
    private readonly List<uint> spriteWorkspaceIds = [];
    private readonly LinkedList<uint> recentSpriteIds = [];
    private readonly string spriteWorkspacePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstoniaMapWorkbench", "sprite-workspace.json");
    private readonly string recentMapsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstoniaMapWorkbench", "recent-maps.json");
    private readonly string mapTutorialStatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstoniaMapWorkbench", "map-tutorial-dismissed");
    private readonly List<string> recentMapPaths = [];
    private ToolStripMenuItem? recentMapsMenu;
    private MapEditingTutorialForm? mapTutorial;
    private readonly CheckedListBox flagSearch = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly TextBox templateSearch = new() { PlaceholderText = "Search item or character template", Dock = DockStyle.Fill };
    private readonly ListBox results = new() { Dock = DockStyle.Fill };
    private readonly ListBox templates = new() { Dock = DockStyle.Fill };
    private readonly TextBox details = ReadOnlyTextBox();
    private readonly TextBox findings = ReadOnlyTextBox();
    private readonly TextBox activeFlags = ReadOnlyTextBox();
    private readonly Label selectionSummary = new() { AutoSize = true, Text = "Selection: none" };
    private readonly TabControl centerTabs = new() { Dock = DockStyle.Fill };
    private TabPage? resultsPage;
    private readonly TabControl rightTabs = new() { Dock = DockStyle.Fill };
    private TabPage? validationPage;
    private TabPage? spriteBrowserPage;
    private readonly Label status = new() { AutoSize = true, Text = "Open a Server 3 .map file." };
    private MapCanvas canvas;
    private readonly NumericUpDown x = Number(0, 255), y = Number(0, 255), width = Number(1, 256, 1), height = Number(1, 256, 1);
    private readonly NumericUpDown gs1 = Number(0, ushort.MaxValue), gs2 = Number(0, ushort.MaxValue), fs1 = Number(0, ushort.MaxValue), fs2 = Number(0, ushort.MaxValue);
    private readonly TextBox item = new(), character = new();
    private readonly ComboBox paintScope = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly CheckedListBox editFlags = new() { CheckOnClick = true, Height = 165 };
    private readonly Button apply = new() { Text = "Apply tile / rectangle", AutoSize = true };
    private readonly Button preview = new() { Text = "Preview changes", AutoSize = true };
    private readonly Button undo = new() { Text = "Undo", AutoSize = true };
    private readonly Button redo = new() { Text = "Redo", AutoSize = true };
    private readonly TrackBar zoom = new() { Minimum = 10, Maximum = 80, Value = 40, TickFrequency = 10, Dock = DockStyle.Fill };
    private readonly CheckBox collisionOverlay = new() { Text = "Collision overlay", AutoSize = true };
    private readonly CheckBox gridOverlay = new() { Text = "Grid", AutoSize = true };
    private readonly CheckBox wallsOverlay = new() { Text = "Show walls / ceilings", Checked = true, AutoSize = true };
    private readonly CheckBox charactersOverlay = new() { Text = "Show NPCs", Checked = true, AutoSize = true };
    private readonly CheckBox lowerWalls = new() { Text = "Preview cut wall variants", AutoSize = true };
    private readonly ToolTip mapViewToolTips = new() { AutoPopDelay = 15000, InitialDelay = 400, ReshowDelay = 100, ShowAlways = true };
    private readonly CheckBox freeDraw = new() { Text = "Free draw preview", AutoSize = true };
    private readonly CheckBox stampMode = new() { Text = "Brush", Appearance = Appearance.Button, AutoSize = true, Image = BrushIcon(), TextImageRelation = TextImageRelation.ImageBeforeText };
    private Dictionary<Point, TileSnapshot> strokeBefore = [];
    private Stack<TileChange> undoStack = new();
    private Stack<TileChange> redoStack = new();
    private HashSet<Point> selectedTiles = [];
    private HashSet<Point> freeDrawTiles = [];
    private readonly string layoutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstoniaMapWorkbench", "layout.json");
    private readonly SplitContainer mainSplit = new() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, FixedPanel = FixedPanel.Panel1 };
    private readonly SplitContainer centerRightSplit = new() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
    private TileSnapshot? copiedTile;
    private MapRegionClipboard? copiedRegion;
    private SpriteClipboard? copiedSprite;
    private MapDocument? map;
    private ServerMapProfile? profile;
    private HashSet<string> itemTemplates = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> characterTemplates = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, uint> characterSprites = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, uint> itemSprites = new(StringComparer.OrdinalIgnoreCase);
    private SpriteArchive? spriteArchive;
    private readonly Dictionary<TabPage, MapWorkspace> workspaces = [];
    private MapWorkspace? activeWorkspace;
    private bool switchingWorkspace;
    private string artDiagnostic = "not checked";

    public MainForm(string[]? startupArguments = null)
    {
        canvas = CreateMapCanvas();
        Text = "Astonia Map Workbench - local workspace editor";
        MinimumSize = new Size(1180, 760); Width = 1450; Height = 920;
        KeyPreview = true;
        KeyDown += (_, e) => HandleKeyCommand(e);
        FormClosing += (_, _) => SaveLayout();
        Shown += (_, _) =>
        {
            ConfigureLayout();
            if (!File.Exists(mapTutorialStatePath)) ShowMapEditingTutorial();
        };
        stampMode.CheckedChanged += (_, _) => canvas.PaintMode = stampMode.Checked;
        mapViewToolTips.SetToolTip(stampMode, "Brush mode: left-drag paints the copied tile (all four layers, item, NPC, flags). Middle-drag pans; Ctrl-drag selects a rectangle.");
        zoom.ValueChanged += (_, _) => canvas.TileWidth = zoom.Value;
        collisionOverlay.CheckedChanged += (_, _) => { canvas.ShowCollisionOverlay = collisionOverlay.Checked; canvas.RefreshTile(); };
        gridOverlay.CheckedChanged += (_, _) => { canvas.ShowGrid = gridOverlay.Checked; canvas.RefreshTile(); };
        wallsOverlay.CheckedChanged += (_, _) => { canvas.ShowWalls = wallsOverlay.Checked; canvas.RefreshTile(); };
        charactersOverlay.CheckedChanged += (_, _) => { canvas.ShowCharacters = charactersOverlay.Checked; canvas.RefreshTile(); };
        lowerWalls.CheckedChanged += (_, _) => { canvas.LowerWalls = lowerWalls.Checked; canvas.RefreshTile(); };
        mapViewToolTips.SetToolTip(lowerWalls, "Forces configured cut-sprite variants on foreground wall layers. In the game, F8 toggles nocut and disables automatic wall cutting; this preview does not simulate the player's position or neighboring-tile rules.");
        freeDraw.CheckedChanged += (_, _) => { if (BrushPreviewActive()) PreviewFreeDraw(); else { freeDrawTiles.Clear(); canvas.ClearPreview(); } };
        apply.Click += (_, _) => ApplyEdit(); preview.Click += (_, _) => PreviewEdit(); undo.Click += (_, _) => Undo(); redo.Click += (_, _) => Redo();
        paintScope.SelectedIndexChanged += (_, _) => UpdateSelectionSummary();
        foreach (var input in new[] { x, y, width, height }) input.ValueChanged += (_, _) => UpdateSelectionSummary();
        paintScope.Items.AddRange(["Whole tile (replace all)", "Ground layer 1", "Ground layer 2", "Wall / ceiling layer 1", "Wall / ceiling layer 2", "Flags only", "Clear whole tile", "Clear ground layer 1", "Clear ground layer 2", "Clear wall / ceiling layer 1", "Clear wall / ceiling layer 2", "Clear flags", "Item only", "NPC only"]);
        paintScope.SelectedIndex = 5;

        LoadRecentMaps();
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File");
        file.DropDownItems.Add("Open map...", null, (_, _) => OpenMap());
        recentMapsMenu = new ToolStripMenuItem("Open recent maps");
        RefreshRecentMapsMenu();
        file.DropDownItems.Add(recentMapsMenu);
        file.DropDownItems.Add("Save verbatim workspace copy...", null, (_, _) => SaveWorkspaceCopy());
        file.DropDownItems.Add("Save edited workspace map...", null, (_, _) => SaveEditedMap());
        file.DropDownItems.Add("Export review manifest...", null, (_, _) => ExportManifest());
        menu.Items.Add(file);
        var edit = new ToolStripMenuItem("Edit");
        edit.DropDownItems.Add(new ToolStripMenuItem("Copy tile (4 layers, item, NPC, flags)", null, (_, _) => CopyTile()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.C });
        edit.DropDownItems.Add(new ToolStripMenuItem("Paste tile onto selection", null, (_, _) => PasteTile()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.V });
        edit.DropDownItems.Add("Copy selected region (Ctrl+C)", null, (_, _) => CopySelectedRegion());
        edit.DropDownItems.Add("Paste region at selected tile (Ctrl+V)", null, (_, _) => PasteCopiedRegion());
        edit.DropDownItems.Add("Cut selected region (Ctrl+X)", null, (_, _) => CutSelectedRegion());
        edit.DropDownItems.Add("Copy selected region to file...", null, (_, _) => CopyRegionToFile());
        edit.DropDownItems.Add("Paste region from file...", null, (_, _) => PasteRegionFromFile());
        edit.DropDownItems.Add("Clear selected region", null, (_, _) => ClearSelectedRegion());
        edit.DropDownItems.Add("Create NPC build draft...", null, (_, _) => CreateNpcBuildDraft());
        edit.DropDownItems.Add("Close current map tab", null, (_, _) => CloseCurrentMapTab());
        var undoCommand = new ToolStripMenuItem("Undo", null, (_, _) => Undo()) { ShortcutKeys = Keys.Control | Keys.Z };
        var redoCommand = new ToolStripMenuItem("Redo", null, (_, _) => Redo()) { ShortcutKeys = Keys.Control | Keys.Y };
        edit.DropDownItems.Add(new ToolStripSeparator()); edit.DropDownItems.Add(undoCommand); edit.DropDownItems.Add(redoCommand);
        menu.Items.Add(edit);
        var profiles = new ToolStripMenuItem("Server profile");
        profiles.DropDownItems.Add("Generate from Server checkout...", null, (_, _) => GenerateProfile());
        profiles.DropDownItems.Add("Load profile...", null, (_, _) => LoadProfile());
        profiles.DropDownItems.Add("Save active profile...", null, (_, _) => SaveProfile());
        menu.Items.Add(profiles); menu.Items.Add(new ToolStripMenuItem("Validate", null, (_, _) => ValidateMap())); MainMenuStrip = menu;
        var art = new ToolStripMenuItem("Art assets");
        art.DropDownItems.Add("Open Sprite Browser", null, (_, _) => ShowSpriteBrowser());
        art.DropDownItems.Add("Choose compatible pak art folder...", null, (_, _) => ChooseSpriteArchive());
        menu.Items.Add(art);
        var help = new ToolStripMenuItem("Help");
        help.DropDownItems.Add("Map editing tutorial...", null, (_, _) => ShowMapEditingTutorial());
        menu.Items.Add(help);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 33)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 34)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        left.Controls.Add(BuildSpriteSearchPanel(), 0, 0);
        left.Controls.Add(PanelWith("Block / area flags", flagSearch, Button("Find flags", (_, _) => SearchFlags())), 0, 1);
        left.Controls.Add(PanelWith("Item and NPC templates", templateSearch, Button("Find templates", (_, _) => SearchTemplates())), 0, 2);

        var mapActions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        mapActions.Controls.AddRange([
            new Label { Text = "Isometric zoom", AutoSize = true, Padding = new Padding(0, 6, 0, 0) },
            zoom, collisionOverlay, gridOverlay, wallsOverlay, lowerWalls, charactersOverlay, stampMode,
            new Label { Text = "Ctrl-drag selects; Alt-drag previews a brush. Open up to 4 maps; clipboard is shared.", AutoSize = true, Padding = new Padding(0, 6, 0, 0) },
            ToolbarButton("Open map", (_, _) => OpenMap()),
            ToolbarButton("Copy region", (_, _) => { if (IsMapWorkspaceSelected()) CopySelectedRegion(); else status.Text = "Select a map tab first."; }),
            ToolbarButton("Cut / move", (_, _) => { if (IsMapWorkspaceSelected()) CutSelectedRegion(); else status.Text = "Select a map tab first."; }),
            ToolbarButton("Paste", (_, _) => { if (IsMapWorkspaceSelected()) PasteCopiedRegion(); else status.Text = "Select a map tab first."; })
        ]);
        var centerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        centerLayout.Controls.Add(mapActions, 0, 0);
        centerLayout.Controls.Add(centerTabs, 0, 1);
        resultsPage = Tab("Search results", results);
        centerTabs.TabPages.Add(resultsPage);
        centerTabs.SelectedIndexChanged += (_, _) => ActivateWorkspace(centerTabs.SelectedTab);
        results.SelectedIndexChanged += (_, _) =>
        {
            if (results.SelectedItem is not Result result || activeWorkspace is null) return;
            var page = workspaces.FirstOrDefault(entry => ReferenceEquals(entry.Value, activeWorkspace)).Key;
            if (page is not null) centerTabs.SelectedTab = page;
            SelectTile(result.Tile.X, result.Tile.Y);
        };
        templates.SelectedIndexChanged += (_, _) => ApplyTemplateSelection();
        templates.DoubleClick += (_, _) => rightTabs.SelectedIndex = 0;
        spriteWorkspace.SelectedIndexChanged += (_, _) => { if (spriteWorkspace.SelectedItem is uint sprite) SelectPaletteSprite(sprite); ShowSpriteIds(spriteWorkspaceIds); };
        recentSprites.SelectedIndexChanged += (_, _) => { if (recentSprites.SelectedItem is uint sprite) SelectPaletteSprite(sprite); ShowSpriteIds(recentSpriteIds); };
        spriteBrowserStart.TextChanged += (_, _) => UpdateDefaultSpriteBrowserEnd();
        spriteBrowserEnd.TextChanged += (_, _) => { if (!updatingSpriteBrowserRange) spriteBrowserEndWasEdited = true; };
        foreach (var rangeInput in new[] { spriteBrowserStart, spriteBrowserEnd })
            rangeInput.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { BrowseSprites(); e.Handled = true; e.SuppressKeyPress = true; } };
        spriteWorkspace.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.Text) == true ? DragDropEffects.Copy : DragDropEffects.None;
        spriteWorkspace.DragDrop += (_, e) => { if (uint.TryParse(e.Data?.GetData(DataFormats.Text)?.ToString(), out var sprite)) AddSpriteToWorkspace(sprite); };

        validationPage = Tab("Validation", findings);
        spriteBrowserPage = Tab("Sprite browser", BuildSpriteBrowser());
        rightTabs.Multiline = true; rightTabs.SizeMode = TabSizeMode.Fixed; rightTabs.ItemSize = new Size(110, 24);
        rightTabs.TabPages.Add(Tab("Tile editor", BuildEditor())); rightTabs.TabPages.Add(Tab("Tile details", details)); rightTabs.TabPages.Add(validationPage); rightTabs.TabPages.Add(Tab("Templates", templates)); rightTabs.TabPages.Add(spriteBrowserPage);
        mainSplit.Panel1.Controls.Add(left);
        centerRightSplit.Panel1.Controls.Add(centerLayout);
        centerRightSplit.Panel2.Controls.Add(rightTabs);
        mainSplit.Panel2.Controls.Add(centerRightSplit);
        Controls.Add(mainSplit); Controls.Add(status); Controls.Add(menu); status.Dock = DockStyle.Bottom;
        TryLoadDefaultSpriteArchive();
        canvas.Sprites = spriteArchive;
        LoadSpriteWorkspace();
        if (startupArguments?.FirstOrDefault(File.Exists) is { } path) OpenMap(path);
    }

    private Control BuildSpriteSearchPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(6) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = "Find sprite in opened map", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Margin = new Padding(0, 0, 0, 4) }, 0, 0);
        spriteSearch.Margin = new Padding(0, 2, 0, 4);
        panel.Controls.Add(spriteSearch, 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Margin = Padding.Empty, Padding = Padding.Empty };
        var find = new Button { Text = "Find map tiles", AutoSize = true };
        find.Click += (_, _) => SearchSprites();
        var browseId = new Button { Text = "Browse ID / range", AutoSize = true };
        browseId.Click += (_, _) => BrowseSpriteSearch();
        var browseSelection = new Button { Text = "Browse selected tiles", AutoSize = true };
        browseSelection.Click += (_, _) => BrowseSelectedTiles();
        var openBrowser = new Button { Text = "Open sprite browser", AutoSize = true };
        openBrowser.Click += (_, _) => ShowSpriteBrowser();
        actions.Controls.Add(find);
        actions.Controls.Add(browseId);
        actions.Controls.Add(browseSelection);
        actions.Controls.Add(openBrowser);
        panel.Controls.Add(actions, 0, 2);
        spriteSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            SearchSprites();
            e.Handled = true;
            e.SuppressKeyPress = true;
        };
        return panel;
    }

    private MapCanvas CreateMapCanvas()
    {
        var mapCanvas = new MapCanvas
        {
            TileWidth = zoom.Value,
            ShowCollisionOverlay = collisionOverlay.Checked,
            ShowGrid = gridOverlay.Checked,
            ShowWalls = wallsOverlay.Checked,
            LowerWalls = lowerWalls.Checked,
            ShowCharacters = charactersOverlay.Checked,
            PaintMode = stampMode.Checked,
            Sprites = spriteArchive
        };
        mapCanvas.TileInteraction += (_, interaction) => HandleTileInteraction(interaction);
        mapCanvas.StrokeTile += (_, point) => PaintStrokeTile(point);
        mapCanvas.StrokeEnded += (_, _) => EndPaintStroke();
        return mapCanvas;
    }

    private Control BuildEditor()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 2, Padding = new Padding(8) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(panel, "X", x); AddRow(panel, "Y", y); AddRow(panel, "Width", width); AddRow(panel, "Height", height); AddRow(panel, "Selection", selectionSummary);
        AddRow(panel, "Ground sprite 1", gs1); AddRow(panel, "Ground sprite 2", gs2); AddRow(panel, "Foreground sprite 1", fs1); AddRow(panel, "Foreground sprite 2", fs2);
        AddRow(panel, "Paint target", paintScope);
        item.Dock = DockStyle.Fill; character.Dock = DockStyle.Fill; AddRow(panel, "Item template", item); AddRow(panel, "NPC template", character);
        editFlags.Dock = DockStyle.Top; AddRow(panel, "Static flags", editFlags);
        activeFlags.Height = 44; AddRow(panel, "Active flags", activeFlags);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Controls = { preview, apply, ToolbarButton("Delete sprites (Del)", (_, _) => DeleteSelectedSprites()), ToolbarButton("Remove NPC", (_, _) => RemoveSelectedNpcs()), ToolbarButton("Clear flags", (_, _) => ClearSelectedFlags()), undo, redo } }; AddRow(panel, "", buttons);
        return panel;
    }

    private Control BuildSpriteBrowser()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(6) };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var rangeLabel = new Label { Text = "Sprite ID or range", AutoSize = true, Padding = new Padding(0, 6, 6, 0) };
        var browse = new Button { Text = "Browse archive", AutoSize = true };
        browse.Click += (_, _) => BrowseSprites();
        var rangeSeparator = new Label { Text = "to", AutoSize = true, Padding = new Padding(2, 6, 2, 0) };
        var rangeHelp = "Enter a start ID; the end defaults to 500 IDs later. Edit the end ID to choose a custom range. Up to 2,000 sprites are shown per browse.";
        spriteBrowserToolTip.SetToolTip(spriteBrowserStart, rangeHelp);
        spriteBrowserToolTip.SetToolTip(spriteBrowserEnd, rangeHelp);
        spriteBrowserSource.Items.AddRange(["Archive ID/range", "Selection sprites", "Map Ground 1", "Map Ground 2", "Map walls / foreground", "All map sprites", "All current client art"]); spriteBrowserSource.SelectedIndex = 0;
        spriteBrowserTarget.Items.AddRange(["Ground layer 1", "Ground layer 2", "Wall / ceiling layer 1", "Wall / ceiling layer 2"]); spriteBrowserTarget.SelectedIndex = 0;
        var use = new Button { Text = "Use selected sprite", AutoSize = true }; use.Click += (_, _) => UseBrowserSprite(false);
        var copySprite = new Button { Text = "Copy selected sprite", AutoSize = true }; copySprite.Click += (_, _) => CopySelectedSprite();
        var applyBrowser = new Button { Text = "Apply preview", AutoSize = true }; applyBrowser.Click += (_, _) => ApplyEdit();
        var controls = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(2) };
        controls.Controls.Add(rangeLabel); controls.Controls.Add(spriteBrowserStart); controls.Controls.Add(rangeSeparator); controls.Controls.Add(spriteBrowserEnd); controls.Controls.Add(browse); controls.Controls.Add(showMoreSprites); controls.Controls.Add(spriteBrowserResultCount); controls.Controls.Add(spriteBrowserSource); controls.Controls.Add(new Label { Text = "Place on:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) }); controls.Controls.Add(spriteBrowserTarget); controls.Controls.Add(freeDraw); controls.Controls.Add(use); controls.Controls.Add(copySprite); controls.Controls.Add(applyBrowser);
        showMoreSprites.Click += (_, _) => RenderNextSpritePage();
        var groupButtons = new FlowLayoutPanel { AutoSize = true, Controls = { new Label { Text = "Working group: drag thumbnails here", AutoSize = true }, spriteWorkspace, Button("Add selected", (_, _) => { if (selectedBrowserSprite is { } sprite) AddSpriteToWorkspace(sprite); }), Button("Use group sprite", (_, _) => { if (spriteWorkspace.SelectedItem is uint sprite) { SelectBrowserSprite(sprite); UseBrowserSprite(false); } }) } };
        var recentBar = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty };
        recentBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); recentBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        recentBar.RowStyles.Add(new RowStyle(SizeType.AutoSize)); recentBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        recentBar.Controls.Add(new Label { Text = "Recent 5", AutoSize = true }, 0, 0);
        recentSprites.Dock = DockStyle.Fill;
        recentBar.Controls.Add(recentSprites, 0, 1);
        recentBar.Controls.Add(new Label { Text = "Selected sprite", AutoSize = true }, 1, 0);
        var selectedSpritePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = Padding.Empty, Controls = { recentlySelectedPreview, recentlySelectedId } };
        recentBar.Controls.Add(selectedSpritePanel, 1, 1);
        panel.Controls.Add(controls, 0, 0); panel.Controls.Add(groupButtons, 0, 1); panel.Controls.Add(recentBar, 0, 2); panel.Controls.Add(spriteGallery, 0, 3);
        return panel;
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control control)
    {
        var row = panel.RowCount++; panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 7, 8, 3) }, 0, row); panel.Controls.Add(control, 1, row);
    }

    private void OpenMap()
    {
        using var dialog = new OpenFileDialog { Filter = "Server map (*.map)|*.map", Title = "Open Server 3.0S or 3.5S map" };
        var currentZones = FindCurrentServerZones();
        if (currentZones is not null) dialog.InitialDirectory = currentZones;
        if (dialog.ShowDialog(this) == DialogResult.OK) OpenMap(dialog.FileName);
    }

    private void OpenMap(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var existing = workspaces.FirstOrDefault(entry => entry.Value.Map.SourcePath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
            if (existing.Key is not null)
            {
                centerTabs.SelectedTab = existing.Key;
                return;
            }
            if (workspaces.Count >= 4)
            {
                MessageBox.Show(this, "Up to four maps can be open at once. Close a map tab before opening another.", "Map tab limit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var loadedMap = MapDocument.Load(fullPath);
            SaveActiveWorkspace();
            var workspaceCanvas = CreateMapCanvas();
            workspaceCanvas.Sprites = spriteArchive;
            workspaceCanvas.Map = loadedMap;
            var workspace = new MapWorkspace(loadedMap, null, [], [], new Stack<TileChange>(), new Stack<TileChange>(), workspaceCanvas);
            var page = new TabPage(Path.GetFileName(fullPath)) { ToolTipText = fullPath };
            workspaces.Add(page, workspace);
            page.Controls.Add(workspaceCanvas);
            centerTabs.TabPages.Insert(Math.Max(0, centerTabs.TabPages.IndexOf(resultsPage!)), page);
            centerTabs.SelectedTab = page;
            ActivateWorkspace(page);
            AddRecentMap(path);
            var firstSprite = loadedMap.Tiles.Values.SelectMany(tile => tile.SpriteComponents).FirstOrDefault(sprite => sprite != 0);
            artDiagnostic = canvas.Sprites is null ? "no package selected" : canvas.Sprites.Get(firstSprite) is null ? canvas.Sprites.LastError ?? "map sprite missing from package" : $"sprite {firstSprite} decoded";
            findings.Clear(); results.Items.Clear(); details.Clear(); UpdateStatus(); SelectTile(0, 0);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open map", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveActiveWorkspace()
    {
        if (activeWorkspace is null || map is null) return;
        activeWorkspace.Map = map;
        activeWorkspace.Profile = profile;
        activeWorkspace.SelectedTiles = selectedTiles;
        activeWorkspace.FreeDrawTiles = freeDrawTiles;
        activeWorkspace.UndoStack = undoStack;
        activeWorkspace.RedoStack = redoStack;
        activeWorkspace.X = (int)x.Value;
        activeWorkspace.Y = (int)y.Value;
        activeWorkspace.Width = (int)width.Value;
        activeWorkspace.Height = (int)height.Value;
    }

    private void ActivateWorkspace(TabPage? page)
    {
        if (switchingWorkspace || page is null || !workspaces.TryGetValue(page, out var workspace) || ReferenceEquals(activeWorkspace, workspace)) return;
        SaveActiveWorkspace();
        activeWorkspace = workspace;
        map = workspace.Map;
        profile = workspace.Profile;
        canvas = workspace.Canvas;
        selectedTiles = workspace.SelectedTiles;
        freeDrawTiles = workspace.FreeDrawTiles;
        undoStack = workspace.UndoStack;
        redoStack = workspace.RedoStack;
        strokeBefore = [];
        LoadTemplates(Path.GetDirectoryName(map.SourcePath)!);
        canvas.CharacterSprites = characterSprites;
        canvas.ItemSprites = itemSprites;
        canvas.Sprites = spriteArchive;
        canvas.TileWidth = zoom.Value;
        canvas.ShowCollisionOverlay = collisionOverlay.Checked;
        canvas.ShowGrid = gridOverlay.Checked;
        canvas.ShowWalls = wallsOverlay.Checked;
        canvas.LowerWalls = lowerWalls.Checked;
        canvas.ShowCharacters = charactersOverlay.Checked;
        canvas.PaintMode = stampMode.Checked;
        canvas.SetSelection(selectedTiles);
        var selected = selectedTiles.OrderBy(point => point.Y).ThenBy(point => point.X).FirstOrDefault(new Point(workspace.X, workspace.Y));
        canvas.SelectedTile = selected;
        x.Value = workspace.X; y.Value = workspace.Y;
        width.Value = workspace.Width; height.Value = workspace.Height;
        if (profile is null) TryDetectProfile(map.SourcePath);
        PopulateProfileLists();
        if (map.Tiles.TryGetValue((selected.X, selected.Y), out var tile))
        {
            LoadTile(tile);
            ShowDetails(tile);
        }
        if (BrushPreviewActive() && freeDrawTiles.Count > 0) PreviewFreeDraw();
        else canvas.ClearPreview();
        var firstSprite = map.Tiles.Values.SelectMany(tile => tile.SpriteComponents).FirstOrDefault(sprite => sprite != 0);
        artDiagnostic = canvas.Sprites is null ? "no package selected" : canvas.Sprites.Get(firstSprite) is null ? canvas.Sprites.LastError ?? "map sprite missing from package" : $"sprite {firstSprite} decoded";
        UpdateStatus();
    }

    private void CloseCurrentMapTab()
    {
        if (centerTabs.SelectedTab is not { } page || !workspaces.ContainsKey(page)) return;
        var closingActive = ReferenceEquals(workspaces[page], activeWorkspace);
        if (closingActive) SaveActiveWorkspace();
        var removedWorkspace = workspaces[page];
        switchingWorkspace = true;
        workspaces.Remove(page);
        centerTabs.TabPages.Remove(page);
        page.Controls.Remove(removedWorkspace.Canvas);
        page.Dispose();
        removedWorkspace.Canvas.Dispose();
        switchingWorkspace = false;
        if (closingActive)
        {
            activeWorkspace = null;
            map = null;
            profile = null;
            selectedTiles = [];
            freeDrawTiles = [];
            undoStack = new Stack<TileChange>();
            redoStack = new Stack<TileChange>();
            strokeBefore = [];
            if (centerTabs.SelectedTab is { } next && workspaces.ContainsKey(next)) ActivateWorkspace(next);
            else
            {
                canvas = CreateMapCanvas();
                canvas.Sprites = spriteArchive;
                findings.Clear(); details.Clear(); UpdateStatus();
            }
        }
    }

    private void AddRecentMap(string path)
    {
        var fullPath = Path.GetFullPath(path);
        recentMapPaths.RemoveAll(recent => recent.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
        recentMapPaths.Insert(0, fullPath);
        if (recentMapPaths.Count > 3) recentMapPaths.RemoveRange(3, recentMapPaths.Count - 3);
        RefreshRecentMapsMenu();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(recentMapsPath)!);
            File.WriteAllText(recentMapsPath, JsonSerializer.Serialize(recentMapPaths, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private void RefreshRecentMapsMenu()
    {
        if (recentMapsMenu is null) return;
        recentMapsMenu.DropDownItems.Clear();
        recentMapPaths.RemoveAll(path => !File.Exists(path));
        foreach (var path in recentMapPaths.Take(3))
        {
            var entry = new ToolStripMenuItem(Path.GetFileName(path)) { ToolTipText = path };
            entry.Click += (_, _) => OpenMap(path);
            recentMapsMenu.DropDownItems.Add(entry);
        }
        if (recentMapsMenu.DropDownItems.Count == 0)
            recentMapsMenu.DropDownItems.Add(new ToolStripMenuItem("(No recent maps)") { Enabled = false });
    }

    private void LoadRecentMaps()
    {
        try
        {
            if (!File.Exists(recentMapsPath)) return;
            var paths = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(recentMapsPath));
            if (paths is null) return;
            foreach (var path in paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).Take(3))
                recentMapPaths.Add(Path.GetFullPath(path));
        }
        catch { }
    }

    private void SelectTile(int tileX, int tileY, bool loadPaintInputs = true)
    {
        if (map is null) return;
        selectedTiles.Clear(); selectedTiles.Add(new Point(tileX, tileY)); canvas.SetSelection(selectedTiles); canvas.SelectedTile = new Point(tileX, tileY);
        if (!BrushPreviewActive()) { freeDrawTiles.Clear(); canvas.ClearPreview(); }
        x.Value = tileX; y.Value = tileY; width.Value = 1; height.Value = 1;
        var tile = map.GetTile(tileX, tileY); LoadTile(tile, loadPaintInputs); ShowDetails(tile);
        UpdateSelectionSummary();
    }

    private void HandleTileInteraction(TileInteraction interaction)
    {
        if (map is null) return;
        if ((interaction.Modifiers & Keys.Alt) != 0 && BrushPreviewActive() && interaction.SelectionStart is { } previewStart)
        {
            AddFreeDrawTiles(RectanglePoints(previewStart, interaction.Tile));
            return;
        }
        if (placeSpriteMode.Checked && selectedBrowserSprite is not null && (interaction.Modifiers & (Keys.Control | Keys.Shift)) == 0)
        {
            AddFreeDrawTiles([interaction.Tile]);
            SelectTile(interaction.Tile.X, interaction.Tile.Y, false);
            status.Text = $"Previewing sprite {selectedBrowserSprite} at ({interaction.Tile.X},{interaction.Tile.Y}). Apply to commit.";
            return;
        }
        if ((interaction.Modifiers & Keys.Shift) != 0) { SelectMatchingSprite(interaction.Tile); return; }
        if ((interaction.Modifiers & Keys.Control) != 0 && interaction.SelectionStart is { } start)
        {
            SelectSection(start, interaction.Tile);
            return;
        }
        SelectTile(interaction.Tile.X, interaction.Tile.Y);
    }

    private static Point[] RectanglePoints(Point anchor, Point tile)
    {
        anchor = ClampMapPoint(anchor); tile = ClampMapPoint(tile);
        var minX = Math.Min(anchor.X, tile.X); var maxX = Math.Max(anchor.X, tile.X);
        var minY = Math.Min(anchor.Y, tile.Y); var maxY = Math.Max(anchor.Y, tile.Y);
        return Enumerable.Range(minY, maxY - minY + 1).SelectMany(row => Enumerable.Range(minX, maxX - minX + 1).Select(column => new Point(column, row))).ToArray();
    }

    private void SelectSection(Point anchor, Point tile)
    {
        var section = RectanglePoints(anchor, tile);
        SelectTiles(section, "section");
    }

    private static Point ClampMapPoint(Point point) => new(Math.Clamp(point.X, 0, 255), Math.Clamp(point.Y, 0, 255));

    private void SelectMatchingSprite(Point tilePoint)
    {
        var clickedTile = map!.GetTile(tilePoint.X, tilePoint.Y);
        var sprite = PreferredSprite(clickedTile);
        if (sprite == 0) { SelectTile(tilePoint.X, tilePoint.Y); status.Text = "The selected tile has no sprite to match."; return; }
        var foregroundMatch = (clickedTile.ForegroundSprite & 0xffff) == sprite || (clickedTile.ForegroundSprite >> 16) == sprite;
        var matches = map.Tiles.Values.Where(tile => foregroundMatch
            ? (tile.ForegroundSprite & 0xffff) == sprite || (tile.ForegroundSprite >> 16) == sprite
            : (tile.GroundSprite & 0xffff) == sprite || (tile.GroundSprite >> 16) == sprite).Select(tile => new Point(tile.X, tile.Y));
        SelectTiles(matches, $"sprite {sprite} on {(foregroundMatch ? "foreground" : "ground")}");
    }

    private void SelectTiles(IEnumerable<Point> tiles, string kind)
    {
        selectedTiles.Clear(); selectedTiles.UnionWith(tiles.Select(ClampMapPoint));
        if (selectedTiles.Count == 0) return;
        var minX = selectedTiles.Min(tile => tile.X); var maxX = selectedTiles.Max(tile => tile.X);
        var minY = selectedTiles.Min(tile => tile.Y); var maxY = selectedTiles.Max(tile => tile.Y);
        x.Value = minX; y.Value = minY; width.Value = maxX - minX + 1; height.Value = maxY - minY + 1;
        canvas.SetSelection(selectedTiles); canvas.SelectedTile = new Point(minX, minY);
        if (!BrushPreviewActive()) canvas.ClearPreview();
        LoadTile(map!.GetTile(minX, minY), !freeDraw.Checked && !placeSpriteMode.Checked); ShowDetails(map.GetTile(minX, minY));
        UpdateSelectionSummary();
        status.Text = $"{selectedTiles.Count:N0} tiles selected by {kind}. Edit fields, then apply to the selection.";
    }

    private static ushort PreferredSprite(MapTile tile)
    {
        return new[]
        {
            (ushort)(tile.ForegroundSprite & 0xffff), (ushort)(tile.ForegroundSprite >> 16),
            (ushort)(tile.GroundSprite & 0xffff), (ushort)(tile.GroundSprite >> 16)
        }.FirstOrDefault(value => value != 0);
    }

    private void NavigateSelection(KeyEventArgs e)
    {
        if (map is null || e.KeyCode is not (Keys.Left or Keys.Right or Keys.Up or Keys.Down)) return;
        var nextX = (int)x.Value;
        var nextY = (int)y.Value;
        switch (e.KeyCode)
        {
            case Keys.Left: nextX--; break;
            case Keys.Right: nextX++; break;
            case Keys.Up: nextY--; break;
            case Keys.Down: nextY++; break;
        }
        if (nextX is < 0 or > 255 || nextY is < 0 or > 255) return;
        SelectTile(nextX, nextY);
        e.Handled = true;
    }

    private void HandleKeyCommand(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && IsMapWorkspaceSelected() && !IsTextInputFocused())
        {
            DeleteSelectedSprites();
            e.SuppressKeyPress = true;
            return;
        }
        if (e.Control && e.KeyCode == Keys.Z)
        {
            if (e.Shift) Redo(); else Undo();
            e.SuppressKeyPress = true;
            return;
        }
        if (e.Control && e.KeyCode == Keys.Y)
        {
            Redo();
            e.SuppressKeyPress = true;
            return;
        }
        NavigateSelection(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Delete && IsMapWorkspaceSelected() && !IsTextInputFocused())
        {
            DeleteSelectedSprites();
            return true;
        }
        if (keyData == (Keys.Control | Keys.Shift | Keys.C) && IsMapWorkspaceSelected() && !IsTextInputFocused())
        {
            CopyTile();
            return true;
        }
        if (keyData == (Keys.Control | Keys.Shift | Keys.V) && IsMapWorkspaceSelected() && !IsTextInputFocused())
        {
            PasteTile();
            return true;
        }
        if (keyData == (Keys.Control | Keys.C) && rightTabs.SelectedTab == spriteBrowserPage && !IsTextInputFocused())
        {
            CopySelectedSprite();
            return true;
        }
        if (keyData == (Keys.Control | Keys.C) && IsMapWorkspaceSelected() && !IsTextInputFocused())
        {
            CopySelectedRegion();
            return true;
        }
        if (keyData == (Keys.Control | Keys.V) && IsMapWorkspaceSelected() && !IsTextInputFocused())
        {
            PasteCopiedRegion();
            return true;
        }
        if (keyData == (Keys.Control | Keys.X) && IsMapWorkspaceSelected() && !IsTextInputFocused())
        {
            CutSelectedRegion();
            return true;
        }
        if (keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            NavigateSelection(new KeyEventArgs(keyData));
            return true;
        }
        if (keyData == (Keys.Control | Keys.Z)) { Undo(); return true; }
        if (keyData is (Keys.Control | Keys.Y) or (Keys.Control | Keys.Shift | Keys.Z)) { Redo(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private bool IsTextInputFocused()
    {
        return FindFocusedControl(this) is TextBoxBase or ComboBox or NumericUpDown;
    }

    private bool IsMapWorkspaceSelected() =>
        centerTabs.SelectedTab is { } page && workspaces.ContainsKey(page);

    private static Control? FindFocusedControl(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (!child.ContainsFocus) continue;
            return FindFocusedControl(child) ?? child;
        }
        return parent.Focused ? parent : null;
    }

    private void CopyTile()
    {
        if (!RequireMap()) return;
        copiedTile = TileSnapshot.Capture(map!.GetTile((int)x.Value, (int)y.Value));
        copiedRegion = null;
        copiedSprite = null;
        status.Text = $"Copied tile ({x.Value},{y.Value}) with all four layers, item, NPC, and flags. Paste it (Ctrl+Shift+V) or use Brush mode to paint it.";
    }

    private void PasteTile()
    {
        if (!RequireEditable()) return;
        if (copiedTile is null) { status.Text = "Copy a tile first (Edit > Copy tile, Ctrl+Shift+C)."; return; }
        StampCopiedTile(selectedTiles.Count > 1 ? selectedTiles.ToArray() : [new Point((int)x.Value, (int)y.Value)]);
    }

    private void StampCopiedTile(IReadOnlyCollection<Point> targets)
    {
        if (!RequireEditable() || copiedTile is null || targets.Count == 0) return;
        var before = targets.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        foreach (var point in targets) copiedTile.Restore(map!.GetTile(point.X, point.Y));
        var after = targets.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        undoStack.Push(new TileChange(before, after));
        redoStack.Clear();
        canvas.SetSelection(targets);
        var first = targets.First();
        LoadTile(map!.GetTile(first.X, first.Y), false);
        ShowDetails(map.GetTile(first.X, first.Y));
        canvas.RefreshTile();
        UpdateStatus();
        status.Text = $"Stamped copied tile onto {targets.Count:N0} tile(s). Ctrl+Z undoes the whole stamp.";
    }

    private void PaintStrokeTile(Point point)
    {
        if (map is null || point.X is < 0 or > 255 || point.Y is < 0 or > 255) return;
        if (copiedTile is null) { status.Text = "Copy a tile first (Edit > Copy tile, Ctrl+Shift+C), then paint."; return; }
        if (!map.CanEdit) { status.Text = "This map is inspection-only and cannot be painted."; return; }
        if (strokeBefore.ContainsKey(point)) return;
        var tile = map.GetTile(point.X, point.Y);
        strokeBefore[point] = TileSnapshot.Capture(tile);
        copiedTile.Restore(tile);
        canvas.RefreshTile();
    }

    private void EndPaintStroke()
    {
        if (map is null || strokeBefore.Count == 0) return;
        var before = strokeBefore.Values.ToList();
        var after = before.Select(snapshot => TileSnapshot.Capture(map.GetTile(snapshot.X, snapshot.Y))).ToList();
        strokeBefore.Clear();
        undoStack.Push(new TileChange(before, after));
        redoStack.Clear();
        UpdateStatus();
        status.Text = $"Painted {before.Count:N0} tile(s). Ctrl+Z undoes the whole stroke.";
    }

    private static Bitmap BrushIcon()
    {
        var icon = new Bitmap(20, 20);
        using var g = Graphics.FromImage(icon);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var handle = new Pen(Color.SaddleBrown, 3) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        g.DrawLine(handle, 17, 2, 9, 10);
        using var ferrule = new Pen(Color.Silver, 4);
        g.DrawLine(ferrule, 9, 10, 7, 12);
        using var bristles = new SolidBrush(Color.RoyalBlue);
        g.FillEllipse(bristles, 2, 12, 7, 7);
        return icon;
    }

    private void CopyRegionToFile()
    {
        if (!RequireMap() || selectedTiles.Count == 0) return;
        var region = CaptureSelectedRegion();
        if (region is null) return;
        using var dialog = new SaveFileDialog { Filter = "Map region (*.mapregion.json)|*.mapregion.json", FileName = "region.mapregion.json", Title = "Copy selected region" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(region, new JsonSerializerOptions { WriteIndented = true }));
        status.Text = $"Copied {region.Width}x{region.Height} region to {Path.GetFileName(dialog.FileName)}.";
    }

    private void CopySelectedRegion()
    {
        if (!RequireMap() || selectedTiles.Count == 0) return;
        copiedRegion = CaptureSelectedRegion();
        copiedTile = null;
        copiedSprite = null;
        if (copiedRegion is not null)
            status.Text = $"Copied {copiedRegion.Width}x{copiedRegion.Height} region, including empty tiles. Ctrl+V replaces the destination region; Ctrl+Z undoes it.";
    }

    private void CopySelectedSprite()
    {
        if (selectedBrowserSprite is not { } sprite)
        {
            status.Text = "Select a sprite thumbnail first.";
            return;
        }
        copiedSprite = new SpriteClipboard(sprite, Math.Clamp(spriteBrowserTarget.SelectedIndex, 0, 3));
        copiedTile = null;
        copiedRegion = null;
        status.Text = $"Copied sprite {sprite} to the local clipboard for {spriteBrowserTarget.SelectedItem}. Select a map tile and press Ctrl+V to place it.";
    }

    private MapRegionClipboard? CaptureSelectedRegion()
    {
        if (map is null || selectedTiles.Count == 0) return null;
        var minX = selectedTiles.Min(tile => tile.X); var minY = selectedTiles.Min(tile => tile.Y);
        var maxX = selectedTiles.Max(tile => tile.X); var maxY = selectedTiles.Max(tile => tile.Y);
        return new MapRegionClipboard
        {
            Width = maxX - minX + 1,
            Height = maxY - minY + 1,
            Tiles = Enumerable.Range(minY, maxY - minY + 1).SelectMany(row => Enumerable.Range(minX, maxX - minX + 1).Select(column =>
            {
                var tile = map.GetTile(column, row);
                return new MapRegionTile { X = column - minX, Y = row - minY, GroundSprite = tile.GroundSprite, ForegroundSprite = tile.ForegroundSprite, Item = tile.Item, Character = tile.Character, Flags = tile.Flags.ToList() };
            })).ToList()
        };
    }

    private void PasteCopiedRegion()
    {
        if (!RequireEditable()) return;
        if (copiedSprite is { } sprite)
        {
            PasteSprite(sprite);
            return;
        }
        if (copiedRegion is null) { status.Text = "Copy a selected region first (Ctrl+C)."; return; }
        try { PasteRegion(copiedRegion); }
        catch (InvalidDataException ex) { MessageBox.Show(this, ex.Message, "Could not paste region", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void PasteSprite(SpriteClipboard clipboard)
    {
        var targets = GetEditTargets();
        var before = targets.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        foreach (var point in targets)
        {
            var tile = map!.GetTile(point.X, point.Y);
            switch (clipboard.Layer)
            {
                case 0: tile.GroundSprite = (tile.GroundSprite & 0xffff0000) | (clipboard.Sprite & 0xffff); break;
                case 1: tile.GroundSprite = (tile.GroundSprite & 0x0000ffff) | (clipboard.Sprite & 0xffff) << 16; break;
                case 2: tile.ForegroundSprite = (tile.ForegroundSprite & 0xffff0000) | (clipboard.Sprite & 0xffff); break;
                case 3: tile.ForegroundSprite = (tile.ForegroundSprite & 0x0000ffff) | (clipboard.Sprite & 0xffff) << 16; break;
            }
        }
        var after = targets.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        undoStack.Push(new TileChange(before, after));
        redoStack.Clear();
        freeDrawTiles.Clear();
        canvas.ClearPreview();
        canvas.RefreshTile();
        LoadTile(map!.GetTile((int)x.Value, (int)y.Value), false);
        ShowDetails(map.GetTile((int)x.Value, (int)y.Value));
        UpdateStatus();
        status.Text = $"Pasted sprite {clipboard.Sprite} onto {targets.Length:N0} tile(s) in {spriteBrowserTarget.Items[clipboard.Layer]}. Ctrl+Z undoes the paste.";
    }

    private void PasteRegionFromFile()
    {
        if (!RequireEditable()) return;
        using var dialog = new OpenFileDialog { Filter = "Map region (*.mapregion.json)|*.mapregion.json", Title = "Paste region at selected tile" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var region = JsonSerializer.Deserialize<MapRegionClipboard>(File.ReadAllText(dialog.FileName)) ?? throw new InvalidDataException("The region file is empty.");
            PasteRegion(region);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not paste region", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void PasteRegion(MapRegionClipboard region)
    {
        ValidateRegion(region);
        var originX = selectedTiles.Count == 0 ? (int)x.Value : selectedTiles.Min(tile => tile.X);
        var originY = selectedTiles.Count == 0 ? (int)y.Value : selectedTiles.Min(tile => tile.Y);
        if (originX + region.Width > 256 || originY + region.Height > 256)
            throw new InvalidDataException("The region does not fit at the selected origin.");

        var before = Enumerable.Range(0, region.Height)
            .SelectMany(row => Enumerable.Range(0, region.Width)
                .Select(column => TileSnapshot.Capture(map!.GetTile(originX + column, originY + row))))
            .ToList();
        foreach (var tile in region.Tiles)
        {
            var target = map!.GetTile(originX + tile.X, originY + tile.Y);
            target.GroundSprite = tile.GroundSprite;
            target.ForegroundSprite = tile.ForegroundSprite;
            target.Item = tile.Item;
            target.Character = tile.Character;
            target.Flags.Clear();
            foreach (var flag in tile.Flags ?? []) target.Flags.Add(flag);
        }

        var after = Enumerable.Range(0, region.Height)
            .SelectMany(row => Enumerable.Range(0, region.Width)
                .Select(column => TileSnapshot.Capture(map!.GetTile(originX + column, originY + row))))
            .ToList();
        undoStack.Push(new TileChange(before, after));
        redoStack.Clear();
        SelectTiles(RectanglePoints(new Point(originX, originY), new Point(originX + region.Width - 1, originY + region.Height - 1)), "pasted region");
        UpdateStatus();
        status.Text = $"Pasted {region.Width}x{region.Height} region as a full overwrite. Ctrl+Z undoes the whole paste.";
    }

    private void CutSelectedRegion()
    {
        if (!RequireEditable() || selectedTiles.Count == 0) return;
        var region = CaptureSelectedRegion();
        if (region is null) return;
        var originX = selectedTiles.Min(tile => tile.X);
        var originY = selectedTiles.Min(tile => tile.Y);
        var points = RectanglePoints(new Point(originX, originY), new Point(originX + region.Width - 1, originY + region.Height - 1));
        var before = points.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        foreach (var point in points) map!.GetTile(point.X, point.Y).Clear();
        var after = points.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        copiedRegion = region;
        copiedTile = null;
        copiedSprite = null;
        undoStack.Push(new TileChange(before, after));
        redoStack.Clear();
        canvas.RefreshTile();
        UpdateStatus();
        status.Text = $"Cut {region.Width}x{region.Height} region to the local clipboard. Select a destination and press Ctrl+V to move it; Ctrl+Z undoes the cut.";
    }

    private static void ValidateRegion(MapRegionClipboard region)
    {
        if (region.Width is < 1 or > 256 || region.Height is < 1 or > 256 || region.Tiles is null)
            throw new InvalidDataException("The region dimensions or tile list are invalid.");
        if (region.Tiles.Count != region.Width * region.Height)
            throw new InvalidDataException("The region must contain every tile, including empty tiles.");
        var coordinates = new HashSet<Point>();
        foreach (var tile in region.Tiles)
        {
            if (tile.X < 0 || tile.X >= region.Width || tile.Y < 0 || tile.Y >= region.Height || !coordinates.Add(new Point(tile.X, tile.Y)))
                throw new InvalidDataException("The region contains an invalid or duplicate tile coordinate.");
        }
    }

    private void ClearSelectedRegion()
    {
        if (!RequireEditable() || selectedTiles.Count == 0) return;
        var before = selectedTiles.Select(tile => TileSnapshot.Capture(map!.GetTile(tile.X, tile.Y))).ToList();
        foreach (var tile in selectedTiles) map!.GetTile(tile.X, tile.Y).Clear();
        var after = selectedTiles.Select(tile => TileSnapshot.Capture(map!.GetTile(tile.X, tile.Y))).ToList();
        undoStack.Push(new TileChange(before, after)); redoStack.Clear(); canvas.RefreshTile(); UpdateStatus();
    }

    private void LoadTile(MapTile tile, bool loadPaintInputs = true)
    {
        if (loadPaintInputs)
        {
            gs1.Value = (ushort)(tile.GroundSprite & 0xffff); gs2.Value = (ushort)(tile.GroundSprite >> 16); fs1.Value = (ushort)(tile.ForegroundSprite & 0xffff); fs2.Value = (ushort)(tile.ForegroundSprite >> 16);
            item.Text = tile.Item ?? ""; character.Text = tile.Character ?? "";
        }
        activeFlags.Text = tile.Flags.Count == 0 ? "(none)" : string.Join(Environment.NewLine, tile.Flags.OrderBy(flag => flag));
        for (var i = 0; i < editFlags.Items.Count; i++)
        {
            var flag = (string)editFlags.Items[i]!;
            var count = selectedTiles.Count(selected => map!.GetTile(selected.X, selected.Y).Flags.Contains(flag));
            editFlags.SetItemCheckState(i, count == 0 ? CheckState.Unchecked : count == selectedTiles.Count ? CheckState.Checked : CheckState.Indeterminate);
        }
    }

    private void ApplyEdit()
    {
        if (!RequireEditable()) return;
        var targets = GetEditTargets();
        var before = targets.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        var after = PreviewSnapshots(targets);
        var summary = BuildChangeSummary(before, after, targets.Length);
        if ((targets.Length > 1 || paintScope.SelectedIndex == 0) && MessageBox.Show(this, summary + "\r\n\r\nApply this change?", "Confirm map edit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        redoStack.Clear();
        if (freeDraw.Checked && before.Count > 1)
        {
            for (var index = 0; index < before.Count; index++)
            {
                var state = before[index];
                var target = map!.GetTile(state.X, state.Y);
                after[index].Restore(target);
                undoStack.Push(new TileChange([state], [after[index]]));
            }
        }
        else
        {
            for (var index = 0; index < before.Count; index++) after[index].Restore(map!.GetTile(before[index].X, before[index].Y));
            undoStack.Push(new TileChange(before, after));
        }
        var committedTiles = targets.Length;
        freeDrawTiles.Clear(); canvas.ClearPreview(); canvas.RefreshTile();
        var selectedTile = map!.GetTile((int)x.Value, (int)y.Value);
        LoadTile(selectedTile);
        ShowDetails(selectedTile);
        status.Text = $"Committed {committedTiles:N0} tile(s).";
        UpdateStatus();
    }

    private void DeleteSelectedSprites() => MutateSelectedTiles("Deleted sprites", tile =>
    {
        tile.GroundSprite = 0;
        tile.ForegroundSprite = 0;
    });

    private void RemoveSelectedNpcs() => MutateSelectedTiles("Removed NPCs", tile => tile.Character = null);

    private void ClearSelectedFlags() => MutateSelectedTiles("Cleared flags", tile => tile.Flags.Clear());

    private void MutateSelectedTiles(string operation, Action<MapTile> mutate)
    {
        if (!RequireEditable()) return;
        var targets = GetEditTargets();
        var before = targets.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        foreach (var point in targets) mutate(map!.GetTile(point.X, point.Y));
        var after = targets.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        if (before.Zip(after).All(pair => pair.First.Matches(pair.Second)))
        {
            status.Text = $"{operation}: no matching data on the selected tile(s).";
            return;
        }
        undoStack.Push(new TileChange(before, after));
        redoStack.Clear();
        freeDrawTiles.Clear();
        canvas.ClearPreview();
        canvas.RefreshTile();
        var selectedTile = map!.GetTile((int)x.Value, (int)y.Value);
        LoadTile(selectedTile);
        ShowDetails(selectedTile);
        UpdateStatus();
        status.Text = $"{operation} on {targets.Length:N0} tile(s). Ctrl+Z undoes this change.";
    }

    private void PreviewEdit()
    {
        if (!RequireEditable()) return;
        var targets = GetEditTargets();
        var before = targets.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        rightTabs.SelectedIndex = 0;
        canvas.SetPreview(PreviewSnapshots(targets).Select(snapshot => new MapTile { X = snapshot.X, Y = snapshot.Y, Item = snapshot.Item, Character = snapshot.Character }));
        MessageBox.Show(this, BuildChangeSummary(before, PreviewSnapshots(targets), targets.Length), "Map edit preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private Point[] GetEditTargets()
    {
        if (BrushPreviewActive() && freeDrawTiles.Count > 0) return freeDrawTiles.ToArray();
        return selectedTiles.Count > 1
            ? selectedTiles.ToArray()
            : Enumerable.Range((int)y.Value, (int)height.Value).SelectMany(row => Enumerable.Range((int)x.Value, (int)width.Value).Select(column => new Point(column, row))).ToArray();
    }

    private List<TileSnapshot> PreviewSnapshots(IEnumerable<Point> targets)
    {
        return targets.Select(point =>
        {
            var source = map!.GetTile(point.X, point.Y);
            var copy = new MapTile { X = source.X, Y = source.Y, GroundSprite = source.GroundSprite, ForegroundSprite = source.ForegroundSprite, Item = source.Item, Character = source.Character };
            foreach (var flag in source.Flags) copy.Flags.Add(flag);
            ApplyValues(copy);
            return TileSnapshot.Capture(copy);
        }).ToList();
    }

    private void PreviewFreeDraw()
    {
        if (!RequireMap() || freeDrawTiles.Count == 0) return;
        canvas.SetPreview(PreviewMaps(freeDrawTiles));
    }

    private bool BrushPreviewActive() => freeDraw.Checked || placeSpriteMode.Checked;

    private IEnumerable<MapTile> PreviewMaps(IEnumerable<Point> targets)
    {
        return targets.Select(point =>
        {
            var source = map!.GetTile(point.X, point.Y);
            var copy = new MapTile { X = source.X, Y = source.Y, GroundSprite = source.GroundSprite, ForegroundSprite = source.ForegroundSprite, Item = source.Item, Character = source.Character };
            foreach (var flag in source.Flags) copy.Flags.Add(flag);
            ApplyValues(copy);
            return copy;
        });
    }

    private void AddFreeDrawTiles(IEnumerable<Point> tiles)
    {
        if (!RequireMap()) return;
        foreach (var tile in tiles) freeDrawTiles.Add(ClampMapPoint(tile));
        PreviewFreeDraw();
    }

    private string BuildChangeSummary(IReadOnlyList<TileSnapshot> before, IReadOnlyList<TileSnapshot> after, int targetCount)
    {
        var changed = before.Zip(after).Count(pair => !pair.First.Matches(pair.Second));
        var pairs = before.Zip(after).ToArray();
        var ground = pairs.Count(pair => pair.First.Ground != pair.Second.Ground);
        var foreground = pairs.Count(pair => pair.First.Foreground != pair.Second.Foreground);
        var items = pairs.Count(pair => pair.First.Item != pair.Second.Item);
        var characters = pairs.Count(pair => pair.First.Character != pair.Second.Character);
        var flags = pairs.Count(pair => !pair.First.Flags.OrderBy(flag => flag).SequenceEqual(pair.Second.Flags.OrderBy(flag => flag), StringComparer.OrdinalIgnoreCase));
        var target = paintScope.SelectedItem?.ToString() ?? "unknown operation";
        var warning = paintScope.SelectedIndex == 0 ? "WARNING: Whole tile replacement can overwrite ground, walls, flags, items, and NPCs." : "Untouched layers will be preserved.";
        return $"Operation: {target}\r\nTiles selected: {targetCount:N0}\r\nTiles changing: {changed:N0}\r\nDiff: ground {ground:N0}, walls {foreground:N0}, items {items:N0}, NPCs {characters:N0}, flags {flags:N0}\r\nAffected layer: {AffectedLayerName()}\r\n{warning}";
    }

    private string AffectedLayerName() => paintScope.SelectedIndex switch
    {
        0 => "all tile data",
        1 or 2 => "ground",
        3 or 4 => "walls / ceilings",
        5 or 11 => "static flags",
        6 => "all tile data",
        7 or 8 => "ground",
        9 or 10 => "walls / ceilings",
        12 => "item reference",
        13 => "NPC reference",
        _ => "selected operation"
    };

    private void UpdateSelectionSummary()
    {
        var count = selectedTiles.Count > 1 ? selectedTiles.Count : (int)width.Value * (int)height.Value;
        selectionSummary.Text = $"{count:N0} tile(s) | affects: {AffectedLayerName()}";
    }

    private void ApplyValues(MapTile tile)
    {
        uint ground1 = (ushort)gs1.Value, ground2 = (ushort)gs2.Value, foreground1 = (ushort)fs1.Value, foreground2 = (ushort)fs2.Value;
        switch (paintScope.SelectedIndex)
        {
            case 1: tile.GroundSprite = (tile.GroundSprite & 0xffff0000) | ground1; break;
            case 2: tile.GroundSprite = (tile.GroundSprite & 0x0000ffff) | (ground2 << 16); break;
            case 3: tile.ForegroundSprite = (tile.ForegroundSprite & 0xffff0000) | foreground1; break;
            case 4: tile.ForegroundSprite = (tile.ForegroundSprite & 0x0000ffff) | (foreground2 << 16); break;
            case 5: SetFlags(tile); break;
            case 6: tile.GroundSprite = tile.ForegroundSprite = 0; tile.Item = null; tile.Character = null; tile.Flags.Clear(); break;
            case 7: tile.GroundSprite &= 0xffff0000; break;
            case 8: tile.GroundSprite &= 0x0000ffff; break;
            case 9: tile.ForegroundSprite &= 0xffff0000; break;
            case 10: tile.ForegroundSprite &= 0x0000ffff; break;
            case 11: tile.Flags.Clear(); break;
            case 12: tile.Item = string.IsNullOrWhiteSpace(item.Text) ? null : item.Text.Trim(); break;
            case 13: tile.Character = string.IsNullOrWhiteSpace(character.Text) ? null : character.Text.Trim(); break;
            default:
                tile.GroundSprite = ground1 | (ground2 << 16); tile.ForegroundSprite = foreground1 | (foreground2 << 16);
                tile.Item = string.IsNullOrWhiteSpace(item.Text) ? null : item.Text.Trim(); tile.Character = string.IsNullOrWhiteSpace(character.Text) ? null : character.Text.Trim(); SetFlags(tile); break;
        }
    }

    private void SetFlags(MapTile tile)
    {
        for (var i = 0; i < editFlags.Items.Count; i++)
        {
            var flag = (string)editFlags.Items[i]!;
            switch (editFlags.GetItemCheckState(i))
            {
                case CheckState.Checked: tile.Flags.Add(flag); break;
                case CheckState.Unchecked: tile.Flags.Remove(flag); break;
            }
        }
    }

    private void Undo() { if (undoStack.Count == 0 || map is null) return; var change = undoStack.Pop(); Restore(change.Before); redoStack.Push(change); }
    private void Redo() { if (redoStack.Count == 0 || map is null) return; var change = redoStack.Pop(); Restore(change.After); undoStack.Push(change); }
    private void Restore(IEnumerable<TileSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots) snapshot.Restore(map!.GetTile(snapshot.X, snapshot.Y));
        canvas.RefreshTile();
        LoadTile(map!.GetTile((int)x.Value, (int)y.Value));
        UpdateStatus();
    }

    private void SearchSprites()
    {
        if (!RequireMap()) return; if (!TryParseRange(spriteSearch.Text, out var low, out var high)) { spriteSearch.Focus(); MessageBox.Show(this, "Enter a sprite ID or range in the Sprite ID / range box, for example 21006."); return; }
        ShowTiles(map!.Tiles.Values.Where(t => t.SpriteComponents.Any(sprite => sprite >= low && sprite <= high)), "sprite");
    }

    private void BrowseSpriteSearch()
    {
        if (!TryParseRange(spriteSearch.Text, out var low, out var high)) { spriteSearch.Focus(); MessageBox.Show(this, "Enter a sprite ID or range in the Sprite ID / range box, for example 21006."); return; }
        if ((ulong)high - low + 1 > MaxSpriteBrowserResults) { MessageBox.Show(this, $"Browse at most {MaxSpriteBrowserResults:N0} sprite IDs at a time."); return; }
        spriteBrowserEndWasEdited = true;
        spriteBrowserStart.Text = low.ToString();
        spriteBrowserEnd.Text = high.ToString();
        spriteBrowserSource.SelectedIndex = 0;
        ShowSpriteBrowser();
        BrowseSprites();
        SelectBrowserSprite(low);
    }

    private void BrowseSelectedTiles()
    {
        if (!RequireMap()) return;
        spriteBrowserSource.SelectedIndex = 1;
        ShowSpriteBrowser();
        BrowseSprites();
    }

    private void BrowseSprites()
    {
        if (canvas.Sprites is null) { MessageBox.Show(this, "Choose a legacy pak folder first."); return; }
        IEnumerable<uint> ids;
        if (spriteBrowserSource.SelectedIndex == 1)
        {
            if (map is null) { MessageBox.Show(this, "Open a map and select a tile or region first."); return; }
            ids = GetSelectionSpriteIds();
        }
        else if (spriteBrowserSource.SelectedIndex >= 2 && spriteBrowserSource.SelectedIndex <= 5)
        {
            if (map is null) { MessageBox.Show(this, "Open a map before browsing its ground sprites."); return; }
            ids = (spriteBrowserSource.SelectedIndex - 1) switch
            {
                1 => map.Tiles.Values.Select(tile => tile.GroundSprite & 0xffff),
                2 => map.Tiles.Values.Select(tile => tile.GroundSprite >> 16),
                3 => map.Tiles.Values.SelectMany(tile => new[] { tile.ForegroundSprite & 0xffff, tile.ForegroundSprite >> 16 }),
                _ => map.Tiles.Values.SelectMany(tile => tile.SpriteComponents).Select(id => (uint)id)
            };
            ids = ids.Where(id => id != 0).Distinct().OrderBy(id => id);
        }
        else if (spriteBrowserSource.SelectedIndex == 6)
        {
            if (canvas.Sprites is null) { MessageBox.Show(this, "Choose compatible art first."); return; }
            ids = canvas.Sprites.AvailableSpriteIds();
        }
        else
        {
            if (!TryParseRange($"{spriteBrowserStart.Text}-{spriteBrowserEnd.Text}", out var low, out var high)) { MessageBox.Show(this, "Enter a valid start and end sprite ID."); return; }
            var count = (ulong)high - low + 1;
            if (count > MaxSpriteBrowserResults) { MessageBox.Show(this, $"Browse at most {MaxSpriteBrowserResults:N0} sprite IDs at a time."); return; }
            ids = Enumerable.Range(0, (int)count).Select(offset => low + (uint)offset);
        }
        ShowSpriteIds(ids);
    }

    private IEnumerable<uint> GetSelectionSpriteIds()
    {
        IEnumerable<Point> points = selectedTiles.Count > 1
            ? selectedTiles
            : Enumerable.Range((int)y.Value, Math.Min((int)height.Value, 256 - (int)y.Value))
                .SelectMany(row => Enumerable.Range((int)x.Value, Math.Min((int)width.Value, 256 - (int)x.Value)).Select(column => new Point(column, row)));

        return points
            .Select(point => map!.Tiles.TryGetValue((point.X, point.Y), out var tile) ? tile : null)
            .Where(tile => tile is not null)
            .SelectMany(tile => tile!.SpriteComponents)
            .Where(id => id != 0)
            .Select(id => (uint)id)
            .Distinct()
            .OrderBy(id => id);
    }

    private void ShowSpriteIds(IEnumerable<uint> ids)
    {
        spriteGallery.Controls.Clear();
        if (canvas.Sprites is null) { spriteGallery.Controls.Add(new Label { Text = "Choose compatible art first.", ForeColor = Color.White, AutoSize = true }); return; }
        spriteBrowserResultIds.Clear();
        spriteBrowserResultIds.AddRange(ids.Distinct());
        renderedSpriteCount = 0;
        RenderNextSpritePage();
    }

    private void RenderNextSpritePage()
    {
        if (canvas.Sprites is null) return;
        var pageEnd = Math.Min(renderedSpriteCount + SpriteGalleryPageSize, spriteBrowserResultIds.Count);
        spriteGallery.SuspendLayout();
        try
        {
            for (; renderedSpriteCount < pageEnd; renderedSpriteCount++)
            {
                var id = spriteBrowserResultIds[renderedSpriteCount];
                var image = canvas.Sprites.Get(id);
                if (image is null) continue;
                var tile = new Panel { Width = 96, Height = 112, Margin = new Padding(4), BackColor = Color.FromArgb(34, 36, 42), Tag = id };
                var preview = new PictureBox { Width = 96, Height = 84, SizeMode = PictureBoxSizeMode.CenterImage, Image = image.Bitmap, Cursor = Cursors.Hand, Tag = id, BackColor = Color.FromArgb(18, 20, 24), TabStop = true };
                var label = new Label { Text = id.ToString(), Dock = DockStyle.Bottom, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Height = 24 };
                var dragOrigin = Point.Empty;
                preview.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) dragOrigin = e.Location; };
                preview.MouseMove += (_, e) =>
                {
                    if (e.Button == MouseButtons.Left && dragOrigin != Point.Empty && (Math.Abs(e.X - dragOrigin.X) >= SystemInformation.DragSize.Width / 2 || Math.Abs(e.Y - dragOrigin.Y) >= SystemInformation.DragSize.Height / 2))
                    {
                        dragOrigin = Point.Empty;
                        preview.DoDragDrop(id.ToString(), DragDropEffects.Copy);
                    }
                };
                preview.Click += (_, _) => { preview.Focus(); SelectBrowserSprite(id); };
                preview.DoubleClick += (_, _) => SelectBrowserSprite(id);
                tile.Controls.Add(preview); tile.Controls.Add(label); spriteGallery.Controls.Add(tile);
            }
        }
        finally { spriteGallery.ResumeLayout(true); }
        spriteBrowserResultCount.Text = $"{renderedSpriteCount:N0}/{spriteBrowserResultIds.Count:N0} IDs";
        showMoreSprites.Enabled = renderedSpriteCount < spriteBrowserResultIds.Count;
        if (renderedSpriteCount == spriteBrowserResultIds.Count && spriteGallery.Controls.Count == 0)
            spriteGallery.Controls.Add(new Label { Text = "No sprites decoded in this result set.", ForeColor = Color.White, AutoSize = true });
        if (selectedBrowserSprite is { } selected) HighlightBrowserSprite(selected);
    }
    private void SelectPaletteSprite(uint sprite)
    {
        selectedBrowserSprite = sprite;
        UpdateRecentlySelectedPreview(sprite);
        HighlightBrowserSprite(sprite);
        status.Text = $"Sprite {sprite} selected for inspection. Choose a target layer, then click Use selected sprite to load it for painting.";
    }
    private void SelectBrowserSprite(uint sprite) { selectedBrowserSprite = sprite; AddRecentSprite(sprite); UpdateRecentlySelectedPreview(sprite); HighlightBrowserSprite(sprite); status.Text = $"Sprite {sprite} selected for inspection. Click Use selected sprite to load it for painting; selecting a thumbnail does not change the map."; }
    private void UpdateRecentlySelectedPreview(uint sprite)
    {
        recentlySelectedPreview.Image = canvas.Sprites?.Get(sprite)?.Bitmap;
        recentlySelectedId.Text = sprite.ToString();
    }

    private void UpdateDefaultSpriteBrowserEnd()
    {
        if (updatingSpriteBrowserRange || spriteBrowserEndWasEdited || !uint.TryParse(spriteBrowserStart.Text, out var start)) return;
        var end = Math.Min((ulong)uint.MaxValue, (ulong)start + 500).ToString();
        updatingSpriteBrowserRange = true;
        spriteBrowserEnd.Text = end;
        updatingSpriteBrowserRange = false;
    }
    private void HighlightBrowserSprite(uint sprite)
    {
        foreach (Control control in spriteGallery.Controls) if (control is Panel panel) panel.BackColor = panel.Tag is uint id && id == sprite ? Color.FromArgb(190, 150, 45) : Color.FromArgb(34, 36, 42);
    }
    private void UseBrowserSprite(bool returnToEditor = true)
    {
        if (selectedBrowserSprite is not { } sprite) return;
        if (freeDrawTiles.Count > 0 && MessageBox.Show(this, "Using a different sprite will discard the current uncommitted brush preview. Continue?", "Discard brush preview", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var target = spriteBrowserTarget.SelectedIndex;
        if (target == 0) { gs1.Value = sprite; paintScope.SelectedIndex = 1; }
        else if (target == 1) { gs2.Value = sprite; paintScope.SelectedIndex = 2; }
        else if (target == 2) { fs1.Value = sprite; paintScope.SelectedIndex = 3; }
        else { fs2.Value = sprite; paintScope.SelectedIndex = 4; }
        freeDrawTiles.Clear(); canvas.ClearPreview();
        if (returnToEditor) rightTabs.SelectedIndex = 0;
        status.Text = $"Loaded sprite {sprite} into {spriteBrowserTarget.SelectedItem}. Select or preview map tiles, then Apply to commit.";
    }

    private void AddSpriteToWorkspace(uint sprite)
    {
        if (!spriteWorkspaceIds.Contains(sprite)) spriteWorkspaceIds.Add(sprite);
        SaveSpriteWorkspace(); RefreshSpriteLists();
    }

    private void RefreshSpriteLists()
    {
        spriteWorkspace.Items.Clear(); foreach (var sprite in spriteWorkspaceIds) spriteWorkspace.Items.Add(sprite);
        recentSprites.Items.Clear(); foreach (var sprite in recentSpriteIds) recentSprites.Items.Add(sprite);
    }

    private void AddRecentSprite(uint sprite)
    {
        recentSpriteIds.Remove(sprite); recentSpriteIds.AddFirst(sprite);
        while (recentSpriteIds.Count > 5) recentSpriteIds.RemoveLast();
        RefreshSpriteLists(); SaveSpriteWorkspace();
    }

    private void LoadSpriteWorkspace()
    {
        try
        {
            if (!File.Exists(spriteWorkspacePath)) return;
            var data = JsonSerializer.Deserialize<SpriteWorkspaceData>(File.ReadAllText(spriteWorkspacePath));
            if (data is null) return;
            spriteWorkspaceIds.AddRange(data.Workspace ?? []);
            foreach (var sprite in (data.Recent ?? []).Take(5)) recentSpriteIds.AddLast(sprite);
            RefreshSpriteLists();
        }
        catch { }
    }

    private void SaveSpriteWorkspace()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(spriteWorkspacePath)!);
            File.WriteAllText(spriteWorkspacePath, JsonSerializer.Serialize(new SpriteWorkspaceData { Workspace = spriteWorkspaceIds, Recent = recentSpriteIds.ToList() }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private sealed class SpriteWorkspaceData
    {
        public List<uint>? Workspace { get; set; }
        public List<uint>? Recent { get; set; }
    }
    private void ShowSpriteBrowser() => rightTabs.SelectedTab = spriteBrowserPage;
    private void ShowMapEditingTutorial()
    {
        if (mapTutorial is { IsDisposed: false })
        {
            mapTutorial.Activate();
            return;
        }

        mapTutorial = new MapEditingTutorialForm(
            () => map is not null,
            OpenMap,
            () => spriteSearch.Focus(),
            ShowSpriteBrowser,
            BrowseSelectedTiles,
            () => rightTabs.SelectedIndex = 0);
        mapTutorial.Dismissed += (_, _) =>
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(mapTutorialStatePath)!);
                File.WriteAllText(mapTutorialStatePath, "dismissed");
            }
            catch { }
        };
        mapTutorial.FormClosed += (_, _) => mapTutorial = null;
        mapTutorial.Show(this);
    }

    private void SearchFlags()
    {
        if (!RequireMap()) return; var wanted = flagSearch.CheckedItems.Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase); if (wanted.Count == 0) { MessageBox.Show(this, "Select at least one flag."); return; }
        ShowTiles(map!.Tiles.Values.Where(t => wanted.All(t.Flags.Contains)), "flag");
    }
    private void SearchTemplates()
    {
        var needle = templateSearch.Text.Trim(); templates.Items.Clear(); foreach (var name in itemTemplates.Where(n => n.Contains(needle, StringComparison.OrdinalIgnoreCase)).OrderBy(n => n)) templates.Items.Add("item  " + name); foreach (var name in characterTemplates.Where(n => n.Contains(needle, StringComparison.OrdinalIgnoreCase)).OrderBy(n => n)) templates.Items.Add("npc   " + name); rightTabs.SelectedIndex = 3;
    }
    private void ApplyTemplateSelection()
    {
        if (templates.SelectedItem is not string selected) return;
        var separator = selected.IndexOf("  ", StringComparison.Ordinal);
        if (separator < 0) return;
        var kind = selected[..separator].Trim();
        var name = selected[(separator + 2)..].Trim();
        if (kind.Equals("item", StringComparison.OrdinalIgnoreCase)) { item.Text = name; paintScope.SelectedIndex = 12; }
        if (kind.Equals("npc", StringComparison.OrdinalIgnoreCase)) { character.Text = name; paintScope.SelectedIndex = 13; }
        rightTabs.SelectedIndex = 0;
        status.Text = $"Selected {kind} template '{name}'. Choose a tile or region, then Apply tile / rectangle to place it.";
    }

    private void CreateNpcBuildDraft()
    {
        var selectedTemplate = character.Text;
        if (templates.SelectedItem is string selected && selected.StartsWith("npc  ", StringComparison.OrdinalIgnoreCase)) selectedTemplate = selected[5..].Trim();
        using var dialog = new NpcBuildDraftForm(characterTemplates, selectedTemplate, canvas.Sprites, characterSprites.Values);
        dialog.ShowDialog(this);
    }
    private void ShowTiles(IEnumerable<MapTile> tiles, string kind) { results.Items.Clear(); foreach (var tile in tiles.OrderBy(t => t.Y).ThenBy(t => t.X)) results.Items.Add(new Result(kind, tile)); centerTabs.SelectedTab = resultsPage; status.Text = $"{results.Items.Count:N0} {kind} matches. Click one to select it on the map tab."; }

    private void ValidateMap()
    {
        if (!RequireMap()) return;
        var output = map!.Validate(itemTemplates, characterTemplates, profile, canvas.Sprites);
        findings.Text = output.Count == 0 ? "No parser, flag, or template-reference findings.\r\n\r\nStatic validation only; test approved maps in staging." : string.Join(Environment.NewLine, output);
        rightTabs.SelectedTab = validationPage;
    }
    private void SaveWorkspaceCopy()
    {
        if (!RequireMap()) return; using var dialog = SaveDialog("Save exact workspace copy"); if (dialog.ShowDialog(this) != DialogResult.OK) return; if (!SafeDestination(dialog.FileName)) return; map!.SaveWorkspaceCopy(dialog.FileName); status.Text = "Exact source copy saved locally. No server or live runtime was changed.";
    }
    private void SaveEditedMap()
    {
        if (!RequireEditable()) return; ValidateMap(); using var dialog = SaveDialog("Save edited workspace map"); if (dialog.ShowDialog(this) != DialogResult.OK) return; if (!SafeDestination(dialog.FileName)) return;
        try { map!.SaveEditedWorkspaceMap(dialog.FileName); status.Text = "Edited workspace map saved with a .bak backup when replacing a prior workspace file. Review its diff before deployment."; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Save blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private SaveFileDialog SaveDialog(string title) => new() { Filter = "Astonia map (*.map)|*.map", FileName = map is null ? "map.map" : Path.GetFileName(map.SourcePath), Title = title };
    private bool SafeDestination(string destination) { if (map is not null && Path.GetFullPath(destination).Equals(Path.GetFullPath(map.SourcePath), StringComparison.OrdinalIgnoreCase)) { MessageBox.Show(this, "Choose a different workspace path; the opened source is never overwritten."); return false; } return true; }

    private void ExportManifest()
    {
        if (!RequireMap()) return; using var dialog = new SaveFileDialog { Filter = "Markdown (*.md)|*.md", FileName = "map-review-manifest.md" }; if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var report = $"# Map review manifest\n\n- Source map: {map!.SourcePath}\n- Non-empty tiles: {map.Tiles.Count:N0}\n- Server profile: {profile?.Revision ?? "not selected"}\n- Deployment state: NOT DEPLOYED\n\n## Required next steps\n\n- [ ] Review the exact workspace-map diff in the canonical Server 3 checkout.\n- [ ] Run validation and a staging-area test.\n- [ ] Obtain approval for the exact runtime path and restart window.\n";
        File.WriteAllText(dialog.FileName, report, new UTF8Encoding(false));
    }

    private void ChooseSpriteArchive()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose a compatible pak art folder (the folder containing 00000000.pak)" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            spriteArchive = new SpriteArchive(dialog.SelectedPath);
            foreach (var workspace in workspaces.Values) workspace.Canvas.Sprites = spriteArchive;
            canvas.Sprites = spriteArchive;
            UpdateStatus();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not load art assets", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void TryLoadDefaultSpriteArchive()
    {
        var defaultPak = @"C:\Astonia\Astonia Uncharted\Legacy Source CODE\Astonia3 Map Editor\Astonia3 Editor\pak";
        if (Directory.Exists(defaultPak)) spriteArchive = new SpriteArchive(defaultPak);
    }

    private static string? FindCurrentServerZones()
    {
        var zones = new[]
        {
            @"C:\Astonia\Astonia Uncharted\AU 3.0S\astonia_community_server3-main\zones",
            @"C:\Astonia\Astonia Uncharted\AU 3.5S\astonia_community_server35-main\zones"
        };
        return zones.FirstOrDefault(Directory.Exists);
    }

    private string MapSourceLabel()
    {
        if (map is null) return "none";
        if (profile is not null)
        {
            var zoneRoot = Path.GetFullPath(profile.ZoneRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(map.SourcePath).StartsWith(zoneRoot, StringComparison.OrdinalIgnoreCase))
                return profile.Name.Contains("3.5S", StringComparison.OrdinalIgnoreCase) ? "CURRENT SERVER 3.5S" : "CURRENT SERVER 3";
        }
        var path = map.SourcePath.Replace('/', '\\');
        if (path.Contains(@"\Astonia3 Map Editor\Astonia3 Editor\zones\", StringComparison.OrdinalIgnoreCase)) return "LEGACY EDITOR DATA";
        return "EXTERNAL MAP";
    }

    private void GenerateProfile() { using var dialog = new FolderBrowserDialog { Description = "Choose a Server 3.0S or 3.5S checkout (or the 3.5S src folder)" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; try { SetProfile(ServerMapProfile.Generate(dialog.SelectedPath)); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not generate profile"); } }
    private void LoadProfile() { using var dialog = new OpenFileDialog { Filter = "Map profile (*.json)|*.json" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; try { SetProfile(ServerMapProfile.Load(dialog.FileName)); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not load profile"); } }
    private void SaveProfile() { if (profile is null) return; using var dialog = new SaveFileDialog { Filter = "Map profile (*.json)|*.json", FileName = "server3-map-profile.json" }; if (dialog.ShowDialog(this) == DialogResult.OK) profile.Save(dialog.FileName); }
    private void TryDetectProfile(string mapPath) { var directory = new DirectoryInfo(Path.GetDirectoryName(mapPath)!); while (directory.Parent is not null) { if (directory.Name.Equals("zones", StringComparison.OrdinalIgnoreCase)) { try { SetProfile(ServerMapProfile.Generate(directory.Parent.FullName)); } catch { } return; } directory = directory.Parent; } }
    private void SetProfile(ServerMapProfile value) { profile = value; if (activeWorkspace is not null) activeWorkspace.Profile = value; PopulateProfileLists(); UpdateStatus(); }

    private void PopulateProfileLists()
    {
        flagSearch.Items.Clear();
        editFlags.Items.Clear();
        if (profile is null) return;
        foreach (var flag in profile.AuthorableFlags.OrderBy(flag => flag))
        {
            flagSearch.Items.Add(flag);
            editFlags.Items.Add(flag);
        }
    }

    private void LoadTemplates(string zoneDirectory)
    {
        itemTemplates.Clear(); characterTemplates.Clear(); characterSprites.Clear(); itemSprites.Clear(); var generic = Path.Combine(Directory.GetParent(zoneDirectory)?.FullName ?? zoneDirectory, "generic");
        foreach (var directory in new[] { zoneDirectory, generic }.Where(Directory.Exists)) { foreach (var file in Directory.EnumerateFiles(directory, "*.itm")) { LoadTemplateNames(file, itemTemplates); LoadItemSprites(file); } foreach (var file in Directory.EnumerateFiles(directory, "*.chr")) { LoadTemplateNames(file, characterTemplates); LoadCharacterSprites(file); } }
    }

    private void LoadItemSprites(string file)
    {
        string? current = null;
        foreach (var raw in File.ReadLines(file))
        {
            var line = raw.Split('#', 2)[0].Trim();
            if (line.EndsWith(':') && line.Length > 1 && !line.Contains(' ')) { current = line[..^1]; continue; }
            if (current is not null && line.StartsWith("sprite=", StringComparison.OrdinalIgnoreCase) && uint.TryParse(line[7..].Trim(), out var sprite)) { itemSprites[current] = sprite; current = null; }
        }
    }

    private void LoadCharacterSprites(string file)
    {
        string? current = null;
        foreach (var raw in File.ReadLines(file))
        {
            var line = raw.Split('#', 2)[0].Trim();
            if (line.EndsWith(':') && line.Length > 1 && !line.Contains(' ')) { current = line[..^1]; continue; }
            if (current is not null && line.StartsWith("sprite=", StringComparison.OrdinalIgnoreCase) && uint.TryParse(line[7..].Trim(), out var sprite)) { characterSprites[current] = sprite; current = null; }
        }
    }
    private static void LoadTemplateNames(string file, ISet<string> destination)
    {
        foreach (var line in File.ReadLines(file))
        {
            var trimmed = line.Split('#', 2)[0].Trim();
            if (trimmed.EndsWith(':') && trimmed.Length > 1 && !trimmed.Contains(' ')) destination.Add(trimmed[..^1]);
        }
    }
    private void ShowDetails(MapTile tile)
    {
        var ground1 = tile.GroundSprite & 0xffff;
        var ground2 = tile.GroundSprite >> 16;
        var wall1 = tile.ForegroundSprite & 0xffff;
        var wall2 = tile.ForegroundSprite >> 16;
        var wall1Cut = canvas.Sprites?.GetCutSprite(wall1) ?? wall1;
        var wall2Cut = canvas.Sprites?.GetCutSprite(wall2) ?? wall2;
        details.Text = $"Tile: ({tile.X}, {tile.Y})\r\nGround layer 1: {ground1} (not F8-cut)\r\nGround layer 2: {ground2} (not F8-cut)\r\nWall / ceiling layer 1: {wall1} (cut preview: {wall1Cut})\r\nWall / ceiling layer 2: {wall2} (cut preview: {wall2Cut})\r\nItem: {tile.Item ?? "-"}\r\nNPC: {tile.Character ?? "-"}\r\nFlags: {(tile.Flags.Count == 0 ? "-" : string.Join(", ", tile.Flags.OrderBy(f => f)))}\r\nUnsupported: {(tile.UnknownLines.Count == 0 ? "-" : string.Join(" | ", tile.UnknownLines))}";
    }
    private bool RequireMap() { if (map is not null) return true; MessageBox.Show(this, "Open a map first."); return false; }
    private bool RequireEditable() { if (!RequireMap()) return false; if (!map!.CanEdit) { MessageBox.Show(this, "This map contains comments or directives this version cannot preserve. It is safely inspection-only; use Save verbatim workspace copy.", "Editing blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; } return true; }
    private void UpdateStatus() { status.Text = map is null ? "Open a Server 3 .map file." : $"{(map.CanEdit ? "EDITOR READY" : "INSPECTION ONLY")} | source: {MapSourceLabel()} | {map.SourcePath} | {map.Tiles.Count:N0} loaded tiles | art: {artDiagnostic} | profile: {profile?.Revision ?? "none"}"; apply.Enabled = map?.CanEdit == true; undo.Enabled = undoStack.Count != 0; redo.Enabled = redoStack.Count != 0; }
    private void LoadLayout()
    {
        try
        {
            if (!File.Exists(layoutPath)) return;
            var layout = JsonSerializer.Deserialize<WorkbenchLayout>(File.ReadAllText(layoutPath));
            if (layout is null) return;
            mainSplit.SplitterDistance = Math.Clamp(layout.LeftWidth, mainSplit.Panel1MinSize, Math.Max(mainSplit.Panel1MinSize, ClientSize.Width - mainSplit.Panel2MinSize));
            centerRightSplit.SplitterDistance = Math.Clamp(layout.CenterWidth, centerRightSplit.Panel1MinSize, Math.Max(centerRightSplit.Panel1MinSize, mainSplit.Panel2.ClientSize.Width - centerRightSplit.Panel2MinSize));
            zoom.Value = Math.Clamp(layout.Zoom, zoom.Minimum, zoom.Maximum);
        }
        catch { }
    }

    private void ConfigureLayout()
    {
        var monitor = Screen.FromPoint(Cursor.Position);
        StartPosition = FormStartPosition.Manual;
        Location = monitor.WorkingArea.Location;
        WindowState = FormWindowState.Maximized;
        mainSplit.Panel1MinSize = 220;
        mainSplit.Panel2MinSize = 700;
        centerRightSplit.Panel1MinSize = 520;
        centerRightSplit.Panel2MinSize = 300;
        LoadLayout();
    }

    private void SaveLayout()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(layoutPath)!);
            var layout = new WorkbenchLayout(Width, Height, mainSplit.SplitterDistance, centerRightSplit.SplitterDistance, zoom.Value);
            File.WriteAllText(layoutPath, JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
    private static NumericUpDown Number(int min, int max, int initial = 0) => new() { Minimum = min, Maximum = max, Value = initial, Dock = DockStyle.Fill };
    private static TextBox ReadOnlyTextBox() => new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Font = new Font(FontFamily.GenericMonospace, 9) };
    private static TabPage Tab(string title, Control control) { var page = new TabPage(title); page.Controls.Add(control); return page; }
    private static Control Button(string text, EventHandler handler) { var button = new Button { Text = text, AutoSize = true, Dock = DockStyle.Bottom }; button.Click += handler; return button; }
    private static Control ToolbarButton(string text, EventHandler handler) { var button = new Button { Text = text, AutoSize = true }; button.Click += handler; return button; }
    private static Panel PanelWith(string title, params Control[] controls) { var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) }; panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Height = 22 }); foreach (var control in controls.Reverse()) panel.Controls.Add(control); return panel; }
    private static bool TryParseRange(string input, out uint low, out uint high) { var parts = input.Trim().Split('-', 2, StringSplitOptions.TrimEntries); low = high = 0; return uint.TryParse(parts[0], out low) && (parts.Length == 1 || uint.TryParse(parts[1], out high)) && low <= high; }
    private sealed record Result(string Kind, MapTile Tile) { public override string ToString() => $"({Tile.X,3},{Tile.Y,3}) gs={Tile.GroundSprite,-10} fs={Tile.ForegroundSprite,-10} {Kind}"; }
    private sealed record TileChange(IReadOnlyList<TileSnapshot> Before, IReadOnlyList<TileSnapshot> After);
    private sealed record SpriteClipboard(uint Sprite, int Layer);
    private sealed class MapWorkspace(MapDocument map, ServerMapProfile? profile, HashSet<Point> selectedTiles, HashSet<Point> freeDrawTiles, Stack<TileChange> undoStack, Stack<TileChange> redoStack, MapCanvas canvas)
    {
        public MapDocument Map { get; set; } = map;
        public ServerMapProfile? Profile { get; set; } = profile;
        public MapCanvas Canvas { get; } = canvas;
        public HashSet<Point> SelectedTiles { get; set; } = selectedTiles;
        public HashSet<Point> FreeDrawTiles { get; set; } = freeDrawTiles;
        public Stack<TileChange> UndoStack { get; set; } = undoStack;
        public Stack<TileChange> RedoStack { get; set; } = redoStack;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
    }
    private sealed class MapRegionClipboard
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<MapRegionTile> Tiles { get; set; } = [];
    }

    private sealed class MapRegionTile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public uint GroundSprite { get; set; }
        public uint ForegroundSprite { get; set; }
        public string? Item { get; set; }
        public string? Character { get; set; }
        public List<string>? Flags { get; set; }
    }
    private sealed record WorkbenchLayout(int WindowWidth, int WindowHeight, int LeftWidth, int CenterWidth, int Zoom);
    private sealed class TileSnapshot(int x, int y, uint ground, uint foreground, string? item, string? character, IEnumerable<string> flags)
    {
        public int X { get; } = x; public int Y { get; } = y; public uint Ground { get; } = ground; public uint Foreground { get; } = foreground; public string? Item { get; } = item; public string? Character { get; } = character; public string[] Flags { get; } = flags.ToArray();
        public static TileSnapshot Capture(MapTile tile) => new(tile.X, tile.Y, tile.GroundSprite, tile.ForegroundSprite, tile.Item, tile.Character, tile.Flags);
        public bool Matches(TileSnapshot other) => Ground == other.Ground && Foreground == other.Foreground && Item == other.Item && Character == other.Character && Flags.OrderBy(flag => flag).SequenceEqual(other.Flags.OrderBy(flag => flag), StringComparer.OrdinalIgnoreCase);
        public void Restore(MapTile tile) { tile.GroundSprite = Ground; tile.ForegroundSprite = Foreground; tile.Item = Item; tile.Character = Character; tile.Flags.Clear(); foreach (var flag in Flags) tile.Flags.Add(flag); }
    }
}
