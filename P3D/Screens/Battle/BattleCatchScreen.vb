Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Public Class BattleCatchScreen
    Inherits Screen

    Dim Ball As Item
    Dim PokemonScale As Vector3
    Dim AnimationIndex As Integer = 0
    Dim InBall As Boolean = False
    Dim textboxStart As Boolean = False
    Dim showPokedexEntry As Boolean = False
    Dim boxData As String = ""
    Dim sentToBox As Boolean = False
    Dim p As Pokemon
    Dim SpriteVisible As Boolean = False
    Dim BattleScreen As BattleSystem.BattleScreen
    Dim AnimationHasStarted As Boolean = False
    Dim AnimationList As New List(Of BattleSystem.AnimationQueryObject)
    Dim _playIntroSound As Boolean = False

    Public Sub New(ByVal BattleScreen As BattleSystem.BattleScreen, ByVal Ball As Item)
        Me.Identification = Identifications.BattleCatchScreen
        Me.Ball = Ball
        Me.PreScreen = BattleScreen
        Me.UpdateFadeIn = True
        Me.BattleScreen = BattleScreen
        p = BattleScreen.OppPokemon
        Me.SpriteVisible = BattleScreen.OppPokemonNPC.Visible
        SetCamera()
    End Sub

    Public Overrides Sub Draw()
        SkyDome.Draw(45.0F)
        Level.Draw()

        Dim RenderObjects As New List(Of Entity)
        RenderObjects.Add(BattleScreen.OppPokemonNPC)

        If RenderObjects.Count > 0 Then
            RenderObjects = (From r In RenderObjects Order By r.CameraDistance Descending).ToList()
        End If

        For Each [Object] As Entity In RenderObjects
            [Object].Render()
        Next

        If AnimationList.Count > 0 Then
            Dim cIndex As Integer = 0
            Dim cQuery As New List(Of BattleSystem.AnimationQueryObject)
nextIndex:
            If AnimationList.Count > cIndex Then
                Dim cQueryObject As BattleSystem.AnimationQueryObject = AnimationList(cIndex)
                cQuery.Add(cQueryObject)

                If cQueryObject.PassThis = True Then
                    cIndex += 1
                    GoTo nextIndex
                End If
            End If

            cQuery.Reverse()
            For Each cQueryObject As BattleSystem.AnimationQueryObject In cQuery
                cQueryObject.Draw(BattleScreen)
            Next
        End If

        World.DrawWeather(Screen.Level.World.CurrentMapWeather)
        TextBox.Draw()
    End Sub

    Public Sub UpdateAnimations()
        Dim cIndex As Integer = 0
nextIndex:
        If AnimationList.Count > cIndex Then
            Dim cQueryObject As BattleSystem.QueryObject = AnimationList(cIndex)
            cQueryObject.Update(BattleScreen)

            If cQueryObject.IsReady = True Then
                AnimationList.RemoveAt(cIndex)
                If cQueryObject.PassThis = True Then GoTo nextIndex
            Else
                If cQueryObject.PassThis = True Then
                    cIndex += 1
                    GoTo nextIndex
                End If
            End If
        End If
    End Sub

    Private Sub SetCamera()
        Screen.Camera.Position = Vector3.Subtract(
        New Vector3(
            BattleScreen.OppPokemonNPC.Position.X - 2.5F,
            BattleScreen.OppPokemonNPC.Position.Y + 0.25F,
            BattleScreen.OppPokemonNPC.Position.Z + 0.5F),
        BattleSystem.BattleScreen.BattleMapOffset)  ' <-- use the class, not the variable
        Screen.Camera.Pitch = -0.25F
        Screen.Camera.Yaw = MathHelper.Pi * 1.5F + 0.25F
    End Sub

    Public Overrides Sub Update()
        Lighting.UpdateLighting(Screen.Effect)

        If textboxStart = False Then
            textboxStart = True
            TextBox.Show(Core.Player.Name & " used a~" & Ball.Name & "!", {}, False, False)
        End If

        TextBox.Update()
        SkyDome.Update()
        Level.Update()
        BattleScreen.OppPokemonNPC.UpdateEntity()

        DirectCast(Screen.Camera, BattleSystem.BattleCamera).UpdateMatrices()
        DirectCast(Screen.Camera, BattleSystem.BattleCamera).UpdateFrustum()

        If TextBox.Showing = False AndAlso Me.IsCurrentScreen() Then
            UpdateAnimations()
            Select Case AnimationIndex
                Case 0
                    If AnimationHasStarted = False Then
                        Dim Shakes As New List(Of Boolean)
                        For i = 0 To 3
                            If StayInBall() = True Then
                                Select Case i
                                    Case 0 : Shakes.Add(False)
                                    Case 1 : Shakes.Add(True)
                                    Case 2 : Shakes.Add(False)
                                    Case 3 : InBall = True
                                End Select
                            Else
                                Exit For
                                InBall = False
                            End If
                        Next

                        If Core.Player.ShowBattleAnimations <> 0 AndAlso BattleScreen.IsPVPBattle = False Then
                            PokemonScale = BattleScreen.OppPokemonNPC.Scale
                            Dim CatchAnimation = New BattleSystem.AnimationQueryObject(Nothing, False, Nothing)
                            CatchAnimation.AnimationPlaySound("Battle\Pokeball\Throw", 0, 0)

                            Dim BallPosition As New Vector3(BattleScreen.OppPokemonNPC.Position.X - 3, BattleScreen.OppPokemonNPC.Position.Y + 0.15F, BattleScreen.OppPokemonNPC.Position.Z)
                            Dim BallEntity As Entity = CatchAnimation.SpawnEntity(BallPosition, Ball.Texture, New Vector3(0.3F), 1.0F, 0, 0)

                            CatchAnimation.AnimationMove(BallEntity, False, 3, 0.1F, 0, 0.075, False, False, 0F, 0F,,,, 0.025)
                            CatchAnimation.AnimationRotate(BallEntity, False, 0, 0, -0.5, 0, 0, -6 * MathHelper.Pi, 0, 0, False, False, True, False)
                            CatchAnimation.AnimationRotate(BallEntity, False, 0, 0, 6 * MathHelper.Pi, 0, 0, 0, 4, 0, False, False, True, False)
                            CatchAnimation.AnimationPlaySound("Battle\Pokeball\Open", 3, 0)

                            Dim SmokeParticlesClose As Integer = 0
                            Do
                                Dim SmokePosition = New Vector3(
                                    BattleScreen.OppPokemonNPC.Position.X + CSng(Core.Random.Next(-10, 10) / 10.0F),
                                    BattleScreen.OppPokemonNPC.Position.Y - 0.35F,
                                    BattleScreen.OppPokemonNPC.Position.Z + CSng(Core.Random.Next(-10, 10) / 10.0F))
                                Dim SmokeTexture As Texture2D = TextureManager.GetTexture("Textures\Battle\Smoke")
                                Dim SmokeScale = New Vector3(CSng(Core.Random.Next(2, 6) / 10.0F))
                                Dim SmokeSpeed = CSng(Core.Random.Next(1, 3) / 25.0F)
                                Dim SmokeEntity = CatchAnimation.SpawnEntity(SmokePosition, SmokeTexture, SmokeScale, 1, 3, 0)
                                Dim SmokeDestination = New Vector3(
                                    BallEntity.Position.X - SmokePosition.X + 3,
                                    BallEntity.Position.Y - SmokePosition.Y,
                                    BallEntity.Position.Z - SmokePosition.Z - 0.05F)
                                CatchAnimation.AnimationMove(SmokeEntity, True, SmokeDestination.X, SmokeDestination.Y, SmokeDestination.Z, SmokeSpeed, False, False, 3, 0)
                                Threading.Interlocked.Increment(SmokeParticlesClose)
                            Loop While SmokeParticlesClose <= 38

                            CatchAnimation.AnimationScale(BattleScreen.OppPokemonNPC, False, False, 0.0F, 0.0F, 0.0F, 0.035F, 3, 0)
                            CatchAnimation.AnimationMove(BallEntity, False, 3, -0.35, 0, 0.1F, False, False, 8, 0)
                            CatchAnimation.AnimationPlaySound("Battle\Pokeball\Land", 9, 0)

                            For i = 0 To Shakes.Count - 1
                                CatchAnimation.AnimationPlaySound("Battle\Pokeball\Shake", 12 + i * 10, 0)
                                If Shakes(i) = False Then
                                    CatchAnimation.AnimationRotate(BallEntity, False, 0, 0, 0.15F, 0, 0, MathHelper.PiOver4, 12 + i * 10, 0, False, False, True, True)
                                Else
                                    CatchAnimation.AnimationRotate(BallEntity, False, 0, 0, -0.15F, 0, 0, 0 - MathHelper.PiOver4, 12 + i * 10, 0, False, False, True, True)
                                End If
                            Next

                            AnimationList.Add(CatchAnimation)
                        End If
                        AnimationHasStarted = True
                    Else
                        If AnimationList.Count = 0 Then
                            AnimationIndex = 1
                        End If
                    End If

                Case 1
                    If InBall = True Then
                        CatchPokemon()
                        BattleSystem.Battle.Caught = True
                        AnimationIndex = 2
                    Else
                        Core.SetScreen(Me.PreScreen)
                        CType(Core.CurrentScreen, BattleSystem.BattleScreen).Battle.InitializeRound(CType(Core.CurrentScreen, BattleSystem.BattleScreen),
                            New BattleSystem.Battle.RoundConst() With {.StepType = BattleSystem.Battle.RoundConst.StepTypes.Text, .Argument = "It broke free!"})
                    End If

                Case 2
                    If showPokedexEntry = True Then
                        Core.SetScreen(New TransitionScreen(Core.CurrentScreen, New PokedexViewScreen(Core.CurrentScreen, p, True), Color.White, False))
                    End If
                    AnimationIndex = 3

                Case 3
                    Core.SetScreen(New NameObjectScreen(Core.CurrentScreen, p))
                    AnimationIndex = 4

                Case 4
                    If p.CatchBall.ID = 186 Then p.FullRestore()
                    PlayerStatistics.Track("Caught Pokemon", 1)
                    StorePokemon()
                    AnimationIndex = 5

                Case 5
                    Core.SetScreen(Me.PreScreen)
                    BattleSystem.Battle.Won = True
                    CType(Core.CurrentScreen, BattleSystem.BattleScreen).EndBattle(False)
            End Select
        End If
    End Sub

    Private Sub CatchPokemon()
        p.ResetTemp()
        Dim s As String = "Gotcha!~" & p.GetName() & " was caught!"

        If Core.Player.HasPokedex Then
            If Pokedex.GetEntryType(Core.Player.PokedexData, p.Number) < 2 Then
                s &= "*" & p.GetName() & "'s data was~added to the Pokédex."
                showPokedexEntry = True
            End If
        End If

        If p.IsShiny Then
            Core.Player.PokedexData = Pokedex.ChangeEntry(Core.Player.PokedexData, p.Number, 3)
        ElseIf Pokedex.GetEntryType(Core.Player.PokedexData, p.Number) < 3 Then
            Core.Player.PokedexData = Pokedex.ChangeEntry(Core.Player.PokedexData, p.Number, 2)
        End If

        p.SetCatchInfos(Me.Ball, "caught at")

        MusicManager.Pause()
        MusicManager.Play("wild_defeat", False, 0.0F)
        SoundManager.PlaySound("success_catch", True)
        TextBox.Show(s, {}, False, False)
    End Sub

    Private Sub StorePokemon()
        Dim s As String = ""
        If Core.Player.Pokemons.Count < 6 Then
            Core.Player.Pokemons.Add(p)
        Else
            Dim boxName As String = StorageSystemScreen.GetBoxName(StorageSystemScreen.DepositPokemon(p, Player.Temp.PCBoxIndex))
            s = $"It was transferred to Box ""{boxName}"" on the PC."
        End If
        Core.Player.AddPoints(3, "Caught Pokémon.")
        If s <> "" Then TextBox.Show(s)
    End Sub

    Private Function StayInBall() As Boolean
        Dim cp As Pokemon = p
        Dim MaxHP As Integer = cp.MaxHP
        Dim CurrentHP As Integer = cp.HP
        Dim CatchRate As Integer = cp.CatchRate
        Dim BallRate As Single = Ball.CatchMultiplier
        Dim PokemonStartFriendship As Integer = cp.Friendship

        Dim Status As Single = 1.0F
        If cp.Status = Pokemon.StatusProblems.Sleep OrElse cp.Status = Pokemon.StatusProblems.Freeze Then
            Status = 2.5F
        End If

        Dim CaptureRate As Integer = CInt(Math.Floor(((1 + (MaxHP * 3 - CurrentHP * 2) * CatchRate * BallRate * Status) / (MaxHP * 3))))
        If CaptureRate <= 0 Then CaptureRate = 1

        Dim B As Integer = CInt(1048560 / Math.Sqrt(Math.Sqrt(16711680 / CaptureRate)))
        Dim R As Integer = Core.Random.Next(0, 65535 + 1)

        If R > B Then
            cp.Friendship = PokemonStartFriendship
            Return False
        Else
            Return True
        End If
    End Function
End Class