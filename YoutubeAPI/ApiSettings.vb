Public Class ApiSettings

    Private _minAge As Integer
    ''' <summary>
    ''' Set the minimum age inorder for you to do anything regarding that user.
    ''' </summary>
    Public Property minAge As Integer
        Get
            Return _minAge
        End Get
        Set(ByVal value As Integer)
            _minAge = value
        End Set
    End Property

    Private _minSubs As Integer
    ''' <summary>
    ''' Set the minimum amount of subscribers inorder for you to do anything regarding that user.
    ''' </summary>
    Public Property minSubs As Integer
        Get
            Return _minSubs
        End Get
        Set(ByVal value As Integer)
            _minSubs = value
        End Set
    End Property

    Private _minVideos As Integer
    ''' <summary>
    ''' Set the minimum amount of videos the user has inorder for you to do anything regarding that user.
    ''' </summary>
    Public Property minVideos As Integer
        Get
            Return _minVideos
        End Get
        Set(ByVal value As Integer)
            _minVideos = value
        End Set
    End Property

    Private _minVidViews As Integer
    ''' <summary>
    ''' Set the minimum amount of profile views the user has inorder for you to do anything regarding that user.
    ''' </summary>
    Public Property minVidViews As Integer
        Get
            Return _minVidViews
        End Get
        Set(ByVal value As Integer)
            _minVidViews = value
        End Set
    End Property

    Private _checkBlacklistUsers As Boolean = False
    ''' <summary>
    ''' (TRUE / FALSE) Check for blacklisted users you have in a list. If user is blacklisted, the API will skip doing various functions such as sending them messages, commenting on their profiles etc.
    ''' </summary>
    Public Property checkBlacklistUsers As Boolean
        Get
            Return _checkBlacklistUsers
        End Get
        Set(ByVal value As Boolean)
            _checkBlacklistUsers = value
        End Set
    End Property

    Private _checkBlacklistVideos As Boolean = False
    ''' <summary>
    ''' (TRUE / FALSE) Check for blacklisted videos you have in a list. If video is blacklisted, the API will skip doing various functions such as commenting on the video, etc.
    ''' </summary>
    Public Property checkBlacklistVideo As Boolean
        Get
            Return _checkBlacklistVideos
        End Get
        Set(ByVal value As Boolean)
            _checkBlacklistVideos = value
        End Set
    End Property

    Private _waitTime As Integer = 5
    ''' <summary>
    ''' Set the amount of seconds to wait inbetween each request the program has done.
    ''' </summary>
    Public Property waitTime As Integer
        Get
            Return _waitTime
        End Get
        Set(ByVal time As Integer)
            _waitTime = time
        End Set
    End Property

    Private _captchaWaitTime As Integer = 5
    ''' <summary>
    ''' Set the amount of seconds to wait after a captcha has appeared.
    ''' </summary>
    Public Property CaptchawaitTime As Integer
        Get
            Return _captchaWaitTime
        End Get
        Set(ByVal time As Integer)
            _captchaWaitTime = time
        End Set
    End Property

    Private _CaptchaShow As Boolean = False
    ''' <summary>
    ''' If TRUE, then user will have to handle captcha, if FALSE, then captchaWaitTime will activate.
    ''' </summary>
    Public Property CaptchaShow As Boolean
        Get
            Return _CaptchaShow
        End Get
        Set(ByVal show As Boolean)
            _CaptchaShow = show
        End Set
    End Property

    Private _CaptchaFailAmount As Integer = 3
    ''' <summary>
    ''' Set the amount of times you can enter an incorrect captcha.
    ''' </summary>
    Public Property CaptchaFailAmount As Integer
        Get
            Return _CaptchaFailAmount
        End Get
        Set(ByVal amount As Integer)
            _CaptchaFailAmount = amount
        End Set
    End Property

    Private _blacklistedUsers As List(Of String)
    ''' <summary>
    ''' Set the list of blacklisted users.
    ''' </summary>
    Public Property blacklistedUsers As List(Of String)
        Get
            Return _blacklistedUsers
        End Get
        Set(ByVal users As List(Of String))
            _blacklistedUsers = users
        End Set
    End Property

    Private _blacklistedVideos As List(Of String)
    ''' <summary>
    ''' Set the list of blacklisted videos. *User the video ID.
    ''' </summary>
    Public Property blacklistedVideos As List(Of String)
        Get
            Return _blacklistedVideos
        End Get
        Set(ByVal videoIDs As List(Of String))
            _blacklistedVideos = videoIDs
        End Set
    End Property
End Class
