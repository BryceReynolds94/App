# PagerBuddy-App

PagerBuddy is an OpenSource project to propagate alerts for members of emergency services (firebrigades, medical, catastrophe response, ...). The project consists of three components: "Interfaces" forward incoming external alerts (f.e. from a physical pager) or generated alerts to the server. The "[server](https://github.com/PagerBuddy/Server)" handles incoming information from many different interfaces, aggregates alerts and sends notifications to users (currently supporting Telegram and PagerBuddy-App). Users can optionally install the "app" to be notified in an urgent manner about alerts on their smartphone.

This repo is for the "app" component.

Current features:
* Receive alerts from PagerBuddy-Server through FCM (Android) or APNs (iOS)
* Play sound and show notification independant of current phone settings
* Android: Set time filters and customise alert
* Authentication and setup using Telegram

## App Installation

1. Get the app from the appropriate [store](bartunik.de/pagerbuddy)

2. Authenticate using your Telegram credentials (phone number, code is sent in Telegram)

3. Alerts are automatically loaded from server and set up

4. Accept all permission prompts - these are necessary to ensure a notification is displayed correctly

## Development Setup
PagerBuddy-App uses the Xamarin platform: Xamarin.Forms for UI elements and general implementation, Xamarin.Android/Xamarin.iOS for platform specific interactions (permissions, notifications, incoming alerts, ...).

1. Set up your Xamarin IDE (we use [Visual Studio](https://dotnet.microsoft.com/en-us/learn/xamarin/hello-world-tutorial/install))

2. Clone this repository (if you have [git](https://git-scm.com/downloads))
   ```
   git clone https://github.com/PagerBuddy/App.git
   ```

3. Open the .sln in Visual Studio and enjoy

4. To run the projects you will need to do some platform-specific setup:
   * You can use your smartphone or a simulator for Android: [Instructions](https://dotnet.microsoft.com/en-us/learn/xamarin/hello-world-tutorial/devicesetup)
   * For iOS you will need a mac that meets the [requirements](https://developer.apple.com/support/xcode/) for the current version of Xcode. You also need a [Apple developer account](https://developer.apple.com/programs/enroll/) (subject to a fee: ~100€/year). To be able to receive and test push notifications you need an actual iPhone. Yes, Apple makes our life difficult...
