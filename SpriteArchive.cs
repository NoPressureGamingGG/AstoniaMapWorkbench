using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace AstoniaMapWorkbench;

/// <summary>Read-only decoder for the legacy Astonia sprite .pak format.</summary>
internal sealed class SpriteArchive
{
    private readonly string pakDirectory;
    private readonly Dictionary<int, PakFile> packs = [];
    private readonly Dictionary<uint, SpriteImage?> sprites = [];
    private readonly Dictionary<uint, uint> runtimeBaseSprites = [];
    private readonly Dictionary<uint, uint> characterBaseSprites = [];
    private readonly Dictionary<uint, uint> cutSprites = [];
    private readonly List<ZipArchive> currentArchives = [];

    public string PakDirectory => pakDirectory;
    public string? LastError { get; private set; }

    public SpriteArchive(string pakDirectory)
    {
        if (!Directory.Exists(pakDirectory)) throw new DirectoryNotFoundException($"Sprite package directory not found: {pakDirectory}");
        this.pakDirectory = pakDirectory;
        LoadCurrentClientArchives();
        LoadRuntimeVariants();
    }

    public SpriteImage? Get(uint sprite)
    {
        if (sprite == 0) return null;
        if (sprites.TryGetValue(sprite, out var cached)) return cached;
        var image = TryGetCurrent(sprite);
        image ??= TryGetRaw(sprite);
        if (image is not null) return sprites[sprite] = image;
        if (runtimeBaseSprites.TryGetValue(sprite, out var baseSprite) && baseSprite != sprite)
        {
            image = TryGetRaw(baseSprite);
            if (image is not null) return sprites[sprite] = image;
        }
        return sprites[sprite] = null;
    }

    public SpriteImage? GetCharacterFrame(uint character)
    {
        var baseCharacter = characterBaseSprites.TryGetValue(character, out var mapped) ? mapped : character;
        return Get(100000 + baseCharacter * 1000);
    }

    public uint GetCutSprite(uint sprite)
    {
        var resolved = runtimeBaseSprites.TryGetValue(sprite, out var baseSprite) ? baseSprite : sprite;
        if (!IsWallFamily(resolved) || !cutSprites.TryGetValue(resolved, out var cut)) return resolved;
        return cut;
    }

    public bool IsWallSprite(uint sprite)
    {
        var resolved = runtimeBaseSprites.TryGetValue(sprite, out var baseSprite) ? baseSprite : sprite;
        return IsWallFamily(resolved);
    }

    private static bool IsWallFamily(uint sprite) =>
        sprite is >= 13000 and <= 15999 or >= 17000 and <= 17043 or >= 22000 and <= 23238 or >= 59000 and <= 59999;

    private SpriteImage? TryGetCurrent(uint sprite)
    {
        var filename = $"{sprite:00000000}.png";
        foreach (var archive in currentArchives)
        {
            var entry = archive.GetEntry(filename);
            if (entry is null) continue;
            try
            {
                using var stream = entry.Open();
                using var source = Image.FromStream(stream);
                return DecodeClientPng(new Bitmap(source));
            }
            catch (Exception ex) { LastError = $"Sprite {sprite}: {ex.Message}"; }
        }
        return null;
    }

    private static SpriteImage? DecodeClientPng(Bitmap source)
    {
        var left = source.Width;
        var top = source.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        {
            var color = source.GetPixel(x, y);
            var transparent = color.A == 0 || (color.R == 255 && color.G == 0 && color.B == 255);
            if (transparent) continue;
            left = Math.Min(left, x); top = Math.Min(top, y);
            right = Math.Max(right, x); bottom = Math.Max(bottom, y);
        }

        if (right < left || bottom < top) { source.Dispose(); return null; }
        var cropped = new Bitmap(right - left + 1, bottom - top + 1, PixelFormat.Format32bppArgb);
        for (var y = top; y <= bottom; y++) for (var x = left; x <= right; x++)
        {
            var color = source.GetPixel(x, y);
            if (color.R == 255 && color.G == 0 && color.B == 255) color = Color.Transparent;
            cropped.SetPixel(x - left, y - top, color);
        }
        var xOffset = (short)(-(source.Width / 2) + left);
        var yOffset = (short)(-(source.Height / 2) + top);
        source.Dispose();
        return new SpriteImage(cropped, xOffset, yOffset);
    }

    private SpriteImage? TryGetRaw(uint sprite)
    {
        try
        {
            var block = (int)(sprite / 1000) * 1000;
            if (!packs.TryGetValue(block, out var pack))
            {
                var file = Path.Combine(pakDirectory, $"{block:00000000}.pak");
                packs[block] = pack = PakFile.Load(file);
            }
            return pack.Decode(sprite);
        }
        catch (Exception ex)
        {
            LastError = $"Sprite {sprite}: {ex.Message}";
            return null;
        }
    }

    private void LoadRuntimeVariants()
    {
        // These extended wall IDs are present in the Server 3 maps but are
        // rendered by the client as color/light variants of the base sprites.
        runtimeBaseSprites[59155] = 14030;
        runtimeBaseSprites[59156] = 14031;
        runtimeBaseSprites[59157] = 14032;
        runtimeBaseSprites[59158] = 14033;
        // Current client wall families used by the Cameron dungeon. Keep the
        // explicit mappings as a fallback when metadata is unavailable.
        for (uint sprite = 59171; sprite <= 59174; sprite++) cutSprites[sprite] = sprite + 4;
        for (uint sprite = 14090; sprite <= 14093; sprite++) cutSprites[sprite] = sprite + 4;
        for (uint sprite = 14303; sprite <= 14311; sprite++) cutSprites[sprite] = sprite + 18;
        var directory = new DirectoryInfo(pakDirectory);
        while (directory is not null)
        {
            var config = Path.Combine(directory.FullName, "AU Client", "astonia_community_client-server3", "res", "config", "animated_variants.json");
            if (File.Exists(config))
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(config));
                    foreach (var entry in document.RootElement.GetProperty("animated_variants").EnumerateArray())
                    {
                        if (entry.TryGetProperty("id", out var id) && entry.TryGetProperty("base_sprite", out var baseSprite)) runtimeBaseSprites[id.GetUInt32()] = baseSprite.GetUInt32();
                    }
                }
                catch { }
                var root = Path.GetDirectoryName(config)!;
                LoadCharacterVariants(Path.Combine(root, "character_variants.json"));
                LoadCutMetadata(Path.Combine(root, "sprite_metadata.json"));
                return;
            }
            directory = directory.Parent;
        }
    }

    private void LoadCurrentClientArchives()
    {
        var directory = new DirectoryInfo(pakDirectory);
        while (directory is not null)
        {
            var resourceRoot = Path.Combine(directory.FullName, "AU Client", "astonia_community_client-server3", "res");
            if (Directory.Exists(resourceRoot))
            {
                foreach (var name in new[] { "gx1_mod.zip", "gx1_patch.zip", "gx1.zip" })
                {
                    var path = Path.Combine(resourceRoot, name);
                    if (!File.Exists(path)) continue;
                    try { currentArchives.Add(ZipFile.OpenRead(path)); } catch { }
                }
                return;
            }
            directory = directory.Parent;
        }
    }

    private void LoadCharacterVariants(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var entry in document.RootElement.GetProperty("character_variants").EnumerateArray())
                if (entry.TryGetProperty("id", out var id) && entry.TryGetProperty("base_sprite", out var baseSprite)) characterBaseSprites[id.GetUInt32()] = baseSprite.GetUInt32();
        }
        catch { }
    }

    private void LoadCutMetadata(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var entry in document.RootElement.GetProperty("sprite_metadata").EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var id)) continue;
                var first = id.GetUInt32(); var last = entry.TryGetProperty("id_end", out var end) ? end.GetUInt32() : first;
                if (entry.TryGetProperty("cut_sprite", out var cut)) for (var sprite = first; sprite <= last; sprite++) cutSprites[sprite] = cut.GetUInt32();
                else if (entry.TryGetProperty("cut_offset", out var offset)) for (var sprite = first; sprite <= last; sprite++) cutSprites[sprite] = sprite + offset.GetUInt32();
            }
        }
        catch { }
    }

    internal sealed record SpriteImage(Bitmap Bitmap, short XOffset, short YOffset);

    private sealed class PakFile
    {
        private readonly string path;
        private readonly Dictionary<uint, Entry> entries;
        private readonly ushort[] palette;

        private PakFile(string path, Dictionary<uint, Entry> entries, ushort[] palette)
        {
            this.path = path; this.entries = entries; this.palette = palette;
        }

        public static PakFile Load(string path)
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            _ = reader.ReadUInt32(); // package timestamp
            var count = reader.ReadInt32();
            if (count < 0 || count > 10000) throw new InvalidDataException("Invalid sprite package index.");
            var entries = new Dictionary<uint, Entry>(count);
            var lastEnd = 0L;
            for (var i = 0; i < count; i++)
            {
                var offset = reader.ReadUInt32(); var total = reader.ReadUInt32(); var id = reader.ReadUInt32(); var type = reader.ReadInt32();
                entries[id] = new Entry(offset, total, type);
                lastEnd = Math.Max(lastEnd, (long)offset + total);
            }
            reader.BaseStream.Position = lastEnd;
            var paletteCount = reader.ReadInt32() & 0x00ffffff;
            if (paletteCount < 0 || paletteCount > 256) throw new InvalidDataException("Invalid sprite package palette.");
            var palette = new ushort[paletteCount];
            for (var i = 0; i < paletteCount; i++) palette[i] = reader.ReadUInt16();
            return new PakFile(path, entries, palette);
        }

        public SpriteImage? Decode(uint id)
        {
            if (!entries.TryGetValue(id, out var entry)) return null;
            using var reader = new BinaryReader(File.OpenRead(path));
            reader.BaseStream.Position = entry.Offset;
            var compressedLength = reader.ReadInt32(); var uncompressedLength = reader.ReadInt32();
            if (compressedLength < 0 || uncompressedLength < 0 || compressedLength > entry.TotalSize) throw new InvalidDataException("Invalid compressed sprite entry.");
            var compressed = reader.ReadBytes(compressedLength);
            var width = reader.ReadUInt16(); var height = reader.ReadUInt16(); var xOffset = reader.ReadInt16(); var yOffset = reader.ReadInt16();
            if (width == 0 || height == 0 || width > 512 || height > 512) throw new InvalidDataException($"Invalid sprite dimensions {width}x{height}.");
            var data = Inflate(compressed, uncompressedLength);
            var pixels = DecodePixels(entry.Type, data, width, height);
            if (pixels is null) throw new InvalidDataException("Sprite pixel stream is malformed.");
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var locked = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try { Marshal.Copy(pixels, 0, locked.Scan0, pixels.Length); }
            finally { bitmap.UnlockBits(locked); }
            return new SpriteImage(bitmap, xOffset, yOffset);
        }

        private static byte[] Inflate(byte[] compressed, int expectedLength)
        {
            using var input = new MemoryStream(compressed);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream(expectedLength);
            zlib.CopyTo(output);
            var result = output.ToArray();
            if (result.Length != expectedLength) throw new InvalidDataException("Sprite decompression length mismatch.");
            return result;
        }

        private byte[]? DecodePixels(int type, byte[] data, int width, int height)
        {
            var pixels = new byte[width * height * 4]; var source = 0;
            for (var i = 0; i < width * height; i++)
            {
                if (source >= data.Length) return null;
                var alpha = data[source++]; ushort color = 0;
                if (type == 1)
                {
                    if (alpha != 0)
                    {
                        if (source + 2 >= data.Length) return null;
                        color = (ushort)((data[source++] << 10) | (data[source++] << 5) | data[source++]);
                    }
                }
                else if (type == 2)
                {
                    if (alpha != 0)
                    {
                        if (source >= data.Length) return null;
                        var index = data[source++]; if (index >= palette.Length) return null; color = palette[index];
                    }
                }
                else if (type == 3)
                {
                    if (palette.Length <= 128)
                    {
                        if (alpha != 0)
                        {
                            var mask = palette.Length <= 64 ? 0xc0 : 0x80;
                            var index = alpha & ~mask; alpha = (byte)((alpha & mask) == mask ? 31 : (alpha & mask) >> 3);
                            if (index >= palette.Length) return null; color = palette[index];
                        }
                    }
                    else
                    {
                        if (alpha != 0)
                        {
                            if (source >= data.Length) return null;
                            var index = data[source++]; if (index >= palette.Length) return null; color = palette[index];
                        }
                    }
                }
                else return null;
                var output = i * 4;
                pixels[output] = (byte)((color & 31) * 255 / 31);
                pixels[output + 1] = (byte)(((color >> 5) & 31) * 255 / 31);
                pixels[output + 2] = (byte)(((color >> 10) & 31) * 255 / 31);
                pixels[output + 3] = (byte)(alpha == 0 ? 0 : Math.Min(255, alpha * 255 / 31));
            }
            return pixels;
        }

        private sealed record Entry(uint Offset, uint TotalSize, int Type);
    }
}
