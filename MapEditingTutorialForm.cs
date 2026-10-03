namespace AstoniaMapWorkbench;

internal sealed class MapEditingTutorialForm : Form
{
    private readonly Func<bool> hasOpenMap;
    private readonly Action openMap;
    private readonly Step[] steps;
    private readonly Label heading = new() { AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) };
    private readonly TextBox description = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = SystemColors.Control, ScrollBars = ScrollBars.Vertical };
    private readonly Label progress = new() { AutoSize = true };
    private readonly Label status = new() { AutoSize = true };
    private readonly Button action = new() { AutoSize = true };
    private readonly Button back = new() { Text = "Back", AutoSize = true };
    private readonly Button next = new() { Text = "Next", AutoSize = true };
    private readonly Button skip = new() { Text = "Skip tutorial", AutoSize = true };
    private int stepIndex;

    public event EventHandler? Dismissed;

    public MapEditingTutorialForm(
        Func<bool> hasOpenMap,
        Action openMap,
        Action focusSpriteSearch,
        Action openSpriteBrowser,
        Action browseSelectedTiles,
        Action openTileEditor)
    {
        this.hasOpenMap = hasOpenMap;
        this.openMap = openMap;
        Text = "Map Editing Tutorial";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(440, 360);
        Size = new Size(500, 430);
        ShowInTaskbar = false;

        steps =
        [
            new Step("1. Open a map", "Open a compatible Server 3 .map file to work on. The Workbench edits a local workspace copy; it does not change a running server.", "Open map...", openMap, RequiresMap: true),
            new Step("2. Find a sprite", "Enter a sprite ID or range in the left-side box. Find map tiles locates existing uses; Browse ID / range opens those sprites directly in the browser.", "Focus sprite ID box", focusSpriteSearch),
            new Step("3. Inspect a selection", "Select a tile or Ctrl-drag a rectangle, then use Browse selected tiles. The browser shows sprites from all four tile layers. Selecting a thumbnail only inspects it; it does not paint.", "Browse selected tiles", browseSelectedTiles),
            new Step("4. Change a sprite", "Select a thumbnail, choose its target layer, then click Use selected sprite. Preview the edit before Apply tile / rectangle commits it. Use Undo if the result is wrong.", "Open Sprite Browser", openSpriteBrowser),
            new Step("5. Validate and save", "Choose Validate, then save an edited workspace map to a separate path. Review the saved diff before promoting it. The Workbench never deploys or restarts a server.", "Open Tile editor", openTileEditor)
        ];

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(14) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(description, 0, 1);
        layout.Controls.Add(action, 0, 2);
        layout.Controls.Add(status, 0, 3);
        var navigation = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        navigation.Controls.Add(skip);
        navigation.Controls.Add(next);
        navigation.Controls.Add(back);
        layout.Controls.Add(navigation, 0, 4);
        Controls.Add(layout);

        action.Click += (_, _) => steps[stepIndex].Action();
        back.Click += (_, _) => { if (stepIndex > 0) { stepIndex--; ShowStep(); } };
        next.Click += (_, _) => Advance();
        skip.Click += (_, _) => Close();
        FormClosed += (_, e) => { if (e.CloseReason == CloseReason.UserClosing) Dismissed?.Invoke(this, EventArgs.Empty); };
        ShowStep();
    }

    private void Advance()
    {
        if (steps[stepIndex].RequiresMap && !hasOpenMap())
        {
            status.Text = "Open a map before continuing.";
            return;
        }

        if (stepIndex == steps.Length - 1)
        {
            Close();
            return;
        }

        stepIndex++;
        ShowStep();
    }

    private void ShowStep()
    {
        var step = steps[stepIndex];
        heading.Text = step.Title;
        description.Text = step.Description;
        progress.Text = $"Step {stepIndex + 1} of {steps.Length}";
        status.Text = "";
        action.Text = step.ActionLabel;
        action.Enabled = !(step.RequiresMap && hasOpenMap());
        next.Text = stepIndex == steps.Length - 1 ? "Finish" : "Next";
        back.Enabled = stepIndex > 0;
    }

    private sealed record Step(string Title, string Description, string ActionLabel, Action Action, bool RequiresMap = false);
}