using System.Diagnostics;

namespace UniLibrary.Api.Services
{
    public class PdfPreviewService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PdfPreviewService> _logger;

        private static readonly HashSet<string> ConvertibleExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".doc",
            ".docx",
            ".txt",
            ".rtf",
            ".odt"
        };

        public PdfPreviewService(IConfiguration configuration, ILogger<PdfPreviewService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PdfPreviewResult> CreatePreviewPdfAsync(byte[] originalBytes, string originalFileName)
        {
            string extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (extension == ".pdf")
            {
                return PdfPreviewResult.Ok(originalBytes, true);
            }

            if (!ConvertibleExtensions.Contains(extension))
            {
                return PdfPreviewResult.Fail("Для цього типу файлу PDF-передперегляд поки не підтримується.");
            }

            string tempRoot = Path.Combine(Path.GetTempPath(), "unilibrary-preview", Guid.NewGuid().ToString("N"));
            string inputDirectory = Path.Combine(tempRoot, "input");
            string outputDirectory = Path.Combine(tempRoot, "output");

            Directory.CreateDirectory(inputDirectory);
            Directory.CreateDirectory(outputDirectory);

            string inputPath = Path.Combine(inputDirectory, $"source{extension}");

            try
            {
                await File.WriteAllBytesAsync(inputPath, originalBytes);

                string converterPath = GetLibreOfficePath();

                ProcessStartInfo startInfo = new()
                {
                    FileName = converterPath,
                    Arguments = $"--headless --convert-to pdf --outdir \"{outputDirectory}\" \"{inputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using Process process = new()
                {
                    StartInfo = startInfo
                };

                process.Start();

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                bool finished = await WaitForExitAsync(process, TimeSpan.FromSeconds(60));

                if (!finished)
                {
                    TryKill(process);
                    return PdfPreviewResult.Fail("LibreOffice не встиг виконати конвертацію за 60 секунд.");
                }

                string output = await outputTask;
                string error = await errorTask;

                if (process.ExitCode != 0)
                {
                    string details = string.IsNullOrWhiteSpace(error) ? output : error;
                    return PdfPreviewResult.Fail($"LibreOffice не зміг конвертувати файл у PDF. {details}".Trim());
                }

                string? pdfPath = Directory
                    .GetFiles(outputDirectory, "*.pdf")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

                if (pdfPath is null)
                {
                    return PdfPreviewResult.Fail("Після конвертації PDF-файл не був створений.");
                }

                byte[] pdfBytes = await File.ReadAllBytesAsync(pdfPath);
                return PdfPreviewResult.Ok(pdfBytes, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF preview conversion failed.");

                return PdfPreviewResult.Fail(
                    "Не вдалося створити PDF-передперегляд. Перевірте, чи встановлено LibreOffice, або вкажіть шлях у appsettings.json: PdfPreview:LibreOfficePath."
                );
            }
            finally
            {
                TryDeleteDirectory(tempRoot);
            }
        }

        private string GetLibreOfficePath()
        {
            string? configuredPath = _configuration["PdfPreview:LibreOfficePath"];

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return configuredPath;
            }

            string[] commonWindowsPaths =
            [
                @"C:\Program Files\LibreOffice\program\soffice.exe",
                @"C:\Program Files (x86)\LibreOffice\program\soffice.exe"
            ];

            foreach (string path in commonWindowsPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return OperatingSystem.IsWindows() ? "soffice.exe" : "libreoffice";
        }

        private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
        {
            Task waitTask = process.WaitForExitAsync();
            Task completedTask = await Task.WhenAny(waitTask, Task.Delay(timeout));
            return completedTask == waitTask;
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // ignore
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch
            {
                // ignore
            }
        }
    }

    public class PdfPreviewResult
    {
        public bool Success { get; set; }
        public bool OriginalIsPdf { get; set; }
        public byte[]? PdfBytes { get; set; }
        public string? ErrorMessage { get; set; }

        public static PdfPreviewResult Ok(byte[] pdfBytes, bool originalIsPdf)
        {
            return new PdfPreviewResult
            {
                Success = true,
                OriginalIsPdf = originalIsPdf,
                PdfBytes = pdfBytes
            };
        }

        public static PdfPreviewResult Fail(string errorMessage)
        {
            return new PdfPreviewResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
