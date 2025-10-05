﻿Namespace BattleSystem.Moves.Normal

    Public Class Struggle

        Inherits Attack

        Public Sub New()
            '#Definitions
            Me.Type = New Element(Element.Types.Normal)
            Me.ID = 165
            Me.OriginalPP = 1
            Me.CurrentPP = 1
            Me.MaxPP = 1
            Me.Power = 50
            Me.Accuracy = 0
            Me.Category = Categories.Physical
            Me.ContestCategory = ContestCategories.Tough
            Me.Name = Localization.GetString("move_name_" & Me.ID,"Struggle")
            Me.Description = "An attack that is used in desperation only if the user has no PP. It also hurts the user slightly."
            Me.CriticalChance = 0
            Me.IsHMMove = False
            Me.Target = Targets.OneAdjacentTarget
            Me.Priority = 0
            Me.TimesToAttack = 1
            '#End

            '#SpecialDefinitions
            Me.MakesContact = True
            Me.ProtectAffected = True
            Me.MagicCoatAffected = False
            Me.SnatchAffected = False
            Me.MirrorMoveAffected = True
            Me.KingsrockAffected = True
            Me.CounterAffected = True

            Me.DisabledWhileGravity = False
            Me.UseEffectiveness = False
            Me.ImmunityAffected = False
            Me.RemovesOwnFrozen = False
            Me.HasSecondaryEffect = False

            Me.IsHealingMove = False
            Me.IsRecoilMove = False

            Me.IsDamagingMove = True
            Me.IsProtectMove = False


            Me.IsAffectedBySubstitute = True
            Me.IsOneHitKOMove = False
            Me.IsWonderGuardAffected = False
            Me.CanGainSTAB = False
            Me.UseAccEvasion = False
            '#End
        End Sub
        Public Overrides Function DeductPP(ByVal own As Boolean, ByVal BattleScreen As BattleScreen) As Boolean
            Return False
        End Function
        Public Overrides Sub MoveSelected(own As Boolean, BattleScreen As BattleScreen)
            Dim p As Pokemon = BattleScreen.OwnPokemon
            If own = True Then
                BattleScreen.BattleQuery.Add(New TextQueryObject(p.GetDisplayName() & " has no usable attacks left!"))
            End If
        End Sub

        Public Overrides Sub PreAttack(own As Boolean, BattleScreen As BattleScreen)
            Dim p As Pokemon = BattleScreen.OppPokemon
            If own = False Then
                BattleScreen.Battle.ChangeCameraAngle(1, False, BattleScreen)
                BattleScreen.BattleQuery.Add(New TextQueryObject(p.GetDisplayName() & " has no usable attacks left!"))
            End If
        End Sub

        Public Overrides Sub MoveHits(own As Boolean, BattleScreen As BattleScreen)
            Dim p As Pokemon = BattleScreen.OwnPokemon
            If own = False Then
                p = BattleScreen.OppPokemon
            End If

            Dim recoilDamage As Integer = CInt(Math.Ceiling(p.MaxHP / 4))
            If recoilDamage <= 0 Then
                recoilDamage = 1
            End If

            BattleScreen.Battle.ReduceHP(recoilDamage, own, own, BattleScreen, p.GetDisplayName() & " is damaged by recoil!", "move:struggle")
        End Sub

    End Class

End Namespace