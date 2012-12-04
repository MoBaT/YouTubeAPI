Imports YoutubeAPI.Utility
Imports YoutubeAPI.Utility.Http

Public Class AccountInformation
    Private _Username As String = String.Empty
    Public Property Username As String
        Get
            Return _Username
        End Get
        Set(ByVal value As String)
            _Username = value
        End Set
    End Property

    Private _Password As String = String.Empty
    Public Property Password As String
        Get
            Return _Password
        End Get
        Set(ByVal value As String)
            _Password = value
        End Set
    End Property

    Private _Cookies As New List(Of HttpCookie)
    Public Property Cookies As List(Of HttpCookie)
        Get
            Return _Cookies
        End Get
        Set(ByVal value As List(Of HttpCookie))
            _Cookies = value
        End Set
    End Property

    Public Sub New(ByVal Username As String, ByVal Password As String)
        _Username = Username
        _Password = Password
    End Sub
    Public Sub New(ByVal Username As String, ByVal Password As String, ByVal aiCookies() As HttpCookie)
        _Username = Username
        _Password = Password
        _Cookies.AddRange(aiCookies)
    End Sub
End Class
