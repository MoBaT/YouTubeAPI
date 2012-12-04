Public Class ApiSettings
    Private _minAge As Integer
    Public Property minAge As Integer
        Get
            Return _minAge
        End Get
        Set(ByVal value As Integer)
            _minAge = value
        End Set
    End Property

    Private _minSubs As Integer
    Public Property minSubs As Integer
        Get
            Return _minSubs
        End Get
        Set(ByVal value As Integer)
            _minSubs = value
        End Set
    End Property

    Private _minVideos As Integer
    Public Property minVideos As Integer
        Get
            Return _minVideos
        End Get
        Set(ByVal value As Integer)
            _minVideos = value
        End Set
    End Property

    Private _minVidViews As Integer
    Public Property minVidViews As Integer
        Get
            Return _minVidViews
        End Get
        Set(ByVal value As Integer)
            _minVidViews = value
        End Set
    End Property

    Private _checkBlacklistUsers As Boolean = False
    Public Property checkBlacklistUsers As Boolean
        Get
            Return _checkBlacklistUsers
        End Get
        Set(ByVal value As Boolean)
            _checkBlacklistUsers = value
        End Set
    End Property

    Private _checkBlacklistVideos As Boolean = False
    Public Property checkBlacklistVideo As Boolean
        Get
            Return _checkBlacklistVideos
        End Get
        Set(ByVal value As Boolean)
            _checkBlacklistVideos = value
        End Set
    End Property

    Private _waitTime As Integer = 5
    Public Property waitTime As Integer
        Get
            Return _waitTime
        End Get
        Set(ByVal time As Integer)
            _waitTime = time
        End Set
    End Property

    Private _captchaWaitTime As Integer = 5
    Public Property CaptchawaitTime As Integer
        Get
            Return _CaptchawaitTime
        End Get
        Set(ByVal time As Integer)
            _CaptchawaitTime = time
        End Set
    End Property

    Private _CaptchaShow As Boolean = False
    Public Property CaptchaShow As Boolean
        Get
            Return _CaptchaShow
        End Get
        Set(ByVal show As Boolean)
            _CaptchaShow = show
        End Set
    End Property

    Private _CaptchaFailAmount As Integer = 3
    Public Property CaptchaFailAmount As Integer
        Get
            Return _CaptchaFailAmount
        End Get
        Set(ByVal amount As Integer)
            _CaptchaFailAmount = amount
        End Set
    End Property

    Private _blacklistedUsers As List(Of String)
    Public Property blacklistedUsers As List(Of String)
        Get
            Return _blacklistedUsers
        End Get
        Set(ByVal users As List(Of String))
            _blacklistedUsers = users
        End Set
    End Property

    Private _blacklistedVideos As List(Of String)
    Public Property blacklistedVideos As List(Of String)
        Get
            Return _blacklistedVideos
        End Get
        Set(ByVal videoIDs As List(Of String))
            _blacklistedVideos = videoIDs
        End Set
    End Property
End Class
