namespace AstoniaMapWorkbench;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--sprite-check")
        {
            var archive = new SpriteArchive(args[1]);
            var image = archive.Get(uint.Parse(args[2]));
            Console.WriteLine(image is null ? archive.LastError ?? "sprite missing" : $"ok {image.Bitmap.Width}x{image.Bitmap.Height} offset {image.XOffset},{image.YOffset}");
            Environment.ExitCode = image is null ? 2 : 0;
            return;
        }
        if (args.Length == 4 && args[0] == "--sprite-preview")
        {
            var archive = new SpriteArchive(args[1]);
            var image = archive.Get(uint.Parse(args[2]));
            if (image is null) { File.WriteAllText(args[3] + ".error.txt", archive.LastError ?? "sprite missing"); Environment.ExitCode = 2; return; }
            image.Bitmap.Save(args[3]);
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(args));
    }
}
