using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenCvSharp;
using Tesseract;

namespace ChromeScreenOCR
{
    class Program
    {
        // Import necessary Windows API functions
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool OpenIcon(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        static string GROQ_API_KEY = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        const string AI_VISION_MODEL = "llama-3.2-11b-vision-preview";
        const string AI_TEXT_MODEL = "llama-3.2-90b-text-preview";

        static async Task Main(string[] args)
        {
            try
            {
                string prevText = "";
                string prevImage = null;

                // Bring Chrome to the front and get the window handle
                IntPtr chromeWindow = BringChromeToFront();
                if (chromeWindow == IntPtr.Zero)
                {
                    Console.WriteLine("Cannot proceed without Chrome window.");
                    return;
                }

                while (true)
                {
                    // Take a screenshot of the Chrome window and perform OCR on it
                    string base64Image = ScreenshotWindowToBase64(chromeWindow);
                    string extractedText = OcrFromBase64(base64Image);

                    string currentImage = base64Image;
                    string currentText = extractedText;

                    if (prevImage != null)
                    {
                        // Check for text changes
                        if (currentText != prevText)
                        {
                            var textMessages = BuildTextQuery(extractedText);
                            string textResponse = await TextQuery(textMessages);
                            Console.WriteLine($"\nText response: {textResponse}");
                        }

                        // Uncomment the following lines if you want to check for image changes
                        // if (ImagesAreDifferent(currentImage, prevImage))
                        // {
                        //     Console.WriteLine("Image on the screen has changed.");
                        //     // Handle image change as needed
                        // }

                    }

                    // Update previous text and image
                    prevText = currentText;
                    prevImage = currentImage;

                    Thread.Sleep(10000); // Sleep for 10 seconds
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script terminated: {ex.Message}");
            }
        }

        public static IntPtr BringChromeToFront()
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("chrome");
            if (processes.Length > 0)
            {
                var chromeProcess = processes[0];
                IntPtr handle = chromeProcess.MainWindowHandle;
                if (handle == IntPtr.Zero)
                {
                    Console.WriteLine("Chrome window not found.");
                    return IntPtr.Zero;
                }

                if (IsIconic(handle))
                {
                    OpenIcon(handle);
                }

                SetForegroundWindow(handle);
                Console.WriteLine("Chrome window has been brought to the front.");
                return handle;
            }
            else
            {
                Console.WriteLine("Chrome process not found.");
                return IntPtr.Zero;
            }
        }

        public static string ScreenshotWindowToBase64(IntPtr hWnd)
        {
            RECT rect;
            if (!GetWindowRect(hWnd, out rect))
            {
                Console.WriteLine("Unable to get window rectangle.");
                return null;
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics gfx = Graphics.FromImage(bmp);
            gfx.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            gfx.Dispose();

            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                byte[] imageBytes = ms.ToArray();
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        public static string OcrFromBase64(string base64Image)
        {
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                Bitmap image = new Bitmap(ms);
                string extractedText = "";

                try
                {
                    using (var ocrEngine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                    {
                        using (var pix = PixConverter.ToPix(image))
                        {
                            using (var page = ocrEngine.Process(pix))
                            {
                                extractedText = page.GetText();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during OCR: {ex.Message}");
                }

                return extractedText;
            }
        }

        public class Message
        {
            [JsonProperty("role")]
            public string Role { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }
        }

        public static List<Message> BuildTextQuery(string text)
        {
            string query = $"How would you answer the following:\n{text}";
            var messages = new List<Message>
            {
                new Message { Role = "user", Content = query }
            };
            return messages;
        }

        public static async Task<string> TextQuery(List<Message> messages)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GROQ_API_KEY}");

                var requestData = new
                {
                    model = AI_TEXT_MODEL,
                    messages = messages,
                    temperature = 1,
                    max_tokens = 8192,
                    top_p = 1,
                    stream = false,
                    stop = (string)null
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.groq.com/v1/chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseText);
                    return responseText;
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                    return null;
                }
            }
        }

        public static bool ImagesAreDifferent(string base64Img1, string base64Img2, double threshold = 1000)
        {
            byte[] img1Data = Convert.FromBase64String(base64Img1);
            byte[] img2Data = Convert.FromBase64String(base64Img2);

            Mat img1 = Cv2.ImDecode(img1Data, ImreadModes.Color);
            Mat img2 = Cv2.ImDecode(img2Data, ImreadModes.Color);

            if (img1.Size() != img2.Size())
            {
                return true;
            }

            Mat diff = new Mat();
            Cv2.Absdiff(img1, img2, diff);
            Scalar sumScalar = Cv2.Sum(diff);
            double diffSum = sumScalar.Val0 + sumScalar.Val1 + sumScalar.Val2;

            return diffSum > threshold;
        }
    }
}
