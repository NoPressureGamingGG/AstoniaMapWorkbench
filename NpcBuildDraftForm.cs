using System.Drawing.Drawing2D;
using System.Text.Json;

namespace AstoniaMapWorkbench;

internal sealed class NpcBuildDraftForm : Form
{
    private const string NoTemplatesPlaceholder = "(Open a map to load .chr templates)";
    private const string UnknownDriverDescription = "No catalogued Server 3.0S driver";
    private static readonly IReadOnlyDictionary<int, string> DriverDescriptions = new Dictionary<int, string>
    {
        [0] = "Player character driver",
        [3] = "Tutor",
        [5] = "Disconnected player-character handler",
        [6] = "Generic merchant",
        [7] = "Generic aggressive NPC",
        [8] = "Gwendylon, quest giver",
        [9] = "Yoakin, quest giver",
        [10] = "Balltrap enemy",
        [11] = "Terion, village NPC",
        [12] = "James, castle NPC",
        [15] = "Nook, castle NPC",
        [16] = "Lydia, castle NPC",
        [17] = "Robber enemy",
        [18] = "Sanoa, city NPC/enemy",
        [19] = "Asturin, city NPC/enemy",
        [20] = "Reskin, city NPC",
        [21] = "Guiwynn, quest giver",
        [22] = "Bank",
        [23] = "Logain, quest giver",
        [24] = "Seymour, quest giver",
        [25] = "Lamp-extinguishing ghost",
        [26] = "Superior zombie",
        [27] = "Clan master",
        [28] = "Clan clerk",
        [29] = "Clan guard",
        [30] = "Kelly, area 3 NPC",
        [31] = "Moonie, stops to eat spiders",
        [32] = "Astro 1, tells moon stories",
        [33] = "Astro 2, Moonie quest giver",
        [34] = "Special vampire",
        [35] = "Thomas, Moonie quest giver",
        [36] = "Sir Jones, Moonie quest giver",
        [37] = "Macro daemon",
        [38] = "Special vampire 2",
        [39] = "Gatekeeper welcome",
        [40] = "Gatekeeper fight",
        [41] = "Clan robber",
        [42] = "Random military quest giver",
        [43] = "Military advisor",
        [44] = "Friendly fire-area demon",
        [45] = "Fire-area mission giver",
        [46] = "Hostile fire-area demon",
        [47] = "Palace guard",
        [48] = "Arena master",
        [49] = "Arena fighter",
        [50] = "Arena manager",
        [51] = "Dungeon master",
        [52] = "Dungeon fighter",
        [53] = "Random quest master",
        [54] = "Swamp Clara, quest giver",
        [55] = "Generic profession teacher",
        [56] = "Swamp monster",
        [57] = "Palace Islena",
        [58] = "Forest imp, quest giver",
        [59] = "Forest monster",
        [60] = "Forest William, quest giver",
        [61] = "Forest hermit, quest giver",
        [62] = "Two Cities guard",
        [63] = "Two Cities barkeeper",
        [64] = "Penter",
        [65] = "Two Cities servant",
        [66] = "Thieves' guild guard",
        [67] = "Thieves' guild master",
        [68] = "Two Cities robber",
        [69] = "Sanwyn, military quest giver",
        [70] = "Skelly, quest giver",
        [71] = "Two Cities alchemist, quest giver",
        [72] = "Player-trade middleman",
        [73] = "Nomad",
        [74] = "Standard LQ NPC",
        [75] = "LQ command parser",
        [76] = "Mad hermit",
        [77] = "Pents tester",
        [78] = "Strategy worker/fighter",
        [79] = "Strategy command parser",
        [80] = "Strategy boss/quest NPC",
        [81] = "Ratling",
        [82] = "Ruby, first side-story quest giver",
        [83] = "Warped-area fighter",
        [84] = "Warped-area master",
        [85] = "Generic janitor",
        [86] = "Shrike werewolf",
        [87] = "Werewolf",
        [88] = "Smuggler commander",
        [89] = "Smuggler leader",
        [90] = "Carlos, dragon-breath quest giver",
        [91] = "Count Brannington",
        [92] = "Countess Brannington",
        [93] = "Brannington daughter",
        [94] = "Brannington spirit",
        [95] = "Brannington guard",
        [96] = "Brenneth Brannington",
        [97] = "Forest Brannington forester",
        [98] = "Past-maxes raiser",
        [99] = "Broklin",
        [100] = "Aristocrat",
        [101] = "Yoatin",
        [102] = "White Robber boss",
        [103] = "Dwarf chief",
        [104] = "Grinnich",
        [105] = "Shanra",
        [106] = "Sentinel",
        [107] = "Golem keyholder",
        [108] = "Lost dwarf/miner",
        [109] = "Dwarf shaman",
        [110] = "Dwarf smith",
        [111] = "Mission giver",
        [112] = "Mission fighter",
        [113] = "Club master",
        [114] = "Teufelheim demon",
        [115] = "Teufelheim gambler",
        [116] = "Teufelheim quest giver",
        [117] = "Teufelheim rat",
        [118] = "Caligar guard (3.0S; Clan Vault in 3.5S)",
        [119] = "Caligar Glori",
        [120] = "Caligar Arquin",
        [121] = "Caligar smith",
        [122] = "Caligar Homden",
        [123] = "Caligar guard 2",
        [124] = "Caligar Skelly",
        [125] = "Caligar guard (3.5S)",
        [130] = "Rouven, Imperial Vault",
        [131] = "Rammy, Arkhata",
        [132] = "Jaz, Arkhata",
        [133] = "Bridge guard, Arkhata",
        [134] = "Fiona, Arkhata",
        [135] = "Gladiator, Arkhata",
        [136] = "Nop, Arkhata",
        [137] = "Ramin, Arkhata",
        [138] = "Arkhata Skelly",
        [139] = "Arkhata monk",
        [140] = "Book eater, Arkhata",
        [141] = "Captain, Arkhata",
        [142] = "Judge, Arkhata",
        [143] = "Fortress guard, Arkhata",
        [144] = "Jada, Arkhata",
        [145] = "Pot maker, Arkhata",
        [146] = "Hunter, Arkhata",
        [147] = "Thaipan, Arkhata",
        [148] = "Trainer, Arkhata",
        [149] = "Kidnappee, Arkhata",
        [150] = "Arkhata clerk",
        [151] = "Arkhata prisoner",
        [152] = "Krenach, Arkhata",
        [186] = "Monk worker",
        [187] = "Monk Govida, salt-mine quest",
        [188] = "Monk Gatama, salt-mine quest",
        [189] = "Lab 5 master daemon",
        [190] = "Lab 5 mage",
        [191] = "Lab 5 Seyan",
        [192] = "Lab 4 Seyan",
        [193] = "Lab 4 Gnalb",
        [194] = "Lab 3 pass guard",
        [195] = "Lab 3 prisoner",
        [196] = "Lab 2 herald",
        [197] = "Lab 2 crypt demon",
        [198] = "Lab 2 undead",
        [199] = "Lab 1 torch gnome"
    };
    private readonly ComboBox baseTemplate = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox displayName = new() { Dock = DockStyle.Fill };
    private readonly TextBox description = MultiLineBox();
    private readonly NumericUpDown sprite = new() { Minimum = 0, Maximum = uint.MaxValue, Width = 150 };
    private readonly PictureBox selectedSpritePreview = new() { Width = 44, Height = 44, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(24, 26, 30) };
    private readonly Label selectedSpriteLabel = new() { AutoSize = true, Text = "Sprite 0", Margin = new Padding(4, 14, 4, 4) };
    private readonly NumericUpDown driver = new() { Minimum = 0, Maximum = ushort.MaxValue, Dock = DockStyle.Fill };
    private readonly Label driverDescription = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0), ForeColor = SystemColors.GrayText };
    private readonly TextBox driverArguments = MultiLineBox();
    private readonly TextBox flags = MultiLineBox();
    private readonly TextBox values = MultiLineBox();
    private readonly TextBox items = MultiLineBox();
    private readonly TextBox spells = MultiLineBox();
    private readonly TextBox notes = MultiLineBox();
    private readonly SpriteArchive? spriteArchive;
    private readonly uint[] characterSpriteIds;
    private readonly ToolTip fieldToolTips = new() { AutoPopDelay = 15000, InitialDelay = 100, ReshowDelay = 50, ShowAlways = true };

    public NpcBuildDraftForm(IEnumerable<string> templates, string? selectedTemplate, SpriteArchive? spriteArchive, IEnumerable<uint> knownCharacterSprites)
    {
        Text = "NPC Build Draft - engineer handoff";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 760);
        Size = new Size(900, 900);
        this.spriteArchive = spriteArchive;
        characterSpriteIds = (spriteArchive?.AvailableCharacterIds() ?? Enumerable.Empty<uint>())
            .Concat(knownCharacterSprites)
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var templateNames = templates.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (templateNames.Length == 0)
        {
            baseTemplate.Items.Add(NoTemplatesPlaceholder);
            baseTemplate.SelectedIndex = 0;
        }
        else
        {
            baseTemplate.Items.AddRange(templateNames.Cast<object>().ToArray());
            if (!string.IsNullOrWhiteSpace(selectedTemplate) && baseTemplate.Items.Contains(selectedTemplate)) baseTemplate.SelectedItem = selectedTemplate;
            else baseTemplate.SelectedIndex = 0;
        }

        var content = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 3, Padding = new Padding(10) };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(content, "Base .chr template", baseTemplate, 34, "Existing internal NPC template key from the opened map area or generic .chr files. This is a reference for the engineer; its stats are not copied automatically.");
        AddRow(content, "NPC name override", displayName, 30, "Optional player-facing name written as name=. This is not the internal template key used by ch=.");
        AddRow(content, "Description override", description, 64, "Optional player-facing description written as description= in the character template.");
        var spriteInput = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty, AutoSize = true };
        var browseSprites = new Button { Text = "Browse sprites...", AutoSize = true };
        browseSprites.Click += (_, _) => BrowseCharacterSprites();
        spriteInput.Controls.Add(sprite);
        spriteInput.Controls.Add(browseSprites);
        spriteInput.Controls.Add(selectedSpritePreview);
        spriteInput.Controls.Add(selectedSpriteLabel);
        AddRow(content, "Character sprite ID", spriteInput, 54, "Character appearance ID used by sprite=. Browse to preview available character art, or enter an ID. The thumbnail shows the current selection.");
        var driverInput = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        driverInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        driverInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        driverInput.Controls.Add(driver, 0, 0);
        driverInput.Controls.Add(driverDescription, 1, 0);
        fieldToolTips.SetToolTip(driverDescription, "Descriptions use the Server 3.0S driver catalog. Most non-generic drivers are specific to a named NPC, quest, or area.");
        AddRow(content, "Driver ID", driverInput, 30, "Numeric server AI routine used by driver=. It controls NPC behavior; it is not the sprite or template ID.");
        AddRow(content, "Driver arguments", driverArguments, 54, "Optional text passed to the selected driver as arg=. Argument names and syntax depend on that driver.");
        AddRow(content, "Character flags", flags, 68, "One CF_* character flag per line. These affect the character at runtime and are different from map-tile flags.");
        AddRow(content, "V_* value overrides", values, 115, "One V_NAME=value entry per line for attributes or skills, such as V_HP=14. The engineer should verify names and valid values.");
        AddRow(content, "Carried item template names", items, 72, "One internal .itm template key per line. These become item= entries; use template keys, not display names or sprite IDs.");
        AddRow(content, "Spell item template names", spells, 72, "One internal spell-item .itm template key per line. These become spell= entries, not spell names or numeric IDs.");
        AddRow(content, "Engineer notes / requested behavior", notes, 90, "Describe intended behavior, appearance, or open questions for the engineer. Notes are included in the proposal but are not applied by the editor.");

        var explanation = new Label
        {
            Text = "This exports a reviewable proposal only. It does not modify a .chr file or live server. Put one flag, V_* override, item, or spell per line.",
            AutoSize = true,
            MaximumSize = new Size(820, 0),
            Padding = new Padding(4)
        };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        var export = new Button { Text = "Export draft...", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        fieldToolTips.SetToolTip(export, "Save a .npcbuild.json proposal for engineer review. This does not change server .chr or map files.");
        export.Click += (_, _) => ExportDraft();
        actions.Controls.Add(export);
        actions.Controls.Add(cancel);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(6) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        explanation.Dock = DockStyle.Fill;
        layout.Controls.Add(content, 0, 0);
        layout.Controls.Add(explanation, 0, 1);
        layout.Controls.Add(actions, 0, 2);
        Controls.Add(layout);
        sprite.ValueChanged += (_, _) => UpdateSelectedSpritePreview();
        driver.ValueChanged += (_, _) => UpdateDriverDescription();
        UpdateSelectedSpritePreview();
        UpdateDriverDescription();
        AcceptButton = export;
        CancelButton = cancel;
    }

    private void BrowseCharacterSprites()
    {
        if (spriteArchive is null || characterSpriteIds.Length == 0)
        {
            MessageBox.Show(this, "No character sprites are available. Choose a compatible art archive first.", "Character sprites", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var picker = new CharacterSpritePickerForm(spriteArchive, characterSpriteIds, (uint)sprite.Value);
        if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedSprite is uint selected) sprite.Value = selected;
    }

    private void UpdateSelectedSpritePreview()
    {
        var id = (uint)sprite.Value;
        selectedSpritePreview.Image = spriteArchive?.GetCharacterFrame(id)?.Bitmap;
        selectedSpriteLabel.Text = $"Sprite {id}";
    }

    private void UpdateDriverDescription()
    {
        driverDescription.Text = DriverDescriptions.TryGetValue((int)driver.Value, out var text) ? text : UnknownDriverDescription;
    }

    private void AddRow(TableLayoutPanel panel, string label, Control control, int height, string helpText)
    {
        var row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        var help = new HelpMarker();
        fieldToolTips.SetToolTip(help, helpText);
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 7, 10, 3) }, 0, row);
        panel.Controls.Add(help, 1, row);
        panel.Controls.Add(control, 2, row);
    }

    private static TextBox MultiLineBox() => new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };

    private sealed class HelpMarker : Control
    {
        public HelpMarker()
        {
            Size = new Size(16, 16);
            Margin = new Padding(0, 8, 0, 0);
            ForeColor = SystemColors.HotTrack;
            Cursor = Cursors.Help;
            Font = new Font(Font.FontFamily, 8F, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ForeColor);
            e.Graphics.DrawEllipse(pen, 0.5F, 0.5F, ClientSize.Width - 1, ClientSize.Height - 1);
            TextRenderer.DrawText(e.Graphics, "?", Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    private void ExportDraft()
    {
        if (baseTemplate.SelectedItem is not string template || template == NoTemplatesPlaceholder)
        {
            MessageBox.Show(this, "Open a Server 3 map first so the Workbench can load its area and generic .chr templates.", "NPC build draft", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog { Filter = "NPC build draft (*.npcbuild.json)|*.npcbuild.json", FileName = $"{template}.npcbuild.json", Title = "Export NPC build draft" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var draft = new NpcBuildDraft
        {
            BaseTemplate = template,
            Name = EmptyToNull(displayName.Text),
            Description = EmptyToNull(description.Text),
            Sprite = (uint)sprite.Value,
            Driver = (int)driver.Value,
            DriverArguments = EmptyToNull(driverArguments.Text),
            CharacterFlags = Lines(flags.Text),
            ValueOverrides = Lines(values.Text),
            Items = Lines(items.Text),
            Spells = Lines(spells.Text),
            EngineerNotes = EmptyToNull(notes.Text)
        };
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(draft, new JsonSerializerOptions { WriteIndented = true }));
        DialogResult = DialogResult.OK;
        Close();
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static List<string> Lines(string value) => value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(line => line.Length > 0).ToList();
}

internal sealed class NpcBuildDraft
{
    public int SchemaVersion { get; set; } = 1;
    public string ArtifactType { get; set; } = "AstoniaMapWorkbench.NpcBuildDraft";
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? BaseTemplate { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public uint Sprite { get; set; }
    public int Driver { get; set; }
    public string? DriverArguments { get; set; }
    public List<string> CharacterFlags { get; set; } = [];
    public List<string> ValueOverrides { get; set; } = [];
    public List<string> Items { get; set; } = [];
    public List<string> Spells { get; set; } = [];
    public string? EngineerNotes { get; set; }
    public string Status { get; set; } = "PROPOSAL ONLY - not applied to server files";
}

internal sealed class CharacterSpritePickerForm : Form
{
    private readonly SpriteArchive sprites;
    private readonly uint[] spriteIds;
    private readonly TextBox filter = new() { Dock = DockStyle.Top, PlaceholderText = "Filter by sprite ID" };
    private readonly ListBox results = new() { Dock = DockStyle.Fill };
    private readonly ComboBox previewScale = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly PictureBox preview = new() { SizeMode = PictureBoxSizeMode.Normal, BackColor = Color.FromArgb(24, 26, 30) };
    private readonly Label previewLabel = new() { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(4) };
    private readonly Button choose = new() { Text = "Use sprite", AutoSize = true, Enabled = false };
    private Panel? previewPanel;
    private Bitmap? scaledPreview;

    public uint? SelectedSprite { get; private set; }

    public CharacterSpritePickerForm(SpriteArchive sprites, IEnumerable<uint> spriteIds, uint selectedSprite)
    {
        this.sprites = sprites;
        this.spriteIds = spriteIds.Distinct().OrderBy(id => id).ToArray();
        Text = "Choose character sprite";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(620, 440);
        Size = new Size(760, 540);

        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(8) };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        previewScale.Items.AddRange(["Native (1x)", "Pixel art (2x)", "Smooth (2x)"]);
        previewScale.SelectedIndex = 1;

        var previewColumn = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        previewColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        previewColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var scaleControls = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(4), Controls = { new Label { Text = "Preview size:", AutoSize = true, Margin = new Padding(3, 7, 4, 3) }, previewScale } };
        var previewPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), AutoScroll = true };
        this.previewPanel = previewPanel;
        previewPanel.Controls.Add(preview);
        previewPanel.Controls.Add(previewLabel);
        previewPanel.Resize += (_, _) => CenterPreview();
        previewColumn.Controls.Add(scaleControls, 0, 0);
        previewColumn.Controls.Add(previewPanel, 0, 1);
        content.Controls.Add(results, 0, 0);
        content.Controls.Add(previewColumn, 1, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        choose.Click += (_, _) =>
        {
            if (results.SelectedItem is not uint id) return;
            SelectedSprite = id;
            DialogResult = DialogResult.OK;
            Close();
        };
        actions.Controls.Add(choose);
        actions.Controls.Add(cancel);
        Controls.Add(content);
        Controls.Add(filter);
        Controls.Add(actions);
        filter.TextChanged += (_, _) => RefreshResults(selectedSprite);
        results.SelectedIndexChanged += (_, _) => ShowPreview();
        previewScale.SelectedIndexChanged += (_, _) => ShowPreview();
        FormClosed += (_, _) =>
        {
            preview.Image = null;
            scaledPreview?.Dispose();
            scaledPreview = null;
        };
        AcceptButton = choose;
        CancelButton = cancel;
        RefreshResults(selectedSprite);
    }

    private void RefreshResults(uint preferredSprite)
    {
        var query = filter.Text.Trim();
        results.BeginUpdate();
        results.Items.Clear();
        foreach (var id in spriteIds.Where(id => query.Length == 0 || id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)))
            results.Items.Add(id);
        results.EndUpdate();

        if (results.Items.Contains(preferredSprite)) results.SelectedItem = preferredSprite;
        else if (results.Items.Count > 0) results.SelectedIndex = 0;
        else ShowPreview();
    }

    private void ShowPreview()
    {
        preview.Image = null;
        scaledPreview?.Dispose();
        scaledPreview = null;
        if (results.SelectedItem is not uint id)
        {
            preview.Size = new Size(1, 1);
            previewLabel.Text = "No matching character sprites.";
            choose.Enabled = false;
            CenterPreview();
            return;
        }

        var image = sprites.GetCharacterFrame(id);
        previewPanel!.AutoScrollPosition = Point.Empty;
        if (image is not null)
        {
            var scale = previewScale.SelectedIndex == 0 ? 1 : 2;
            scaledPreview = new Bitmap(image.Bitmap.Width * scale, image.Bitmap.Height * scale);
            using var graphics = Graphics.FromImage(scaledPreview);
            graphics.InterpolationMode = previewScale.SelectedIndex == 2 ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = previewScale.SelectedIndex == 2 ? PixelOffsetMode.HighQuality : PixelOffsetMode.Half;
            graphics.DrawImage(image.Bitmap, new Rectangle(0, 0, scaledPreview.Width, scaledPreview.Height));
            preview.Image = scaledPreview;
            preview.Size = scaledPreview.Size;
        }
        else
        {
            preview.Size = new Size(1, 1);
        }

        var mode = previewScale.SelectedIndex switch { 0 => "native", 2 => "smooth 2x", _ => "pixel-art 2x" };
        previewLabel.Text = image is null ? $"Sprite ID {id} (preview unavailable)" : $"Sprite ID {id} ({mode})";
        choose.Enabled = true;
        CenterPreview();
    }

    private void CenterPreview()
    {
        if (previewPanel is null) return;
        preview.Left = preview.Width < previewPanel.ClientSize.Width ? (previewPanel.ClientSize.Width - preview.Width) / 2 : 0;
        preview.Top = preview.Height < previewPanel.ClientSize.Height ? (previewPanel.ClientSize.Height - preview.Height) / 2 : 0;
    }
}