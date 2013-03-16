Imports YoutubeAPI.Utility
Imports YoutubeAPI.Utility.Http
Imports YoutubeAPI.ApiSettings
Imports System.Text

Public Class YouTube
    Implements IDisposable
    Private Http As New Http

    Private _RequestStop As Boolean = False
    ''' <summary>
    ''' If TRUE, stops any request that is currently ongoing.
    ''' </summary>
    Public Property RequestStop As Boolean
        Get
            Return _RequestStop
        End Get
        Set(ByVal value As Boolean)
            _RequestStop = value
        End Set
    End Property

    Private _RequestPause As Boolean = False
    ''' <summary>
    ''' If TRUE, pauses the request that is currently ongoing. If FALSE, the request will start out where it left off.
    ''' </summary>
    Public Property RequestPause As Boolean
        Get
            Return _RequestPause
        End Get
        Set(ByVal value As Boolean)
            _RequestPause = value
        End Set
    End Property

    Private _Online As Boolean = False
    Public Function Online()
        Return _Online
    End Function

    Private _PersistentLogin As Boolean

    Private _Account As AccountInformation = Nothing
    ''' <summary>
    ''' Current account that is logged in at the moment.
    ''' </summary>
    Public Property Account As AccountInformation
        Get
            Return _Account
        End Get
        Set(ByVal value As AccountInformation)
            _Account = value
        End Set
    End Property

    Private _AccountsArray As New List(Of AccountInformation)
    ''' <summary>
    ''' Array of logged in accounts that can be reused.
    ''' </summary>
    Public Property AccountsArray As List(Of AccountInformation)
        Get
            Return _AccountsArray
        End Get
        Set(ByVal Account As List(Of AccountInformation))
            _AccountsArray = Account
        End Set
    End Property

    Private _Settings As New ApiSettings
    ''' <summary>
    ''' APISettings used to set the settings of various parts of the API.
    ''' </summary>
    Public Property Settings As ApiSettings
        Get
            Return _Settings
        End Get
        Set(ByVal Settings As ApiSettings)
            _Settings = Settings
        End Set
    End Property

    ''' <summary>
    ''' Parameter takes in APISettings
    ''' </summary>
    Public Sub New(ByVal Settings As ApiSettings)
        _Settings = Settings
    End Sub

    ''' <summary>
    ''' Notify used to handle events outside the class.
    ''' </summary>
    Public Event Notify(ByVal Report() As Object)

    ''' <summary>
    ''' Dispose of API.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        GC.SuppressFinalize(Me)
    End Sub

    ''' <summary>
    ''' Login to YouTube with the master account.
    ''' </summary>
    ''' <param name="Username">YouTube email as a string</param>
    ''' <param name="Password">YouTube password as a string</param>
    Public Sub login(ByVal Username As String, ByVal Password As String)
        RequestPause = False
        RequestStop = False
        Http = New Http

        Dim check = _AccountsArray.Find(Function(aiInfo As AccountInformation) aiInfo.Username.Equals(Username) And aiInfo.Password.Equals(Password))
        If Not TypeOf check Is AccountInformation Then
            _Account = New AccountInformation(Username, Password)
        Else
            _Account = check
        End If

        Try
            _Online = False
            With Http
                ' Persistant cookies are used so you don't have to keep relogging in with accounts you logged in before.
                If Not Me.Account.Cookies.Count = 0 Then
                    _PersistentLogin = True
                    ' Stories the cookie to the specefic account in the ACCOUNT class.
                    Http.AddCookie(Me.Account.Cookies.ToArray)
                End If

                ' If there's not a login for the user beging logged in, then login the user.
                Dim hr As HttpResponse = Nothing
                If Not _PersistentLogin Then
ManualLogin:
                    hr = .GetResponse("https://accounts.google.com/ServiceLogin?uilel=3&service=youtube&passive=true&continue=http%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26nomobiletemp%3D1%26hl%3Den_US%26next%3D%252F&hl=en_US&ltmpl=sso")
                    ' Check for errors
                    If Not hr.Exception Is Nothing Then
                        Dim he As HttpError = .ProcessException(hr.Exception)
                        RaiseEvent Notify(New Object(1) {"ERROR_LOGIN", "Could not retrieve log in form. "})
                        _Online = False
                        Exit Try
                    End If

                    If Not hr.Html.ToLower.Contains("value=""sign in""") Then
                        RaiseEvent Notify(New Object(1) {"ERROR_LOGIN", "Could not retrieve log in form; Missing element."})
                        _Online = False
                        Exit Try
                    End If

                    ' POST DATA for youtube to login
                    Dim PostData As New StringBuilder
                    PostData.Append("continue=" & .UrlEncode(.ParseFormNameText(hr.Html, "continue")))
                    PostData.Append("&service=youtube")
                    PostData.Append("&uilel=" & .UrlEncode(.ParseFormNameText(hr.Html, "uilel")))
                    PostData.Append("&dsh=" & .UrlEncode(.ParseFormNameText(hr.Html, "dsh")))
                    PostData.Append("&ltmpl=" & .UrlEncode(.ParseFormNameText(hr.Html, "ltmpl")))
                    PostData.Append("&hl=en_US")
                    PostData.Append("&ltmpl=" & .UrlEncode(.ParseFormNameText(hr.Html, "ltmpl")))
                    PostData.Append("&GALX=" & .UrlEncode(.ParseFormNameText(hr.Html, "GALX")))
                    PostData.Append("&pstMsg=1")
                    PostData.Append("&dnConn=https%3A%2F%2Faccounts.youtube.com")
                    PostData.Append("&checkConnection=")
                    PostData.Append("&checkedDomains=")
                    PostData.Append("&timeStmp=")
                    PostData.Append("&secTok=")
                    PostData.Append("&Email=" & .UrlEncode(Me.Account.Username))
                    PostData.Append("&Passwd=" & .UrlEncode(Me.Account.Password))
                    PostData.Append("&signIn=Sign+in")
                    PostData.Append("&PersistentCookie=yes")
                    PostData.Append("&rmShown=1")

                    .Referer = .LastResponseUri
                    ' SEND POST DATA
                    hr = .GetResponse("https://accounts.google.com/ServiceLoginAuth", PostData.ToString)
                    If Not hr.Exception Is Nothing Then
                        Dim he As HttpError = .ProcessException(hr.Exception)
                        RaiseEvent Notify(New Object(1) {"ERROR_LOGIN", "Could not submit log in credentials. "})
                        _Online = False
                        Exit Try
                    End If

                    ' Check the HTML for invalid credentials
                    If hr.Html.Contains("The username or password you entered is incorrect.") Then
                        RaiseEvent Notify(New Object(1) {"ERROR_LOGIN", "Invalid login credentials."})
                        _Online = False
                        Exit Try
                    End If
                End If

                ' CHECK YOUTUBE FRONTPAGE TO SEE IF LOGGED IN.
                hr = .GetResponse("http://www.youtube.com/")
                If Not hr.Exception Is Nothing Then
                    Dim he As HttpError = .ProcessException(hr.Exception)
                    RaiseEvent Notify(New Object(1) {"ERROR_LOGIN", "Could not retrieve homepage. "})
                    _Online = False
                    Exit Try
                End If

                ' ERROR FILE CHECKING
                If Not hr.Html.Contains("document.logoutForm.submit(); return false;") Then
                    RaiseEvent Notify(New Object(1) {"ERROR_LOGIN", "Unknown error."})
                    _Online = False
                    Exit Try
                End If

            End With

            _Online = True
            RaiseEvent Notify(New Object(0) {"PASS_LOGIN"})

        Catch ex As Exception
            RaiseEvent Notify(New Object(1) {"ERROR_LOGIN", "Unknown error."})
            _Online = False
            Exit Try
        Finally
            ' STORE LOGIN COOKIES.
            If _Online = True AndAlso Not _PersistentLogin Then
                Me.Account.Cookies.AddRange(Me.Http.GetAllCookies.ToArray) ' Store login cookies :o
                _AccountsArray.Add(_Account)
            End If
        End Try

        RaiseEvent Notify(New Object(0) {"COMPLETED_LOGIN"})
    End Sub '~~~~~~~~~



    'Public Sub Sync_Videos()
    '    Dim vids As New syncVideos
    '    AddHandler vids.Notify, AddressOf doEvents
    '    vids.run(iAccounts.getAccount(0))
    '    RemoveHandler vids.Notify, AddressOf doEvents
    '    vids.Dispose()

    'End Sub '''''''''''

    'Public Sub Sync_Favorites()
    '    Dim favs As New syncVideos
    '    AddHandler favs.Notify, AddressOf doEvents
    '    favs.run(iAccounts.getAccount(0))
    '    RemoveHandler favs.Notify, AddressOf doEvents
    '    favs.Dispose()
    'End Sub '''''''''''

    'Public Sub Sync_Subscriptions()
    '    Dim subscriptions As New syncVideos
    '    AddHandler subscriptions.Notify, AddressOf doEvents
    '    subscriptions.run(iAccounts.getAccount(0))
    '    RemoveHandler subscriptions.Notify, AddressOf doEvents
    '    subscriptions.Dispose()
    'End Sub '''''''''''

    'Public Sub Sync_Subscribers()
    '    Dim subscribers As New syncVideos
    '    AddHandler subscribers.Notify, AddressOf doEvents
    '    subscribers.run(iAccounts.getAccount(0))
    '    RemoveHandler subscribers.Notify, AddressOf doEvents
    '    subscribers.Dispose()
    'End Sub ''''''''''''

    Public Sub Sync_Contacts()
        'Dim SyncContacts_feed As Feed(Of Video)
        'Dim SyncContacts_index As Integer = 1
        'Try
        '    Dim catID As Integer = Convert.ToInt32(GetSQL("SELECT id FROM categories WHERE name= 'My Contacts' AND userID = '" & GLOBAL_USERID & "' AND info = '0'"))
        '    SetSQL("DELETE FROM users WHERE userID='" & GLOBAL_USERID & "' AND category = '" & catID & "'")

        '    While True
        '        SyncContacts_feed = yt_request.GetStandardFeed("http://gdata.youtube.com/feeds/api/users/default/contacts?start-index=" & SyncContacts_index & "&max-results=10")

        '        For Each entry As Video In SyncContacts_feed.Entries
        '            Try
        '                SetSQL("INSERT INTO users (category, userID, name) VALUES ('" & catID & "', '" & GLOBAL_USERID & "', '" & entry.Title.ToString & "')")
        '                RaiseEvent Notify(New Object(0) {"incrementTotalContacts"})
        '            Catch ex As Exception
        '                'do this
        '            End Try
        '        Next

        '        SyncContacts_index += 10
        '        If SyncContacts_index >= SyncContacts_feed.TotalResults + 10 Then
        '            Exit While
        '        End If
        '    End While
        'Catch e As Google.GData.Client.ClientFeedException
        '    MsgBox(e.ToString)
        'End Try
        'RaiseEvent Notify(New Object(0) {"doneContacts"})
    End Sub ' Disabled Youtube

    Public Sub ConactRequest(ByVal usernames As ArrayList)

        'For i As Integer = 0 To usernames.Count - 1
        '    Try
        '        With Http
        '            Dim hr As HttpResponse = .GetResponse("http://www.youtube.com/user/" & usernames.Item(i).ToString)
        '            If Not hr.Exception Is Nothing Then
        '                Dim he As HttpError = .ProcessException(hr.Exception)
        '                RaiseEvent Notify(New Object(1) {"httpError", he.Message})
        '                Exit Try
        '            End If

        '            Dim sb As New StringBuilder
        '            Dim session_token As String
        '            session_token = Split(hr.Html, "window.ajax_session_info = 'session_token=")(1)
        '            session_token = Split(session_token, "'")(0)

        '            sb.Append("session_token=" & .UrlEncode(session_token))
        '            sb.Append("&messages=[{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "box_method" & Chr(34) & "," & Chr(34) & "request" & Chr(34) & ":{" & Chr(34) & "name" & Chr(34) & ":" & Chr(34) & "user_profile" & Chr(34) & "," & Chr(34) & "x_position" & Chr(34) & ":0," & Chr(34) & "y_position" & Chr(34) & ":0," & Chr(34) & "palette" & Chr(34) & ":" & Chr(34) & "default" & Chr(34) & "," & Chr(34) & "method" & Chr(34) & ":" & Chr(34) & "add_friend" & Chr(34) & "," & Chr(34) & "params" & Chr(34) & ":{" & Chr(34) & "username" & Chr(34) & ":" & Chr(34) & usernames.Item(i).ToString & Chr(34) & "}}}]")

        '            .Referer = .LastResponseUri
        '            hr = .GetResponse("http://www.youtube.com/profile_ajax?action_ajax=1&user=" & usernames.Item(i).ToString & "&new=1&box_method=add_friend&box_name=user_profile", sb.ToString)
        '            If Not hr.Exception Is Nothing Then
        '                Dim he As HttpError = .ProcessException(hr.Exception)
        '                RaiseEvent Notify(New Object(1) {"httpError", he.Message})
        '                Exit Try
        '            End If

        '        End With

        '    Catch ex As Exception

        '    End Try
        'Next
    End Sub 'Disabled on YT right now


    ''' <summary>
    ''' Unsubscribe to YouTube users from your master account.
    ''' </summary>
    ''' <param name="usernames">Takes in a list of YouTube usernames to unsubscribe from.</param>
    Public Sub unsubscribe(ByVal usernames As List(Of String))
        login(_AccountsArray.Item(0).Username.ToString, _AccountsArray.Item(0).Password.ToString)

        If Not _Account.Cookies.Count = 0 Then

            ' INCREMENT AMOUNT OF USERS TO UNSUBSCRIBE FROM
            RaiseEvent Notify(New Object(1) {"USER_TOTAL", usernames.Count})

            ' LOOP THROUGH THE AMOUNT OF USERS TO UNSUBSCRIBE FROM
            For i As Integer = 0 To usernames.Count - 1
                ' NOTIFY OF NEXT USER THAT IS GOING TO BE UNSUBSCRIBED
                If Not _RequestStop = True Then

                    If Not i = (usernames.Count - 1) Then
                        RaiseEvent Notify(New Object(1) {"USER_NEXT", usernames.Item(i + 1).ToString})
                    Else
                        RaiseEvent Notify(New Object(1) {"USER_NEXT", ""})
                    End If

                    Try
                        With Http
                            ' GET RESPONSE FROM USER PAGE ON YOUTUBE
                            Dim hr As HttpResponse = .GetResponse("http://www.youtube.com/user/" & usernames.Item(i).ToString)
                            If Not hr.Exception Is Nothing Then
                                Dim he As HttpError = .ProcessException(hr.Exception)
                                RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, he.Message})
                                Exit Try
                            End If

                            ' CHECK IS USER IS BLACKLISTED
                            If checkBlacklistUser(usernames.Item(i).ToString) = True Then
                                RaiseEvent Notify(New Object(2) {"SKIPPED", usernames.Item(i).ToString, "Blacklisted!"})
                                Exit Try
                            End If

                            '' CHECK IF USER ACCOUNT IS ACTIVE
                            'Dim reason As String = checkAccount(hr.Html, usernames.Item(i).ToString, 2)
                            'If Not reason = Nothing Then
                            '    RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, reason})
                            '    Exit Try
                            'End If

                            ' CHECK FILTERS TO SEE IF IT PASSES OR FAILS
                            If checkFilters(hr.Html) = False Then
                                RaiseEvent Notify(New Object(2) {"SKIPPED", usernames.Item(i).ToString, "Filtered out!"})
                                Exit Try
                            End If

                            ' CREATE STRING BUILDER TO BUILD REPONSE TO UNSUBSCRIBE FROM YOUTUBE
                            Dim sb As New StringBuilder
                            Dim s As String
                            Dim session_token As String = Nothing
                            Dim response As String = Nothing

                            ' BASIC SPLITS TO FIND SUB ID, SESSION TOKEN.
                            s = Split(hr.Html, "data-subscription-id=" & Chr(34))(1)
                            s = Split(s, Chr(34))(0)
                            session_token = Split(hr.Html, "'subscription_ajax', " & Chr(34))(1)
                            session_token = Split(session_token, Chr(34))(0)
                            sb.Append("s=" & .UrlEncode(s))
                            sb.Append("&session_token=" & .UrlEncode(session_token))

                            .Referer = .LastResponseUri
                            ' GET REPONSE WITH REPONSE SENT
                            hr = .GetResponse("http://www.youtube.com/subscription_ajax?action_remove_subscriptions=1", sb.ToString)


                            ' CHECK TO SEE IF YOUTUBE RETURNED A SUCESS OR ERROR
                            If hr.Html.Contains("{" & Chr(34) & "response" & Chr(34) & ": {}}") Then
                                RaiseEvent Notify(New Object(1) {"PASS", usernames.Item(i).ToString})
                                Exit Try
                            ElseIf hr.Html.Contains("{" & Chr(34) & "errors" & Chr(34) & ": [" & Chr(34) & "Invalid request") Then
                                RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, "Invalid Request!"})
                                Exit Try
                            End If

                            ' HTTP ERRORS
                            If Not hr.Exception Is Nothing Then
                                Dim he As HttpError = .ProcessException(hr.Exception)
                                RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, he.Message})
                                Exit Try
                            End If
                        End With

                    Catch ex As Exception
                        RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, "Unknown Error!"})
                    End Try

                    ' UPDATE THE STATISTICS TO SEE HOW MANY USERS LEFT
                    RaiseEvent Notify(New Object(1) {"USER_LEFT", (usernames.Count - 1) - i})

                    ' SLEEP TO AMOUNT OF TIME SPECEFIED IN CONFIG FILE
                    If Not i = usernames.Count - 1 Then
                        sleep(_Settings.waitTime)
                    End If
                Else
                    Exit For
                End If
            Next
            ' SHOW COMPLTED NOTIFICATION
            RaiseEvent Notify(New Object(0) {"COMPLETED"})
        End If
    End Sub '~~~~~~~~~

    ''' <summary>
    ''' Subscribe to YouTube users from your master account.
    ''' </summary>
    ''' <param name="usernames">Takes in a list of YouTube usernames to subscribe to.</param>
    Public Sub subscribe(ByVal usernames As List(Of String))
        login(_AccountsArray.Item(0).Username.ToString, _AccountsArray.Item(0).Password.ToString)

        If Not _Account.Cookies.Count = 0 Then

            RaiseEvent Notify(New Object(1) {"USER_TOTAL", usernames.Count})

            ' LOOP THROUGH SUBSCRIBTION OF USERS
            For i As Integer = 0 To usernames.Count - 1
                If Not _RequestStop = True Then
                    ' NOTIFY OF NEXT UPCOMING USERNAME
                    If Not i = (usernames.Count - 1) Then
                        RaiseEvent Notify(New Object(1) {"USER_NEXT", usernames.Item(i + 1).ToString})
                    Else
                        RaiseEvent Notify(New Object(1) {"USER_NEXT", ""})
                    End If

                    Try
                        With Http
                            ' GET RESPONSE OF USER PAGE
                            Dim hr As HttpResponse = .GetResponse("http://www.youtube.com/user/" & usernames.Item(i).ToString)
                            If Not hr.Exception Is Nothing Then
                                Dim he As HttpError = .ProcessException(hr.Exception)
                                RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, he.Message})
                                Exit Try
                            End If

                            ' CHECK TO SEE IF USER BLACKLISTED
                            If checkBlacklistUser(usernames.Item(i).ToString) = True Then
                                RaiseEvent Notify(New Object(2) {"SKIPPED", usernames.Item(i).ToString, "Blacklisted!"})
                                Exit Try
                            End If

                            '' CHECK TO SEE IF USER ACCOUNT IS ACTIVE
                            'Dim reason As String = checkAccount(hr.Html, usernames.Item(i).ToString, 1)
                            'If Not reason = Nothing Then
                            '    RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, reason})
                            '    Exit Try
                            'End If

                            ' CHECK TO SEE IF USER PASSES OR FAILS FILTERS
                            If checkFilters(hr.Html) = False Then
                                RaiseEvent Notify(New Object(2) {"SKIPPED", usernames.Item(i).ToString, "Filtered out!"})
                                Exit Try
                            End If


                            ' CREATE STRINGBUILDER OF POST DATA
                            Dim sb As New StringBuilder
                            Dim c As String
                            Dim session_token As String

                            c = Split(hr.Html, "data-subscription-value=" & Chr(34))(1)
                            c = Split(c, Chr(34))(0)
                            session_token = Split(hr.Html, "'subscription_ajax', " & Chr(34))(1)
                            session_token = Split(session_token, Chr(34))(0)

                            sb.Append("session_token=" & .UrlEncode(session_token))
                            sb.Append("&c=" & .UrlEncode(c))

                            .Referer = .LastResponseUri
                            ' SUBMIT POST DATA
                            hr = .GetResponse("http://www.youtube.com/subscription_ajax?action_create_subscription_to_channel=1&feature=channels3", sb.ToString)

                            'CHECK TO SEE IF USER IS SUBSCRIBED OR IT FAILED
                            If hr.Html.Contains(Chr(34) & "new_subscription" & Chr(34) & ": true") Then
                                RaiseEvent Notify(New Object(1) {"PASS", usernames.Item(i).ToString})
                                Exit Try
                            ElseIf hr.Html.Contains("{" & Chr(34) & "errors" & Chr(34) & ": [" & Chr(34) & "Invalid request") Then
                                RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, "Invalid Request!"})
                                Exit Try
                            ElseIf hr.Html.Contains(Chr(34) & "new_subscription" & Chr(34) & ": false") Then
                                RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, "Could not subscribe!"})
                                Exit Try
                            End If

                            ' HTTP ERROR
                            If Not hr.Exception Is Nothing Then
                                Dim he As HttpError = .ProcessException(hr.Exception)
                                RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, he.Message})
                                Exit Try
                            End If
                        End With

                        ' UNKNOWN ERROR
                    Catch ex As Exception
                        RaiseEvent Notify(New Object(2) {"ERROR", usernames.Item(i).ToString, "Unknown Error!"})
                    End Try

                    ' NOTIFY HOW MANY USERS ARE LEFT
                    RaiseEvent Notify(New Object(1) {"USER_LEFT", (usernames.Count - 1) - i})
                    If Not i = usernames.Count - 1 Then
                        sleep(_Settings.waitTime)
                    End If
                Else
                    Exit For
                End If
            Next
            ' COMPLETED NOTIFICATION
            RaiseEvent Notify(New Object(0) {"COMPLETED"})
        End If
    End Sub '~~~~~~~~~~.

    ''' <summary>
    ''' Send messages to a list of YouTube users and also embed a video. This dosen't require a master account and you can have a dictionary of other accounts to use.
    ''' </summary>
    ''' <param name="Usernames">Takes in a list of YouTube usernames to message.</param>
    ''' <param name="Messages">Takes in a list of Messages to alternate between.</param>
    ''' <param name="Accounts">Takes in a Dictionary of YouTube accounts to alternate between.</param>
    ''' <param name="videoIds">Takes in a list of videoIds to alternate between.</param>
    Public Sub sendMessages(ByVal Usernames As List(Of String), ByVal Messages As Dictionary(Of String, String), ByVal Accounts As Dictionary(Of String, String), ByVal videoIds As List(Of String))
        Dim account_index As Integer = 0
        Dim video_index As Integer = 0
        Dim videoID As String = Nothing

        Dim subject As String = Nothing
        Dim body As String = Nothing
        Dim message_index As Integer = 0

        ' UPDATE AMOUNT OF USERS TO SEND MESSAGES TO
        RaiseEvent Notify(New Object(1) {"USER_TOTAL", Usernames.Count})

        ' LOOP THROUGH USERS
        For i As Integer = 0 To Usernames.Count - 1
            If Not _RequestStop = True Then
                ' SHOW UPCOMING USER TO SEND MESSAGE TO
                If Not i = (Usernames.Count - 1) Then
                    RaiseEvent Notify(New Object(1) {"USER_NEXT", Usernames.Item(i + 1).ToString})
                Else
                    RaiseEvent Notify(New Object(1) {"USER_NEXT", ""})
                End If

                ' ALTERNATING BETWEEN THE SUBJECTS THAT WERE SENT TO THE FUNCTION TO SEND TO USER
                If Messages.Count >= 1 Then
                    subject = Messages.ElementAt(message_index).Key.ToString()
                    body = Messages.ElementAt(message_index).Value.ToString()
                    message_index += 1

                    If message_index > Messages.Count - 1 Then
                        message_index = 0
                    End If
                Else
                    Exit For
                End If

                ' ALTERNATE BETWEEN THE VIDEOIDS SENT TO THE FUNCTION TO SEND TO USER
                If videoIds.Count >= 1 Then
                    videoID = videoIds.Item(video_index).ToString
                    video_index += 1

                    If video_index > videoIds.Count - 1 Then
                        video_index = 0
                    End If
                Else
                    videoID = Nothing
                End If

                ' ALTERNATE BETWEEN USER ACCOUNTS FROM WHERE THE USER RECIEVING THE MESSAGE WILL RECIEVE FROM.
                If Not Accounts.Count <= 0 Then
                    login(Accounts.Keys(account_index).ToString, Accounts.Values(account_index).ToString)
                    account_index += 1

                    If account_index > Accounts.Count - 1 Then
                        account_index = 0
                    End If
                Else
                    login(Accounts.Keys(account_index).ToString, Accounts.Values(account_index).ToString)
                End If

                'Http.ClearCookies()
                'Http.AddCookie(_Account.Cookies.ToArray)

                Dim url As String = "http://www.youtube.com/inbox?to_users=" & Usernames.Item(i).ToString & "&action_compose=1"
                Try
                    With Http
                        ' GET HTML OF URL TO GET SESSION DATA FOR POST DATA
                        Dim hr As HttpResponse = .GetResponse(url)
                        If Not hr.Exception Is Nothing Then
                            Dim he As HttpError = .ProcessException(hr.Exception)
                            RaiseEvent Notify(New Object(1) {"ERROR", he.Message})
                            Exit Try
                        End If

                        ' CHECK IF THE MESSAGE BEING SEND IS BEING SENT TO A BLACKLISTED USER
                        If checkBlacklistUser(Usernames.Item(i).ToString) = True Then
                            RaiseEvent Notify(New Object(2) {"SKIPPED", Usernames.Item(i).ToString, "Blacklisted!"})
                            Exit Try
                        End If

                        ' CREATE POST DATA
                        Dim sb As New StringBuilder
                        Dim session_token As String
                        session_token = Split(hr.Html, "inbox = new yt.sharing.inbox.Inbox(")(1)
                        session_token = Split(session_token, ";")(0)
                        session_token = Split(session_token, "'session_token=")(1)
                        session_token = Split(session_token, "'")(0)

                        sb.Append("session_token=" & .UrlEncode(session_token))
                        sb.Append("&messages=[{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "send_message" & Chr(34) & "," & Chr(34) & "request" & Chr(34) & ":{" & Chr(34) & "to_users" & Chr(34) & ":" & Chr(34) & Usernames.Item(i).ToString & Chr(34) & "," & Chr(34) & "to_all_subscribers" & Chr(34) & ":" & Chr(34) & "false" & Chr(34) & "," & Chr(34) & "subject" & Chr(34) & ":" & Chr(34) & subject & Chr(34) & "," & Chr(34))
                        If Not videoID = Nothing Then
                            sb.Append("video_id" & Chr(34) & ":" & Chr(34) & videoID & Chr(34) & "," & Chr(34))
                        End If
                        sb.Append("message_text" & Chr(34) & ":" & Chr(34) & body & Chr(34) & "}}]")

                        .Referer = url
                        hr = .GetResponse("http://www.youtube.com/inbox_ajax?action_ajax=1&type=send_message", sb.ToString)

                        ' SEE IF THE REPONSE FROM YOUTUBE WAS OK OR IT WAS BAD
                        If hr.Html.Contains("Your message has been sent!" & Chr(34) & ", " & Chr(34) & "success" & Chr(34) & ": true") Then
                            RaiseEvent Notify(New Object(1) {"PASS", Usernames.Item(i).ToString})
                            Exit Try
                        ElseIf hr.Html.Contains("has enabled friend lock") Then
                            RaiseEvent Notify(New Object(2) {"ERROR", Usernames.Item(i).ToString, "User has enabled friend lock!"})
                            Exit Try
                        ElseIf hr.Html.Contains("No user named") Then
                            RaiseEvent Notify(New Object(2) {"ERROR", Usernames.Item(i).ToString, "Non-existent user!"})
                            Exit Try
                        ElseIf hr.Html.Contains("account has been closed") Then
                            RaiseEvent Notify(New Object(2) {"ERROR", Usernames.Item(i).ToString, "Account has been closed!"})
                            Exit Try
                        ElseIf hr.Html.Contains("You have recently posted several messages.") Then
                            sleep(_Settings.CaptchawaitTime)
                        ElseIf Not hr.Exception Is Nothing Then
                            Dim he As HttpError = .ProcessException(hr.Exception)
                            RaiseEvent Notify(New Object(1) {"ERROR", he.Message})
                            Exit Try
                        End If

                    End With

                    ' UNKNOWN ERROR
                Catch ex As Exception
                    RaiseEvent Notify(New Object(2) {"ERROR", Usernames.Item(i).ToString, "Unknown error!"})
                End Try

                ' SHOW AMOUNT OF USERS LEFT TO SEND TO
                RaiseEvent Notify(New Object(1) {"USER_LEFT", (Usernames.Count - 1) - i})
                If Not i = Usernames.Count - 1 Then
                    sleep(_Settings.waitTime)
                End If

            Else
                Exit For
            End If
        Next

        ' NOTIFICATION OF COMPLETION
        RaiseEvent Notify(New Object(0) {"COMPLETED"})
    End Sub '~~~~~~~~~~.

    ''' <summary>
    ''' Comments on YouTube videos of your choice. This dosen't require a master account and you can have a dictionary of other accounts to use.
    ''' </summary>
    ''' <param name="videoIDs">Takes in a list of videoID's to comment on/</param>
    ''' <param name="comments">Takes in a list of comments to alternate between.</param>
    ''' <param name="Accounts">Takes in a Dictionary of YouTube accounts to alternate between.</param>
    Public Sub videoComment(ByVal videoIDs As List(Of String), ByVal comments As List(Of String), ByVal Accounts As Dictionary(Of String, String))
        Dim account_index As Integer = 0
        Dim comment_index As Integer = 0
        Dim current_comment As String = Nothing

        RaiseEvent Notify(New Object(1) {"USER_TOTAL", videoIDs.Count})

        For i As Integer = 0 To videoIDs.Count - 1
            If Not _RequestStop = True Then

                If Not i = (videoIDs.Count - 1) Then
                    RaiseEvent Notify(New Object(1) {"USER_NEXT", videoIDs.Item(i + 1).ToString})
                Else
                    RaiseEvent Notify(New Object(1) {"USER_NEXT", ""})
                End If

                Try

                    If Not Accounts.Count <= 0 Then
                        login(Accounts.Keys(account_index).ToString, Accounts.Values(account_index).ToString)
                        account_index += 1

                        If account_index > Accounts.Count - 1 Then
                            account_index = 0
                        End If
                    Else
                        login(Accounts.Keys(account_index).ToString, Accounts.Values(account_index).ToString)
                    End If

                    'Http.ClearCookies()
                    'Http.AddCookie(_Account.Cookies.ToArray)

                    If comments.Count <= 0 Then
                        Exit For
                    Else
                        current_comment = comments.Item(comment_index).ToString
                        comment_index += 1
                        If comment_index > comments.Count - 1 Then
                            comment_index = 0
                        End If
                    End If

                    With Http
redo:
                        Dim hr As HttpResponse = .GetResponse("http://www.youtube.com/watch?v=" & videoIDs.Item(i).ToString)
                        If Not hr.Exception Is Nothing Then
                            Dim he As HttpError = .ProcessException(hr.Exception)
                            RaiseEvent Notify(New Object(1) {"ERROR", he.Message})
                            Exit Try
                        End If

                        'Make a filter for these videos

                        'Dim reason As String = checkVideo(hr.Html)
                        'If Not reason = Nothing Then
                        '    RaiseEvent Notify(New Object(2) {"Error", videoIDs.Item(i).ToString, reason})
                        '    Exit Try
                        'End If


                        Dim sb As New StringBuilder
                        Dim session_token As String = Nothing
                        session_token = Split(hr.Html, "<input type=" & Chr(34) & "hidden" & Chr(34) & " name=" & Chr(34) & "session_token" & Chr(34) & " value=" & Chr(34))(1)
                        session_token = Split(session_token, Chr(34))(0)

                        Dim plid As String = Nothing
                        plid = Split(hr.Html, "plid" & Chr(34) & ": " & Chr(34))(1)
                        plid = Split(plid, Chr(34))(0)

                        sb.Append("session_token=" & .UrlEncode(session_token))
                        sb.Append("&video_id=" & videoIDs.Item(i).ToString)
                        sb.Append("&form_id=")
                        sb.Append("&source=w")
                        sb.Append("&reply_parent_id=")
                        sb.Append("&comment=" & .UrlEncode(current_comment)) 'do this
                        sb.Append("&screen=" & .UrlEncode("h=1050&w=1680&d=32"))
                        sb.Append("&pid=" & plid)

                        .Referer = .LastResponseUri
                        hr = .GetResponse("http://www.youtube.com/comment_servlet?add_comment=1&return_ajax=true", sb.ToString)


                        If hr.Html.Contains("CDATA[OK]") Then
                            RaiseEvent Notify(New Object(1) {"PASS", videoIDs.Item(i).ToString})
                        ElseIf hr.Html.Contains("CDATA[FAILED]") Then
                            RaiseEvent Notify(New Object(2) {"ERROR", videoIDs.Item(i).ToString, "ERROR!"})
                        ElseIf hr.Html.Contains("INLINE_CAPTCHA") Then
                            If _Settings.CaptchaShow = True Then
                                Dim captcha_err As Integer = 0
captchaRedo:
                                Dim sb2 As New StringBuilder
                                sb2.Append("session_token=" & session_token)
                                hr = .GetResponse("http://www.youtube.com/comment_servlet?gimme_captcha=1&watch5=1", sb2.ToString)
                                If Not hr.Exception Is Nothing Then
                                    Dim he As HttpError = .ProcessException(hr.Exception)
                                    RaiseEvent Notify(New Object(1) {"ERROR", he.Message})
                                    Exit Try
                                End If
                                Dim img As String
                                img = Split(hr.Html, "'/cimg?c=")(1)
                                img = Split(img, "&")(0)

                                Dim c As New frmCaptcha
                                Dim result As String = c.ShowDialog("http://www.youtube.com/cimg?c=" & img)

                                Dim sb3 As New StringBuilder
                                sb3.Append("session_token=" & .UrlEncode(session_token))
                                sb3.Append("&video_id=" & videoIDs.Item(i).ToString)
                                sb3.Append("&form_id=")
                                sb3.Append("&source=w")
                                sb3.Append("&reply_parent_id=")
                                sb3.Append("&comment=" & .UrlEncode(current_comment))
                                sb3.Append("&response=" & result)
                                sb3.Append("&challenge=" & img)
                                sb3.Append("&screen=" & .UrlEncode("h=1050&w=1680&d=32"))
                                sb.Append("&pid=" & plid)

                                .Referer = .LastResponseUri
                                hr = .GetResponse("http://www.youtube.com/comment_servlet?add_comment=1&return_ajax=true", sb3.ToString)
                                If hr.Html.Contains("CDATA[OK]") Then
                                    RaiseEvent Notify(New Object(1) {"PASS", videoIDs.Item(i).ToString})
                                ElseIf hr.Html.Contains("INLINE_CAPTCHAFAIL") Then
                                    captcha_err += 1
                                    If captcha_err > _Settings.CaptchaFailAmount Then
                                        RaiseEvent Notify(New Object(2) {"ERROR", videoIDs.Item(i).ToString, "Captcha MAX tries!"})
                                        Exit Try
                                    Else
                                        GoTo captchaRedo
                                    End If
                                ElseIf Not hr.Exception Is Nothing Then
                                    Dim he As HttpError = .ProcessException(hr.Exception)
                                    RaiseEvent Notify(New Object(1) {"ERROR", he.Message})
                                    Exit Try
                                End If

                            Else
                                sleep(_Settings.CaptchawaitTime)
                                GoTo redo
                            End If
                        ElseIf Not hr.Exception Is Nothing Then
                            Dim he As HttpError = .ProcessException(hr.Exception)
                            RaiseEvent Notify(New Object(1) {"ERROR", he.Message})
                            Exit Try
                        End If

                    End With
                Catch
                    RaiseEvent Notify(New Object(2) {"ERROR", videoIDs.Item(i).ToString, "UNEXPECTED ERROR!"})
                    Exit Try
                End Try

                RaiseEvent Notify(New Object(1) {"USER_LEFT", (videoIDs.Count - 1) - i})
                If Not i = videoIDs.Count - 1 Then
                    sleep(_Settings.waitTime)
                End If
            Else
                Exit For
            End If
        Next

        RaiseEvent Notify(New Object(0) {"COMPLETED"})
    End Sub

    ''' <summary>
    ''' Comments on YouTube Users profiles. This dosen't require a master account and you can have a dictionary of other accounts to use.
    ''' </summary>
    ''' <param name="Usernames">Takes in a list of YouTube usernames to comment on their profile.</param>
    ''' <param name="comments">Takes in a list of comments to alternate between.</param>
    ''' <param name="Accounts">Takes in a Dictionary of YouTube accounts to alternate between.</param>
    Public Sub profileComment(ByVal Usernames As List(Of String), ByVal comments As List(Of String), ByVal Accounts As Dictionary(Of String, String))
        Dim account_index As Integer = 0
        Dim comment_index As Integer = 0
        Dim current_comment As String = ""
        Dim channelid As Integer = 0

        ' SEE AMOUNT OF USERS COMMENTS BEING SENT TO.
        RaiseEvent Notify(New Object(1) {"USER_TOTAL", Usernames.Count})

        ' LOOP THROUGH USERS
        For i As Integer = 0 To Usernames.Count - 1
            If Not _RequestStop = True Then
                ' SHOW NEXT USER IN LINE OF PROFILE COMMENTS
                If Not i = (Usernames.Count - 1) Then
                    RaiseEvent Notify(New Object(1) {"USER_NEXT", Usernames.Item(i + 1).ToString})
                Else
                    RaiseEvent Notify(New Object(1) {"USER_NEXT", ""})
                End If

                Try

                    ' ALTERNATE BETWEEN THE DIFFERENT SPECEFIED ACCOUNTS YOU USED.
                    If Not Accounts.Count <= 0 Then
                        login(Accounts.Keys(account_index).ToString, Accounts.Values(account_index).ToString)
                        account_index += 1

                        If account_index > Accounts.Count - 1 Then
                            account_index = 0
                        End If
                    Else
                        login(Accounts.Keys(account_index).ToString, Accounts.Values(account_index).ToString)
                    End If

                    'Http.ClearCookies()
                    'Http.AddCookie(_Account.Cookies.ToArray)

                    ' EXIT IF NO COMMENTS ARE PUT ELSE ALTERNATE BETWEEN DIFFERENT COMMENTS SPECEFIED
                    If comments.Count <= 0 Then
                        Exit For
                    Else
                        current_comment = comments.Item(comment_index).ToString
                        comment_index += 1
                        If comment_index > comments.Count - 1 Then
                            comment_index = 0
                        End If
                    End If

                    With Http
                        ' GOTO TO REDO THE FUNCTION IF YOU SENT TO MANY COMMENTS AND THEN 
                        ' THE PROGRAM SLEEPS FOR A WHILE AND WILL RESTART WITH THE LAST USER AGAIN.
redo:
                        ' GET RESPONSE OF USERPAGE TO GET POST DATA
                        Dim hr As HttpResponse = .GetResponse("http://www.youtube.com/user/" & Usernames.Item(i).ToString & "/feed?filter=1")
                        ' HTTP ERROR
                        If Not hr.Exception Is Nothing Then
                            Dim he As HttpError = .ProcessException(hr.Exception)
                            RaiseEvent Notify(New Object(1) {"ERROR", he.Message})
                            Exit Try
                        End If

                        'CHECK IF USER IS BLACKLISTED
                        If checkBlacklistUser(Usernames.Item(i).ToString) = True Then
                            RaiseEvent Notify(New Object(2) {"SKIPPED", Usernames.Item(i).ToString, "Blacklisted!"})
                            Exit Try
                        End If

                        ' CHECK IS USER ACCOUNT IS ACTIVE
                        'Dim reason As String = checkAccount(hr.Html, Usernames.Item(i).ToString, 0)
                        'If Not reason = Nothing Then
                        '    RaiseEvent Notify(New Object(2) {"ERROR", Usernames.Item(i).ToString, reason})
                        '    Exit Try
                        'End If

                        ' CHECK USER FILTERS
                        If checkFilters(hr.Html) = False Then
                            RaiseEvent Notify(New Object(2) {"Skipped", Usernames.Item(i).ToString, "Filtered out!"})
                            Exit Try
                        End If


                        ' POST DATA CREATTION
                        Dim sb As New StringBuilder
                        Dim responseURL As String = "http://www.youtube.com/channel_ajax?action_add_comment=1"
                        Dim session_token As String = Split(hr.Html, "name=" & Chr(34) & "session_token" & Chr(34) & " value=" & Chr(34))(1)
                        session_token = Split(session_token, Chr(34))(0)
                        Dim channel_id As String = Split(hr.Html, "name=" & Chr(34) & "channel_id" & Chr(34) & " value=" & Chr(34))(1)
                        channel_id = Split(channel_id, Chr(34))(0)

                        sb.Append("session_token=" & session_token)
                        sb.Append("&channel_id=" & channel_id)
                        sb.Append("&comment=" & current_comment)

                        .Referer = .LastResponseUri
                        hr = .GetResponse(responseURL, sb.ToString)


                        ' CHECK TO SEE IF IT FAILED OR PASSED
                        If hr.Html.Contains("moderate" & Chr(34) & ": false") Or hr.Html.Contains("{" & Chr(34) & "code" & Chr(34) & ": " & Chr(34) & "SUCCESS" & Chr(34) & "}") Then
                            RaiseEvent Notify(New Object(1) {"PASS", Usernames.Item(i).ToString})
                            Exit Try
                        ElseIf hr.Html.Contains("{""code"": ""ERROR"", ""errors"": [""You are unable to use this functionality because you have been blocked.""]}") Then
                            RaiseEvent Notify(New Object(2) {"ERROR", Usernames.Item(i).ToString, "Disabled Commenting!"})
                            Exit Try
                        ElseIf hr.Html.Contains("{" & Chr(34) & "code" & Chr(34) & ": " & Chr(34) & "ERROR" & Chr(34)) Then
                            RaiseEvent Notify(New Object(2) {"ERROR", Usernames.Item(i).ToString, "ERROR!"})
                            Exit Try
                            ' OH MAN, CAPTCHA. SO WE WILL SHOW THE CAPTCHA AND MAKE THE USER PUT IN THE 
                            ' CAPTCHA CODE OR LEFT THE PROGRAM TO SLEEP. THIS IS ALL SPECEFIED IN THE
                            ' CONFIG FILE WHCIH CAN BE ACCESSED FOM THE SETTIONS PAGE
                        ElseIf hr.Html.Contains("captcha") Then
                            ' IF SHOWCAPTCHA IS 1 IN THE CONFIG FILE, THEN USER WILL INPUT CAPTCHA
                            If _Settings.CaptchaShow = True Then
                                Dim captcha_err As Integer = 0
                                ' IF CAPTCHA FILED, REDO THE CAPTCHA WITH THE GOTO
captchaRedo:
                                Dim img As String
                                img = Split(hr.Html, "cimg?c=")(1)
                                img = Split(img, "\")(0)

                                ' SHOW CAPTCHA FORM FOR USER TO INPUT IT
                                Dim c As New frmCaptcha
                                Dim result As String = c.ShowDialog("http://www.youtube.com/cimg?c=" & img)

                                Dim sb2 As New StringBuilder

                                ' CREATE POST DATA WITH THE CAPTCHA
                                If channelid = 0 Then
                                    sb2.Append("session_token=" & .UrlEncode(session_token))
                                    sb2.Append("&messages=[{" & Chr(34) & "type" & Chr(34) & ":" & Chr(34) & "box_method" & Chr(34) & "," & Chr(34) & "request" & Chr(34) & ":{" & Chr(34) & "name" & Chr(34) & ":" & Chr(34) & "user_comments" & Chr(34) & "," & Chr(34) & "x_position" & Chr(34) & ":1," & Chr(34) & "y_position" & Chr(34) & ":42," & Chr(34) & "palette" & Chr(34) & ":" & Chr(34) & "default" & Chr(34) & "," & Chr(34) & "method" & Chr(34) & ":" & Chr(34) & "add_comment" & Chr(34) & "," & Chr(34) & "params" & Chr(34) & ":{" & Chr(34) & "comment" & Chr(34) & ":" & Chr(34) & current_comment & Chr(34) & "," & Chr(34) & "view_all_mode" & Chr(34) & ":" & Chr(34) & "False" & Chr(34) & "," & Chr(34) & "challenge" & Chr(34) & ":" & Chr(34) & img & Chr(34) & "," & Chr(34) & "response" & Chr(34) & ":" & Chr(34) & result & Chr(34) & "}}}]")
                                Else
                                    sb2.Append("session_token=" & session_token)
                                    sb2.Append("&module=10000")
                                    sb2.Append("&channel_id=" & channel_id)
                                    sb2.Append("&comment=" & current_comment)
                                    sb2.Append("&challenge=" & img)
                                    sb2.Append("&response=" & result)
                                End If

                                ' SENT POST DATA
                                hr = .GetResponse(responseURL, sb2.ToString)

                                ' SEE IF THE CAPTCHA PUT IN WAS CORRECT OR NOT.
                                ' IN THE SETTINGS FILE, YOU CAN INSERT HOW MANY ATTEMPTS
                                ' FOR YOU TO DO CATPTCHA BEFORE IT SKIPS THE USER ENTIRELY.
                                If hr.Html.Contains("errors" & Chr(34) & ": [" & Chr(34) & "The response to the letters in the image was not correct") OrElse hr.Html.Contains("errors" & Chr(34) & ": [" & Chr(34) & "The response to the letters on the image was not correct") Then
                                    captcha_err += 1
                                    If captcha_err > _Settings.CaptchaFailAmount Then
                                        RaiseEvent Notify(New Object(2) {"ERROR", Usernames.Item(i).ToString, "Captcha MAX tries!"})
                                        Exit Try
                                    Else
                                        ' REDO CAPTCHA IF YOU FAILED TO PUT IT CORRECT THE FIRST TIME
                                        GoTo captchaRedo
                                    End If
                                    ' SEE IF CAPTCHA WAS SUCESS OR NOT
                                ElseIf hr.Html.Contains("moderate" & Chr(34) & ": false") Or hr.Html.Contains("{" & Chr(34) & "code" & Chr(34) & ": " & Chr(34) & "SUCCESS" & Chr(34) & "}") Then
                                    RaiseEvent Notify(New Object(1) {"PASS", Usernames.Item(i).ToString})
                                    Exit Try
                                ElseIf Not hr.Exception Is Nothing Then
                                    Dim he As HttpError = .ProcessException(hr.Exception)
                                    RaiseEvent Notify(New Object(1) {"ERROR", he.Message})
                                    Exit Try
                                End If
                                ' IF SHOWCAPTCHA IS 0 IN CONFIG, THEN THE PROGRAM WILL WAIT IT OUT FOR CAPTCHA TO GO AWAY AFTER A WHILE
                            Else
                                sleep(_Settings.CaptchawaitTime)
                                GoTo redo
                            End If
                        ElseIf Not hr.Exception Is Nothing Then
                            ' HTTP ERROR
                            Dim he As HttpError = .ProcessException(hr.Exception)
                            RaiseEvent Notify(New Object(1) {"ERROR", he.Message})
                            Exit Try
                        End If

                    End With
                    ' UNEXPECTED ERROR
                Catch ex As Exception
                    RaiseEvent Notify(New Object(2) {"ERROR", Usernames.Item(i).ToString, "UNEXPECTED ERROR!"})
                    Exit Try
                End Try

                'NOTIFY AMOUNT OF USERS LEFT
                RaiseEvent Notify(New Object(1) {"USER_LEFT", (Usernames.Count - 1) - i})
                ' SLEEP AFTER EACH REQUEST
                If Not i = Usernames.Count - 1 Then
                    sleep(_Settings.waitTime)
                End If
            Else
                Exit For
            End If
        Next

        'NOTIFICATION OF COMPLETION
        RaiseEvent Notify(New Object(0) {"COMPLETED"})
    End Sub

    Private Sub sleep(ByVal time As Integer)
        Dim increment As Integer = time

        For i As Integer = 0 To increment - 1
            If _RequestPause = False Then
                If _RequestStop = False Then
                    RaiseEvent Notify(New Object(1) {"TIME_LEFT", increment - i})
                    Threading.Thread.Sleep(1000)
                Else
                    _RequestPause = False
                    Exit For
                End If
            Else
                For i2 As Integer = 0 To 86400
                    If _RequestPause = True Then
                        If _RequestStop = False Then
                            RaiseEvent Notify(New Object(0) {"PAUSED"})
                            Threading.Thread.Sleep(1000)
                        Else
                            RaiseEvent Notify(New Object(0) {"STOPPED"})
                            _RequestPause = False
                            Exit Sub
                        End If
                    Else
                        RaiseEvent Notify(New Object(0) {"RESUMED"})
                        Exit For
                    End If
                Next
            End If
        Next
    End Sub
    Private Function checkFilters(ByVal html As String) As Boolean

        Dim age As Integer = 0, videos As Integer = 0, subs As Integer = 0, vidViews As Integer = 0

        Try
            ' CHECK GLOBAL AMOUNT OF MIN SUBS SPECEFIED IN CONFIG FILE
            If _Settings.minSubs > 0 Then
                ' SPLIT THE INFORMATION FOUND IN HTML OF USER PAGE AND GET THEIR MINSUBS
                Dim lsubs As String = Nothing
                lsubs = Split(html, "/subscribers" & Chr(34))(1)
                lsubs = Split(lsubs, "</a>")(0)
                lsubs = Split(lsubs, "stat-value" & Chr(34) & ">")(1)
                lsubs = Split(lsubs, "<")(0)
                subs = Convert.ToInt32(lsubs.Replace(",", ""))

                ' IF THE FILTER IS NOT SPECEFIC TO WHAT THE USER HAS PUT IN, RETURN FALSE.
                If Not subs >= _Settings.minSubs Then
                    Return False
                End If

            End If

            If _Settings.minVideos > 0 Then

            End If

            If _Settings.minAge > 0 Then
                Dim lage As String = Nothing
                lage = Split(html, "checked name=" & Chr(34) & "is_owner_age_displayed" & Chr(34))(1)
                lage = Split(lage, "<div class=" & Chr(34))(0)
                lage = Split(lage, "fixed-value" & Chr(34) & ">")(1)
                lage = Split(lage, "<")(0)
                age = Convert.ToInt32(lage)

                If Not age >= _Settings.minAge Then
                    Return False
                End If
            End If

            If _Settings.minVideos > 0 Then
                Dim lvidview As String = Nothing
                lvidview = Split(html, "/analytics" & Chr(34))(1)
                lvidview = Split(lvidview, "</a>")(0)
                lvidview = Split(lvidview, "stat-value" & Chr(34) & ">")(1)
                lvidview = Split(lvidview, "<")(0)
                vidViews = Convert.ToInt32(lvidview.Replace(",", ""))

                If Not vidViews >= _Settings.minVideos Then
                    Return False
                End If
            End If
        Catch
            Return False
        End Try

        Return True
    End Function
    Private Function checkBlacklistUser(ByVal username As String) As Boolean
        If _Settings.checkBlacklistUsers = True Then
            Dim b As Boolean = _Settings.blacklistedUsers.Any(Function(s) username.Contains(s))
            If b = True Then
                Return True
            End If
        End If
        Return False
    End Function
    Private Function checkBlacklistVideo(ByVal videoID As String) As Boolean
        If _Settings.checkBlacklistVideo = True Then
            Dim b As Boolean = _Settings.blacklistedVideos.Any(Function(s) videoID.Contains(s))
            If b = True Then
                Return True
            End If
        End If
        Return False
    End Function
End Class