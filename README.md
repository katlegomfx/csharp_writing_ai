# csharp_writing_ai

Please note that you'll need to install several NuGet packages and set up your environment accordingly for the code to work.

### NuGet Packages Required:

- **Tesseract 4.1.0** (for OCR functionality)
- **OpenCvSharp4 4.5.4.20211225** (for image processing)
- **Newtonsoft.Json 13.0.1** (for JSON serialization/deserialization)


### Explanation:

- **BringChromeToFront()**: Uses `Process.GetProcessesByName` to find the Chrome process and brings its window to the foreground using `SetForegroundWindow`.

- **ScreenshotWindowToBase64()**: Captures a screenshot of the Chrome window using `Graphics.CopyFromScreen` and converts it to a Base64 string.

- **OcrFromBase64()**: Performs OCR on the Base64 image using Tesseract OCR. Make sure the `tessdata` folder is correctly placed in your project directory.

- **BuildTextQuery()**: Constructs the message payload to send to the Groq API.

- **TextQuery()**: Sends an HTTP POST request to the Groq API endpoint with the provided messages and handles the response.

- **ImagesAreDifferent()**: Compares two images using OpenCvSharp to determine if they are different beyond a certain threshold.

- **Main()**: The entry point of the application. It brings the Chrome window to the front, enters a loop where it captures screenshots, performs OCR, checks for text changes, and sends queries to the Groq API.

### Important Notes:

- **Groq API**: Replace `"https://api.groq.com/v1/chat/completions"` with the actual Groq API endpoint if different. Ensure that the request payload matches the API's expected format.

- **Tesseract OCR**: You need to have Tesseract OCR installed on your system, and the `tessdata` folder should be in the executable directory or provide the correct path.

- **OpenCvSharp**: Ensure that the native OpenCV libraries are correctly installed and configured.

- **Error Handling**: Additional error handling can be added as needed to make the application more robust.

- **Threading**: The `Thread.Sleep(10000)` call pauses the loop for 10 seconds, similar to `time.sleep(10)` in Python.

- **Async/Await**: The `TextQuery` method is asynchronous to handle the HTTP request without blocking the main thread.

### Usage:

1. **Set Environment Variable**: Ensure that the `GROQ_API_KEY` environment variable is set on your system.

2. **Install Dependencies**: Install the required NuGet packages via the Package Manager Console:
   ```
   Install-Package Tesseract -Version 4.1.0
   Install-Package OpenCvSharp4 -Version 4.5.4.20211225
   Install-Package Newtonsoft.Json -Version 13.0.1
   ```

3. **Compile and Run**: Build the project and run the executable. Make sure Chrome is running so the script can interact with it.

### Disclaimer:

- **Security**: Be cautious with API keys and sensitive data. Do not hard-code them into your source code if the code will be shared.

- **Permissions**: Running this script may require administrative privileges, especially when interacting with other application windows.

- **Performance**: Processing large images and performing OCR can be resource-intensive. Monitor your application's performance and optimize as needed.
