Namespace Items.Medicine

    <Item(63, "Ether")>
    Public Class Ether

        Inherits MedicineItem
        Public Overrides ReadOnly Property PokeDollarPrice As Integer = 1200
        Public Overrides ReadOnly Property Description As String = "This medicine can restore 10 PP to a single selected move that has been learned by a Pokémon."

        Public Sub New()
            _textureRectangle = New Rectangle(360, 48, 24, 24)
        End Sub

        Public Overrides Sub Use()
            Dim selScreen = New PartyScreen(Core.CurrentScreen, Me, AddressOf Me.UseOnPokemon, "Use " & Me.Name, True) With {.Mode = Screens.UI.ISelectionScreen.ScreenMode.Selection, .CanExit = True}
            AddHandler selScreen.SelectedObject, AddressOf UseItemhandler

            Core.SetScreen(selScreen)
        End Sub

        Public Overrides Function UseOnPokemon(ByVal PokeIndex As Integer) As Boolean
            Core.SetScreen(New ChooseAttackScreen(Core.CurrentScreen, Core.Player.Pokemons(PokeIndex), True, True, AddressOf UseOnAttack))
            Dim s As Screen = Core.CurrentScreen
            While s.Identification <> Screen.Identifications.BattleScreen AndAlso s.PreScreen IsNot Nothing
                s = s.PreScreen
            End While
            If s.Identification = Screen.Identifications.BattleScreen Then
                Return False
            Else
                Return True
            End If
        End Function

        Private Sub UseOnAttack(ByVal Pokemon As Pokemon, ByVal AttackIndex As Integer)
            If Pokemon.Attacks(AttackIndex).CurrentPP < Pokemon.Attacks(AttackIndex).MaxPP Then
                Pokemon.Attacks(AttackIndex).CurrentPP = CInt(MathHelper.Clamp(Pokemon.Attacks(AttackIndex).CurrentPP + 10, 0, Pokemon.Attacks(AttackIndex).MaxPP))

                Dim t As String = "Restored PP of~" & Pokemon.Attacks(AttackIndex).Name & "."
                t &= RemoveItem()
                PlayerStatistics.Track("[17]Medicine Items used", 1)

                SoundManager.PlaySound("Use_Item", False)
                Screen.TextBox.Show(t, {}, True, True)
                Dim s As Screen = Core.CurrentScreen
                While s.Identification <> Screen.Identifications.BattleScreen AndAlso s.PreScreen IsNot Nothing
                    s = s.PreScreen
                End While
                If s.Identification = Screen.Identifications.BattleScreen Then
                    Dim TempBattleScreen As BattleSystem.BattleScreen = CType(s, BattleSystem.BattleScreen)

                    TempBattleScreen.BattleQuery.Clear()
                    TempBattleScreen.BattleQuery.Add(TempBattleScreen.FocusBattle())
                    TempBattleScreen.BattleQuery.Insert(0, New BattleSystem.ToggleMenuQueryObject(True))
                    TempBattleScreen.Battle.InitializeRound(TempBattleScreen, New BattleSystem.Battle.RoundConst With {.StepType = BattleSystem.Battle.RoundConst.StepTypes.Item, .Argument = Me.ID.ToString()})
                    Core.SetScreen(TempBattleScreen)
                End If
            Else
                Screen.TextBox.Show("The move already has~full PP.", {}, True, True)
            End If
        End Sub

    End Class

End Namespace
