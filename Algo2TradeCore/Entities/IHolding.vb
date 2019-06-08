Namespace Entities
    Public Interface IHolding
        ReadOnly Property Product As String
        ReadOnly Property Price As Decimal
        ReadOnly Property AveragePrice As Decimal
        ReadOnly Property TradingSymbol As String
        ReadOnly Property T1Quantity As Integer
        ReadOnly Property InstrumentIdentifier As UInteger
        ReadOnly Property ISIN As String
        ReadOnly Property RealisedQuantity As Integer
        ReadOnly Property Quantity As Integer
    End Interface
End Namespace