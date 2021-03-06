YouTubeAPI
--------

#### YouTube Automation API ####

The goal of the project is to create a quick way of doing different YouTube requests in a much larger scale.


#### Current Attributes ####
- Multiple Account support with ability to save cookies and reuse
- Subscribe
- Unsibscribe
- Liking / Disliking Videos
- Multiple Account Profile Commenting
- Multiple Account Video Commenting
- Multiple Account Message Sending
- Settings (Wait Time, Blacklisting(Video + User), Captcha Find, .... More in USAGE)
- ChannelInformation Class allows you to specify a channel username and get information on their channel
- VideoIfnromation Class allows you to specify a videoID and get information about the video

#### USE - SETTINGS ####

- Imports
  - First and formost, you must reference and then import the dll into your project. That must be easy enough.
- Initialization of Settings
  - The main YouTube class requires a constructor of Settings. Settings holds all the settings to the API and makes it easy to modify settings on the fly. I would recommend globally naming it and then initializing it on start so you can use it wherever without redoing the settings over and over again. ```Dim settings As New ApiSettings```
    - ```.waitTime``` (Integer value which will determine the amount of seconds to wait inbetween each request)
    - ```.checkBlacklistedUser``` (Boolean where when true will determine if your List of blacklisted users will be used)
    - ```.blacklistedUsers``` (List of String. List of Blacklisted Users. The users in the list will be skipped for any request if .checkBlacklistedUser is TRUE)
    - ```.checkBlacklistedVideos``` (Boolean, same as BlackListUser but with videos)
    - ```.blacklistedVideos``` (List of String. List of Blacklisted Videos. The videos in the list will be skipped for any request if .checkBlacklistedVideos is TRUE. The string type that will be accepted is the videoID such as: TZFaW3wDFiY, UsifOwVcvVY)
    - ```.captchaShow``` (Boolean where when true wll determine if you want to show the captcha. If fale, captchaWaitTime will be used)
    - ```.captchaFailAmount``` (Boolean, where if captchaShow is TRUE, then this will determine the amount of fail attempts before skipping the request)
    - ```.captchaWaitTime``` (Integer valuewhich will determine the amount of seconds to wait if a captcha if found)
    - ```.minSubs``` (Interger. All the minXXX will be where if a number is set, it will check the channel of the User or Video and check to see if the filters are TRUE or false, if false, it will skip the specefic rwquest)
    - ```.minVidViews``` (SEE minSubs)
    - ```.minDateJoined``` (Date. Can set the minimum date for when a user has joined YouTube)
- Initialization of YouTube Class
  - The YouTube class has a constructor which requires ApiSettings. The documentation for the settings class is above ^. Below are the attributes for the class.
    - ```.Online()``` (Check to see if the User logged in is online)
    - ```.Account()``` (The current account that that class is using. I would not recommend playing with this unless you know what you are doing)
    - ```.AccountsArray()``` (The accounts array is a list of AccountInformation that has been used by the class. It can be a list of 1 - XX amount of users that have been sucessfully logged into YouTube. The AccountInformation class holdes the Credentials of the account and the cookies. I recommend using a global variable to store the account array when you are done using the YouTube class. This is so whenever you want to use the YouTube class elsewhere, you can do ```yt.AccountsArray = Account``` , where Account is the global variable which is a list(Of AccountInformation) and reuse the logged in users you had before instead of logging into each account seperatly.)
    - ```.Settings``` (Alongside the requirement of ApiSettings for the YouTube class, whenever you modify the settings, you can put it into the YouTube class by doing something along the lines of ```yt.settings = myGlobalSettings```)
    - ```.pause()``` (Pauses current class)
    - ```.unpause()``` (unpauses class)
    - ```.terminate()``` (Terminates anything running in class)
    - ```.RequestStoped``` (Return TRUE if stoped, FALSE it not)
    - ```.RequestPaused``` (Return TRUE if paused class, FALSE if not)
    - ```.captchaIMGLink``` (Returns the captcha Image link if there's any captcha found)
    - ```.captchaCode``` (Returns the captcha code you have set and you can set the captcha code if you need to input captcha to continue)
    - ```.Nofity()``` (Used to handled Events that come up in the class. See Captcha Handling for more information)
    - ```.Dispose()``` (Used to Dispose of the Class)

What we have so far. This is just a rough code, please do it on your own for your own liking:
```vb
Dim settings as new ApiSettings
with settings
  Dim blacklistedUsers as new List(Of String)
  blacklistedUsers.add("mike")
  blacklistedUsers.add("kyle")
  
  Dim blacklistedVideos as new List(Of String)
  blacklistedVideos.add("2DFtlNGzMIA")
  blacklistedVideos.add("2DFtlNGzMIB")
  
  .waitTime = 100
  .checkBlacklistedUsers = 1
  .blacklistedUsers = blacklistedUsers
  .blacklistedVideos = blacklistedVideos
  .checkBlacklistedVideos = 1
  .captchaShow = true
  .captchaFailAcount = 3
  .captchaWaitTime = 100
  .minSubs = nothing
  .minVidViews = nothing
  .minDateJoined = "02/01/2005"
end with

Dim yt as new YouTube(settings)
'Let's say the ApiSettings is a global and you modify the waitTime to 200. You can update the YT class with the new values.
settings.waitTime = 200
yt.settings = settings
yt.dispose() 'Dispose YouTube Class
```

#### The Real Meat - USE FUNCTIONS ####
- ```login(username as String, password as String)``` (Once you are done with the basics, you can go onto the real meat. To Login to a user account, just do Login(username, password). If the login is sucessful, the YouTube Classes Account variable will be currently set to the new information with the login cookies. Along with this, the AccountArray will also add the entry of the new account. If you were to login again with another account, the variable Account will be the new users information and the AccountArray will add the new account information. There will then be 2 items in the AccountArray, the last login, and the new login.)
- ```subscribe(usernames as List(Of String)``` (Will take the list of users and subscribe to each one in the list. The account it will use to do the subscriptions will be the master account, the first account you logged into.)
- ```unsubscribe(usernames as List(Of String)``` (When using the unsubscribe function, it takes in a list of usernames. The account which it will be used to subscribe to will be the first account that you logged into, the master account. Unsubscribe function will go down the list of users and subscribe to the users on YouTube.)
- ```sendMessage(Usernames as List(Of String), Messages as Dictionary(Of String (Subject), String (Body)), videoIDs as List(Of String), Accounts as Dictionary(Of String, String))``` (The Send Message Function will send specefic people on your username list messages that the bot alternates between. For example, if you have a list of usernames which contain, Albert, Peter, and Julia, subjects that contain Hello and Howdy, and bodies that Include "What is up" and "Hello Buddy". The function will first send to the User Albert, Subject: Hello, and Body: What is up". The next user will get the next set of Subject and body. If there are more than 2 and only 2 bodies, it will alternates between the 2 bodies after each user. If you inserted a list of videoID's, along with the message, it will attach a video to the message. Videos also alternate similar to how the subject and bodies work. Now for the list of Accounts, If you want to use multiple accounts to send messages and not just the main one, you can create a dictionary of Accounts (username, password) and then the function will Login to the account and alternate between usernames to send to users. Great thing about adding accounts to the function is the Credentials get saved to the .AccountsArray() where you can export it to a variable outside and then reuse the accounts later one. For example if you were to run a different function like Profile Comment, you can put the AccountsArray back into the Class and then the function wont have to relogin the accounts it has logged in before becuase the cookies and credentials were saved! Notice, that the master account you logged in with will not be used. If you want that to be used, you will have to put it into the Accounts Dictionary as a entry.)
- ```videoComment(videoIDs as List(Of String), Comments as List(Of String), Accounts as Dictionary(Of String, String))``` (Similar to how sendMessage works, you insert a list of videoID's to send comments to. Comments will alternate between the amount you have inputted in. The Accounts you put in will alternate and send messages from different accounts each time going down the list of accounts and starting over to the first account when it reaches the end, IF there are more videos to send comments to.)
- ```profileComment(Usernames as List(Of String), Comments as List(Of String), Accounts as Dictionary(Of String, String))``` (Exactly how videoComment works but with users and commenting to their profiles.)
- ```LDlVideos(VideoIDs as List(Of String), Accounts as Dictionary(Of String, String), Options as Boolean)``` (Allows you to Like / Dislike videos.)
What we have now using the Settings from the top part:

```vb
~~~ GLOBAL VARIABLE: Dim AccountsArr as List(Of AccountInformation) ~~~

Dim yt as new YouTube(settings) ' Initialize the YT class
yt.login("Mike", "HelloHello1") ' Login to an account
yt.AccountsArray = AccountsArr ' Alternative if you already have a list of Accounts.

Dim userSub as new List(Of String) ' List of users you add.
userSub.add("Kyle")
userSub.add("Kenny")
userSub.add("Eric")
userSub.add("Stan")

yt.subscribe(userSub) ' SUbscribe to List of users

Dim Messages as new Dictionary(Of String, String) 'Dictionary of Messages(Subject, Body)
Messages.add("Hello", "What's going on buddy?")
Dim Accounts as new Dictionary(Of String, String)
Accounts.add("Peter", "11111")
Accounts.add("Jimmy", "22222")

yt.profileComment(userSub, Messages, Accounts) ' Profile Comment

AccountsArr = yt.AccountsARRAY() ' Grab Login Credentials for any user you have logged into for use later on.
yt.dispose() 'Dispose YouTube Class
```

#### Handling Captcha
In order to handle captcha, you will make use of the yt.notify() function. You can insert a Notify in your main form and handle all the Event Hanlders through there if not threaded.

```
Public Class Form1
    Dim WithEvents yt As New YouTube(Nothing) 'Initialize the YouTube class.
    
    Public Sub Notify(ByVal Report() As Object) Handles yt.Notify
        Select Case Report(0)
            Case "CAPTCHA"
                MsgBox(yt.captchaIMGLink) 'Handle how to view the captcha image link
                yt.captchaCode = "CODE" 'Set the captcha code to what you see in the image.
        End Select
    End Sub
    
    Private Sub doit()
        Dim settings As New ApiSettings
        yt.Settings = settings
        yt.login("@gmail.com", "") ' Login to an account

        Dim ids As New List(Of String)
        ids.Add("7iabkt4F0KM")
        ids.Add("QKREEXxxVLs")
        ids.Add("eMJaHsRUnxc")
        ids.Add("S6xLBnQtCgk")
        ids.Add("5GQygK9L7ko")

        Dim accounts As New Dictionary(Of String, String)
        accounts.Add("@gmail.com", "")
        accounts.Add("@gmail.com", "")

        Dim comm As New List(Of String)
        comm.Add("NICE VIDEO!")
        comm.Add("gREAT VIDEO!!")

        yt.videoComment(ids, comm, accounts)
    End Sub
```
