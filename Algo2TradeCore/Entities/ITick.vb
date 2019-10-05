Namespace Entities
    Public Interface ITick
        ReadOnly Property InstrumentToken As String
        ReadOnly Property Tradable As Boolean
        ReadOnly Property Open As Decimal
        ReadOnly Property High As Decimal
        ReadOnly Property Low As Decimal
        ReadOnly Property Close As Decimal
        ReadOnly Property Volume As Long
        ReadOnly Property AveragePrice As Decimal
        ReadOnly Property LastPrice As Decimal
        ReadOnly Property OI As UInteger
        ReadOnly Property SellQuantity As UInteger
        ReadOnly Property BuyQuantity As UInteger
        ReadOnly Property Timestamp As Date?
        ReadOnly Property LastTradeTime As Date?

        ReadOnly Property Broker As APISource
    End Interface
End Namespace