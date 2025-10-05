Namespace Items.Wings

    <Item(261, "Sticky Wing")>
    Public Class StickyWing

        Inherits Item

        Public Overrides ReadOnly Property CanBeUsed As Boolean = False
        Public Overrides ReadOnly Property CanBeUsedInBattle As Boolean = False
        Public Overrides ReadOnly Property Description As String = "It's a feather that sticks to other feathers, a Pokémon that holds it will find more feathers."
        Public Overrides ReadOnly Property PokeDollarPrice As Integer = 200
        Public Overrides ReadOnly Property FlingDamage As Integer = 20

        Public Sub New()
            _textureRectangle = New Rectangle(456, 240, 24, 24)
        End Sub

    End Class

End Namespace
