Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework
Imports System.Runtime.InteropServices

Namespace GameJolt

    Public Class LogInScreen
        Inherits Screen

        Public Shared BanList As String = ""
        Public Shared BanReasons As String = ""

        Public Shared LoadedGameJoltID As String = ""

        Dim UserName As JoltTextBox
        Dim Token As JoltTextBox

        Dim LogInButton As JoltButton
        Dim CloseButton As JoltButton
        Dim CreateAccountButton As JoltButton
        Dim ManageTokenButton As JoltButton
        Dim OkButton As JoltButton

        ' Bypass button (opens JoinServerScreen)
        Dim BypassButton As JoltButton

        Dim WaitingForResponse As Boolean = False
        Dim WaitingMessage As String = "Please wait..."
        Dim ShowokButton As Boolean = True
        Dim TimeOut As Integer = 0
        Const TimeOutVar As Integer = 500

        Dim DownloadedBanList As Boolean = False

        Dim _tempCloseScreen As Boolean = False ' prevent closing from non-main threads

        Public Sub New(ByVal currentScreen As Screen)
            Me.PreScreen = currentScreen

            Me.Identification = Identifications.GameJoltLoginScreen
            Me.MouseVisible = True
            Me.CanBePaused = False
            Me.CanChat = False
            Me.CanMuteAudio = False

            Me.UserName = New JoltTextBox(FontManager.MainFont, Color.Black, Color.White)
            UserName.Size = New Size(400, 30)

            Me.Token = New JoltTextBox(FontManager.MainFont, Color.Black, Color.White)
            Token.Size = New Size(400, 30)
            Token.IsPassword = True

            Me.LogInButton = New JoltButton("Log in", FontManager.MainFont, New Color(68, 68, 68), New Color(204, 255, 0))
            LogInButton.Size = New Size(90, 30)
            LogInButton.SetDelegate(AddressOf LogIn)

            Me.CloseButton = New JoltButton("Close", FontManager.MainFont, New Color(68, 68, 68), New Color(204, 255, 0))
            CloseButton.Size = New Size(90, 30)
            CloseButton.SetDelegate(AddressOf Me.Close)

            Me.CreateAccountButton = New JoltButton("Create Account", FontManager.MainFont, New Color(68, 68, 68), New Color(204, 255, 0))
            CreateAccountButton.Size = New Size(200, 30)
            CreateAccountButton.SetDelegate(AddressOf Me.CreateAccount)

            Me.ManageTokenButton = New JoltButton("Manage Token", FontManager.MainFont, New Color(68, 68, 68), New Color(204, 255, 0))
            ManageTokenButton.Size = New Size(200, 30)
            ManageTokenButton.SetDelegate(AddressOf OpenTokenPage)

            Me.OkButton = New JoltButton("OK", FontManager.MainFont, New Color(68, 68, 68), New Color(204, 255, 0))
            OkButton.Size = New Size(100, 30)
            OkButton.SetDelegate(AddressOf Me.PressOK)

            ' Bypass button (bottom-right)
            Me.BypassButton = New JoltButton("Bypass", FontManager.MainFont, New Color(68, 68, 68), New Color(204, 255, 0))
            BypassButton.Size = New Size(120, 30)
            BypassButton.SetDelegate(AddressOf OpenBypass)

            Me.UpdatePosition()

            UserName.IsActive = True

            If GameJolt.API.LoggedIn = True Then
                UserName.Text = GameJolt.API.username
                Token.Text = GameJolt.API.token

                LogInButton.Text = "Log out"
                Logger.Log(Logger.LogTypes.Info, "[GameJolt] Pre-filled credentials from active session.")
            Else
                LoadSettings()
            End If

            Dim t As New Threading.Thread(AddressOf DownloadBanList)
            t.IsBackground = True
            t.Start()

            Logger.Log(Logger.LogTypes.Info, "[GameJolt] Login screen initialized.")
        End Sub

        Private Sub SaveSettings()
            If API.LoggedIn = True Then
                Dim cUsername As String = Encryption.EncryptString(UserName.Text)
                Dim cToken As String = Encryption.EncryptString(Token.Text)
                System.IO.File.WriteAllText(GameController.GamePath & "\Save\gamejoltAcc.dat", cUsername & Environment.NewLine & cToken)
                Logger.Log(Logger.LogTypes.Info, "[GameJolt] Saved encrypted GameJolt credentials to disk.")
            End If
        End Sub

        Private Sub LoadSettings()
            If System.IO.File.Exists(GameController.GamePath & "\Save\gamejoltAcc.dat") = True Then
                Dim content() As String = System.IO.File.ReadAllLines(GameController.GamePath & "\Save\gamejoltAcc.dat")

                If content.Length >= 2 Then
                    Try
                        Me.UserName.Text = Encryption.DecryptString(content(0))
                        Me.Token.Text = Encryption.DecryptString(content(1))

                        Deactivate()
                        Me.LogInButton.IsActive = True
                        Logger.Log(Logger.LogTypes.Info, "[GameJolt] Loaded stored credentials (decrypted).")
                    Catch ex As Exception
                        System.IO.File.Delete(GameController.GamePath & "\Save\gamejoltAcc.dat")
                        Logger.Log(Logger.LogTypes.Warning, "Cannot read GameJolt account settings! " & ex.Message)
                    End Try
                End If
            End If
        End Sub

        Public Overrides Sub Draw()
            Me.PreScreen.Draw()

            Canvas.DrawRectangle(Core.ScreenSize, New Color(0, 0, 0, 150), True)
            Canvas.DrawRectangle(New Rectangle(CInt(Core.ScreenSize.Width / 2 - 310), 90, 620, 420), New Color(16, 16, 16), True)
            Canvas.DrawRectangle(New Rectangle(CInt(Core.ScreenSize.Width / 2 - 300), 100, 600, 400), New Color(39, 39, 39), True)

            If DownloadedBanList = True Then
                Core.SpriteBatch.DrawInterfaceString(FontManager.InGameFont, "Sign in with", New Vector2(CSng(Core.ScreenSize.Width / 2 - 280), 130), Color.White)
                Core.SpriteBatch.DrawInterface(TextureManager.LoadDirect("GUI\Logos\GameJolt.png"), New Rectangle(CInt(Core.ScreenSize.Width / 2 - 120), 130, 328, 36), Color.White)

                If WaitingForResponse = True Then
                    Dim textSize As Vector2 = FontManager.MainFont.MeasureString(WaitingMessage)
                    Core.SpriteBatch.DrawInterfaceString(FontManager.MainFont, WaitingMessage, New Vector2(CSng(Core.ScreenSize.Width / 2 - textSize.X / 2), 310 - textSize.Y / 2), Color.White)

                    If ShowokButton = True Then
                        OkButton.Draw()
                    End If
                Else
                    Core.SpriteBatch.DrawInterfaceString(FontManager.MiniFont, "Username:", New Vector2(CSng(Core.ScreenSize.Width / 2) - 200, 195), Color.White)
                    Core.SpriteBatch.DrawInterfaceString(FontManager.MiniFont, "Token:", New Vector2(CSng(Core.ScreenSize.Width / 2) - 200, 275), Color.White)

                    Me.UserName.Draw()
                    Me.Token.Draw()

                    Me.ManageTokenButton.Draw()
                    Me.LogInButton.Draw()
                    Me.CloseButton.Draw()
                    Me.CreateAccountButton.Draw()

                    Me.BypassButton.Draw()
                End If
            Else
                Core.SpriteBatch.DrawInterfaceString(FontManager.MiniFont, "Please wait" & LoadingDots.Dots, New Vector2(CSng(Core.ScreenSize.Width / 2) - 200, 195), Color.White)
            End If
        End Sub

        Public Overrides Sub Update()
            If _tempCloseScreen = True Then
                Core.SetScreen(Me.PreScreen)
                Exit Sub
            End If

            If Me.DownloadedBanList = True Then
                Me.UpdatePosition()

                If WaitingForResponse = True Then
                    If ShowokButton = True Then
                        If Controls.Accept(True, False) = True Then
                            Select Case True
                                Case Core.ScaleScreenRec(OkButton.GetRectangle()).Contains(MouseHandler.MousePosition)
                                    If OkButton.IsActive = True Then
                                        SoundManager.PlaySound("select")
                                        OkButton.DoPress()
                                    Else
                                        Deactivate()
                                        OkButton.IsActive = True
                                    End If
                            End Select
                        End If
                    Else
                        TimeOut -= 1
                        If TimeOut = 100 Then
                            Logger.Log(Logger.LogTypes.Debug, "[GameJolt] Login still waiting... (timeout ticking)")
                        End If
                        If TimeOut <= 0 Then
                            ShowokButton = True
                            WaitingMessage = "Error: Server timeout."
                            Logger.Log(Logger.LogTypes.ErrorMessage, "[GameJolt] Login timed out waiting for server response.")
                        End If
                        If Not API.Exception Is Nothing Then
                            ShowokButton = True
                            WaitingMessage = "Error: " & API.Exception.Message
                            Logger.Log(Logger.LogTypes.ErrorMessage, "[GameJolt] API exception during login: " & API.Exception.Message)
                        End If
                    End If
                Else
                    Me.PressTab()

                    Me.UserName.Update()
                    Me.Token.Update()

                    LogInButton.Update()
                    CloseButton.Update()
                    CreateAccountButton.Update()
                    ManageTokenButton.Update()
                    BypassButton.Update()

                    If Controls.Accept(True, False, False) = True Then
                        Select Case True
                            Case Core.ScaleScreenRec(UserName.GetRectangle()).Contains(MouseHandler.MousePosition)
                                Deactivate()
                                UserName.IsActive = True
                            Case Core.ScaleScreenRec(Token.GetRectangle()).Contains(MouseHandler.MousePosition)
                                Deactivate()
                                Token.IsActive = True
                            Case Core.ScaleScreenRec(ManageTokenButton.GetRectangle()).Contains(MouseHandler.MousePosition)
                                If ManageTokenButton.IsActive = True Then
                                    SoundManager.PlaySound("select")
                                    ManageTokenButton.DoPress()
                                Else
                                    Deactivate()
                                    ManageTokenButton.IsActive = True
                                End If
                            Case Core.ScaleScreenRec(LogInButton.GetRectangle()).Contains(MouseHandler.MousePosition)
                                If LogInButton.IsActive = True Then
                                    SoundManager.PlaySound("select")
                                    LogInButton.DoPress()
                                Else
                                    Deactivate()
                                    LogInButton.IsActive = True
                                End If
                            Case Core.ScaleScreenRec(CloseButton.GetRectangle()).Contains(MouseHandler.MousePosition)
                                If CloseButton.IsActive = True Then
                                    SoundManager.PlaySound("select")
                                    CloseButton.DoPress()
                                Else
                                    Deactivate()
                                    CloseButton.IsActive = True
                                End If
                            Case Core.ScaleScreenRec(CreateAccountButton.GetRectangle()).Contains(MouseHandler.MousePosition)
                                If CreateAccountButton.IsActive = True Then
                                    SoundManager.PlaySound("select")
                                    CreateAccountButton.DoPress()
                                Else
                                    Deactivate()
                                    CreateAccountButton.IsActive = True
                                End If
                            Case Core.ScaleScreenRec(BypassButton.GetRectangle()).Contains(MouseHandler.MousePosition)
                                If BypassButton.IsActive = True Then
                                    SoundManager.PlaySound("select")
                                    BypassButton.DoPress()
                                Else
                                    Deactivate()
                                    BypassButton.IsActive = True
                                End If
                        End Select
                    End If

                    If Controls.Accept(False, False, True) = True Or KeyBoardHandler.KeyPressed(KeyBindings.EnterKey1) = True Then
                        Select Case True
                            Case UserName.IsActive
                                SoundManager.PlaySound("select")
                                UserName.DoPress()
                            Case Token.IsActive
                                SoundManager.PlaySound("select")
                                Token.DoPress()
                            Case ManageTokenButton.IsActive
                                SoundManager.PlaySound("select")
                                ManageTokenButton.DoPress()
                            Case LogInButton.IsActive
                                SoundManager.PlaySound("select")
                                LogInButton.DoPress()
                            Case CloseButton.IsActive
                                SoundManager.PlaySound("select")
                                CloseButton.DoPress()
                            Case CreateAccountButton.IsActive
                                SoundManager.PlaySound("select")
                                CreateAccountButton.DoPress()
                            Case BypassButton.IsActive
                                SoundManager.PlaySound("select")
                                BypassButton.DoPress()
                        End Select
                    End If

                    If Controls.Dismiss(True, False, True) = True Then
                        Core.SetScreen(Me.PreScreen)
                    End If
                End If
            Else
                If Controls.Dismiss() = True Then
                    Core.SetScreen(Me.PreScreen)
                End If
            End If
        End Sub

        Private Sub PressTab()
            Dim direction As Integer = 0

            If KeyBoardHandler.KeyPressed(Keys.Tab) = True Or Controls.Down(True, True, True, False, True, True) = True Then
                direction = 1
            End If
            If Controls.Up(True, True, True, False, True, True) = True Then
                direction = -1
            End If

            If direction <> 0 Then
                Dim l As JoltControl() = {UserName, Token, ManageTokenButton, LogInButton, CreateAccountButton, CloseButton, BypassButton}

                For i = 0 To l.Length - 1
                    If l(i).IsActive = True Then
                        Deactivate()
                        Dim activateIndex As Integer = i + direction
                        If activateIndex > l.Length - 1 Then
                            activateIndex = 0
                        End If
                        If activateIndex < 0 Then
                            activateIndex = l.Length - 1
                        End If
                        l(activateIndex).IsActive = True
                        Exit For
                    End If
                Next
            End If
        End Sub

        Private Sub UpdatePosition()
            UserName.Position = New Vector2(CSng(Core.ScreenSize.Width / 2) - CSng(UserName.Size.Width / 2), 220)
            Token.Position = New Vector2(CSng(Core.ScreenSize.Width / 2) - CSng(Token.Size.Width / 2), 300)

            ManageTokenButton.Position = New Vector2(CSng(Core.ScreenSize.Width / 2) - CSng(ManageTokenButton.Size.Width / 2), 340)

            LogInButton.Position = New Vector2(CSng(Core.ScreenSize.Width / 2) - CSng(LogInButton.Size.Width / 2) - 150, 380)
            CreateAccountButton.Position = New Vector2(CSng(Core.ScreenSize.Width / 2) - CSng(CreateAccountButton.Size.Width / 2), 380)
            CloseButton.Position = New Vector2(CSng(Core.ScreenSize.Width / 2) - CSng(CloseButton.Size.Width / 2) + 150, 380)

            OkButton.Position = New Vector2(CSng(Core.ScreenSize.Width / 2) - CSng(OkButton.Size.Width / 2), 400)

            Dim innerRight As Single = CSng(Core.ScreenSize.Width / 2) + 300
            Dim margin As Single = 20.0F
            Dim bypassX As Single = innerRight - margin - BypassButton.Size.Width
            Dim bypassY As Single = 500 - margin - BypassButton.Size.Height
            BypassButton.Position = New Vector2(bypassX, bypassY)
        End Sub

        Private Sub Deactivate()
            UserName.IsActive = False
            Token.IsActive = False
            LogInButton.IsActive = False
            CloseButton.IsActive = False
            CreateAccountButton.IsActive = False
            ManageTokenButton.IsActive = False
            BypassButton.IsActive = False
            OkButton.IsActive = False
        End Sub

        Private Sub Close()
            SaveSettings()
            Core.SetScreen(Me.PreScreen)
        End Sub

        Private Sub CreateAccount()
            Try
                Process.Start(Classified.RegisterUrl)
                Logger.Log(Logger.LogTypes.Info, "[GameJolt] Opening registration page in browser.")
            Catch ex As Exception
                Logger.Log(Logger.LogTypes.ErrorMessage, "[GameJolt] Failed to open registration URL: " & ex.Message)
            End Try
        End Sub

        Private Sub OpenTokenPage()
            Try
                Process.Start(Classified.TokenManageUrl)
                Logger.Log(Logger.LogTypes.Info, "[GameJolt] Opening token management page.")
            Catch ex As Exception
                Logger.Log(Logger.LogTypes.ErrorMessage, "[GameJolt] Failed to open token page: " & ex.Message)
            End Try
        End Sub

        ' open the join server screen (bypass)
        Private Sub OpenBypass()
            Try
                SoundManager.PlaySound("select")
            Catch
            End Try
            Core.SetScreen(New JoinServerScreen(Me))
        End Sub

        Private Sub LogIn()
            If GameJolt.API.LoggedIn = True Then
                GameJolt.SessionManager.Close()

                GameJolt.API.LoggedIn = False
                GameJolt.API.username = ""
                GameJolt.API.token = ""

                UserName.Text = ""
                Token.Text = ""

                LogInButton.Text = "Log in"

                If System.IO.File.Exists(GameController.GamePath & "\Save\gamejoltAcc.dat") = True Then
                    System.IO.File.Delete(GameController.GamePath & "\Save\gamejoltAcc.dat")
                End If

                Logger.Log(Logger.LogTypes.Info, "[GameJolt] Logged out and cleared stored credentials.")
            Else
                Dim api As New APICall(AddressOf VerifyVersion)
                AddHandler api.CallFails, AddressOf VerifyVersionFail
                api.GetStorageData("ONLINEVERSION", False)

                WaitingMessage = "Please wait..."
                WaitingForResponse = True
                ShowokButton = False
                TimeOut = TimeOutVar

                Logger.Log(Logger.LogTypes.Info, $"[GameJolt] Login attempt for user ""{UserName.Text}"".")
            End If
        End Sub

        Private Sub VerifyVersionFail(ByVal ex As Exception)
            Logger.Log(Logger.LogTypes.Message, "[GameJolt] ONLINEVERSION missing; continuing login.")
            Dim api As New APICall(AddressOf VerifyResult)
            AddHandler api.CallFails, AddressOf AuthFail
            api.VerifyUser(UserName.Text, Token.Text)
        End Sub

        Private Sub AuthFail(ByVal ex As Exception)
            WaitingForResponse = True
            ShowokButton = True
            WaitingMessage = "Login failed: " & ex.Message
            GameJolt.API.LoggedIn = False
            LogInButton.Text = "Log in"
            Logger.Log(Logger.LogTypes.ErrorMessage, "[GameJolt] Auth HTTP error: " & ex.Message)
        End Sub

        Private Sub VerifyVersion(ByVal result As String)
            Dim list As List(Of GameJolt.API.JoltValue) = GameJolt.API.HandleData(result)

            Dim ok As Boolean = False
            If list IsNot Nothing AndAlso list.Count > 0 Then
                Boolean.TryParse(list(0).Value, ok)
            End If

            If Not ok Then
                Dim api As New APICall(AddressOf VerifyResult)
                AddHandler api.CallFails, AddressOf AuthFail
                api.VerifyUser(UserName.Text, Token.Text)
                Exit Sub
            End If

            If Version.Parse(list(1).Value) <= Version.Parse(GameController.GAMEVERSION) _
               OrElse GameController.IS_DEBUG_ACTIVE = True Then

                Dim api As New APICall(AddressOf VerifyResult)
                AddHandler api.CallFails, AddressOf AuthFail
                api.VerifyUser(UserName.Text, Token.Text)
            Else
                WaitingForResponse = True
                GameJolt.API.LoggedIn = False
                WaitingMessage = "The version of your game does not match with" & Environment.NewLine &
                                 "the version required to play online. If you have" & Environment.NewLine &
                                 "the lastest version of the game, the game is" & Environment.NewLine &
                                 "getting updated right now." & Environment.NewLine & Environment.NewLine & Environment.NewLine &
                                 "Your version: " & GameController.GAMEVERSION & Environment.NewLine &
                                 "Required version: " & list(1).Value
                ShowokButton = True
                LogInButton.Text = "Log in"
            End If
        End Sub

        ' ---------- helpers ----------
        Private Function GetField(ByVal items As List(Of GameJolt.API.JoltValue), ByVal name As String) As String
            If items Is Nothing Then Return Nothing
            For Each it In items
                If String.Equals(it.Name, name, StringComparison.OrdinalIgnoreCase) Then
                    Return it.Value
                End If
            Next
            Return Nothing
        End Function

        Private Shared Function Truthy(ByVal value As String) As Boolean
            If value Is Nothing Then Return False
            Dim v As String = value.Trim().ToLowerInvariant()
            Return v = "true" OrElse v = "1" OrElse v = "yes" OrElse v = "ok" OrElse v = "success"
        End Function

        Private Function TruthyKey(ByVal items As List(Of GameJolt.API.JoltValue), ByVal key As String) As Boolean
            Return Truthy(GetField(items, key))
        End Function
        ' --------------------------------

        ' --- Safe hooks into Player without hard compile-time fields ---
        Private Sub ApplyPlayerGJMeta()
            ' Write username/id to the player object only if those properties exist.
            TrySetPlayerProperty("GameJoltName", GameJolt.API.username)
            TrySetPlayerProperty("GameJoltID", LoadedGameJoltID)
        End Sub

        Private Sub TrySetPlayerProperty(propName As String, value As Object)
            If Core.Player Is Nothing Then Exit Sub
            Try
                Dim t = Core.Player.GetType()
                Dim p = t.GetProperty(propName, Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.IgnoreCase)
                If p IsNot Nothing AndAlso p.CanWrite Then
                    p.SetValue(Core.Player, value, Nothing)
                End If
            Catch
                ' ignore if not present
            End Try
        End Sub

        Private Sub EnsureOnlineSave()
            ' Mark the current save as a GameJolt/online save so servers won't reject it
            If GameJolt.API.LoggedIn AndAlso Core.Player IsNot Nothing Then
                Try
                    Core.Player.IsGameJoltSave = True
                Catch
                    ' property not present in this build ? ignore
                End Try
            End If
        End Sub

        Private Sub VerifyResult(ByVal result As String)
            Try
                Logger.Log(Logger.LogTypes.Debug, "[GameJolt] Auth body: " & If(result, "").Substring(0, Math.Min(300, If(result, "").Length)))
            Catch
            End Try

            Dim list As List(Of GameJolt.API.JoltValue) = GameJolt.API.HandleData(result)

            Dim success As Boolean =
                TruthyKey(list, "success") OrElse
                TruthyKey(list, "authenticated") OrElse
                TruthyKey(list, "authorized") OrElse
                TruthyKey(list, "ok") OrElse
                TruthyKey(list, "status")

            If Not success AndAlso Not String.IsNullOrEmpty(result) Then
                Dim bodyL = result.ToLowerInvariant()
                If bodyL.Contains("success:true") OrElse bodyL.Contains("success: true") _
                   OrElse bodyL.Contains("success:1") OrElse bodyL.Contains("status:success") _
                   OrElse bodyL.Contains("authenticated:true") Then
                    success = True
                End If
            End If

            If success Then
                ' Persist API login state
                GameJolt.API.username = UserName.Text
                GameJolt.API.token = Token.Text
                GameJolt.API.LoggedIn = True

                ' Keep the returned user_id if present
                Dim uid As String = GetField(list, "user_id")
                If Not String.IsNullOrEmpty(uid) Then
                    GameJolt.LogInScreen.LoadedGameJoltID = uid
                End If

                ' Mark save + stamp optional player metadata safely
                EnsureOnlineSave()
                ApplyPlayerGJMeta()

                WaitingForResponse = False
                ShowokButton = False
                WaitingMessage = "Login successful."
                LogInButton.Text = "Log out"

                Me.SaveSettings()

                Try
                    Dim apiOpen As New APICall(AddressOf DummyHandler)
                    apiOpen.OpenSession()
                Catch
                End Try

                ' Go straight to server select after successful login
                Core.SetScreen(New JoinServerScreen(Me.PreScreen))
                Return
            Else
                Dim serverMsg As String = GetField(list, "message")
                If String.IsNullOrWhiteSpace(serverMsg) Then serverMsg = GetField(list, "error")
                If String.IsNullOrWhiteSpace(serverMsg) Then
                    serverMsg = "Cannot connect to account!" & Environment.NewLine &
                                "You have to use your Token," & Environment.NewLine &
                                "not your Password."
                End If

                WaitingForResponse = True
                GameJolt.API.LoggedIn = False
                WaitingMessage = serverMsg
                ShowokButton = True
                LogInButton.Text = "Log in"

                ' still try to keep save marked if they were already online
                EnsureOnlineSave()
                ApplyPlayerGJMeta()
            End If
        End Sub

        Private Sub HandleUserData(ByVal result As String)
            Dim list As List(Of GameJolt.API.JoltValue) = GameJolt.API.HandleData(result)
            For Each Item As GameJolt.API.JoltValue In list
                If Item.Name = "id" Then
                    LoadedGameJoltID = Item.Value
                    Logger.Log(Logger.LogTypes.Info, $"[GameJolt] Received user id={LoadedGameJoltID} for ""{UserName.Text}"".")
                    If GameController.UPDATEONLINEVERSION = True And GameController.IS_DEBUG_ACTIVE = True Then
                        Dim APICall As New APICall
                        APICall.SetStorageDataRestricted("ONLINEVERSION", GameController.GAMEVERSION)
                        Logger.Debug("UPDATED ONLINE VERSION TO: " & GameController.GAMEVERSION)
                    End If
                    LogInButton.Text = "Log out"
                    Me.SaveSettings()
                    Me._tempCloseScreen = True
                    Exit For
                End If
            Next
        End Sub

        Private Sub DummyHandler(ByVal result As String)
        End Sub

        Private Sub PressOK()
            Me.WaitingForResponse = False
            OkButton.IsActive = False
            ShowokButton = False
            Logger.Log(Logger.LogTypes.Info, "[GameJolt] Dismissed login dialog.")
        End Sub

        Public Shared ReadOnly Property UserBanned(ByVal GameJoltID As String) As Boolean
            Get
                Dim ID_list() As String = BanList.SplitAtNewline()
                For i As Integer = 0 To ID_list.Count() - 1
                    If ID_list(i).GetSplit(0, "|") = GameJoltID Then
                        Return True
                    End If
                Next
                Return False
            End Get
        End Property

        Public Shared ReadOnly Property BanReasonIDForUser(ByVal User_ID As String) As String
            Get
                Dim ID_list() As String = BanList.SplitAtNewline()
                For i As Integer = 0 To ID_list.Count() - 1
                    If ID_list(i).GetSplit(0, "|") = User_ID Then
                        Return ID_list(i).GetSplit(1, "|")
                    End If
                Next
                Return "0"
            End Get
        End Property

        Public Shared ReadOnly Property GetBanReasonByID(ByVal banReasonID As String) As String
            Get
                For Each reasonString As String In BanReasons.SplitAtNewline()
                    Dim reason As String = reasonString.GetSplit(1, "|")
                    Dim reasonID As String = reasonString.GetSplit(0, "|")
                    If reasonID = banReasonID Then
                        Return reason
                    End If
                Next
                Return ""
            End Get
        End Property

        Private Sub DownloadBanList()
            Try
                Dim w As New System.Net.WebClient
                BanList = w.DownloadString("https://raw.githubusercontent.com/AirysDark/P3D-DATA/main/banlist.dat")
                BanReasons = w.DownloadString("https://raw.githubusercontent.com/AirysDark/P3D-DATA/main/banreasons.dat")
                Logger.Log(Logger.LogTypes.Message, "Retrieved ban list data.")
                Me.DownloadedBanList = True
            Catch ex As Exception
                Logger.Log(Logger.LogTypes.ErrorMessage, "Failed to fetch ban list data!")
                Logger.Log(Logger.LogTypes.Debug, ex.Message)
            End Try
        End Sub

        ' ---------------- Navigation helpers ----------------
        Public Shared Sub OpenOrLogin(ByVal target As Screen)
            If GameJolt.API.LoggedIn Then
                Core.SetScreen(target)
            Else
                Core.SetScreen(New GameJolt.LogInScreen(target))
            End If
        End Sub

        Public Shared Sub KickFromOnlineScreen(ByVal setToScreen As Screen)
            If Core.Player IsNot Nothing AndAlso GameJolt.API.LoggedIn = False Then
                Try
                    If Core.Player.IsGameJoltSave = True Then
                        Core.SetScreen(New GameJolt.LogInScreen(setToScreen))
                    End If
                Catch
                    ' IsGameJoltSave not present ? ignore
                End Try
            End If
        End Sub

        ' ---------------- UI classes ----------------
        Private Class JoltControl
            Public IsActive As Boolean = False
        End Class

        Private Class JoltTextBox
            Inherits JoltControl

            Dim _text As String = ""
            Dim _password As Boolean = False

            Dim _backcolor As Color
            Dim _forecolor As Color

            Dim _font As SpriteFont

            Public Position As New Vector2(0)
            Public Size As New Size(0, 0)
            Public MaxChars As Integer = -1

            Public Sub New(ByVal Font As SpriteFont, ByVal BackColor As Color, ByVal FontColor As Color)
                Me._font = Font
                Me._backcolor = BackColor
                Me._forecolor = FontColor
            End Sub

            Public Property IsPassword() As Boolean
                Get
                    Return Me._password
                End Get
                Set(value As Boolean)
                    Me._password = value
                End Set
            End Property

            Public Property Text() As String
                Get
                    Return Me._text
                End Get
                Set(value As String)
                    Me._text = value
                End Set
            End Property

            Public Property BackColor() As Color
                Get
                    Return Me._backcolor
                End Get
                Set(value As Color)
                    Me._backcolor = value
                End Set
            End Property

            Public Property FontColor() As Color
                Get
                    Return Me._forecolor
                End Get
                Set(value As Color)
                    Me._forecolor = value
                End Set
            End Property

            Public Property Font() As SpriteFont
                Get
                    Return Me._font
                End Get
                Set(value As SpriteFont)
                    Me._font = value
                End Set
            End Property

            Public Sub Draw()
                Dim useColor As Color = _backcolor
                Dim useFontColor As Color = _forecolor
                If Me.IsActive = True Then
                    useColor = _forecolor
                    useFontColor = _backcolor
                End If

                Canvas.DrawRectangle(New Rectangle(CInt(Me.Position.X), CInt(Me.Position.Y), Me.Size.Width, Me.Size.Height), useColor, True)

                Dim useText As String = Me._text
                If Me._password = True Then
                    useText = New String("x"c, Me._text.Length)
                End If

                If IsActive = True Then
                    If MaxChars < 0 Or MaxChars > Me._text.Length Then
                        useText &= "_"
                    End If
                End If

                Core.SpriteBatch.DrawInterfaceString(Me._font, useText, Me.Position, useFontColor)
            End Sub

            Public Sub Update()
                If Me.IsActive = True Then
                    KeyBindings.GetInput(Me._text, Me.MaxChars, True, True)
                End If
            End Sub

            Public Function GetRectangle() As Rectangle
                Return New Rectangle(CInt(Me.Position.X), CInt(Me.Position.Y), Me.Size.Width, Me.Size.Height)
            End Function

            Public Sub DoPress()
                Core.SetScreen(New InputScreen(Core.CurrentScreen, "", InputScreen.InputModes.Text, Me._text, 32, New List(Of Texture2D), New InputScreen.ConfirmInput(AddressOf ReturnSetText)) With {.PasswordMode = Me._password})
            End Sub

            Private Sub ReturnSetText(ByVal result As String)
                Me._text = result
            End Sub
        End Class

        Private Class JoltButton
            Inherits JoltControl

            Dim _text As String
            Dim _backColor As Color
            Dim _textColor As Color
            Dim _font As SpriteFont

            Public Position As Vector2 = New Vector2(0)
            Public Size As Size = New Size(0, 0)

            Public Visible As Boolean = True

            Public Delegate Sub Press()
            Public DoPress As Press

            Public Sub New(ByVal Text As String, ByVal Font As SpriteFont, ByVal BackColor As Color, ByVal TextColor As Color)
                Me._text = Text
                Me._backColor = BackColor
                Me._textColor = TextColor
                Me._font = Font
            End Sub

            Public Sub SetDelegate(ByVal DelegateSub As Press)
                Me.DoPress = DelegateSub
            End Sub

            Public Sub Update()
            End Sub

            Public Sub Draw()
                If Visible = True Then
                    Dim useColor As Color = _backColor
                    Dim useFontColor As Color = _textColor
                    If Me.IsActive = True Then
                        useColor = _textColor
                        useFontColor = _backColor
                    End If

                    Canvas.DrawRectangle(New Rectangle(CInt(Me.Position.X), CInt(Me.Position.Y), Me.Size.Width, Me.Size.Height), useColor, True)
                    Core.SpriteBatch.DrawInterfaceString(Me._font, Me._text, New Vector2(Me.Position.X + CSng(Me.Size.Width / 2 - Me._font.MeasureString(Me._text).X / 2), Me.Position.Y + CSng(Me.Size.Height / 2 - Me._font.MeasureString(Me._text).Y / 2)), useFontColor)
                End If
            End Sub

            Public Property Text As String
                Get
                    Return Me._text
                End Get
                Set(value As String)
                    Me._text = value
                End Set
            End Property

            Public Function GetRectangle() As Rectangle
                Return New Rectangle(CInt(Me.Position.X), CInt(Me.Position.Y), Me.Size.Width, Me.Size.Height)
            End Function
        End Class

    End Class

End Namespace