using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NovelGame2D
{
    public class ImageRepair
    {
        public static void RunRepair()
        {
            string contentDir = @"E:\GAME\NovelGame2D\Content\Images";
            string[] files = { "bg.png", "char.png", "char_smile.png", "char_smile_transparent.png", "char_transparent.png" };

            Console.WriteLine("--- Image Repair Tool ---");
            foreach (var fileName in files)
            {
                string path = Path.Combine(contentDir, fileName);
                if (File.Exists(path))
                {
                    try
                    {
                        using (var image = Image.Load<Rgba32>(path))
                        {
                            // Save as standard PNG to fix any corruption/format issues
                            image.SaveAsPng(path);
                            Console.WriteLine($"[OK] Repaired and re-saved: {fileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to fix {fileName}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[SKIP] Not found: {fileName}");
                }
            }
            Console.WriteLine("--------------------------");
        }
    }
}
