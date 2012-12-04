Imports System.Collections.Specialized
Imports System.IO
Imports System.IO.Compression
Imports System.Net
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.HttpUtility

Namespace Utility
    Public Class Http

#Region "Structures"
        Public Structure HttpProxy
            Dim Server As String
            Dim Port As Integer
            Dim UserName As String
            Dim Password As String

            Public Sub New(ByVal pServer As String, ByVal pPort As Integer, Optional ByVal pUserName As String = "", Optional ByVal pPassword As String = "")
                Server = pServer
                Port = pPort
                UserName = pUserName
                Password = pPassword
            End Sub
        End Structure
        Structure UploadData
            Dim Contents As Byte()
            Dim FileName As String
            Dim FieldName As String
            Public Sub New(ByVal uContents As Byte(), ByVal uFileName As String, ByVal uFieldName As String)
                Contents = uContents
                FileName = uFileName
                FieldName = uFieldName
            End Sub
        End Structure
#End Region

#Region "Properties"
        Private _TimeOut As Integer = 15000
        Public Property TimeOut() As Integer
            Get
                Return _TimeOut
            End Get
            Set(ByVal value As Integer)
                _TimeOut = value
            End Set
        End Property

        Private _Proxy As HttpProxy = New HttpProxy
        Public Property Proxy() As HttpProxy
            Get
                Return _Proxy
            End Get
            Set(ByVal value As HttpProxy)
                _Proxy = value
            End Set
        End Property

        Private _UserAgent As String = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:7.0.1) Gecko/20100101 Firefox/7.0.1"
        Public Property Useragent() As String
            Get
                Return _UserAgent
            End Get
            Set(ByVal value As String)
                _UserAgent = value
            End Set
        End Property

        Private _Referer As String = String.Empty
        Public Property Referer() As String
            Get
                Return _Referer
            End Get
            Set(ByVal value As String)
                _Referer = value
            End Set
        End Property

        Private _AutoRedirect As Boolean = True
        Public Property AutoRedirect As Boolean
            Get
                Return _AutoRedirect
            End Get
            Set(ByVal value As Boolean)
                _AutoRedirect = value
            End Set
        End Property

        Private _StoreCookies As Boolean = True
        Public Property StoreCookies() As Boolean
            Get
                Return _StoreCookies
            End Get
            Set(ByVal value As Boolean)
                _StoreCookies = value
            End Set
        End Property

        Private _SendCookies As Boolean = True
        Public Property SendCookies() As Boolean
            Get
                Return _SendCookies
            End Get
            Set(ByVal value As Boolean)
                _SendCookies = value
            End Set
        End Property

        Private _GetImage As Boolean = False
        Public Property GetImage As Boolean
            Get
                Return _GetImage
            End Get
            Set(ByVal value As Boolean)
                _GetImage = value
            End Set
        End Property

        Private _LastResponseUri As String = String.Empty
        Public Property LastResponseUri As String
            Get
                Return _LastResponseUri
            End Get
            Set(ByVal value As String)
                _LastResponseUri = value
            End Set
        End Property

        Private _KeepAlive As Boolean = True
        Public Property KeepAlive As Boolean
            Get
                Return _KeepAlive
            End Get
            Set(ByVal value As Boolean)
                _KeepAlive = value
            End Set
        End Property

        Private _Version As System.Version = HttpVersion.Version11
        Public Property Version As System.Version
            Get
                Return _Version
            End Get
            Set(ByVal value As System.Version)
                _Version = value
            End Set
        End Property

        'Private _Accept As String = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
        Private _Accept As String = "*/*"
        Public Property Accept As String
            Get
                Return _Accept
            End Get
            Set(ByVal value As String)
                _Accept = value
            End Set
        End Property

        Private _AllowExpect100 As Boolean = False
        Public Property AllowExpect100 As Boolean
            Get
                Return _AllowExpect100
            End Get
            Set(ByVal value As Boolean)
                _AllowExpect100 = value
            End Set
        End Property

        Private _ContentType As String = "application/x-www-form-urlencoded; charset=UTF-8"
        Public Property ContentType As String
            Get
                Return _ContentType
            End Get
            Set(ByVal value As String)
                _ContentType = value
            End Set
        End Property

        Private _DebugMode As Boolean = False
        Public Property DebugMode As Boolean
            Get
                Return _DebugMode
            End Get
            Set(ByVal value As Boolean)
                _DebugMode = value
            End Set
        End Property

        Private _AcceptEncoding As String = "gzip, deflate"
        Public Property AcceptEncoding As String
            Get
                Return _AcceptEncoding
            End Get
            Set(ByVal value As String)
                _AcceptEncoding = value
            End Set
        End Property

        Private _AcceptLanguage As String = "en-us,en;q=0.5"
        Public Property AcceptLanguage As String
            Get
                Return _AcceptLanguage
            End Get
            Set(ByVal value As String)
                _AcceptLanguage = value
            End Set
        End Property

        Private _AcceptCharset As String = "ISO-8859-1,utf-8;q=0.7,*;q=0.7"
        Public Property AcceptCharset As String
            Get
                Return _AcceptCharset
            End Get
            Set(ByVal value As String)
                _AcceptCharset = value
            End Set
        End Property
#End Region

        Public RedirectBlacklist As New List(Of String)

        Private Const LineFeed = vbCrLf
        Private Request As HttpWebRequest = Nothing

        Private Cookies As List(Of HttpCookie)
        Private fCookie As String = String.Empty

        Public Sub New()
            System.Net.ServicePointManager.DefaultConnectionLimit = 500
            System.Net.ServicePointManager.Expect100Continue = Me.AllowExpect100
            System.Net.ServicePointManager.ServerCertificateValidationCallback = AddressOf AcceptAllCertifications
            System.Net.ServicePointManager.UseNagleAlgorithm = False
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3
            Cookies = New List(Of HttpCookie)
        End Sub

        Public Sub CancelRequest()
            Try
                If Request Is Nothing Then Exit Try
                If Request.HaveResponse = False Then Request.Abort()
            Catch ex As Exception
                Debug.Print(ex.ToString)
            End Try
        End Sub

        Public Function GetResponse(ByVal Uri As String, Optional ByVal PostData As String = "", Optional ByVal Headers As NameValueCollection = Nothing) As HttpResponse
            Dim hr As New HttpResponse
            Dim Response As HttpWebResponse = Nothing

request:
            Try
                If String.IsNullOrEmpty(Uri) Then
                    hr = New HttpResponse()
                    hr.Exception = New Exception("Uri was empty.")
                    Return hr
                End If

                _LastResponseUri = Uri

                Request = DirectCast(WebRequest.Create(Uri), HttpWebRequest)
                With Request
                    Dim ServicePoint As ServicePoint = .ServicePoint
                    Dim Prop As Reflection.PropertyInfo = ServicePoint.[GetType]().GetProperty("HttpBehaviour", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                    Prop.SetValue(ServicePoint, CByte(0), Nothing)

                    .Method = IIf(String.IsNullOrEmpty(PostData), "GET", "POST")
                    If Me.DebugMode Then Debug.Print(String.Format("[{0}] {1} >> {2}", Now.ToString("hh:mm:ss tt").ToLower, .Method, Uri))

                    .AllowWriteStreamBuffering = False
                    .AllowAutoRedirect = False
                    .KeepAlive = Me.KeepAlive
                    .UserAgent = Me.Useragent
                    .ContentType = IIf(.Method = "POST", Me.ContentType, String.Empty)
                    .AutomaticDecompression = DecompressionMethods.GZip And DecompressionMethods.Deflate
                    .Accept = Me.Accept
                    .Timeout = Me.TimeOut
                    .ProtocolVersion = Me.Version
                    .ServicePoint.Expect100Continue = Me.AllowExpect100
                    If Not String.IsNullOrEmpty(Me.Referer) Then .Referer = Me.Referer

                    If Not String.IsNullOrEmpty(Me.Proxy.Server) Then
                        .Proxy = New WebProxy(Me.Proxy.Server, Me.Proxy.Port)
                        If Not String.IsNullOrEmpty(Me.Proxy.UserName) Then .Proxy.Credentials = New NetworkCredential(Me.Proxy.UserName, Me.Proxy.Password)
                    End If

                    If Me.SendCookies Then
                        Dim c As String = GetCookies(New Uri(Uri).Host)
                        If Not String.IsNullOrEmpty(c) Then .Headers.Add("Cookie", c)
                    End If

                    .Headers.Add(HttpRequestHeader.AcceptEncoding, Me.AcceptEncoding)
                    .Headers.Add(HttpRequestHeader.AcceptLanguage, Me.AcceptLanguage)
                    .Headers.Add(HttpRequestHeader.AcceptCharset, Me.AcceptCharset)

                    If Not Headers Is Nothing Then
                        For Index As Integer = 0 To (Headers.Count - 1)
                            If Headers.Keys(Index) = "Content-Type" Then
                                .ContentType = Headers(Index)
                            Else
                                .Headers.Add(Headers.Keys(Index), Headers(Index))
                            End If
                        Next
                    End If

                    If Not String.IsNullOrEmpty(PostData) Then
                        Dim byteArray As Byte() = Encoding.UTF8.GetBytes(PostData)
                        .ContentLength = byteArray.Length
                        Dim dataStream As Stream = .GetRequestStream()
                        dataStream.Write(byteArray, 0, byteArray.Length)
                        dataStream.Close() : dataStream.Dispose() : dataStream = Nothing
                    End If
                    PostData = String.Empty

                    Response = CType(.GetResponse(), HttpWebResponse)
                    If Me.StoreCookies Then ParseCookies(Response)

                    hr.WebResponse = Response

                    If Me.AutoRedirect Then
                        Select Case hr.WebResponse.StatusCode
                            Case HttpStatusCode.Found, HttpStatusCode.Redirect, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently, HttpStatusCode.RedirectMethod
                                Uri = hr.WebResponse.Headers("Location")
                                Try
                                    Dim u As New Uri(Uri)
                                Catch ex As Exception
                                    Uri = "http://" & Request.RequestUri.Host & IIf(Uri.StartsWith("/"), Uri, "/" & Uri)
                                End Try
                                Dim b As Boolean = True
                                For Each r As String In RedirectBlacklist
                                    If Uri.ToLower.Contains(r.ToLower) Then
                                        b = False
                                        Exit For
                                    End If
                                Next
                                PostData = String.Empty
                                If b Then GoTo request
                        End Select
                    End If

                    If Not hr.WebResponse Is Nothing Then hr.Headers = hr.WebResponse.Headers.ToString
                    If Not hr.WebResponse.Headers(HttpResponseHeader.ContentType) Is Nothing Then
                        If hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("text/") Or _
                           hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("/xml") Or _
                           hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("/json") Or _
                            hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("application/") Then
                            GoTo getHtml
                        ElseIf hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("image/") Then
                            'If GetImage Then hr.Image = System.Drawing.Image.FromStream(Response.GetResponseStream)
                        Else
                            Debug.Print("Unknown content type: " & hr.WebResponse.Headers(HttpResponseHeader.ContentType))
                        End If
                    Else
getHtml:
                        If Not hr.WebResponse Is Nothing Then hr.Html = HtmlDecode(ProcessResponse(hr.WebResponse))
                        If Me.AutoRedirect Then
                            If hr.Html.ToLower.Contains("<meta http-equiv=""refresh") Then
                                Uri = ParseMetaRefreshUrl(hr.Html)
                                Dim b As Boolean = True
                                For Each r As String In RedirectBlacklist
                                    If Uri.ToLower.Contains(r.ToLower) Then
                                        b = False
                                        Exit For
                                    End If
                                Next
                                PostData = String.Empty
                                If b Then GoTo request
                            End If
                        End If
                    End If

                End With
            Catch we As WebException
                hr.Exception = we
                If Not we.Response Is Nothing Then
                    hr.WebResponse = we.Response
                    hr.Html = ProcessResponse(hr.WebResponse)
                End If
            Catch ex As Exception
                hr.Exception = ex
            Finally
                Request = Nothing : Response = Nothing
                Me.Referer = String.Empty
            End Try
            Return hr
        End Function
        Public Function GetUploadResponse(ByVal Uri As String, ByVal Headers As NameValueCollection, ByVal Fields As NameValueCollection, ByVal ParamArray Upload As UploadData()) As HttpResponse
            Dim hr As New HttpResponse
            Dim Response As HttpWebResponse = Nothing
            Dim Boundary As String = Guid.NewGuid().ToString().Replace("-", "")

request:
            Try
                If String.IsNullOrEmpty(Uri) Then
                    hr = New HttpResponse()
                    hr.Exception = New Exception("Uri was empty.")
                    Return hr
                End If
                If Fields Is Nothing AndAlso Upload Is Nothing Then
                    hr = GetResponse(Uri)
                    Exit Try
                End If

                _LastResponseUri = Uri

                Request = DirectCast(WebRequest.Create(Uri), HttpWebRequest)
                With Request
                    Dim ServicePoint As ServicePoint = .ServicePoint
                    Dim Prop As Reflection.PropertyInfo = ServicePoint.[GetType]().GetProperty("HttpBehaviour", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                    Prop.SetValue(ServicePoint, CByte(0), Nothing)

                    .Method = "POST"
                    If Me.DebugMode Then Debug.Print(String.Format("[{0}] POST (Multi-Part) >> {1}", Now.ToString("hh:mm:ss tt").ToLower, Uri))

                    .AllowWriteStreamBuffering = False
                    .AllowAutoRedirect = False
                    .KeepAlive = Me.KeepAlive
                    .UserAgent = Me.Useragent
                    .ContentType = IIf(.Method = "POST", Me.ContentType, String.Empty)
                    .ContentType = "multipart/form-data; boundary=" & Boundary
                    .AutomaticDecompression = DecompressionMethods.GZip And DecompressionMethods.Deflate
                    .Accept = Me.Accept
                    .Timeout = Me.TimeOut
                    .ProtocolVersion = Me.Version
                    .ServicePoint.Expect100Continue = Me.AllowExpect100
                    If Not String.IsNullOrEmpty(Me.Referer) Then .Referer = Me.Referer

                    If Me.SendCookies Then
                        Dim c As String = GetCookies(New Uri(Uri).Host)
                        If Not String.IsNullOrEmpty(c) Then .Headers.Add("Cookie", c)
                    End If

                    If Not String.IsNullOrEmpty(Me.Proxy.Server) Then
                        .Proxy = New WebProxy(Me.Proxy.Server, Me.Proxy.Port)
                        If Not String.IsNullOrEmpty(Me.Proxy.UserName) Then .Proxy.Credentials = New NetworkCredential(Me.Proxy.UserName, Me.Proxy.Password)
                    End If

                    .Headers.Add(HttpRequestHeader.AcceptEncoding, Me.AcceptEncoding)
                    .Headers.Add(HttpRequestHeader.AcceptLanguage, Me.AcceptLanguage)
                    .Headers.Add(HttpRequestHeader.AcceptCharset, Me.AcceptCharset)

                    If Not Headers Is Nothing Then
                        For Index As Integer = 0 To (Headers.Count - 1)
                            If Headers.Keys(Index) = "Content-Type" Then
                                .ContentType = Headers(Index)
                            Else
                                .Headers.Add(Headers.Keys(Index), Headers(Index))
                            End If
                        Next
                    End If

                    Dim PostData As New MemoryStream()
                    Dim Writer As New StreamWriter(PostData)
                    With Writer
                        If Fields IsNot Nothing Then
                            For Index As Integer = 0 To (Fields.Count - 1)
                                .Write(("--" & Boundary) + LineFeed)
                                .Write("Content-Disposition: form-data; name=""{0}""{1}{1}{2}{1}", Fields.Keys(Index), LineFeed, Fields(Index))
                            Next
                        End If
                        If Not (Upload Is Nothing) Then
                            For Each u As UploadData In Upload
                                .Write(("--" & Boundary) + LineFeed)
                                .Write("Content-Disposition: form-data; name=""{0}""; filename=""{1}""{2}", u.FieldName, u.FileName, LineFeed)
                                .Write(("Content-Type: " & GetContentType(u.FileName) & LineFeed) & LineFeed)
                                .Flush()
                                If Not (u.Contents Is Nothing) Then PostData.Write(u.Contents, 0, u.Contents.Length)
                                .Write(LineFeed)
                            Next
                        End If
                        .Write("--{0}--{1}", Boundary, LineFeed)
                        .Flush()
                    End With

                    .ContentLength = PostData.Length
                    Using s As Stream = .GetRequestStream()
                        PostData.WriteTo(s)
                    End Using
                    PostData.Close()

                    Fields = Nothing : Upload = Nothing

                    Response = CType(.GetResponse(), HttpWebResponse)
                    If Me.StoreCookies Then ParseCookies(Response)

                    hr.WebResponse = Response

                    If Me.AutoRedirect Then
                        Select Case hr.WebResponse.StatusCode
                            Case HttpStatusCode.Found, HttpStatusCode.Redirect, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently, HttpStatusCode.RedirectMethod
                                Uri = hr.WebResponse.Headers("Location")
                                Try
                                    Dim u As New Uri(Uri)
                                Catch ex As Exception
                                    Uri = "http://" & Request.RequestUri.Host & IIf(Uri.StartsWith("/"), Uri, "/" & Uri)
                                End Try
                                Dim b As Boolean = True
                                For Each r As String In RedirectBlacklist
                                    If Uri.ToLower.Contains(r.ToLower) Then
                                        b = False
                                        Exit For
                                    End If
                                Next
                                If b Then GoTo request
                        End Select
                    End If

                    If Not hr.WebResponse Is Nothing Then hr.Headers = hr.WebResponse.Headers.ToString
                    If Not hr.WebResponse.Headers(HttpResponseHeader.ContentType) Is Nothing Then
                        If hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("text/") Or _
                           hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("/xml") Or _
                           hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("/json") Or _
                            hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("application/") Then
                            GoTo getHtml
                        ElseIf hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("image/") Then
                            'If GetImage Then hr.Image = System.Drawing.Image.FromStream(Response.GetResponseStream)
                        Else
                            Debug.Print("Unknown content type: " & hr.WebResponse.Headers(HttpResponseHeader.ContentType))
                        End If
                    Else
getHtml:
                        If Not hr.WebResponse Is Nothing Then hr.Html = HtmlDecode(ProcessResponse(hr.WebResponse))
                        If Me.AutoRedirect Then
                            If hr.Html.ToLower.Contains("<meta http-equiv=""refresh") Then
                                Uri = ParseMetaRefreshUrl(hr.Html)
                                Dim b As Boolean = True
                                For Each r As String In RedirectBlacklist
                                    If Uri.ToLower.Contains(r.ToLower) Then
                                        b = False
                                        Exit For
                                    End If
                                Next
                                If b Then GoTo request
                            End If
                        End If
                    End If

                End With
            Catch we As WebException
                hr.Exception = we
                If Not we.Response Is Nothing Then
                    hr.WebResponse = we.Response
                    hr.Html = ProcessResponse(hr.WebResponse)
                End If
            Catch ex As Exception
                hr.Exception = ex
            Finally
                Request = Nothing : Response = Nothing
                Me.Referer = String.Empty
            End Try
            Return hr
        End Function
        Public Function GetUploadResponse(ByVal Uri As String, ByVal Path As String, Optional ByVal Headers As NameValueCollection = Nothing) As HttpResponse
            Dim hr As New HttpResponse
            Dim Response As HttpWebResponse = Nothing

request:
            Try
                If String.IsNullOrEmpty(Uri) Then
                    hr = New HttpResponse()
                    hr.Exception = New Exception("Uri was empty.")
                    Return hr
                End If

                _LastResponseUri = Uri

                Request = DirectCast(WebRequest.Create(Uri), HttpWebRequest)
                With Request
                    Dim ServicePoint As ServicePoint = .ServicePoint
                    Dim Prop As Reflection.PropertyInfo = ServicePoint.[GetType]().GetProperty("HttpBehaviour", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
                    Prop.SetValue(ServicePoint, CByte(0), Nothing)

                    .Method = IIf(String.IsNullOrEmpty(Path), "GET", "POST")
                    If Me.DebugMode Then Debug.Print(String.Format("[{0}] {1}{2} >> {3}", Now.ToString("hh:mm:ss tt").ToLower, .Method, IIf(.Method = "POST", " Upload", String.Empty), Uri))

                    .AllowWriteStreamBuffering = False
                    .AllowAutoRedirect = False
                    .KeepAlive = Me.KeepAlive
                    .UserAgent = Me.Useragent
                    .ContentType = GetContentType(Path)
                    .AutomaticDecompression = DecompressionMethods.GZip And DecompressionMethods.Deflate
                    .Accept = Me.Accept
                    .Timeout = Me.TimeOut
                    .ProtocolVersion = Me.Version
                    .ServicePoint.Expect100Continue = Me.AllowExpect100
                    If Not String.IsNullOrEmpty(Me.Referer) Then .Referer = Me.Referer

                    If Not Headers Is Nothing Then
                        For Index As Integer = 0 To (Headers.Count - 1)
                            If Headers.Keys(Index) = "Content-Type" Then
                                .ContentType = Headers(Index)
                            Else
                                .Headers.Add(Headers.Keys(Index), Headers(Index))
                            End If
                        Next
                    End If

                    If Me.SendCookies Then
                        Dim c As String = GetCookies(New Uri(Uri).Host)
                        If Not String.IsNullOrEmpty(c) Then .Headers.Add("Cookie", c)
                    End If

                    If Not String.IsNullOrEmpty(Me.Proxy.Server) Then
                        .Proxy = New WebProxy(Me.Proxy.Server, Me.Proxy.Port)
                        If Not String.IsNullOrEmpty(Me.Proxy.UserName) Then .Proxy.Credentials = New NetworkCredential(Me.Proxy.UserName, Me.Proxy.Password)
                    End If

                    .Headers.Add(HttpRequestHeader.AcceptEncoding, Me.AcceptEncoding)
                    .Headers.Add(HttpRequestHeader.AcceptLanguage, Me.AcceptLanguage)
                    .Headers.Add(HttpRequestHeader.AcceptCharset, Me.AcceptCharset)
                    .Headers.Add(HttpRequestHeader.CacheControl, "no-cache")

                    Dim PostData As New MemoryStream()
                    Dim Writer As New StreamWriter(PostData)
                    With Writer
                        Dim pInfo() As Byte = System.IO.File.ReadAllBytes(Path)
                        PostData.Write(pInfo, 0, pInfo.Length)
                        .Flush()
                    End With

                    .ContentLength = PostData.Length
                    Using s As Stream = .GetRequestStream()
                        PostData.WriteTo(s)
                    End Using
                    PostData.Close()

                    Path = String.Empty

                    Response = CType(.GetResponse(), HttpWebResponse)
                    If Me.StoreCookies Then ParseCookies(Response)

                    Writer.Close() : Writer.Dispose() : Writer = Nothing
                    PostData.Close() : PostData.Dispose() : PostData = Nothing

                    hr.WebResponse = Response

                    If Me.AutoRedirect Then
                        Select Case hr.WebResponse.StatusCode
                            Case HttpStatusCode.Found, HttpStatusCode.Redirect, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently, HttpStatusCode.RedirectMethod
                                Uri = hr.WebResponse.Headers("Location")
                                Try
                                    Dim u As New Uri(Uri)
                                Catch ex As Exception
                                    Uri = "http://" & Request.RequestUri.Host & IIf(Uri.StartsWith("/"), Uri, "/" & Uri)
                                End Try
                                Dim b As Boolean = True
                                For Each r As String In RedirectBlacklist
                                    If Uri.ToLower.Contains(r.ToLower) Then
                                        b = False
                                        Exit For
                                    End If
                                Next
                                If b Then GoTo request
                        End Select
                    End If

                    If Not hr.WebResponse Is Nothing Then hr.Headers = hr.WebResponse.Headers.ToString
                    If Not hr.WebResponse.Headers(HttpResponseHeader.ContentType) Is Nothing Then
                        If hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("text/") Or _
                            hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("/xml") Or _
                            hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("/json") Or _
                            hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("application/") Then
                            GoTo getHtml
                        ElseIf hr.WebResponse.Headers(HttpResponseHeader.ContentType).Contains("image/") Then
                            'If GetImage Then hr.Image = System.Drawing.Image.FromStream(Response.GetResponseStream)
                        Else
                            Debug.Print("Unknown content type: " & hr.WebResponse.Headers(HttpResponseHeader.ContentType))
                        End If
                    Else
getHtml:
                        If Not hr.WebResponse Is Nothing Then hr.Html = HtmlDecode(ProcessResponse(hr.WebResponse))
                        If Me.AutoRedirect Then
                            If hr.Html.ToLower.Contains("<meta http-equiv=""refresh") Then
                                Uri = ParseMetaRefreshUrl(hr.Html)
                                Dim b As Boolean = True
                                For Each r As String In RedirectBlacklist
                                    If Uri.ToLower.Contains(r.ToLower) Then
                                        b = False
                                        Exit For
                                    End If
                                Next
                                If b Then GoTo request
                            End If
                        End If
                    End If

                End With
            Catch we As WebException
                hr.Exception = we
                If Not we.Response Is Nothing Then
                    hr.WebResponse = we.Response
                    hr.Html = ProcessResponse(hr.WebResponse)
                End If
            Catch ex As Exception
                hr.Exception = ex
            Finally
                Request = Nothing : Response = Nothing
                Me.Referer = String.Empty
            End Try
            Return hr
        End Function

        Private Function ProcessResponse(ByVal Response As System.Net.HttpWebResponse) As String
            Try
                Dim sb As New StringBuilder
                With Response
                    Dim Stream As System.IO.Stream = .GetResponseStream

                    If (Response.ContentEncoding.ToLower().Contains("gzip")) Then
                        Stream = New GZipStream(Stream, CompressionMode.Decompress)
                    ElseIf (Response.ContentEncoding.ToLower().Contains("deflate")) Then
                        Stream = New DeflateStream(Stream, CompressionMode.Decompress)
                    End If

                    Dim Reader As New StreamReader(Stream)
                    Dim Buffer(1024) As [Char]
                    Dim Read As Integer = Reader.Read(Buffer, 0, 1024)
                    While Read > 0
                        Dim outputData As New [String](Buffer, 0, Read)
                        outputData = Replace(outputData, vbNullChar, String.Empty)
                        sb.Append(outputData)
                        Read = Reader.Read(Buffer, 0, 1024)
                    End While
                    Reader.Close() : Reader.Dispose() : Reader = Nothing
                    Stream.Close() : Stream.Dispose() : Stream = Nothing
                End With
                Return sb.ToString
            Catch ex As Exception
                Return String.Empty
            End Try
        End Function
        Public Function ProcessException(ByVal Ex As Object) As HttpError
            Dim Result As New HttpError
            Dim Message As String = String.Empty

            Result.Exception = Ex

            If TypeOf Ex Is WebException Then
                Dim we As WebException = DirectCast(Ex, WebException)
                Select Case we.Status
                    Case WebExceptionStatus.Timeout
                        Message = "Timed out."
                        If Not String.IsNullOrEmpty(Me.Proxy.Server) Then Result.IsProxyError = True
                    Case WebExceptionStatus.ConnectFailure
                        If we.Message.Trim = "Unable to connect to the remote server" Then
                            Message = IIf(Not String.IsNullOrEmpty(Me.Proxy.Server), "Could not connect to proxy server.", "Could not connect to server.")
                            If Not String.IsNullOrEmpty(Me.Proxy.Server) Then Result.IsProxyError = True
                        Else
                            Message = we.Message
                        End If
                    Case WebExceptionStatus.ProtocolError
                        If we.Message.Trim = "The remote server returned an error: (407) Proxy Authentication Required." Then
                            Message = IIf(Not String.IsNullOrEmpty(Me.Proxy.UserName), "Invalid authentication credentials.", "Authentication credentials not provided.")
                            Result.IsProxyError = True
                        Else
                            Message = we.Message
                        End If
                    Case WebExceptionStatus.KeepAliveFailure
                        Message = IIf(Not String.IsNullOrEmpty(Me.Proxy.Server), "Disconnected from proxy server.", we.Message)
                        If Not String.IsNullOrEmpty(Me.Proxy.Server) Then Result.IsProxyError = True
                    Case WebExceptionStatus.ProtocolError
                        If we.Message.Trim = "The remote server returned an error: (500) Internal Server Error." Then
                            Message = "Internal server error."
                            If Not String.IsNullOrEmpty(Me.Proxy.Server) Then Result.IsProxyError = True
                        Else
                            Message = we.Message
                        End If
                    Case Else
                        Message = we.Message
                        Debug.Print("Exception else: " & we.Status & " - " & CInt(we.Status))
                End Select
            Else
                Message = TryCast(Ex, Exception).Message
            End If
            Result.Message = Message
            Return Result
        End Function

        Public Function FixData(ByVal Data As String) As String
            Return HtmlDecode(Data.Replace("\/\/", "//").Replace("\/", "/").Replace("\""", """").Replace("\u003e", ">").Replace("\u003c", "<").Replace("\u003a", ":").Replace("\u003b", ";").Replace("\u003f", "?").Replace("\u003d", "=").Replace("\u002f", "/").Replace("\u0026", "&").Replace("\u002b", "+").Replace("\u0025", "%").Replace("\u0027", "'").Replace("\u007b", "{").Replace("\u007d", "}").Replace("\u007c", "|").Replace("\u0022", """").Replace("\u0023", "#").Replace("\u0021", "!").Replace("\u0024", "$").Replace("\u0040", "@").Replace("\002f", "/").Replace("\r\n", vbCrLf & vbCrLf).Replace("\n", vbCrLf)).Replace("\x3a", ":").Replace("\x2f", "/").Replace("\x3f", "?").Replace("\x3d", "=").Replace("\x26", "&")
        End Function

        Public Sub ClearCookies()
            Me.Cookies.Clear()
        End Sub
        Public Sub AddCookie(ByVal c As HttpCookie)
            Cookies.Add(c)
        End Sub
        Public Sub AddCookie(ByVal c() As HttpCookie)
            Cookies.AddRange(c)
        End Sub
        Public Sub RemoveCookie(ByVal c As HttpCookie)
            Me.Cookies.Remove(c)
        End Sub
        Public Sub RemoveCookie(ByVal Name As String)
            Dim c As HttpCookie = FindCookie(Name)
            If Not c Is Nothing Then RemoveCookie(c)
        End Sub
        Public Sub RemoveDuplicateCookies()
            Dim c As List(Of HttpCookie) = Me.Cookies.Distinct.ToList
            ClearCookies()
            AddCookie(c.ToArray)
        End Sub
        Public Function FindCookie(ByVal Name As String) As HttpCookie
            Dim Result As HttpCookie = Nothing
            For Each c As HttpCookie In Cookies
                If c.Name = Name Then
                    Result = c
                    Exit For
                End If
            Next
            Return Result
        End Function
        Public Function GetAllCookies() As List(Of HttpCookie)
            Return Me.Cookies
        End Function

        Private Function FindCookie(ByVal c As Utility.Http.HttpCookie) As Boolean ' Search predicate
            Return IIf(c.Name.ToLower.Replace(" ", "").Trim = fCookie.ToLower.Replace(" ", "").Trim, True, False)
        End Function
        Private Function GetCookies() As String
            Dim Result As String = String.Empty
            With Cookies
                If .Count = 0 Then Return Result
                For Each item As HttpCookie In Cookies
                    Result &= item.Name & "=" & item.Value & "; "
                Next
                If Result.EndsWith("; ") Then Result = Result.Substring(0, Result.Length - 2)
            End With
            Return Result
        End Function
        Private Function GetCookies(ByVal Domain As String) As String
            Dim Result As String = String.Empty
            With Cookies
                If .Count = 0 Then Return Result
                For Each item As HttpCookie In Cookies
                    If item.Domain.ToLower.Trim = Domain.ToLower.Trim Then
                        Result &= item.Name & IIf(String.IsNullOrEmpty(item.Value), "", "=" & item.Value) & "; "
                    Else
                        If item.Domain.StartsWith(".") AndAlso Domain.Contains(item.Domain) Then
                            Result &= item.Name & IIf(String.IsNullOrEmpty(item.Value), "", "=" & item.Value) & "; "
                        ElseIf item.Domain.StartsWith(".") AndAlso CountOccurance(Domain, ".") = 1 Then
                            Result &= item.Name & IIf(String.IsNullOrEmpty(item.Value), "", "=" & item.Value) & "; "
                        ElseIf item.Domain.Contains(item.Domain) Then
                            Result &= item.Name & IIf(String.IsNullOrEmpty(item.Value), "", "=" & item.Value) & "; "
                        Else
                            Debug.Print("here: " & Domain & " / " & item.Domain)
                        End If
                    End If
                Next
                If Result.EndsWith("; ") Then Result = Result.Substring(0, Result.Length - 2)
            End With
            Return Result
        End Function

        Private Sub ParseCookies(ByVal Response As HttpWebResponse)
            Try
                Dim Data As String = Response.Headers("Set-Cookie")
                If Data = Nothing Then Exit Sub
                If String.IsNullOrEmpty(Data) Then Exit Sub
                Data = Data.Replace("Mon,", "Mon").Replace("Tue,", "Tue").Replace("Wed,", "Wed").Replace("Thu,", "Thu").Replace("Fri,", "Fri").Replace("Sat,", "Sat").Replace("Sun,", "Sun")
                Data = Data.Replace("Monday,", "Mon").Replace("Tuesday,", "Tue").Replace("Wednesday,", "Wed").Replace("Thursday,", "Thurs").Replace("Friday,", "Fri").Replace("Saturday,", "Sat").Replace("Sunday,", "Sun")

                For Each c As String In Data.Split(",")
                    Dim Cookie As New HttpCookie
                    With Cookie
                        .Name = c.Split("=")(0).Trim

                        If c.Contains(";") Then
                            .Value = c.Substring(0, c.IndexOf(";"))
                            .Value = .Value.Split("=")(1).Trim
                        Else
                            .Value = c.Split("=")(1).Trim
                        End If

                        If Not .Value.ToLower = "deleted" Then
                            For Each Parameter As String In Split(c, ";")
                                Parameter = Parameter.Trim
                                If Parameter.Contains("=") Then
                                    Dim Key As String = Parameter.Split("=")(0).Trim
                                    Dim Value As String = Parameter.Substring(Parameter.IndexOf("=") + 1).Trim

                                    Select Case Key.ToLower
                                        Case .Name.ToLower
                                            .Value = Value
                                        Case "path"
                                            .Path = Value
                                        Case "expires"
                                            Try
                                                If Value.ToLower.EndsWith("utc") Then
                                                    Value = Value.Substring(0, Value.ToLower.IndexOf("utc")).Trim
                                                    Try
                                                        .Expires = System.DateTime.Parse(Value)
                                                    Catch ex As Exception
                                                        .Expires = Nothing
                                                    End Try
                                                Else
                                                    .Expires = Value
                                                End If
                                            Catch ex As Exception
                                                ' TODO: Custom date parse here (Example: Conversion from string "Fri 16-Dec-2011 04:04:32 UTC" to type 'Date' is not valid.)
                                                .Expires = Nothing
                                            End Try
                                        Case "domain"
                                            If CountOccurance(Value, ".") = 1 Then Value = Response.ResponseUri.Host 'Value = Response.ResponseUri.Host
                                            .Domain = Value
                                        Case Else
                                            Debug.Print("unknown with value: " & Key)
                                    End Select
                                Else
                                    Select Case Parameter.ToLower
                                        Case "secure"
                                            .Secure = True
                                        Case "httponly"
                                            .HttpOnly = True
                                        Case Else
                                            Debug.Print("unknown with no value: " & Parameter)
                                    End Select
                                End If
                            Next

                            If String.IsNullOrEmpty(.Path) Then .Path = Response.ResponseUri.AbsolutePath
                            If String.IsNullOrEmpty(.Domain) Then .Domain = Response.ResponseUri.Host

                            fCookie = .Name
                            Dim Index As Integer = Cookies.FindIndex(AddressOf FindCookie)
                            If Not Index = -1 Then Cookies.RemoveAt(Index)
                            Cookies.Add(Cookie)
                        Else
                            Dim Index As Integer = Cookies.FindIndex(AddressOf FindCookie)
                            If Not Index = -1 Then Cookies.RemoveAt(Index)
                        End If
                    End With
                Next
                RemoveDuplicateCookies()
            Catch ex As Exception
                Debug.Print(ex.ToString)
            End Try
        End Sub

        Public Function UrlEncode(ByVal Data As String) As String
            Return HttpUtility.UrlEncode(Data)
        End Function
        Public Function UrlEncode2(ByVal Value As String) As String
            ' Proper casing of UrlEncode (MS is encodes lowercase)

            Dim Unused As String = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~"
            Dim sb As New StringBuilder()
            For Each c As Char In Value
                sb.Append(IIf(Unused.IndexOf(c) <> -1, c, "%" + String.Format("{0:X2}", Convert.ToInt32(c))))
            Next
            Return sb.ToString()
        End Function
        Public Function UrlDecode(ByVal Data As String) As String
            Return HttpUtility.UrlDecode(Data)
        End Function
        Public Function HtmlEncode(ByVal Data As String) As String
            Return HttpUtility.HtmlEncode(Data)
        End Function
        Public Function HtmlDecode(ByVal Data As String) As String
            Return HttpUtility.HtmlDecode(Data)
        End Function

        Public Function EscapeUnicode(ByVal Data As String) As String
            Return Regex.Unescape(Data)
        End Function

        Public Function ParseMetaRefreshUrl(ByVal Html As String) As String
            If String.IsNullOrEmpty(Html) Then Return String.Empty
            Dim Result As String = Html.Substring(Html.ToLower.IndexOf("<meta http-equiv=""refresh""") + "<meta http-equiv=""refresh""".Length)
            Result = ParseBetween(Result.ToLower, "url=", """", "url=".Length).Trim
            If Result.StartsWith("'") Then Result = Result.Substring(1)
            If Result.EndsWith("'") Then Result = Result.Substring(0, Result.Length - 1)
            Return Result
        End Function
        Public Function ParseBetween(ByVal Html As String, ByVal Before As String, ByVal After As String, ByVal Offset As Integer) As String
            If String.IsNullOrEmpty(Html) Then Return String.Empty
            If Html.Contains(Before) Then
                Dim Result As String = Html.Substring(Html.IndexOf(Before) + Offset)
                If Result.Contains(After) Then
                    If Not String.IsNullOrEmpty(After) Then Result = Result.Substring(0, Result.IndexOf(After))
                End If
                Return Result
            Else
                Return String.Empty
            End If
        End Function
        Public Function ParseFormIdText(ByVal Html As String, ByVal Id As String, Optional ByVal Highlighter As String = """") As String
            If String.IsNullOrEmpty(Html) Then Return String.Empty
            Dim value As String = String.Empty
            Try
                Html = Html.Substring(Html.IndexOf("id=" & Highlighter & Id & Highlighter) + 5)
                value = ParseBetween(Html, "value=" & Highlighter, Highlighter, 7)
            Catch
            End Try
            Return value
        End Function
        Public Function ParseFormNameText(ByVal Html As String, ByVal Name As String, Optional ByVal Highlighter As String = """") As String
            If String.IsNullOrEmpty(Html) Then Return String.Empty
            Dim value As String = String.Empty
            Try
                Html = Html.Substring(Html.IndexOf("name=" & Highlighter & Name & Highlighter) + 5)
                value = ParseBetween(Html, "value=" & Highlighter, Highlighter, 7)
            Catch
            End Try
            Return value
        End Function
        Public Function ParseFormClassText(ByVal Html As String, ByVal ClassName As String, Optional ByVal Highlighter As String = """") As String
            If String.IsNullOrEmpty(Html) Then Return String.Empty
            Dim value As String = String.Empty
            Try
                Html = Html.Substring(Html.IndexOf("class=" & Highlighter & ClassName & Highlighter) + 7)
                value = ParseBetween(Html, "value=" & Highlighter, Highlighter, 7)
            Catch
            End Try
            Return value
        End Function

        Public Function TrimHtml(ByVal Data As String) As String
            Return IIf(String.IsNullOrEmpty(Data), String.Empty, Regex.Replace(Data, "<.*?>", ""))
        End Function

        Public Function TimeStamp() As String
            Return CInt(Now.Subtract(CDate("1.1.1970 00:00:00")).TotalSeconds).ToString
        End Function

        Public Function GetContentType(ByVal Path As String) As String
            Dim Result As String = "application/octet-stream"
            Select Case New FileInfo(Path).Extension.ToLower

                Case ".atom", ".xml"
                    Result = "application/atom+xml"
                Case ".json"
                    Result = "application/json"
                Case ".js"
                    Result = "application/javascript"
                Case ".ogg"
                    Result = "application/ogg"
                Case ".pdf"
                    Result = "application/pdf"
                Case ".ps"
                    Result = "application/postscript"
                Case ".woff"
                    Result = "application/x-woff"
                Case ".xhtml", ".xht", ".xml", ".html", ".htm"
                    Result = "application/xhtml+xml"
                Case ".dtd"
                    Result = "application/xml-dtd"
                Case ".zip"
                    Result = "application/zip"
                Case ".gz"
                    Result = "application/x-gzip"

                Case ".au", ".snd"
                    Result = "audio/basic"
                Case ".rmi", ".mid"
                    Result = "audio/mid"
                Case ".mp3"
                    Result = "audio/mpeg"
                Case ".aiff", ".aifc", ".aif"
                    Result = "audio/x-aiff"
                Case ".m3u"
                    Result = "audio/x-mpegurl"
                Case ".ra"
                    Result = "audio/x-pn-realaudio"
                Case ".ram"
                    Result = "audio/x-pn-realaudio"
                Case ".wav"
                    Result = "audio/x-wav"

                Case ".bmp"
                    Result = "image/bmp"
                Case ".cod"
                    Result = "image/cis-cod"
                Case ".gif"
                    Result = "image/gif"
                Case ".ief"
                    Result = "image/ief"
                Case ".jpe", ".jpeg", ".jpg"
                    Result = "image/jpeg"
                Case ".jfif"
                    Result = "image/pipeg"
                Case ".jpeg"
                    Result = "image/pjpeg"
                Case ".png"
                    Result = "image/png"
                Case ".svg"
                    Result = "image/svg+xml"
                Case ".tif", ".tiff"
                    Result = "image/tiff"
                Case ".ras"
                    Result = "image/x-cmu-raster"
                Case ".cmx"
                    Result = "image/x-cmx"
                Case ".ico"
                    Result = "image/x-icon"
                Case ".png"
                    Result = "image/x-png"
                Case ".pnm"
                    Result = "image/x-portable-anymap"
                Case ".pbm"
                    Result = "image/x-portable-bitmap"
                Case ".pgm"
                    Result = "image/x-portable-graymap"
                Case ".ppm"
                    Result = "image/x-portable-pixmap"
                Case ".rgb"
                    Result = "image/x-rgb"
                Case ".xbm"
                    Result = "image/x-xbitmap"
                Case ".xpm"
                    Result = "image/x-xpixmap"
                Case ".xwd"
                    Result = "image/x-xwindowdump"

                Case ".mp2"
                    Result = "video/mpeg"
                Case ".mpa"
                    Result = "video/mpeg"
                Case ".mpe"
                    Result = "video/mpeg"
                Case ".mpeg"
                    Result = "video/mpeg"
                Case ".mpg"
                    Result = "video/mpeg"
                Case ".mpv2"
                    Result = "video/mpeg"
                Case ".mov", ".qt"
                    Result = "video/quicktime"
                Case ".lsf", ".lsx"
                    Result = "video/x-la-asf"
                Case ".asf", ".asr", ".asx"
                    Result = "video/x-ms-asf"
                Case ".avi"
                    Result = "video/x-msvideo"
                Case ".movie"
                    Result = "video/x-sgi-movie"

                Case Else
                    Result = "application/octet-stream"
            End Select
            Return Result
        End Function

        Public Function CountOccurance(ByVal Data As String, ByVal Search As String, Optional ByVal CaseSensitive As Boolean = False) As Integer
            Return (IIf(CaseSensitive, (Data.Length - (Data.Replace(Search, "").Length)), (Data.Length - (Data.ToLower.Replace(Search.ToLower, "").Length))) / Search.Length)
        End Function

        Private Function AcceptAllCertifications(ByVal sender As Object, ByVal certification As System.Security.Cryptography.X509Certificates.X509Certificate, ByVal chain As System.Security.Cryptography.X509Certificates.X509Chain, ByVal sslPolicyErrors As System.Net.Security.SslPolicyErrors) As Boolean
            Return True
        End Function

        <Serializable()> Public Class HttpCookie
            Public Name As String = String.Empty
            Public Value As String = String.Empty
            Public Domain As String = String.Empty
            Public Path As String = String.Empty
            Public Expires As Date = Nothing
            Public HttpOnly As Boolean = False
            Public Secure As Boolean = False

            Public Sub New()
            End Sub
        End Class
        Public Class HttpResponse
            Public WebResponse As HttpWebResponse = Nothing
            Public Exception As Object = Nothing
            Public Html As String = String.Empty
            Public Headers As String = String.Empty
            'Public Image As Image = Nothing
        End Class
        Public Class HttpError
            Public Exception As Object = Nothing
            Public Message As String = String.Empty
            Public IsProxyError As Boolean = False
        End Class
    End Class
End Namespace