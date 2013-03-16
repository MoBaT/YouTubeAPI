Imports System.Net

Public Class VideoInformation
    Public title As String
    Public link As String
    Public description As String
    Public publishedDate As Date
    Public category As String
    Public license As String
    Public channelName As String
    Public channelVideoAmount As Integer
    Public channelSubscribers As Integer
    Public commentAmount As Integer
    Public videoImage As String
    Public views As Integer
    Public likes As Integer
    Public dislikes As Integer

    Public Sub getVideoInformation(ByVal videoID As String)
        Dim wc As New WebClient
        Dim source As String = wc.DownloadString("http://www.youtube.com/watch?v=" & videoID)

        Title = splitBetween(source, "<meta name=""title"" content=""", Chr(34) & ">")
        Link = "http://www.youtube.com/watch?v=" & videoID
        videoImage = splitBetween(source, "<meta property=""og:image"" content=""", Chr(34))
        publishedDate = Format(CDate(splitBetween(source, "class=""watch-video-date"" >", "</")))
        Description = splitBetween(source, "<p id=""eow-description"" >", "</p>")
        Category = splitBetween(source, "<p id=""eow-category""><a href=""/", Chr(34))
        License = splitBetween(source, "<p id=""eow-reuse"">", "</p>").ToString.Replace(vbLf, "")
        channelName = splitBetween(source, "yt-user-videos""href=""/user/", "/")

        Dim datasession As String = splitBetween(source, "data-sessionlink=""", Chr(34))
        channelVideoAmount = Integer.Parse(splitBetween(source, "videos""data-sessionlink=""" & datasession & "&amp;feature=watch"">", " "))

        channelSubscribers = Integer.Parse(splitBetween(source, "subscriber-count-branded-horizontal"">", "</").ToString.Replace(",", ""))
        commentAmount = Integer.Parse(splitBetween(source, "<strong>All Comments</strong> (", ")").ToString.Replace(",", ""))
        Views = Integer.Parse(splitBetween(source, "<span class=""watch-view-count"">", "</").ToString.Replace(",", ""))
        Likes = Integer.Parse(splitBetween(source, "<span class=""likes-count"">", "</").ToString.Replace(",", ""))
        Dislikes = Integer.Parse(splitBetween(source, "<span class=""dislikes-count"">", "</").ToString.Replace(",", ""))
    End Sub

    Private Function splitBetween(ByVal source As String, ByVal first As String, ByVal second As String)
        Dim inbetween As String = Nothing

        inbetween = Split(source, first)(1)
        inbetween = Split(inbetween, second)(0)

        Return inbetween
    End Function
End Class
