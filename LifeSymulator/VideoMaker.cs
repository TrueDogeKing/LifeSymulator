using System.Diagnostics;
using System.IO;

namespace LifeSymulator
{
    // makes mp4 from png frames using ffmpeg (WPF can't encode video by itself)
    // ffmpeg.exe has to be next to LifeSymulator.exe or on PATH
    public static class VideoMaker
    {
        public static string? FindFfmpeg()
        {
            string local = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(local)) return local;

            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string f = Path.Combine(dir.Trim(), "ffmpeg.exe");
                    if (File.Exists(f)) return f;
                }
                catch { } // weird entries in PATH
            }
            return null;
        }

        // frames have to be named frame_00001.png, frame_00002.png ...
        // returns error message or null when ok
        public static async Task<string?> MakeVideo(string ffmpeg, string framesFolder, int fps, string outFile)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = ffmpeg;
            psi.WorkingDirectory = framesFolder;
            // pad: h264 needs even width and height, yuv420p so every player can open it
            psi.Arguments = $"-y -loglevel error -framerate {fps} -i frame_%05d.png " +
                            "-vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\" -c:v libx264 -pix_fmt yuv420p " +
                            $"\"{outFile}\"";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;

            using (var p = Process.Start(psi)!)
            {
                string err = await p.StandardError.ReadToEndAsync();
                await p.WaitForExitAsync();
                if (p.ExitCode != 0)
                    return err.Trim() == "" ? "ffmpeg exit code " + p.ExitCode : err.Trim();
            }
            return null;
        }
    }
}
