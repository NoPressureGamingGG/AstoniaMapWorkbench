using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace AstoniaMapWorkbench;

/// <summary>
/// Isometric, editor-facing map viewport. Its 40x20 diamond projection and
/// layer order follow the legacy editor/client renderer, rather than a flat
/// diagnostic grid.
/// </summary>
internal sealed class MapCanvas : Control
{
    private const float NativeTileWidth = 40f;
    private const float NativeTileHeight = 20f;
    private int tileWidth = 40;
    private MapDocument? map;
    private Point selected = new(-1, -1);
    private Point viewCenter = new(24, 24);
    private Point dragStart;
    private Point dragCenter;
    private bool panning;
    private bool leftPressed;
    private bool selectionDragging;
    private Point selectionStartTile;
    private Point selectionEndTile;
    private Keys selectionModifiers;
    private SpriteArchive? sprites;
    private IReadOnlyDictionary<string, uint> characterSprites = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, uint> itemSprites = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<Point, MapTile> previewTiles = new Dictionary<Point, MapTile>();

    public event EventHandler<TileInteraction>? TileInteraction;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MapDocument? Map
    {
        get => map;
        set
        {
            map = value; selected = new Point(-1, -1); selectedTiles.Clear();
            if (map?.Tiles.Count > 0)
            {
                viewCenter = new Point(
                    (int)Math.Round(map.Tiles.Values.Average(tile => tile.X)),
                    (int)Math.Round(map.Tiles.Values.Average(tile => tile.Y)));
            }
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Point SelectedTile
    {
        get => selected;
        set
        {
            if (selected.X < 0) viewCenter = value;
            selected = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyCollection<Point> SelectedTiles => selectedTiles;

    private readonly HashSet<Point> selectedTiles = [];

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TileWidth
    {
        get => tileWidth;
        set { tileWidth = Math.Clamp(value, 10, 80); Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SpriteArchive? Sprites
    {
        get => sprites;
        set { sprites = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCollisionOverlay { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowWalls { get; set; } = true;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LowerWalls { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCharacters { get; set; } = true;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyDictionary<string, uint> CharacterSprites
    {
        get => characterSprites;
        set { characterSprites = value; Invalidate(); }
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyDictionary<string, uint> ItemSprites
    {
        get => itemSprites;
        set { itemSprites = value; Invalidate(); }
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowGrid { get; set; }

    public MapCanvas()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        MinimumSize = new Size(640, 480);
        BackColor = Color.FromArgb(18, 20, 24);
        Cursor = Cursors.Cross;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (map is null) return;
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;

        if (ShowGrid) DrawGrid(e.Graphics);

        // Legacy order: GS1, GS2, FS1, FS2. Within each layer, map depth is
        // x+y, so sprites naturally overlap toward the player-facing edge.
        DrawLayer(e.Graphics, tile => tile.GroundSprite & 0xffff, -10);
        DrawLayer(e.Graphics, tile => tile.GroundSprite >> 16, 0);
        if (ShowCharacters) DrawCharacters(e.Graphics);
        DrawLayer(e.Graphics, tile => LowerWalls ? sprites?.GetCutSprite((ushort)(tile.ForegroundSprite & 0xffff)) ?? (tile.ForegroundSprite & 0xffff) : tile.ForegroundSprite & 0xffff, -9, !ShowWalls);
        DrawLayer(e.Graphics, tile => LowerWalls ? sprites?.GetCutSprite((ushort)(tile.ForegroundSprite >> 16)) ?? (tile.ForegroundSprite >> 16) : tile.ForegroundSprite >> 16, 1, !ShowWalls);
        DrawItems(e.Graphics);
        DrawOverlays(e.Graphics);
        DrawSelectionPreview(e.Graphics);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        if (e.Button is MouseButtons.Middle or MouseButtons.Left)
        {
            dragStart = e.Location; dragCenter = viewCenter;
            leftPressed = e.Button == MouseButtons.Left;
            if (leftPressed && (ModifierKeys & Keys.Control) != 0)
            {
                selectionDragging = true;
                selectionStartTile = ScreenToTile(e.Location);
                selectionEndTile = selectionStartTile;
                selectionModifiers = ModifierKeys;
                Invalidate();
                return;
            }
            panning = e.Button == MouseButtons.Middle;
            if (panning) Cursor = Cursors.SizeAll;
            return;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (selectionDragging)
        {
            AutoPanAtEdge(e.Location);
            selectionEndTile = ScreenToTile(e.Location);
            Invalidate();
            return;
        }
        if (leftPressed && !panning && DistanceSquared(dragStart, e.Location) >= 16) { panning = true; Cursor = Cursors.SizeAll; }
        if (!panning) return;
        var start = ScreenToTile(dragStart, dragCenter);
        var current = ScreenToTile(e.Location, dragCenter);
        viewCenter = new Point(Math.Clamp(dragCenter.X + start.X - current.X, 0, 255), Math.Clamp(dragCenter.Y + start.Y - current.Y, 0, 255));
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button is not (MouseButtons.Left or MouseButtons.Middle)) return;
        var wasPanning = panning;
        var wasSelecting = selectionDragging;
        panning = false;
        leftPressed = false;
        selectionDragging = false;
        Cursor = Cursors.Cross;
        if (wasSelecting && e.Button == MouseButtons.Left)
        {
            selectionEndTile = ScreenToTile(e.Location);
            TileInteraction?.Invoke(this, new TileInteraction(selectionEndTile, selectionModifiers, selectionStartTile));
            Invalidate();
            return;
        }
        if (!wasPanning && e.Button == MouseButtons.Left)
        {
            var tile = ScreenToTile(e.Location);
            if (tile.X is >= 0 and <= 255 && tile.Y is >= 0 and <= 255)
            {
                selected = tile;
                selectedTiles.Clear();
                selectedTiles.Add(tile);
                Invalidate();
                TileInteraction?.Invoke(this, new TileInteraction(tile, ModifierKeys));
            }
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        TileWidth = Math.Clamp(tileWidth + Math.Sign(e.Delta) * 4, 10, 80);
    }

    public void RefreshTile() => Invalidate();

    public void SetPreview(IEnumerable<MapTile> tiles) => previewTiles = tiles.ToDictionary(tile => new Point(tile.X, tile.Y));
    public void ClearPreview() { previewTiles = new Dictionary<Point, MapTile>(); Invalidate(); }

    public void SetSelection(IEnumerable<Point> tiles)
    {
        selectedTiles.Clear();
        foreach (var tile in tiles.Where(tile => tile.X is >= 0 and <= 255 && tile.Y is >= 0 and <= 255)) selectedTiles.Add(tile);
        if (selectedTiles.Count > 0) selected = selectedTiles.First();
        Invalidate();
    }

    private void DrawGrid(Graphics graphics)
    {
        using var pen = new Pen(Color.FromArgb(48, 112, 122, 132));
        var radius = Math.Max(ClientSize.Width, ClientSize.Height) / Math.Max(tileWidth, 1) + 4;
        var minX = Math.Max(0, viewCenter.X - radius);
        var maxX = Math.Min(255, viewCenter.X + radius);
        var minY = Math.Max(0, viewCenter.Y - radius);
        var maxY = Math.Min(255, viewCenter.Y + radius);
        var halfWidth = tileWidth / 2f;
        var halfHeight = tileWidth / 4f;
        for (var y = minY; y <= maxY; y++) for (var x = minX; x <= maxX; x++)
        {
            var anchor = TileToScreen(x, y);
            if (InViewport(anchor)) graphics.DrawPolygon(pen, Diamond(anchor, halfWidth, halfHeight));
        }
    }

    private void DrawSelectionPreview(Graphics graphics)
    {
        if (!selectionDragging) return;
        var minX = Math.Clamp(Math.Min(selectionStartTile.X, selectionEndTile.X), 0, 255);
        var maxX = Math.Clamp(Math.Max(selectionStartTile.X, selectionEndTile.X), 0, 255);
        var minY = Math.Clamp(Math.Min(selectionStartTile.Y, selectionEndTile.Y), 0, 255);
        var maxY = Math.Clamp(Math.Max(selectionStartTile.Y, selectionEndTile.Y), 0, 255);
        using var brush = new SolidBrush(Color.FromArgb(48, 80, 170, 255));
        using var pen = new Pen(Color.FromArgb(190, 120, 210, 255), 1);
        for (var y = minY; y <= maxY; y++) for (var x = minX; x <= maxX; x++)
        {
            var diamond = Diamond(TileToScreen(x, y), tileWidth / 2f, tileWidth / 4f);
            graphics.FillPolygon(brush, diamond);
            graphics.DrawPolygon(pen, diamond);
        }
    }

    private void AutoPanAtEdge(Point location)
    {
        const int edge = 28;
        var dx = location.X < edge ? -1 : location.X > ClientSize.Width - edge ? 1 : 0;
        var dy = location.Y < edge ? -1 : location.Y > ClientSize.Height - edge ? 1 : 0;
        if (dx == 0 && dy == 0) return;
        viewCenter = new Point(Math.Clamp(viewCenter.X + dx, 0, 255), Math.Clamp(viewCenter.Y + dy, 0, 255));
    }

    private void DrawLayer(Graphics graphics, Func<MapTile, uint> chooseSprite, int yAdjust, bool hideWallSprites = false)
    {
        if (map is null || sprites is null) return;
        foreach (var tile in PreviewOrMapTiles().OrderBy(tile => tile.X + tile.Y).ThenBy(tile => tile.X))
        {
            var anchor = TileToScreen(tile.X, tile.Y);
            if (!InViewport(anchor)) continue;
            var sprite = chooseSprite(tile);
            if (sprite == 0) continue;
            if (hideWallSprites && sprites.IsWallSprite(sprite)) continue;
            var image = sprites.Get(sprite);
            if (image is null) continue;
            var scale = tileWidth / NativeTileWidth;
            var destination = new RectangleF(
                anchor.X + image.XOffset * scale,
                anchor.Y + (image.YOffset + yAdjust) * scale,
                image.Bitmap.Width * scale,
                image.Bitmap.Height * scale);
            graphics.DrawImage(image.Bitmap, destination);
        }
    }

    private void DrawCharacters(Graphics graphics)
    {
        if (map is null || sprites is null) return;
        foreach (var tile in PreviewOrMapTiles().Where(tile => tile.Character is not null).OrderBy(tile => tile.X + tile.Y).ThenBy(tile => tile.X))
        {
            if (tile.Character is null || !characterSprites.TryGetValue(tile.Character, out var sprite) || sprite == 0) continue;
            var image = sprites.GetCharacterFrame(sprite);
            if (image is null) continue;
            var anchor = TileToScreen(tile.X, tile.Y);
            if (!InViewport(anchor)) continue;
            var scale = tileWidth / NativeTileWidth;
            graphics.DrawImage(image.Bitmap, new RectangleF(anchor.X + image.XOffset * scale, anchor.Y + image.YOffset * scale, image.Bitmap.Width * scale, image.Bitmap.Height * scale));
        }
    }

    private void DrawItems(Graphics graphics)
    {
        if (map is null || sprites is null) return;
        foreach (var tile in PreviewOrMapTiles().Where(tile => tile.Item is not null).OrderBy(tile => tile.X + tile.Y).ThenBy(tile => tile.X))
        {
            if (!itemSprites.TryGetValue(tile.Item!, out var sprite) || sprite == 0) continue;
            var image = sprites.Get(sprite); if (image is null) continue;
            var anchor = TileToScreen(tile.X, tile.Y); if (!InViewport(anchor)) continue;
            var scale = tileWidth / NativeTileWidth;
            graphics.DrawImage(image.Bitmap, new RectangleF(anchor.X + image.XOffset * scale, anchor.Y + (image.YOffset - 8) * scale, image.Bitmap.Width * scale, image.Bitmap.Height * scale));
        }
    }

    private IEnumerable<MapTile> PreviewOrMapTiles() => previewTiles.Count == 0 ? map?.Tiles.Values ?? Enumerable.Empty<MapTile>() : previewTiles.Values;

    private void DrawOverlays(Graphics graphics)
    {
        if (map is null) return;
        var halfWidth = tileWidth / 2f;
        var halfHeight = tileWidth / 4f;
        foreach (var tile in map.Tiles.Values)
        {
            var anchor = TileToScreen(tile.X, tile.Y);
            if (!InViewport(anchor)) continue;
            if (ShowCollisionOverlay)
            {
                var color = OverlayColor(tile);
                if (color.A != 0)
                {
                    using var brush = new SolidBrush(color);
                    graphics.FillPolygon(brush, Diamond(anchor, halfWidth, halfHeight));
                }
            }
            if (selectedTiles.Contains(new Point(tile.X, tile.Y)))
            {
                using var pen = new Pen(Color.Gold, 2);
                graphics.DrawPolygon(pen, Diamond(anchor, halfWidth, halfHeight));
            }
        }
    }

    private PointF TileToScreen(int x, int y)
    {
        var scale = tileWidth / NativeTileWidth;
        return new PointF(
            ClientSize.Width / 2f + ((x - viewCenter.X) - (y - viewCenter.Y)) * NativeTileWidth / 2f * scale,
            ClientSize.Height / 2f + ((x - viewCenter.X) + (y - viewCenter.Y)) * NativeTileHeight / 2f * scale);
    }

    private Point ScreenToTile(Point screen, Point? centerOverride = null)
    {
        var center = centerOverride ?? viewCenter;
        var scale = tileWidth / NativeTileWidth;
        var horizontal = (screen.X - ClientSize.Width / 2f) / (NativeTileWidth / 2f * scale);
        var vertical = (screen.Y - ClientSize.Height / 2f) / (NativeTileHeight / 2f * scale);
        return new Point((int)Math.Round(center.X + (horizontal + vertical) / 2f), (int)Math.Round(center.Y + (vertical - horizontal) / 2f));
    }

    private bool InViewport(PointF point) => point.X > -160 && point.Y > -160 && point.X < ClientSize.Width + 160 && point.Y < ClientSize.Height + 160;
    private static int DistanceSquared(Point first, Point second) => (first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y);
    private static PointF[] Diamond(PointF center, float halfWidth, float halfHeight) => [new(center.X, center.Y - halfHeight), new(center.X + halfWidth, center.Y), new(center.X, center.Y + halfHeight), new(center.X - halfWidth, center.Y)];
    private static Color OverlayColor(MapTile tile)
    {
        if (tile.Flags.Contains("MF_MOVEBLOCK")) return Color.FromArgb(112, 210, 45, 45);
        if (tile.Flags.Contains("MF_SIGHTBLOCK")) return Color.FromArgb(112, 60, 60, 75);
        if (tile.Flags.Contains("MF_SOUNDBLOCK")) return Color.FromArgb(112, 40, 80, 220);
        return Color.Transparent;
    }
}

internal sealed record TileInteraction(Point Tile, Keys Modifiers, Point? SelectionStart = null);
