Namespace Items.Berries

    <Item(2064, "Roseli")>
    Public Class RoseliBerry

        Inherits Berry

        Public Sub New()
            MyBase.New(64800, "If held by a Pokémon, this Berry will lessen the damage taken from one supereffective Fairy-type attack.", "3.2cm", "Soft", 1, 5)

            Me.Spicy = 0
            Me.Dry = 0
            Me.Sweet = 25
            Me.Bitter = 0
            Me.Sour = 10

            Me.Type = Element.Types.Fairy
            Me.Power = 80
        End Sub

    End Class

End Namespace
