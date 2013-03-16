Imports System.Net
Imports System.Text.RegularExpressions

Public Class ChannelInformation
    Public link As String
    Public about As String
    Public dateJoined As Date
    Public country As String
    Public subscribers As String
    Public totalVideoViews As String

    Public Sub getVideoInformation(ByVal username As String)
        Dim wc As New WebClient
        Dim source As String = wc.DownloadString("http://www.youtube.com/" & username)

        Dim collection As MatchCollection
        Dim regex As Regex = New Regex("(?<=<span class=" & Chr(34) & "stat-value" & Chr(34) & ">).+?(?=</span>)")
        collection = regex.Matches(source)

        subscribers = Integer.Parse(collection.Item(0).ToString.Replace(",", ""))
        totalVideoViews = Integer.Parse(collection.Item(1).ToString.Replace(",", ""))

        regex = New Regex("(?<=<span class=" & Chr(34) & "value" & Chr(34) & ">).+?(?=</span>)")
        collection = regex.Matches(source)

        dateJoined = Format(CDate(collection.Item(0).ToString))
        country = collection.Item(1).ToString
        link = "http://www.youtube.com/" & username
        about = splitBetween(source, "<div class=""user-profile-item profile-description"">", "</div>")

    End Sub

    Private Function splitBetween(ByVal source As String, ByVal first As String, ByVal second As String)
        Dim inbetween As String = Nothing

        inbetween = Split(source, first)(1)
        inbetween = Split(inbetween, second)(0)

        Return inbetween
    End Function
End Class