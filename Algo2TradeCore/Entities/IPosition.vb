Namespace Entities
    Public Interface IPosition
        ReadOnly Property Quantity As Integer
        ReadOnly Property InstrumentIdentifier As UInteger
        ReadOnly Property SellPrice As Decimal
        ReadOnly Property BuyPrice As Decimal
        ReadOnly Property AveragePrice As Decimal
        ReadOnly Property TradingSymbol As String
        ReadOnly Property Exchange As String
        ReadOnly Property Product As String
        ReadOnly Property Broker As APISource
    End Interface
End Namespace