using System.Text;
using System.Text.Json;

namespace AstoniaMapWorkbench;

internal sealed class MainForm : Form
{
    private readonly TextBox spriteSearch = new() { PlaceholderText = "Sprite ID or range, for example 21400-21499", Dock = DockStyle.Fill };
    private readonly TextBox spriteBrowserRange = new() { Text = "12000-12020", Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel spriteGallery = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8), BackColor = Color.FromArgb(24, 26, 30) };
    private readonly CheckedListBox flagSearch = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly TextBox templateSearch = new() { PlaceholderText = "Search item or character template", Dock = DockStyle.Fill };
    private readonly ListBox results = new() { Dock = DockStyle.Fill };
    private readonly ListBox templates = new() { Dock = DockStyle.Fill };
    private readonly TextBox details = ReadOnlyTextBox();
    private readonly TextBox findings = ReadOnlyTextBox();
    private readonly TextBox activeFlags = ReadOnlyTextBox();
    private readonly TabControl centerTabs = new() { Dock = DockStyle.Fill };
    private readonly TabControl rightTabs = new() { Dock = DockStyle.Fill };
    private TabPage? validationPage;
    private TabPage? spriteBrowserPage;
    private readonly Label status = new() { AutoSize = true, Text = "Open a Server 3 .map file." };
    private readonly MapCanvas canvas = new();
    private readonly NumericUpDown x = Number(0, 255), y = Number(0, 255), width = Number(1, 256, 1), height = Number(1, 256, 1);
    private readonly NumericUpDown gs1 = Number(0, ushort.MaxValue), gs2 = Number(0, ushort.MaxValue), fs1 = Number(0, ushort.MaxValue), fs2 = Number(0, ushort.MaxValue);
    private readonly TextBox item = new(), character = new();
    private readonly ComboBox paintScope = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly CheckedListBox editFlags = new() { CheckOnClick = true, Height = 165 };
    private readonly Button apply = new() { Text = "Apply tile / rectangle", AutoSize = true };
    private readonly Button undo = new() { Text = "Undo", AutoSize = true };
    private readonly Button redo = new() { Text = "Redo", AutoSize = true };
    private readonly TrackBar zoom = new() { Minimum = 10, Maximum = 80, Value = 40, TickFrequency = 10, Dock = DockStyle.Fill };
    private readonly CheckBox collisionOverlay = new() { Text = "Collision overlay", AutoSize = true };
    private readonly CheckBox gridOverlay = new() { Text = "Grid", AutoSize = true };
    private readonly CheckBox wallsOverlay = new() { Text = "Show walls / ceilings", Checked = true, AutoSize = true };
    private readonly CheckBox charactersOverlay = new() { Text = "Show NPCs", Checked = true, AutoSize = true };
    private readonly CheckBox lowerWalls = new() { Text = "Lower walls (F8 view)", AutoSize = true };
    private readonly Stack<TileChange> undoStack = new();
    private readonly Stack<TileChange> redoStack = new();
    private readonly HashSet<Point> selectedTiles = [];
    private readonly string layoutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstoniaMapWorkbench", "layout.json");
    private readonly SplitContainer mainSplit = new() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, FixedPanel = FixedPanel.Panel1 };
    private readonly SplitContainer centerRightSplit = new() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
    private TileSnapshot? copiedTile;
    private MapDocument? map;
    private ServerMapProfile? profile;
    private readonly HashSet<string> itemTemplates = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> characterTemplates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, uint> characterSprites = new(StringComparer.OrdinalIgnoreCase);
    private string artDiagnostic = "not checked";

    public MainForm(string[]? startupArguments = null)
    {
        Text = "Astonia Map Workbench - local workspace editor";
        MinimumSize = new Size(1180, 760); Width = 1450; Height = 920;
        KeyPreview = true;
        KeyDown += (_, e) => HandleKeyCommand(e);
        FormClosing += (_, _) => SaveLayout();
        Shown += (_, _) => ConfigureLayout();
        canvas.TileInteraction += (_, interaction) => HandleTileInteraction(interaction);
        zoom.ValueChanged += (_, _) => canvas.TileWidth = zoom.Value;
        collisionOverlay.CheckedChanged += (_, _) => { canvas.ShowCollisionOverlay = collisionOverlay.Checked; canvas.RefreshTile(); };
        gridOverlay.CheckedChanged += (_, _) => { canvas.ShowGrid = gridOverlay.Checked; canvas.RefreshTile(); };
        wallsOverlay.CheckedChanged += (_, _) => { canvas.ShowWalls = wallsOverlay.Checked; canvas.RefreshTile(); };
        charactersOverlay.CheckedChanged += (_, _) => { canvas.ShowCharacters = charactersOverlay.Checked; canvas.RefreshTile(); };
        lowerWalls.CheckedChanged += (_, _) => { canvas.LowerWalls = lowerWalls.Checked; canvas.RefreshTile(); };
        apply.Click += (_, _) => ApplyEdit(); undo.Click += (_, _) => Undo(); redo.Click += (_, _) => Redo();
        paintScope.Items.AddRange(["Whole tile (replace all)", "Ground layer 1", "Ground layer 2", "Wall / ceiling layer 1", "Wall / ceiling layer 2", "Flags only", "Clear whole tile", "Clear ground layer 1", "Clear ground layer 2", "Clear wall / ceiling layer 1", "Clear wall / ceiling layer 2", "Clear flags", "Item only", "NPC only"]);
        paintScope.SelectedIndex = 5;

        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File");
        file.DropDownItems.Add("Open map...", null, (_, _) => OpenMap());
        file.DropDownItems.Add("Save verbatim workspace copy...", null, (_, _) => SaveWorkspaceCopy());
        file.DropDownItems.Add("Save edited workspace map...", null, (_, _) => SaveEditedMap());
        file.DropDownItems.Add("Export review manifest...", null, (_, _) => ExportManifest());
        menu.Items.Add(file);
        var edit = new ToolStripMenuItem("Edit");
        edit.DropDownItems.Add("Copy tile", null, (_, _) => CopyTile());
        edit.DropDownItems.Add("Paste tile", null, (_, _) => PasteTile());
        edit.DropDownItems.Add("Copy selected region to file...", null, (_, _) => CopyRegionToFile());
        edit.DropDownItems.Add("Paste region from file...", null, (_, _) => PasteRegionFromFile());
        edit.DropDownItems.Add("Clear selected region", null, (_, _) => ClearSelectedRegion());
        var undoCommand = new ToolStripMenuItem("Undo", null, (_, _) => Undo()) { ShortcutKeys = Keys.Control | Keys.Z };
        var redoCommand = new ToolStripMenuItem("Redo", null, (_, _) => Redo()) { ShortcutKeys = Keys.Control | Keys.Y };
        edit.DropDownItems.Add(new ToolStripSeparator()); edit.DropDownItems.Add(undoCommand); edit.DropDownItems.Add(redoCommand);
        menu.Items.Add(edit);
        var profiles = new ToolStripMenuItem("Server profile");
        profiles.DropDownItems.Add("Generate from Server 3 checkout...", null, (_, _) => GenerateProfile());
        profiles.DropDownItems.Add("Load profile...", null, (_, _) => LoadProfile());
        profiles.DropDownItems.Add("Save active profile...", null, (_, _) => SaveProfile());
        menu.Items.Add(profiles); menu.Items.Add(new ToolStripMenuItem("Validate", null, (_, _) => ValidateMap())); MainMenuStrip = menu;
        var art = new ToolStripMenuItem("Art assets");
        art.DropDownItems.Add("Open Sprite Browser", null, (_, _) => ShowSpriteBrowser());
        art.DropDownItems.Add("Choose compatible pak art folder...", null, (_, _) => ChooseSpriteArchive());
        menu.Items.Add(art);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 33)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 34)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        left.Controls.Add(PanelWith("Find sprite in opened map", spriteSearch, Button("Find map tiles", (_, _) => SearchSprites()), Button("Open sprite browser", (_, _) => ShowSpriteBrowser())), 0, 0);
        left.Controls.Add(PanelWith("Block / area flags", flagSearch, Button("Find flags", (_, _) => SearchFlags())), 0, 1);
        left.Controls.Add(PanelWith("Item and NPC templates", templateSearch, Button("Find templates", (_, _) => SearchTemplates())), 0, 2);

        var mapPage = new TabPage("Map canvas");
        var scroll = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 20, 24) }; scroll.Controls.Add(canvas);
        var mapLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 }; mapLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); mapLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mapLayout.Controls.Add(new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Controls = { new Label { Text = "Isometric zoom" }, zoom, collisionOverlay, gridOverlay, wallsOverlay, lowerWalls, charactersOverlay, new Label { Text = "Left-drag to pan." } } }, 0, 0); mapLayout.Controls.Add(scroll, 0, 1); mapPage.Controls.Add(mapLayout);
        centerTabs.TabPages.Add(mapPage); centerTabs.TabPages.Add(Tab("Search results", results));
        results.SelectedIndexChanged += (_, _) => { if (results.SelectedItem is Result r) SelectTile(r.Tile.X, r.Tile.Y); };
        templates.SelectedIndexChanged += (_, _) => ApplyTemplateSelection();

        validationPage = Tab("Validation", findings);
        spriteBrowserPage = Tab("Sprite browser", BuildSpriteBrowser());
        rightTabs.TabPages.Add(Tab("Tile editor", BuildEditor())); rightTabs.TabPages.Add(Tab("Tile details", details)); rightTabs.TabPages.Add(validationPage); rightTabs.TabPages.Add(Tab("Templates", templates)); rightTabs.TabPages.Add(spriteBrowserPage);
        mainSplit.Panel1.Controls.Add(left);
        centerRightSplit.Panel1.Controls.Add(centerTabs);
        centerRightSplit.Panel2.Controls.Add(rightTabs);
        mainSplit.Panel2.Controls.Add(centerRightSplit);
        Controls.Add(mainSplit); Controls.Add(status); Controls.Add(menu); status.Dock = DockStyle.Bottom;
        TryLoadDefaultSpriteArchive();
        if (startupArguments?.FirstOrDefault(File.Exists) is { } path) OpenMap(path);
    }

    private Control BuildEditor()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 2, Padding = new Padding(8) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(panel, "X", x); AddRow(panel, "Y", y); AddRow(panel, "Width", width); AddRow(panel, "Height", height);
        AddRow(panel, "Ground sprite 1", gs1); AddRow(panel, "Ground sprite 2", gs2); AddRow(panel, "Foreground sprite 1", fs1); AddRow(panel, "Foreground sprite 2", fs2);
        AddRow(panel, "Paint target", paintScope);
        item.Dock = DockStyle.Fill; character.Dock = DockStyle.Fill; AddRow(panel, "Item template", item); AddRow(panel, "NPC template", character);
        editFlags.Dock = DockStyle.Top; AddRow(panel, "Static flags", editFlags);
        activeFlags.Height = 44; AddRow(panel, "Active flags", activeFlags);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Controls = { apply, undo, redo } }; AddRow(panel, "", buttons);
        return panel;
    }

    private Control BuildSpriteBrowser()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(6) };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var rangeLabel = new Label { Text = "Sprite ID or range", AutoSize = true, Padding = new Padding(0, 6, 6, 0) };
        var browse = new Button { Text = "Browse archive", AutoSize = true };
        browse.Click += (_, _) => BrowseSprites();
        var controls = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); controls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controls.Controls.Add(rangeLabel, 0, 0); controls.Controls.Add(spriteBrowserRange, 1, 0); controls.Controls.Add(browse, 2, 0);
        panel.Controls.Add(controls, 0, 0); panel.Controls.Add(spriteGallery, 0, 1);
        return panel;
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control control)
    {
        var row = panel.RowCount++; panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 7, 8, 3) }, 0, row); panel.Controls.Add(control, 1, row);
    }

    private void OpenMap()
    {
        using var dialog = new OpenFileDialog { Filter = "Server 3 map (*.map)|*.map", Title = "Open current or legacy Server 3 map" };
        var currentZones = FindCurrentServerZones();
        if (currentZones is not null) dialog.InitialDirectory = currentZones;
        if (dialog.ShowDialog(this) == DialogResult.OK) OpenMap(dialog.FileName);
    }

    private void OpenMap(string path)
    {
        try
        {
            map = MapDocument.Load(path); canvas.Map = map; LoadTemplates(Path.GetDirectoryName(path)!); canvas.CharacterSprites = characterSprites; TryDetectProfile(path); undoStack.Clear(); redoStack.Clear();
            var firstSprite = map.Tiles.Values.SelectMany(tile => tile.SpriteComponents).FirstOrDefault(sprite => sprite != 0);
            artDiagnostic = canvas.Sprites is null ? "no package selected" : canvas.Sprites.Get(firstSprite) is null ? canvas.Sprites.LastError ?? "map sprite missing from package" : $"sprite {firstSprite} decoded";
            findings.Clear(); results.Items.Clear(); details.Clear(); UpdateStatus(); SelectTile(0, 0);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not open map", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SelectTile(int tileX, int tileY)
    {
        if (map is null) return;
        selectedTiles.Clear(); selectedTiles.Add(new Point(tileX, tileY)); canvas.SetSelection(selectedTiles); canvas.SelectedTile = new Point(tileX, tileY);
        x.Value = tileX; y.Value = tileY; width.Value = 1; height.Value = 1;
        var tile = map.GetTile(tileX, tileY); LoadTile(tile); ShowDetails(tile);
    }

    private void HandleTileInteraction(TileInteraction interaction)
    {
        if (map is null) return;
        if ((interaction.Modifiers & Keys.Shift) != 0) { SelectMatchingSprite(interaction.Tile); return; }
        if ((interaction.Modifiers & Keys.Control) != 0 && interaction.SelectionStart is { } start)
        {
            SelectSection(start, interaction.Tile);
            return;
        }
        SelectTile(interaction.Tile.X, interaction.Tile.Y);
    }

    private void SelectSection(Point anchor, Point tile)
    {
        var minX = Math.Min(anchor.X, tile.X); var maxX = Math.Max(anchor.X, tile.X);
        var minY = Math.Min(anchor.Y, tile.Y); var maxY = Math.Max(anchor.Y, tile.Y);
        SelectTiles(Enumerable.Range(minY, maxY - minY + 1).SelectMany(row => Enumerable.Range(minX, maxX - minX + 1).Select(column => new Point(column, row))), "section");
    }

    private void SelectMatchingSprite(Point tilePoint)
    {
        var clickedTile = map!.GetTile(tilePoint.X, tilePoint.Y);
        var sprite = PreferredSprite(clickedTile);
        if (sprite == 0) { SelectTile(tilePoint.X, tilePoint.Y); status.Text = "The selected tile has no sprite to match."; return; }
        var matches = map.Tiles.Values.Where(tile => tile.SpriteComponents.Contains(sprite)).Select(tile => new Point(tile.X, tile.Y));
        SelectTiles(matches, $"sprite {sprite}");
    }

    private void SelectTiles(IEnumerable<Point> tiles, string kind)
    {
        selectedTiles.Clear(); selectedTiles.UnionWith(tiles);
        if (selectedTiles.Count == 0) return;
        var minX = selectedTiles.Min(tile => tile.X); var maxX = selectedTiles.Max(tile => tile.X);
        var minY = selectedTiles.Min(tile => tile.Y); var maxY = selectedTiles.Max(tile => tile.Y);
        x.Value = minX; y.Value = minY; width.Value = maxX - minX + 1; height.Value = maxY - minY + 1;
        canvas.SetSelection(selectedTiles); canvas.SelectedTile = new Point(minX, minY);
        LoadTile(map!.GetTile(minX, minY)); ShowDetails(map.GetTile(minX, minY));
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

    private void CopyTile()
    {
        if (!RequireMap()) return;
        copiedTile = TileSnapshot.Capture(map!.GetTile((int)x.Value, (int)y.Value));
        status.Text = $"Copied tile ({x.Value},{y.Value}). Select another tile and use Edit > Paste tile.";
    }

    private void PasteTile()
    {
        if (!RequireEditable() || copiedTile is null) return;
        var target = map!.GetTile((int)x.Value, (int)y.Value);
        var before = TileSnapshot.Capture(target);
        copiedTile.Restore(target);
        var after = TileSnapshot.Capture(target);
        undoStack.Push(new TileChange([before], [after]));
        redoStack.Clear();
        LoadTile(target);
        canvas.RefreshTile();
        UpdateStatus();
    }

    private void CopyRegionToFile()
    {
        if (!RequireMap() || selectedTiles.Count == 0) return;
        var minX = selectedTiles.Min(tile => tile.X); var minY = selectedTiles.Min(tile => tile.Y);
        var maxX = selectedTiles.Max(tile => tile.X); var maxY = selectedTiles.Max(tile => tile.Y);
        var region = new MapRegionClipboard
        {
            Width = maxX - minX + 1,
            Height = maxY - minY + 1,
            Tiles = Enumerable.Range(minY, maxY - minY + 1).SelectMany(row => Enumerable.Range(minX, maxX - minX + 1).Select(column =>
            {
                var tile = map!.GetTile(column, row);
                return new MapRegionTile { X = column - minX, Y = row - minY, GroundSprite = tile.GroundSprite, ForegroundSprite = tile.ForegroundSprite, Item = tile.Item, Character = tile.Character, Flags = tile.Flags.ToList() };
            })).ToList()
        };
        using var dialog = new SaveFileDialog { Filter = "Map region (*.mapregion.json)|*.mapregion.json", FileName = "region.mapregion.json", Title = "Copy selected region" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(region, new JsonSerializerOptions { WriteIndented = true }));
        status.Text = $"Copied {region.Width}x{region.Height} region to {Path.GetFileName(dialog.FileName)}.";
    }

    private void PasteRegionFromFile()
    {
        if (!RequireEditable()) return;
        using var dialog = new OpenFileDialog { Filter = "Map region (*.mapregion.json)|*.mapregion.json", Title = "Paste region at selected tile" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var region = JsonSerializer.Deserialize<MapRegionClipboard>(File.ReadAllText(dialog.FileName)) ?? throw new InvalidDataException("The region file is empty.");
            var originX = (int)x.Value; var originY = (int)y.Value;
            if (originX + region.Width > 256 || originY + region.Height > 256) throw new InvalidDataException("The region does not fit at the selected origin.");
            var before = region.Tiles.Select(tile => TileSnapshot.Capture(map!.GetTile(originX + tile.X, originY + tile.Y))).ToList();
            foreach (var tile in region.Tiles)
            {
                var target = map!.GetTile(originX + tile.X, originY + tile.Y);
                target.GroundSprite = tile.GroundSprite; target.ForegroundSprite = tile.ForegroundSprite; target.Item = tile.Item; target.Character = tile.Character;
                target.Flags.Clear(); foreach (var flag in tile.Flags ?? []) target.Flags.Add(flag);
            }
            var after = region.Tiles.Select(tile => TileSnapshot.Capture(map!.GetTile(originX + tile.X, originY + tile.Y))).ToList();
            undoStack.Push(new TileChange(before, after)); redoStack.Clear(); SelectTiles(region.Tiles.Select(tile => new Point(originX + tile.X, originY + tile.Y)), "pasted region"); UpdateStatus();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not paste region", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void ClearSelectedRegion()
    {
        if (!RequireEditable() || selectedTiles.Count == 0) return;
        var before = selectedTiles.Select(tile => TileSnapshot.Capture(map!.GetTile(tile.X, tile.Y))).ToList();
        foreach (var tile in selectedTiles) map!.GetTile(tile.X, tile.Y).Clear();
        var after = selectedTiles.Select(tile => TileSnapshot.Capture(map!.GetTile(tile.X, tile.Y))).ToList();
        undoStack.Push(new TileChange(before, after)); redoStack.Clear(); canvas.RefreshTile(); UpdateStatus();
    }

    private void LoadTile(MapTile tile)
    {
        gs1.Value = (ushort)(tile.GroundSprite & 0xffff); gs2.Value = (ushort)(tile.GroundSprite >> 16); fs1.Value = (ushort)(tile.ForegroundSprite & 0xffff); fs2.Value = (ushort)(tile.ForegroundSprite >> 16);
        item.Text = tile.Item ?? ""; character.Text = tile.Character ?? "";
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
        var targets = selectedTiles.Count > 1
            ? selectedTiles.ToArray()
            : Enumerable.Range((int)y.Value, (int)height.Value).SelectMany(row => Enumerable.Range((int)x.Value, (int)width.Value).Select(column => new Point(column, row))).ToArray();
        var before = targets.Select(point => TileSnapshot.Capture(map!.GetTile(point.X, point.Y))).ToList();
        foreach (var state in before) ApplyValues(map!.GetTile(state.X, state.Y));
        var change = new TileChange(before, before.Select(state => TileSnapshot.Capture(map!.GetTile(state.X, state.Y))).ToList()); undoStack.Push(change); redoStack.Clear();
        canvas.RefreshTile(); ShowDetails(map!.GetTile((int)x.Value, (int)y.Value)); UpdateStatus();
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
        if (!RequireMap()) return; if (!TryParseRange(spriteSearch.Text, out var low, out var high)) { MessageBox.Show(this, "Enter one sprite ID or a range."); return; }
        ShowTiles(map!.Tiles.Values.Where(t => t.SpriteComponents.Any(sprite => sprite >= low && sprite <= high)), "sprite");
    }

    private void BrowseSprites()
    {
        if (canvas.Sprites is null) { MessageBox.Show(this, "Choose a legacy pak folder first."); return; }
        if (!TryParseRange(spriteBrowserRange.Text, out var low, out var high)) { MessageBox.Show(this, "Enter one sprite ID or a range."); return; }
        if (high - low > 500) { MessageBox.Show(this, "Browse at most 501 sprite IDs at a time."); return; }
        spriteGallery.Controls.Clear();
        for (var id = low; id <= high; id++)
        {
            var image = canvas.Sprites.Get(id);
            if (image is null) continue;
            var tile = new Panel { Width = 96, Height = 112, Margin = new Padding(4), BackColor = Color.FromArgb(34, 36, 42), Tag = id };
            var preview = new PictureBox { Width = 96, Height = 84, SizeMode = PictureBoxSizeMode.CenterImage, Image = image.Bitmap, Cursor = Cursors.Hand, Tag = id, BackColor = Color.FromArgb(18, 20, 24) };
            var label = new Label { Text = id.ToString(), Dock = DockStyle.Bottom, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Height = 24 };
            preview.Click += (_, _) => { spriteSearch.Text = id.ToString(); SearchSprites(); };
            tile.Controls.Add(preview); tile.Controls.Add(label); spriteGallery.Controls.Add(tile);
        }
        if (spriteGallery.Controls.Count == 0) spriteGallery.Controls.Add(new Label { Text = "No sprites decoded in that range.", ForeColor = Color.White, AutoSize = true });
    }
    private void ShowSpriteBrowser() => rightTabs.SelectedTab = spriteBrowserPage;
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
        status.Text = $"Selected {kind} template '{name}'. Choose a tile or region, then Apply tile / rectangle to place it.";
    }
    private void ShowTiles(IEnumerable<MapTile> tiles, string kind) { results.Items.Clear(); foreach (var tile in tiles.OrderBy(t => t.Y).ThenBy(t => t.X)) results.Items.Add(new Result(kind, tile)); centerTabs.SelectedIndex = 1; status.Text = $"{results.Items.Count:N0} {kind} matches. Click one to select it on the canvas."; }

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
        try { canvas.Sprites = new SpriteArchive(dialog.SelectedPath); UpdateStatus(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not load art assets", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void TryLoadDefaultSpriteArchive()
    {
        var defaultPak = @"C:\Astonia\Astonia Uncharted\Legacy Source CODE\Astonia3 Map Editor\Astonia3 Editor\pak";
        if (Directory.Exists(defaultPak)) canvas.Sprites = new SpriteArchive(defaultPak);
    }

    private static string? FindCurrentServerZones()
    {
        var zones = @"C:\Astonia\Astonia Uncharted\AU 3.0S\astonia_community_server3-main\zones";
        return Directory.Exists(zones) ? zones : null;
    }

    private string MapSourceLabel()
    {
        if (map is null) return "none";
        var path = map.SourcePath.Replace('/', '\\');
        if (path.Contains(@"\AU 3.0S\astonia_community_server3-main\zones\", StringComparison.OrdinalIgnoreCase)) return "CURRENT SERVER 3";
        if (path.Contains(@"\Astonia3 Map Editor\Astonia3 Editor\zones\", StringComparison.OrdinalIgnoreCase)) return "LEGACY EDITOR DATA";
        return "EXTERNAL MAP";
    }

    private void GenerateProfile() { using var dialog = new FolderBrowserDialog { Description = "Choose the authoritative Server 3 checkout" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; try { SetProfile(ServerMapProfile.Generate(dialog.SelectedPath)); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not generate profile"); } }
    private void LoadProfile() { using var dialog = new OpenFileDialog { Filter = "Map profile (*.json)|*.json" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; try { SetProfile(ServerMapProfile.Load(dialog.FileName)); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not load profile"); } }
    private void SaveProfile() { if (profile is null) return; using var dialog = new SaveFileDialog { Filter = "Map profile (*.json)|*.json", FileName = "server3-map-profile.json" }; if (dialog.ShowDialog(this) == DialogResult.OK) profile.Save(dialog.FileName); }
    private void TryDetectProfile(string mapPath) { var directory = new DirectoryInfo(Path.GetDirectoryName(mapPath)!); while (directory.Parent is not null) { if (directory.Name.Equals("zones", StringComparison.OrdinalIgnoreCase)) { try { SetProfile(ServerMapProfile.Generate(directory.Parent.FullName)); } catch { } return; } directory = directory.Parent; } }
    private void SetProfile(ServerMapProfile value) { profile = value; flagSearch.Items.Clear(); editFlags.Items.Clear(); foreach (var flag in value.AuthorableFlags.OrderBy(f => f)) { flagSearch.Items.Add(flag); editFlags.Items.Add(flag); } UpdateStatus(); }

    private void LoadTemplates(string zoneDirectory)
    {
        itemTemplates.Clear(); characterTemplates.Clear(); characterSprites.Clear(); var generic = Path.Combine(Directory.GetParent(zoneDirectory)?.FullName ?? zoneDirectory, "generic");
        foreach (var directory in new[] { zoneDirectory, generic }.Where(Directory.Exists)) { foreach (var file in Directory.EnumerateFiles(directory, "*.itm")) LoadTemplateNames(file, itemTemplates); foreach (var file in Directory.EnumerateFiles(directory, "*.chr")) { LoadTemplateNames(file, characterTemplates); LoadCharacterSprites(file); } }
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
    private void ShowDetails(MapTile tile) => details.Text = $"Tile: ({tile.X}, {tile.Y})\r\nGround: {tile.GroundSprite}\r\nForeground: {tile.ForegroundSprite}\r\nItem: {tile.Item ?? "-"}\r\nNPC: {tile.Character ?? "-"}\r\nFlags: {(tile.Flags.Count == 0 ? "-" : string.Join(", ", tile.Flags.OrderBy(f => f)))}\r\nUnsupported: {(tile.UnknownLines.Count == 0 ? "-" : string.Join(" | ", tile.UnknownLines))}";
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
    private static Panel PanelWith(string title, params Control[] controls) { var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) }; panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Height = 22 }); foreach (var control in controls.Reverse()) panel.Controls.Add(control); return panel; }
    private static bool TryParseRange(string input, out uint low, out uint high) { var parts = input.Trim().Split('-', 2, StringSplitOptions.TrimEntries); low = high = 0; return uint.TryParse(parts[0], out low) && (parts.Length == 1 || uint.TryParse(parts[1], out high)) && low <= high; }
    private sealed record Result(string Kind, MapTile Tile) { public override string ToString() => $"({Tile.X,3},{Tile.Y,3}) gs={Tile.GroundSprite,-10} fs={Tile.ForegroundSprite,-10} {Kind}"; }
    private sealed record TileChange(IReadOnlyList<TileSnapshot> Before, IReadOnlyList<TileSnapshot> After);
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
        public int X { get; } = x; public int Y { get; } = y; private uint Ground { get; } = ground; private uint Foreground { get; } = foreground; private string? Item { get; } = item; private string? Character { get; } = character; private string[] Flags { get; } = flags.ToArray();
        public static TileSnapshot Capture(MapTile tile) => new(tile.X, tile.Y, tile.GroundSprite, tile.ForegroundSprite, tile.Item, tile.Character, tile.Flags);
        public void Restore(MapTile tile) { tile.GroundSprite = Ground; tile.ForegroundSprite = Foreground; tile.Item = Item; tile.Character = Character; tile.Flags.Clear(); foreach (var flag in Flags) tile.Flags.Add(flag); }
    }
}
