Imports System.Text
Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports System.Diagnostics

Namespace GameJolt

    ' ---------- helpers (OUTSIDE the class) ----------
    Public Module VbHelpers
        <Runtime.CompilerServices.Extension()>
        Public Function Truthy(ByVal s As String) As Boolean
            If s Is Nothing Then Return False
            s = s.Trim()
            Return String.Equals(s, "true", StringComparison.OrdinalIgnoreCase) _
                   OrElse s = "1" _
                   OrElse String.Equals(s, "yes", StringComparison.OrdinalIgnoreCase)
        End Function
    End Module
    ' -------------------------------------------------

    Public Class APICall

        Public Structure JoltParameter
            Dim Name As String
            Dim Value As String
        End Structure

        Public Enum RequestMethod
            [GET]
            POST
        End Enum

        ' ------------------------- API BASE -------------------------
        ' Tries to read Classified.Remote_Profile_URL (or RemoteProfileUrl). Falls back to localhost.
        Private Shared ReadOnly Property API_BASE As String
            Get
                Dim url As String = TryGetRemoteProfileUrl()
                If String.IsNullOrWhiteSpace(url) Then
                    Return "http://127.0.0.1:8080/api/game/v1_1"
                End If

                Dim s = url.TrimEnd("/"c)
                If s.EndsWith("/users", StringComparison.OrdinalIgnoreCase) Then
                    s = s.Substring(0, s.Length - "/users".Length)
                End If
                Return s.TrimEnd("/"c)
            End Get
        End Property

        Private Shared Function TryGetRemoteProfileUrl() As String
            Try
                Dim t = GetType(Classified)

                ' FIELD: Remote_Profile_URL
                Dim f = t.GetField("Remote_Profile_URL",
                    Reflection.BindingFlags.Public Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.IgnoreCase)
                If f IsNot Nothing Then
                    Dim v = TryCast(f.GetValue(Nothing), String)
                    If Not String.IsNullOrWhiteSpace(v) Then Return v
                End If

                ' PROPERTY: Remote_Profile_URL
                Dim p = t.GetProperty("Remote_Profile_URL",
                    Reflection.BindingFlags.Public Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.IgnoreCase)
                If p IsNot Nothing Then
                    Dim v = TryCast(p.GetValue(Nothing, Nothing), String)
                    If Not String.IsNullOrWhiteSpace(v) Then Return v
                End If

                ' FIELD: RemoteProfileUrl
                f = t.GetField("RemoteProfileUrl",
                    Reflection.BindingFlags.Public Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.IgnoreCase)
                If f IsNot Nothing Then
                    Dim v = TryCast(f.GetValue(Nothing), String)
                    If Not String.IsNullOrWhiteSpace(v) Then Return v
                End If

                ' PROPERTY: RemoteProfileUrl
                p = t.GetProperty("RemoteProfileUrl",
                    Reflection.BindingFlags.Public Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.IgnoreCase)
                If p IsNot Nothing Then
                    Dim v = TryCast(p.GetValue(Nothing, Nothing), String)
                    If Not String.IsNullOrWhiteSpace(v) Then Return v
                End If
            Catch
                ' ignore and fall back
            End Try

            Return Nothing
        End Function
        ' -----------------------------------------------------------

        ' ------------------------- APIURL -------------------------
        Private Class APIURL
            Private ReadOnly _pairs As New List(Of KeyValuePair(Of String, String))()
            Private _basePath As String = ""

            Public Sub New(ByVal baseURL As String)
                _basePath = If(String.IsNullOrWhiteSpace(baseURL), "/", baseURL)
                If Not _basePath.StartsWith("/") Then _basePath = "/" & _basePath
            End Sub

            Public Sub AddKeyValuePair(ByVal Key As String, ByVal Value As String)
                _pairs.Add(New KeyValuePair(Of String, String)(Key, Value))
            End Sub

            Public ReadOnly Property GetURL As String
                Get
                    Dim sb As New StringBuilder()
                    sb.Append(API_BASE).Append(_basePath)

                    If _pairs.Count > 0 Then
                        sb.Append("?"c)
                        For i = 0 To _pairs.Count - 1
                            If i > 0 Then sb.Append("&"c)
                            sb.Append(_pairs(i).Key).
                               Append("="c).
                               Append(UrlEncoder.Encode(_pairs(i).Value))
                        Next
                    End If

                    Return sb.ToString()
                End Get
            End Property
        End Class
        ' ----------------------------------------------------------

        Public Delegate Sub DelegateCallSub(ByVal result As String)
        Public CallSub As DelegateCallSub

        Private username As String
        Private token As String
        Private loggedIn As Boolean

        Private Shared ReadOnly CONST_GAMEID As String = Classified.GameJolt_Game_ID
        Private Shared ReadOnly CONST_GAMEKEY As String = Classified.GameJolt_Game_Key

        Private exThrown As System.Exception = Nothing

        Public Event CallFails(ByVal ex As Exception)
        Public Event CallSucceeded(ByVal returnData As String)

        ' --------- logging wrappers ----------
        Private Shared Sub TraceInfo(msg As String)
            Try : ClientTrace.LogInfo(msg) : Catch : Debug.WriteLine("[INFO] " & msg) : End Try
        End Sub
        Private Shared Sub TraceWarn(msg As String)
            Try : ClientTrace.LogWarn(msg) : Catch : Debug.WriteLine("[WARN] " & msg) : End Try
        End Sub
        Private Shared Sub TraceError(msg As String)
            Try : ClientTrace.LogError(msg) : Catch : Debug.WriteLine("[ERROR] " & msg) : End Try
        End Sub
        Private Shared Sub TraceDebug(msg As String)
            Try : ClientTrace.LogDebug(msg) : Catch : Debug.WriteLine("[DEBUG] " & msg) : End Try
        End Sub
        ' ------------------------------------

        Private ReadOnly Property GameID() As String
            Get
                Return StringObfuscation.DeObfuscate(CONST_GAMEID)
            End Get
        End Property

        Private ReadOnly Property GameKey() As String
            Get
                Return StringObfuscation.DeObfuscate(CONST_GAMEKEY)
            End Get
        End Property

        ' ===== token vs password mode =====
        Private Enum CredentialKind
            Token
            Password
        End Enum
        Private _credKind As CredentialKind = CredentialKind.Token

        Public Sub UseTokenAuth()
            _credKind = CredentialKind.Token
        End Sub

        Public Sub UsePasswordAuth()
            _credKind = CredentialKind.Password
        End Sub

        Private Function CredParamName() As String
            Return If(_credKind = CredentialKind.Password, "password", "user_token")
        End Function

        Private Sub AddAuth(u As APIURL, Optional requireLogin As Boolean = True)
            If requireLogin AndAlso Not loggedIn Then
                Throw New Exception("User not logged in!")
            End If
            u.AddKeyValuePair("username", If(username, String.Empty))
            u.AddKeyValuePair(CredParamName(), If(token, String.Empty))
        End Sub
        ' ===================================

        Public Sub New(ByVal CallSub As DelegateCallSub)
            Me.CallSub = CallSub
            Me.username = API.username
            Me.token = API.token
            Me.loggedIn = API.LoggedIn
        End Sub

        Public Sub New()
            Me.username = API.username
            Me.token = API.token
            Me.loggedIn = API.LoggedIn
        End Sub

        Public Sub SetCredentials(newUsername As String, newSecret As String, Optional isLoggedIn As Boolean = True)
            API.username = newUsername
            API.token = newSecret
            Me.username = newUsername
            Me.token = newSecret
            Me.loggedIn = isLoggedIn
        End Sub

        ' ------------------------- AUTH -------------------------
        Public Sub VerifyUser(ByVal newUsername As String, ByVal newSecret As String)
            SetCredentials(If(newUsername, "").Trim(), If(newSecret, "").Trim(), False)

            Dim url As New APIURL("/users/auth")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url, requireLogin:=False)

            TraceInfo($"LOGIN attempt user=""{username}"" mode={_credKind}")
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

#Region "Storage"

        Public Sub SetStorageData(ByVal key As String, ByVal data As String, ByVal useUsername As Boolean)
            If useUsername Then
                Dim url As New APIURL("/data-store/set")
                url.AddKeyValuePair("game_id", GameID)
                AddAuth(url)
                url.AddKeyValuePair("key", key)
                Initialize(url.GetURL(), RequestMethod.POST, data)
            Else
                Dim url As New APIURL("/data-store/set")
                url.AddKeyValuePair("game_id", GameID)
                url.AddKeyValuePair("key", key)
                Initialize(url.GetURL(), RequestMethod.POST, data)
            End If
        End Sub

        Public Sub UpdateStorageData(ByVal key As String, ByVal value As String, ByVal operation As String, ByVal useUsername As Boolean)
            If useUsername Then
                Dim url As New APIURL("/data-store/update")
                url.AddKeyValuePair("game_id", GameID)
                AddAuth(url)
                url.AddKeyValuePair("key", key)
                url.AddKeyValuePair("operation", operation)
                url.AddKeyValuePair("value", value)
                Initialize(url.GetURL(), RequestMethod.GET)
            Else
                Dim url As New APIURL("/data-store/update")
                url.AddKeyValuePair("game_id", GameID)
                url.AddKeyValuePair("key", key)
                url.AddKeyValuePair("operation", operation)
                url.AddKeyValuePair("value", value)
                Initialize(url.GetURL(), RequestMethod.GET)
            End If
        End Sub

        Public Sub SetStorageData(ByVal keys() As String, ByVal dataItems() As String, ByVal useUsernames() As Boolean)
            If keys.Length <> dataItems.Length OrElse keys.Length <> useUsernames.Length Then
                Dim ex As New Exception("The data arrays do not have the same lengths.")
                ex.Data.Add("Keys Length", keys.Length)
                ex.Data.Add("Data Length", dataItems.Length)
                ex.Data.Add("Username permission Length", useUsernames.Length)
                Throw ex
            End If

            Dim url As String = API_BASE & "/batch?game_id=" & GameID & "&parallel=true"
            Dim postDataURL As New StringBuilder()

            For i = 0 To keys.Length - 1
                Dim k As String = keys(i)
                Dim data As String = dataItems(i)
                Dim useUsername As Boolean = useUsernames(i)

                Dim authPart As String = ""
                If useUsername Then
                    authPart = "&username=" & UrlEncoder.Encode(username) &
                               "&" & CredParamName() & "=" & UrlEncoder.Encode(token)
                End If

                postDataURL.Append("&requests[]=").Append(UrlEncoder.Encode(
                    GetHashedURL("/data-store/set" &
                                 "?game_id=" & GameID &
                                 authPart &
                                 "&key=" & UrlEncoder.Encode(k) &
                                 "&data=" & UrlEncoder.Encode(data))))
            Next

            Initialize(url, RequestMethod.POST, postDataURL.ToString())
        End Sub

        Public Sub SetStorageDataRestricted(ByVal key As String, ByVal data As String)
            Dim url As String = API_BASE & "/data-store/set" &
                                "?game_id=" & GameID & "&key=" & UrlEncoder.Encode(key) &
                                "&restriction_username=" & UrlEncoder.Encode(API.username) &
                                "&restriction_user_token=" & UrlEncoder.Encode(API.token)
            Initialize(url, RequestMethod.POST, data)
        End Sub

        Public Sub GetStorageData(ByVal key As String, ByVal useUsername As Boolean)
            If useUsername Then
                Dim url As New APIURL("/data-store/get")
                url.AddKeyValuePair("game_id", GameID)
                AddAuth(url)
                url.AddKeyValuePair("key", key)
                Initialize(url.GetURL(), RequestMethod.GET)
            Else
                Dim url As New APIURL("/data-store/get")
                url.AddKeyValuePair("game_id", GameID)
                url.AddKeyValuePair("key", key)
                Initialize(url.GetURL(), RequestMethod.GET)
            End If
        End Sub

        Public Sub GetStorageData(ByVal keys() As String, ByVal useUsername As Boolean)
            Dim url As New StringBuilder(API_BASE & "/batch")
            Dim first As Boolean = True
            For Each k As String In keys
                Dim sep = If(first, "?", "&")
                Dim authPart As String = ""
                If useUsername Then
                    authPart = "&username=" & UrlEncoder.Encode(username) &
                               "&" & CredParamName() & "=" & UrlEncoder.Encode(token)
                End If
                url.Append(sep).Append("requests[]=").Append(UrlEncoder.Encode(
                    GetHashedURL("/data-store/get" &
                                 "?game_id=" & GameID &
                                 authPart &
                                 "&key=" & UrlEncoder.Encode(k))))
                first = False
            Next
            url.Append("&game_id=").Append(GameID)
            Initialize(url.ToString(), RequestMethod.GET)
        End Sub

        Public Sub FetchUserdata(ByVal username As String)
            Dim url As New APIURL("/users")
            url.AddKeyValuePair("game_id", GameID)
            url.AddKeyValuePair("username", username)
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

        Public Sub FetchUserdataByID(ByVal user_id As String)
            Dim url As New APIURL("/users")
            url.AddKeyValuePair("game_id", GameID)
            url.AddKeyValuePair("user_id", user_id)
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

        Public Sub GetKeys(ByVal useUsername As Boolean, ByVal pattern As String)
            Dim url As New APIURL("/data-store/get-keys")
            url.AddKeyValuePair("game_id", GameID)
            If useUsername Then AddAuth(url)
            If Not String.IsNullOrWhiteSpace(pattern) Then url.AddKeyValuePair("pattern", pattern)
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

        Public Sub RemoveKey(ByVal key As String, ByVal useUsername As Boolean)
            If String.IsNullOrWhiteSpace(key) Then Throw New Exception("Key is required")

            Dim url As New APIURL("/data-store/remove")
            url.AddKeyValuePair("game_id", GameID)
            If useUsername Then AddAuth(url)
            url.AddKeyValuePair("key", key)
            Initialize(url.GetURL(), RequestMethod.POST)
        End Sub

#End Region

#Region "Sessions"

        ' Replace in APICall.OpenSession()
        Private Shared _sessionIsOpen As Boolean = False
        Private Shared _lastOpenTick As Integer = 0

        Public Sub OpenSession()
            ' Use Environment.TickCount instead of TickCount64 for older .NET
            Dim nowTick As Integer = Environment.TickCount
            If _sessionIsOpen AndAlso Math.Abs(nowTick - _lastOpenTick) < 2000 Then
                TraceDebug("OpenSession() suppressed (already open).")
                Return
            End If

            Dim url As New APIURL("/sessions/open")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url)
            Initialize(url.GetURL(), RequestMethod.GET)

            _sessionIsOpen = True
            _lastOpenTick = nowTick
        End Sub

        ' Keep sessions alive without relying on /sessions/ping
        Public Sub CheckSession()
            ' /sessions/open is idempotent on most servers; use it as a keepalive
            OpenSession()
        End Sub

        Public Sub PingSession()
            Dim url As New APIURL("/sessions/ping")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url)
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

        Public Sub CloseSession()
            Dim url As New APIURL("/sessions/close")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url)
            Initialize(url.GetURL(), RequestMethod.GET)
            _sessionIsOpen = False
        End Sub

#End Region

#Region "Trophy"

        Public Sub FetchAllTrophies()
            Dim url As New APIURL("/trophies")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url)
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

        Public Sub FetchAllAchievedTrophies()
            Dim url As New APIURL("/trophies")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url)
            url.AddKeyValuePair("achieved", "true")
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

        Public Sub FetchTrophy(ByVal trophy_id As Integer)
            Dim url As New APIURL("/trophies")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url)
            url.AddKeyValuePair("trophy_id", trophy_id.ToString())
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

        Public Sub TrophyAchieved(ByVal trophy_id As Integer)
            Dim url As New APIURL("/trophies/add-achieved")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url)
            url.AddKeyValuePair("trophy_id", trophy_id.ToString())
            Initialize(url.GetURL(), RequestMethod.POST)
        End Sub

        Public Sub RemoveTrophyAchieved(ByVal trophy_id As Integer)
            Dim url As New APIURL("/trophies/remove-achieved")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url)
            url.AddKeyValuePair("trophy_id", trophy_id.ToString())
            Initialize(url.GetURL(), RequestMethod.POST)
        End Sub

#End Region

#Region "ScoreTable"

        Public Sub FetchTable(ByVal score_count As Integer, ByVal table_id As String)
            Dim url As New APIURL("/scores")
            url.AddKeyValuePair("game_id", GameID)
            url.AddKeyValuePair("limit", score_count.ToString())
            url.AddKeyValuePair("table_id", table_id)
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

        Public Sub FetchUserRank(ByVal table_id As String, ByVal sort As Integer)
            Dim url As New APIURL("/scores/get-rank")
            url.AddKeyValuePair("game_id", GameID)
            url.AddKeyValuePair("sort", sort.ToString())
            url.AddKeyValuePair("table_id", table_id)
            Initialize(url.GetURL(), RequestMethod.GET)
        End Sub

        Public Sub AddScore(ByVal score As String, ByVal sort As Integer, ByVal table_id As String)
            Dim url As New APIURL("/scores/add")
            url.AddKeyValuePair("game_id", GameID)
            AddAuth(url)
            url.AddKeyValuePair("score", score)
            url.AddKeyValuePair("sort", sort.ToString())
            url.AddKeyValuePair("table_id", table_id)
            Initialize(url.GetURL(), RequestMethod.POST)
        End Sub

#End Region

#Region "Friends"

        Public Sub FetchFriendList()
            Try
                Dim url As New APIURL("/friends")
                url.AddKeyValuePair("game_id", GameID)
                AddAuth(url)
                TraceInfo("Friends: GET " & url.GetURL())
                Initialize(url.GetURL(), RequestMethod.GET)
            Catch ex As Exception
                TraceError("FetchFriendList(): " & ex.Message)
            End Try
        End Sub

        Public Sub FetchFriendList(ByVal user_id As String)
            Try
                Dim url As New APIURL("/friends")
                url.AddKeyValuePair("game_id", GameID)
                AddAuth(url)
                url.AddKeyValuePair("user_id", user_id)
                TraceInfo("Friends: GET " & url.GetURL())
                Initialize(url.GetURL(), RequestMethod.GET)
            Catch ex As Exception
                TraceError("FetchFriendList(user_id): " & ex.Message)
            End Try
        End Sub

        Public Sub AddFriend(ByVal friend_username As String)
            Try
                Dim url As New APIURL("/friends/add")
                url.AddKeyValuePair("game_id", GameID)
                AddAuth(url)
                url.AddKeyValuePair("friend_username", friend_username)
                TraceInfo("Friends: POST " & url.GetURL())
                Initialize(url.GetURL(), RequestMethod.POST)
            Catch ex As Exception
                TraceError("AddFriend(): " & ex.Message)
            End Try
        End Sub

        Public Sub RemoveFriend(ByVal friend_username As String)
            Try
                Dim url As New APIURL("/friends/remove")
                url.AddKeyValuePair("game_id", GameID)
                AddAuth(url)
                url.AddKeyValuePair("friend_username", friend_username)
                TraceInfo("Friends: POST " & url.GetURL())
                Initialize(url.GetURL(), RequestMethod.POST)
            Catch ex As Exception
                TraceError("RemoveFriend(): " & ex.Message)
            End Try
        End Sub

#End Region

        ' --- fields for current request ---
        Private url As String = ""
        Private PostData As String = ""

        Private Function GetHashedURL(ByVal pathOrUrl As String) As String
            Dim full As String
            If pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then
                full = pathOrUrl
            Else
                If Not pathOrUrl.StartsWith("/") Then pathOrUrl = "/" & pathOrUrl
                full = API_BASE & pathOrUrl
            End If

            Using m As MD5 = MD5.Create()
                Dim data() As Byte = m.ComputeHash(Encoding.UTF8.GetBytes(full & GameKey))
                Dim sb As New StringBuilder(data.Length * 2)
                For i = 0 To data.Length - 1
                    sb.Append(data(i).ToString("x2"))
                Next
                Return full & If(full.Contains("?"), "&", "?") & "signature=" & sb.ToString()
            End Using
        End Function

        Private Sub Initialize(ByVal url As String, ByVal method As RequestMethod, Optional ByVal PostData As String = "")
            exThrown = Nothing

            Dim formatted As String = If(url.Contains("?"), url & "&format=keypair", url & "?format=keypair")
            Dim newurl As String = GetHashedURL(formatted)

            TraceDebug("INIT " & method.ToString() & " " & newurl)

            If method = RequestMethod.POST Then
                Me.url = newurl
                Me.PostData = PostData

                Dim t As New Threading.Thread(AddressOf POSTRequst)
                t.IsBackground = True
                t.Start()
            Else
                Try
                    Dim request As HttpWebRequest = CType(WebRequest.Create(newurl), HttpWebRequest)
                    request.Method = "GET"
                    request.KeepAlive = False
                    request.Proxy = Nothing
                    request.ServicePoint.Expect100Continue = False
                    request.Timeout = 8000
                    request.ReadWriteTimeout = 8000
                    request.UserAgent = "P3D-Client/0.59.3"

                    TraceInfo($"API REQUEST -> GET {newurl}")
                    request.BeginGetResponse(AddressOf EndResult, request)
                Catch ex As Exception
                    API.APICallCount -= 1
                    TraceError($"API EXCEPTION (Begin GET) {ex.GetType().Name}: {ex.Message} url={newurl}")
                    RaiseEvent CallFails(ex)
                End Try
            End If

            API.APICallCount += 1
        End Sub

        Private Sub POSTRequst()
            Dim gotData As String = ""
            Dim gotDataSuccess As Boolean = False

            Try
                Dim request As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                request.Method = "POST"
                request.KeepAlive = False
                request.Proxy = Nothing
                request.ServicePoint.Expect100Continue = False
                request.AllowWriteStreamBuffering = True
                request.Timeout = 8000
                request.ReadWriteTimeout = 8000
                request.UserAgent = "P3D-Client/0.59.3"

                Dim bodyBytes() As Byte
                If String.IsNullOrEmpty(PostData) Then
                    bodyBytes = Array.Empty(Of Byte)()
                    request.ContentType = "application/x-www-form-urlencoded"
                Else
                    Dim body As String = "data=" & UrlEncoder.Encode(PostData)
                    bodyBytes = Encoding.UTF8.GetBytes(body)
                    request.ContentType = "application/x-www-form-urlencoded"
                End If

                request.ContentLength = bodyBytes.Length
                TraceInfo($"API REQUEST -> POST {url} bodyLen={bodyBytes.Length}")

                If bodyBytes.Length > 0 Then
                    Using reqStream = request.GetRequestStream()
                        reqStream.Write(bodyBytes, 0, bodyBytes.Length)
                    End Using
                End If

                Using response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
                    Using rs = response.GetResponseStream()
                        Using sr As New StreamReader(rs, Encoding.UTF8)
                            gotData = sr.ReadToEnd()
                        End Using
                    End Using
                    TraceInfo($"API RESPONSE <- {CInt(response.StatusCode)} len={gotData.Length} url={url}")
                End Using

                gotDataSuccess = True
            Catch ex As WebException
                Try
                    Dim statusCode As String = "?"
                    Dim payload As String = ""
                    If ex.Response IsNot Nothing Then
                        Dim resp = CType(ex.Response, HttpWebResponse)
                        statusCode = CInt(resp.StatusCode).ToString()
                        Using rs = resp.GetResponseStream()
                            Using sr As New StreamReader(rs, Encoding.UTF8)
                                payload = sr.ReadToEnd()
                            End Using
                        End Using
                    End If
                    TraceError($"API EXCEPTION (POST) WebException status={statusCode} url={url} payloadLen={payload.Length}")
                    RaiseEvent CallFails(New Exception($"HTTP {statusCode}: {payload}"))
                Catch
                    TraceError($"API EXCEPTION (POST) {ex.GetType().Name}: {ex.Message} url={url}")
                    RaiseEvent CallFails(ex)
                End Try
            Catch ex As Exception
                TraceError($"API EXCEPTION (POST) {ex.GetType().Name}: {ex.Message} url={url}")
                RaiseEvent CallFails(ex)
            Finally
                API.APICallCount -= 1
            End Try

            If gotDataSuccess Then
                If CallSub IsNot Nothing Then
                    CallSub(gotData)
                    RaiseEvent CallSucceeded(gotData)
                End If
            End If
        End Sub

        Private Sub EndResult(ByVal result As IAsyncResult)
            Dim data As String = ""

            Try
                If result.IsCompleted Then
                    Dim request As HttpWebRequest = CType(result.AsyncState, HttpWebRequest)
                    Using response As HttpWebResponse = CType(request.EndGetResponse(result), HttpWebResponse)
                        Using rs = response.GetResponseStream()
                            Using sr As New StreamReader(rs, Encoding.UTF8)
                                data = sr.ReadToEnd()
                                TraceDebug("BODY (first 300): " & If(data, "").Substring(0, Math.Min(300, If(data, "").Length)))
                            End Using
                        End Using
                        TraceInfo($"API RESPONSE <- {CInt(response.StatusCode)} len={data.Length} url={request.Address}")
                    End Using
                End If
            Catch ex As WebException
                Try
                    Dim statusCode As String = "?"
                    Dim payload As String = ""
                    If ex.Response IsNot Nothing Then
                        Dim resp = CType(ex.Response, HttpWebResponse)
                        statusCode = CInt(resp.StatusCode).ToString()
                        Using rs = resp.GetResponseStream()
                            Using sr As New StreamReader(rs, Encoding.UTF8)
                                payload = sr.ReadToEnd()
                            End Using
                        End Using
                    End If
                    Dim req = TryCast(result.AsyncState, HttpWebRequest)
                    TraceError($"API EXCEPTION (GET) WebException status={statusCode} url={If(req IsNot Nothing, req.Address.ToString(), "?")} payloadLen={payload.Length}")
                    RaiseEvent CallFails(New Exception($"HTTP {statusCode}: {payload}"))
                Catch
                    TraceError($"API EXCEPTION (GET) {ex.GetType().Name}: {ex.Message}")
                    RaiseEvent CallFails(ex)
                End Try
            Catch ex As Exception
                TraceError($"API EXCEPTION (GET) {ex.GetType().Name}: {ex.Message}")
                RaiseEvent CallFails(ex)
            Finally
                API.APICallCount -= 1
            End Try

            If data <> "" AndAlso CallSub IsNot Nothing Then
                RaiseEvent CallSucceeded(data)
                CallSub(data)
            End If
        End Sub

        ' ----------------- Minimal URL encoder fallback -----------------
        Private NotInheritable Class UrlEncoder
            Private Sub New()
            End Sub
            Public Shared Function Encode(s As String) As String
                Return Uri.EscapeDataString(If(s, ""))
            End Function
        End Class

    End Class

End Namespace