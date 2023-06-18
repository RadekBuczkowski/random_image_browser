# Random Image Browser &nbsp; ![Icon](src/Resources/butterfly.ico?raw=true)

### Description

This is an image viewer implementing smooth animations and fluid repeated scrolling, similar to the browsing experience on an iPhone.
The program allows users to view multiple images on a single page and provides the ability to zoom in on each image with a mouse click.

All image transitions, including zooming, navigation, rotations, and changing image arrangements, **are accompanied by animations**.
This creates a highly satisfying user experience as images seamlessly move and scale from their original positions to the desired destination.

The browser groups images with similar names and shuffles the groups by default. Alternatively, it can display all images in a random order.
The program provides various configuration options and is designed to be easy-to-use and user-friendly.

By default, the browser supports the following image types: **jpg, jpeg, gif (including animated gifs), png, webp, avif**. However, users
can easily enable less common image extensions in the program's Options after launching the application.

Currently, the browser is available in three languages: **English, Danish, and German**. The default language is determined
by the operating system, but users can change it in the configuration settings. Detailed help window in each language can be accessed
by pressing F1 within the application.

### Compiled version

The compiled version of the application is available for installation using ClickOnce. You can access it
[at this link](https://htmlpreview.github.io/?https://raw.githubusercontent.com/RadekBuczkowski/random_image_browser/main/ClickOnce/Publish.html).

ClickOnce provides automatic updates when launching the application. Please note that the ClickOnce installation does not include attached certificates. While signing the application with a certificate is a costly endeavor (at $300 per year), it is not essential. If you choose to use ClickOnce, you need to disregard any warnings about an untrusted provider.

Alternatively, you can download the compiled version of the application as a tar.gz file [from this link](https://raw.githubusercontent.com/RadekBuczkowski/random_image_browser/main/publish/RandomImageBrowser.tar.gz).
Copy the content of the archive file to a folder. No installation is needed. Unlike ClickOnce, the archive file does not include the .NET Core 7 runtime.
If you don't already have it, you can download it [from here](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-7.0.4-windows-x64-installer).

### Performance

This application extensively utilizes the graphics card. To ensure smoothness and improve overall performance, it is recommended to close other applications that use the graphics card, particularly web browsers.

If, for any reason, the application becomes less fluid, you can press F4 to restart it and resolve the issue. The restart process is quick and is barely noticeable to the user. It maintains the entire browser state, window state, and image order. Additionally, there is an option to enable automatic restart after viewing a certain number of images.

### Screen-shots

Below are three screenshots showcasing a selection of available image arrangements.

![Icon](demo/demo1.jpg?raw=true)

![Icon](demo/demo2.jpg?raw=true)

![Icon](demo/demo3.jpg?raw=true)
