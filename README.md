# kinect

fun stuff you can do with an xbox kinect sensor

## Prerequisites

-   an xbox 360 kinect sensor + usb adapter
-   [Kinect for Windows SDK v1.8](https://www.microsoft.com/en-gb/download/details.aspx?id=40278)
-   [Kinect for Windows Developer Toolkit v1.8](https://www.microsoft.com/en-us/download/details.aspx?id=40276) for examples
-   .NET framework 4.8

### Adding the driver to C# project

-   open in Visual Studio
-   under the Solution menu right click _References_ and _Add reference..._
-   navigate to the directory where the xbox driver is installed and select `Microsoft.Kinect.dll`
-   now you can import: `using Microsoft.Kinect;`
