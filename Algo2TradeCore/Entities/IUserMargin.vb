Namespace Entities
    Public Interface IUserMargin
        ReadOnly Property Enabled As Boolean
        ReadOnly Property NetAmount As Decimal
        ReadOnly Property AvailableMargin As Decimal
        ReadOnly Property UtilisedMargin As Decimal
        ReadOnly Property Broker As APISource
    End Interface
End Namespace
